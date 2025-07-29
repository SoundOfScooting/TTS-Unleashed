using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MonoMod.Cil;
using UnityEngine;

namespace Unleashed;

#if TRUE_ULTIMATE_POWER
	#warning TICK TOCK...
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

	internal const string CONFIG_MANAGER_GUID = "com.bepis.bepinex.configurationmanager";

	public static readonly Colour PluginColour = Colour.Purple;
	public static readonly Colour ErrorColour  = Colour.Red;

	public static Main Instance;
	public static ManualLogSource Log;
	public static Harmony Harmony;

	private static string loadErrors;
	private void Awake()
	{
		Instance = this;
		Log      = Logger;
		try
		{
//			Debug.developerConsoleVisible = true;

			Log.LogInfo(">> Patching...");
			Harmony = new(PLUGIN_GUID);
			Harmony.PatchAll(typeof(PatchMenuSplash));
			Harmony.PatchAll(typeof(PatchMenuCursor));
			Harmony.PatchAll();

			Log.LogInfo(">> Configuring...");
			Settings.Load();

			Log.LogInfo(">> Hooking events...");
			Events.Load();

			Log.LogInfo(">> Reticulating splines...");
			// SingleplayerNoSteam.Load();

			Log.LogInfo(">> Load complete. <<");
		}
		catch (Exception e)
		{
			Log.LogError(e);
			loadErrors = e.ToString();
			throw;
		}
	}

	private static class PatchMenuSplash
	{
		[HarmonyPostfix]
		[HarmonyPatch(typeof(Chat), nameof(Chat.SingletonInit))]
		private static void Splash()
		{
			Chat.LogSystem($"--[[ {PLUGIN_NAME} v{PLUGIN_VERSION} ]]--", PluginColour, true);
			if (loadErrors is not null)
			{
				Chat.LogSystem("Errors occurred while loading!", ErrorColour, true);
				Chat.LogSystem(loadErrors, ErrorColour);
			}
		}
	}
	private static class PatchMenuCursor
	{
		[HarmonyILManipulator]
		[HarmonyPatch(typeof(NetworkUI), nameof(NetworkUI.Init))]
		private static void CursorColor(ILContext il)
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

