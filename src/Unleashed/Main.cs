global using HarmonyLib;
global using Mono.Cecil.Cil;
global using MonoMod.Cil;
global using NewNet;
global using UnityEngine;
global using Unleashed.Extensions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Unleashed.Settings;

namespace Unleashed;

#if TRUE_ULTIMATE_POWER
	#warning TRUE_ULTIMATE_POWER=1
#endif
#if DEBUG_COMPAT
	#warning DEBUG_COMPAT=1
#endif

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
[BepInProcess("Tabletop Simulator.exe")]
[BepInDependency(CONFIG_MANAGER_GUID, BepInDependency.DependencyFlags.SoftDependency)]
public sealed class Main : BaseUnityPlugin
{
	public const string PLUGIN_GUID    = PluginInfo.PLUGIN_GUID;
	public const string PLUGIN_NAME    = PluginInfo.PLUGIN_NAME;
	public const string PLUGIN_VERSION = PluginInfo.PLUGIN_VERSION;
	public const string PLUGIN_ABBR    = "UZ";
	const string PLUGIN_REPO = "https://github.com/SoundOfScooting/TTS-Unleashed";
	const string PLUGIN_URL  = $"{PLUGIN_REPO}/releases/tag/v{PLUGIN_VERSION}";

	internal const string CONFIG_MANAGER_GUID = "com.bepis.bepinex.configurationmanager";

	public static readonly Colour PluginColour = Colour.Purple;
	public static readonly Colour ErrorColour  = Colour.Red;

	public static Main Instance   { get; private set; }
	public static Harmony Harmony { get; private set; }
	public static ManualLogSource Log   => Instance.Logger;
	public static new ConfigFile Config => ((BaseUnityPlugin) Instance).Config;

	static string loadErrors;
	void Awake()
	{
		Instance = this;
		try
		{
			// Debug.developerConsoleVisible = true;

			Log.LogInfo(">> Spinning up...");
			Harmony = new(PLUGIN_GUID);
			Harmony.PatchAll(typeof(PatchMenuSplash));
			Harmony.PatchAll(typeof(PatchMenuVersion));
			Harmony.PatchAll(typeof(PatchMenuCursor));

			Log.LogInfo(">> Configuring...");
			SettingAttribute.Load();

			Log.LogInfo(">> Patching...");
			Harmony.PatchAll(typeof(Main).Assembly);

			Log.LogInfo(">> Reticulating splines...");
			Events.Load();

			Log.LogInfo(">> Load complete. <<");
		}
		catch (Exception e)
		{
			Log.LogError(e);
			loadErrors = e.ToString();
			throw;
		}
	}

	static class PatchMenuSplash
	{
		[HarmonyPostfix]
		[HarmonyPatch(typeof(Chat), nameof(Chat.SingletonInit))]
		static void Splash()
		{
			Chat.LogSystem($"--[[ {PLUGIN_NAME} v{PLUGIN_VERSION} ]]--", PluginColour, true);
			if (loadErrors is not null)
			{
				Chat.LogSystem("Errors occurred while loading!", ErrorColour, true);
				Chat.LogSystem(loadErrors, ErrorColour);
			}
		}
	}
	static class PatchMenuVersion
	{
		[HarmonyPostfix]
		[HarmonyPatch(typeof(NetworkUI), nameof(NetworkUI.Start))]
		static void VersionNumber()
		{
			var hotfixLabel = NetworkUI.Instance.GUIDisconnected.transform
				.Find("Version/Version Hotfix")
				.GetComponent<UILabel>();
			var numberLabel = NetworkUI.Instance.GUIDisconnected.transform
				.Find("Version/Version Number")
				.GetComponent<UILabel>();

			var gameObject = new GameObject("Version Modded");
			gameObject.CopyParent(numberLabel);

			var moddedLabel   = gameObject.CopyComponent(numberLabel);
			moddedLabel.color = loadErrors is null ? PluginColour : ErrorColour;
			moddedLabel.text  = $"+{PLUGIN_ABBR} v{PLUGIN_VERSION}";
			moddedLabel.SetAnchor(NetworkUI.Instance.GUIUIRoot,
				left:   numberLabel.leftAnchor  .relative, numberLabel.leftAnchor  .absolute,
				bottom: numberLabel.bottomAnchor.relative, numberLabel.bottomAnchor.absolute,
				right:  numberLabel.rightAnchor .relative, numberLabel.rightAnchor .absolute,
				top:    numberLabel.topAnchor   .relative, numberLabel.topAnchor   .absolute
			);
			moddedLabel.ResetAndUpdateAnchors();
			var dy = numberLabel.bottomAnchor.absolute - hotfixLabel.bottomAnchor.absolute;
			hotfixLabel.bottomAnchor.absolute -= dy;
			hotfixLabel.topAnchor   .absolute -= dy;
			hotfixLabel.ResetAndUpdateAnchors();
			numberLabel.bottomAnchor.absolute -= dy;
			numberLabel.topAnchor   .absolute -= dy;
			numberLabel.ResetAndUpdateAnchors();

			gameObject.CopyComponent<BoxCollider2D>(numberLabel);
			gameObject.CopyComponent<UIButton>(numberLabel);
			gameObject.CopyComponent<UIOpenURL>(numberLabel)
				.URL = PLUGIN_URL;
			gameObject.AddComponent <TweenColor>();
		}
	}
	static class PatchMenuCursor
	{
		[Setting]
		static readonly Setting<string> MenuCursorColor = new()
		{
			MigrateFrom = [
				("General", "Menu Player Color"), // 0.1.0
			],
			Section     = Section.Menu,
			Key         = "Menu Cursor Color",
			Default     = PluginColour.Label,
			Acceptable  = new AcceptableValueList<string>(Colour.AllPlayerLabels),
			Description =
				"""
				The color of the cursor on the main menu.
				""",
			OnChanged = value =>
			{
				if (loadErrors is null)
				if (Network.peerType == NetworkPeerMode.Disconnected)
					Utilities.SetCursor(
						NetworkUI.Instance.StringColorToCursorTexture(value),
						NetworkUI.HardwareCursorOffest
					);
			},
		};
		[PowerSetting]
		static readonly Setting<string> MenuErrorColor = new()
		{
			Section     = Section.Menu,
			Key         = "Menu Error Color",
			Default     = ErrorColour.Label,
			Acceptable  = new AcceptableValueList<string>(Colour.AllPlayerLabels),
			Description =
				"""
				The color of the cursor on the main menu (if the mod failed to load).
				""",
			Attributes  = new()
			{
				IsAdvanced = true,
			},
		};

		[HarmonyILManipulator]
		[HarmonyPatch(typeof(NetworkUI), nameof(NetworkUI.Init))]
		static void CursorColor(ILContext il)
		{
			var c = new ILCursor(il);
			c.GotoNext(MoveType.Before,
				// Utilities.SetCursor(WhiteCursorTexture, HardwareCursorOffest);
				x => x.MatchLdfld(AccessTools.Field(typeof(NetworkUI), nameof(NetworkUI.WhiteCursorTexture)))
			);
			c.MoveAfterLabels();
			c.Remove();
			c.EmitDelegate(Texture2D(NetworkUI __instance) =>
				__instance.StringColorToCursorTexture(
					loadErrors is null ? MenuCursorColor.Value : MenuErrorColor.Value
				)
			);
		}
	}
}

