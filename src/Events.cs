using System;
using System.Linq;
using HarmonyLib;
using NewNet;

namespace Unleashed;

[HarmonyPatch]
public static class Events
{
	private static bool addingAllPlayers; // annoying

	public static void Load()
	{
		NetworkEvents.OnServerInitialized += OnServerInitialized;
		NetworkEvents.OnConnectedToServer += OnConnectedToServer;
		// NetworkEvents.OnPlayerConnected   += OnPlayerConnected;
		EventManager .OnPlayersAdd        += OnPlayersAdd;
	}
	private static void OnServerInitialized() =>
		addingAllPlayers = false;
	private static void OnConnectedToServer()
	{
		addingAllPlayers = true;
		// failsafe
		Wait.Time(() => addingAllPlayers = false, 5f);
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UINotepad), nameof(UINotepad.UpdateNotepadRPC))]
	private static void UpdateNotepadRPCPrefix() =>
		// rpc from server in NetworkUI.OnPlayerConnect after all players added
		addingAllPlayers = false;

	private static void OnPlayersAdd(PlayerState playerState)
	{
		// Chat.Log($"OnPlayersAdd {playerState.id}", Main.PluginColour);
		if (playerState.id == NetworkID.ID)
		{
			playerState.X().IsModded = true;
			MainUI.StartConnected();
			return;
		}
		if (!addingAllPlayers && Settings.EntryAutoJoinMessage.Value is not (null or []))
			Chat.SendChatMessage(Settings.EntryAutoJoinMessage.Value);

		if (Network.isServer && Settings.AutoPromoteIDs.Contains(playerState.steamId))
			Wait.Frames(() =>
				PlayerManager.Instance.PromoteThisPlayer(playerState.name)
			);
	}
}