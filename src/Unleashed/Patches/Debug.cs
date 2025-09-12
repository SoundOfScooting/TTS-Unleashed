namespace Unleashed.Patches;

[HarmonyPatch]
static class Debug
{
#if TRUE_ULTIMATE_POWER
	static Settings.Setting<bool> EntryAllRewards => Settings.DebugAllRewards;
	// static Settings.Setting<bool> EntryAllDLC => Settings.DebugAllDLC;
#endif
	static Settings.Setting<bool> EntryDeveloperMode => Settings.DebugDeveloperMode;

#if TRUE_ULTIMATE_POWER
	[HarmonyILManipulator]
	[HarmonyPatch(typeof(SteamManager), nameof(SteamManager.Init))]
	static void SteamManagerInitIL(ILContext il)
	{
		var c = new ILCursor(il);
		c.GotoNext(MoveType.Before,
			// bKickstarterPointer = SteamApps.BIsSubscribedApp(KickstarterPointer);
			x => x.MatchStsfld(AccessTools.Field(typeof(SteamManager), nameof(SteamManager.bKickstarterPointer)))
		);
		c.EmitDelegate(bool(bool subscribed) =>
			EntryAllRewards.Value || subscribed
		);
		c.GotoNext(MoveType.Before,
			// bKickstarterGold = SteamApps.BIsSubscribedApp(KickstarterGold);
			x => x.MatchStsfld(AccessTools.Field(typeof(SteamManager), nameof(SteamManager.bKickstarterGold)))
		);
		c.EmitDelegate(bool(bool subscribed) =>
			EntryAllRewards.Value || subscribed
		);
	}
	// [HarmonyPrefix]
	// [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.IsSubscribedApp))]
	// static bool SteamManagerIsSubscribedAppPrefix(ref bool __result)
	// {
	// 	if (EntryAllDLC.Value)
	// 	{
	// 		__result = true;
	// 		return false;
	// 	}
	// 	return true;
	// }
#endif

	[HarmonyPrefix]
	// [HarmonyPatch(typeof(Developer), nameof(Developer.HasName))]
	[HarmonyPatch(typeof(Developer), nameof(Developer.HasSteamID))]
	static bool DeveloperHasPrefix(ref bool __result)
	{
		if (EntryDeveloperMode.Value)
		{
			__result = true;
			return false;
		}
		return true;
	}
}

