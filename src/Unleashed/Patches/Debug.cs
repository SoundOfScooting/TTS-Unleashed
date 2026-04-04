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
		SteamManager.IsKickstarterPointer = SteamApps.BIsSubscribedApp(SteamManager.KickstarterPointer);
		SteamManager.IsKickstarterGold    = SteamApps.BIsSubscribedApp(SteamManager.KickstarterGold);
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

	[HarmonyPrefix]
	[HarmonyPatch(typeof(SteamApps), nameof(SteamApps.BIsSubscribedApp))]
	static bool BIsSubscribedAppPrefix(AppId_t appID, ref bool __result)
	{
		switch (appID)
		{
			case var a1 when a1 == SteamManager.KickstarterPointer:
			case var a2 when a2 == SteamManager.KickstarterGold:
				if (AllRewards.Value)
					return (false, __result = true).Item1;
				break;
			// default:
			// 	if (ALLDLC.Value)
			// 		return (false, __result = true).Item1;
			// 	break;
		}
		if (Offline.NoSteam)
		{
			__result = false;
			return false;
		}
		return true;
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

