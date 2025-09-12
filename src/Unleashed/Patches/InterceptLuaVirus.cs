namespace Unleashed.Patches;

[HarmonyPatch]
static class InterceptLuaVirus
{
	static Settings.Setting<bool> Enabled => Settings.EntryInterceptLuaVirus;

	[HarmonyPrefix]
	[HarmonyPatch(typeof(LuaScript), nameof(LuaScript.DoString))]
	static bool DoStringPrefix(LuaScript __instance) =>
		ExecuteScriptPrefix(__instance, __instance.script_code);
	[HarmonyPrefix]
	[HarmonyPatch(typeof(LuaScript), nameof(LuaScript.ExecuteScript))]
	static bool ExecuteScriptPrefix(LuaScript __instance, string script)
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

