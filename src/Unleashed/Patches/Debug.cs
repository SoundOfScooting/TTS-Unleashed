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
		OnChanged = _ => UpdateRewards(),
	};
	// [PowerSetting]
	// static readonly DebugSetting<bool> AllDLC = new()
	// {
	// 	Key     = "All DLC",
	// 	Default = false,
	// };
	static void UpdateRewards()
	{
		SteamManager.IsKickstarterPointer = SteamApps.BIsSubscribedApp(SteamManager.KickstarterPointer);
		SteamManager.IsKickstarterGold    = SteamApps.BIsSubscribedApp(SteamManager.KickstarterGold);
	}

	[HarmonyPrefix]
	// [HarmonyPatch(typeof(Developer), nameof(Developer.HasName))]
	[HarmonyPatch(typeof(Developer), nameof(Developer.HasSteamID))]
	static bool DeveloperHasPrefix(ref bool __result)
	{
		if (DeveloperMode.Value)
			return (false, __result = true).Item1;
		return true;
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(SteamManager), nameof(SteamManager.Awake))]
	static void AwakePostfix()
	{
		if (Offline.NoSteam && !SteamManager.isEverInitialized)
			UpdateRewards();
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(SteamApps), nameof(SteamApps.BIsSubscribedApp))]
	static bool BIsSubscribedAppPrefix(AppId_t appID, ref bool __result)
	{
		if (OverrideIsSubscribed(appID))
			return (false, __result = true).Item1;
		if (Offline.NoSteam)
			return (false, __result = false).Item1;
		return true;
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(SteamManager), nameof(SteamManager.IsSubscribedApp))]
	static bool IsSubscribedAppPrefix(int appId, ref bool __result)
	{
		if (OverrideIsSubscribed(new((uint) appId)))
			return (false, __result = true).Item1;
		if (Offline.NoSteam)
			return (false, __result = false).Item1;
		return true;
	}
	static bool OverrideIsSubscribed(AppId_t appID)
	{
		switch (appID)
		{
			case var a1 when a1 == SteamManager.KickstarterPointer:
			case var a2 when a2 == SteamManager.KickstarterGold:
				if (AllRewards.Value)
					return true;
				return false;
			default:
				// if (ALLDLC.Value)
				// 	return true;
				return false;
		}
	}

	// [HarmonyPostfix]
	// [HarmonyPatch(typeof(NetworkUI), nameof(NetworkUI.SetSpecificPlayerName))]
	// static void NetworkUISetSpecificPlayerNamePostfix(NetworkUI __instance, string newName)
	// {
	// 	if (!Network.IsServer || __instance.IsHotseat || __instance.playerIDToSet == -1)
	// 		return;

	// 	var player = PlayerManager.Instance.PlayerStateFromID(__instance.playerIDToSet);
	// 	player.name = newName;
	// 	// update playerName which does nothing
	// 	__instance.RPC(player.networkPlayer, __instance.UpdateName, newName);

	// 	// would trigger Auto Join Message
	// 	var playerData = new PlayerManager.PlayerData(player);
	// 	PlayerManager.Instance.RemovePlayer(player.ID);
	// 	PlayerManager.Instance.AddPlayer   (playerData);
	// 	// PlayerManager.Instance.RPC(RPCTarget.Others, PlayerManager.Instance.RPCRemovePlayer, player.ID);
	// 	// PlayerManager.Instance.RPC(RPCTarget.Others, PlayerManager.Instance.RPCAddPlayer,    playerData);
	// }
}

