using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace Unleashed;

#if TRUE_ULTIMATE_POWER
	#warning TICK TOCK...
#endif

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
[BepInProcess("Tabletop Simulator.exe")]
[BepInDependency(CONFIG_MANAGER_GUID, BepInDependency.DependencyFlags.SoftDependency)]
public sealed class Main : BaseUnityPlugin
{
	public const string PLUGIN_GUID    = "edu.sos.unleashed";
	public const string PLUGIN_NAME    = "Unleashed";
	public const string PLUGIN_VERSION = "0.1.0";
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
			Harmony.PatchAll(typeof(Main)); // ensure error message
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

	[HarmonyPostfix]
	[HarmonyPatch(typeof(Chat), nameof(Chat.SingletonInit))]
	private static void Splash()
	{
		Chat.LogSystem($"--[[ {PLUGIN_NAME} v{PLUGIN_VERSION} ]]--", PluginColour, true);
		if (loadErrors != null)
		{
			Chat.LogSystem("Errors occurred while loading", ErrorColour, true);
			Chat.LogSystem(loadErrors, ErrorColour);
		}
	}
}

