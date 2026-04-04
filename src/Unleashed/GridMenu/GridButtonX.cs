using static UIGridMenu;

namespace Unleashed.GridMenu;

[HarmonyPatch]
static class GridButtonX
{
	public const string TAG_HOST = $"{Main.PLUGIN_GUID}/Host";
	public const string TAG_GOLD = $"{Main.PLUGIN_GUID}/Gold";

	[HarmonyPrefix]
	[HarmonyPatch(typeof(GridButton), nameof(GridButton.IsSearched))]
	static bool IsSearchedPrefix(GridButton __instance, ref bool __result)
	{
		if (__instance.Tags.Contains(TAG_HOST) && !API.HostModded)
			return (false, __result = false).Item1;
		if (__instance.Tags.Contains(TAG_GOLD) && !SteamManager.IsKickstarterGold)
			return (false, __result = false).Item1;
		return true;
	}
}

