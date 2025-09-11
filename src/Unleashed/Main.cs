global using HarmonyLib;
global using Mono.Cecil.Cil;
global using MonoMod.Cil;
global using NewNet;
global using UnityEngine;

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace Unleashed;

#if TRUE_ULTIMATE_POWER
	#warning TICK TOCK...
#endif

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
[BepInProcess("Tabletop Simulator.exe")]
[BepInDependency(CONFIG_MANAGER_GUID, BepInDependency.DependencyFlags.SoftDependency)]
public sealed class Main : BaseUnityPlugin
{
	public const string PLUGIN_REPO    = "https://github.com/SoundOfScooting/TTS-Unleashed";
	public const string PLUGIN_GUID    = PluginInfo.PLUGIN_GUID;
	public const string PLUGIN_NAME    = PluginInfo.PLUGIN_NAME;
	public const string PLUGIN_VERSION = PluginInfo.PLUGIN_VERSION;
	public const string PLUGIN_ABBR    = "UZ";

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
			Settings.Load();

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
			gameObject.AssignLocalTRS(numberLabel.gameObject);

			var moddedLabel   = gameObject.AddComponent(numberLabel.GetComponent<UILabel>());
			moddedLabel.color = loadErrors is not null ? ErrorColour : PluginColour;
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

			gameObject.AddComponent(numberLabel.GetComponent<BoxCollider2D>());
			gameObject.AddComponent(numberLabel.GetComponent<UIButton>());
			gameObject.AddComponent(numberLabel.GetComponent<UIOpenURL>())
				.URL = $"{PLUGIN_REPO}/releases/tag/v{PLUGIN_VERSION}";
			gameObject.AddComponent<TweenColor>();
		}
	}
	static class PatchMenuCursor
	{
		[HarmonyILManipulator]
		[HarmonyPatch(typeof(NetworkUI), nameof(NetworkUI.Init))]
		static void CursorColor(ILContext il)
		{
			ILCursor c = new(il);
			c.GotoNext(MoveType.Before,
				// Utilities.SetCursor(WhiteCursorTexture, HardwareCursorOffest);
				x => x.MatchLdfld(AccessTools.Field(typeof(NetworkUI), nameof(NetworkUI.WhiteCursorTexture)))
			);
			c.MoveAfterLabels();
			c.Remove();
			c.EmitDelegate(Texture2D(NetworkUI __instance) =>
				__instance.StringColorToCursorTexture(
#if TRUE_ULTIMATE_POWER
					loadErrors is not null ? Settings.EntryMenuErrorColour.Value :
#endif
					Settings.EntryMenuCursorColour.Value
				)
			);
		}
	}
}

