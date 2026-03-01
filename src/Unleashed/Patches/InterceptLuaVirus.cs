using Unleashed.Settings;

namespace Unleashed.Patches;

[HarmonyPatch]
static class InterceptLuaVirus
{
	[Setting]
	static readonly Setting<bool> Enabled = new()
	{
		MigrateFrom = [
			("General", "Intercept Lua Virus"), // 0.1.0
		],
		Section     = Section.Misc,
		Key         = "Intercept Lua Virus",
		Default     = true,
		Description =
			"""
			Host-only: Intercepts the \"tcejbo gninwapS\" Lua virus before it can spread to any other objects.
			NOTE: This does not actually disinfect objects; consider additionally subscribing to CleanerBlock on the Workshop:
			https://steamcommunity.com/sharedfiles/filedetails/?id=2967684892
			""",
	};

	[HarmonyPrefix]
	[HarmonyPatch(typeof(LuaBase), nameof(LuaBase.DoString))]
	static bool DoStringPrefix(LuaBase __instance) =>
		ExecuteScriptPrefix(__instance, __instance.script_code);
	[HarmonyPrefix]
	[HarmonyPatch(typeof(LuaBase), nameof(LuaBase.ExecuteScript))]
	static bool ExecuteScriptPrefix(LuaBase __instance, string script)
	{
		if (!Enabled.Value || !script.Contains("tcejbo gninwapS", StringComparison.OrdinalIgnoreCase))
			return true;
		Chat.SendChat(
			$"Infected Lua script {__instance.GetScriptName()} intercepted by {Main.PluginColour.RGBHex}{Main.PLUGIN_NAME}[-]",
			Main.ErrorColour
		);
		return false;
	}
}

