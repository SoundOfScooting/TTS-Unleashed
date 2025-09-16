using Steamworks;
using Unleashed.Settings;

namespace Unleashed.Patches;

[HarmonyPatch]
static class Debug
{
	[Setting]
	static readonly DebugSetting<bool> DeveloperMode = new()
	{
		Key     = "TTS Developer Mode",
		Default = false,
	};
	[PowerSetting]
	static readonly DebugSetting<bool> AllRewards = new()
	{
		Key       = "All Kickstarter Rewards",
		Default   = false,
		OnLoaded  = UpdateRewards,
		OnChanged = UpdateRewards,
	};
	// [PowerSetting]
	// static readonly DebugSetting<bool> AllDLC = new()
	// {
	// 	Key     = "All DLC",
	// 	Default = false,
	// };
	static void UpdateRewards(bool value)
	{
		SteamManager.bKickstarterPointer = value || SteamApps.BIsSubscribedApp(SteamManager.KickstarterPointer);
		SteamManager.bKickstarterGold    = value || SteamApps.BIsSubscribedApp(SteamManager.KickstarterGold);
	}

	[HarmonyPrefix]
	// [HarmonyPatch(typeof(Developer), nameof(Developer.HasName))]
	[HarmonyPatch(typeof(Developer), nameof(Developer.HasSteamID))]
	static bool DeveloperHasPrefix(ref bool __result)
	{
		if (DeveloperMode.Value)
		{
			__result = true;
			return false;
		}
		return true;
	}

	[HarmonyILManipulator]
	[HarmonyPatch(typeof(SteamManager), nameof(SteamManager.Init))]
	static void SteamManagerInitIL(ILContext il)
	{
		var c = new ILCursor(il);
		c.GotoNext(MoveType.After,
			// bKickstarterGold = SteamApps.BIsSubscribedApp(KickstarterGold);
			x => x.MatchStsfld(AccessTools.Field(typeof(SteamManager), nameof(SteamManager.bKickstarterGold)))
		);
		c.EmitDelegate(void() =>
			UpdateRewards(AllRewards.Value)
		);
	}
	// [HarmonyPrefix]
	// [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.IsSubscribedApp))]
	// static bool SteamManagerIsSubscribedAppPrefix(ref bool __result)
	// {
	// 	if (AllDLC.Value)
	// 	{
	// 		__result = true;
	// 		return false;
	// 	}
	// 	return true;
	// }

	// [HarmonyPostfix]
	// [HarmonyPatch(typeof(NetworkUI), nameof(NetworkUI.SetSpecificPlayerName))]
	// static void NetworkUISetSpecificPlayerNamePostfix(NetworkUI __instance, string newName)
	// {
	// 	if (!Network.isServer || __instance.bHotseat || __instance.playerIDToSet == -1)
	// 		return;

	// 	var player = PlayerManager.Instance.PlayerStateFromID(__instance.playerIDToSet);
	// 	player.name = newName;
	// 	// update playerName which does nothing
	// 	__instance.RPC(player.networkPlayer, __instance.UpdateName, newName);

	// 	// would trigger Auto Join Message
	// 	var playerData = new PlayerManager.PlayerData(player);
	// 	PlayerManager.Instance.RemovePlayer(player.id);
	// 	PlayerManager.Instance.AddPlayer   (playerData);
	// 	// PlayerManager.Instance.RPC(RPCTarget.Others, PlayerManager.Instance.RPCRemovePlayer, player.id);
	// 	// PlayerManager.Instance.RPC(RPCTarget.Others, PlayerManager.Instance.RPCAddPlayer,    playerData);
	// }
}

