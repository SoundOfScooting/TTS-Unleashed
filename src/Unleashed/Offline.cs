using System;
using HarmonyLib;
using NewNet;
using Steamworks;

namespace Unleashed;

[HarmonyPatch]
public static class Offline
{
	public static bool NoSteam      => Utilities.IsLaunchOption("-nosteam");
	public static bool Singleplayer => Network.maxConnections == 0;

	[HarmonyPrefix]
	[HarmonyPatch(typeof(SteamUser),        nameof(SteamUser.GetSteamID))]
	[HarmonyPatch(typeof(SteamManager),     nameof(SteamManager.StringToSteamID))]
	[HarmonyPatch(typeof(UIProfilePotrait), nameof(UIProfilePotrait.OnClick))]
	static bool SkipNoSteam() =>
		!NoSteam; // returns default

	[HarmonyPrefix]
	[HarmonyPatch(typeof(SteamLobbyManager), nameof(SteamLobbyManager.Start))]
	static bool StartPrefix(SteamLobbyManager __instance)
	{
		if (NoSteam)
		{
			NetworkEvents.OnServerInitializing += __instance.ServerInitializing;
			return false;
		}
		return true;
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(Network), nameof(Network.InitializeServer))]
	static bool InitializeServerPrefix()
	{
		if (NoSteam && Singleplayer)
		{
			NetworkEvents.TriggerServerInitializing();
			return false;
		}
		return true;
	}
}

