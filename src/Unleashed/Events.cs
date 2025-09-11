namespace Unleashed;

[HarmonyPatch]
public static class Events
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(NetworkUI), nameof(NetworkUI.Start))]
	static void StartDisconnected()
	{
		Compat.StartDisconnected();
	}
	static void StartConnected()
	{
		Compat.StartConnected();
		MainUI.StartConnected();
	}

	static bool addingAllPlayers; // annoying

	public static void Load()
	{
		NetworkEvents.OnServerInitialized += OnServerInitialized;
		NetworkEvents.OnConnectedToServer += OnConnectedToServer;
		// NetworkEvents.OnPlayerConnected   += OnPlayerConnected;
		EventManager .OnPlayersAdd        += OnPlayersAdd;
	}
	static void OnServerInitialized() =>
		addingAllPlayers = false;
	static void OnConnectedToServer()
	{
		addingAllPlayers = true;
		Wait.Time(() => addingAllPlayers = false, 5f); // failsafe
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UINotepad), nameof(UINotepad.UpdateNotepadRPC))]
	static void UpdateNotepadRPCPrefix() =>
		addingAllPlayers = false; // rpc from server in NetworkUI.OnPlayerConnect after all players added

	static void OnPlayersAdd(PlayerState playerState)
	{
		// Chat.Log($"OnPlayersAdd {playerState.id}", Main.PluginColour);
		if (playerState.id == NetworkID.ID)
		{
			StartConnected();
			return;
		}
		if (NetworkUI.Instance.bHotseat)
			return;

		if (!addingAllPlayers && Settings.EntryAutoJoinMessage.Value is [_, ..] autoJoin)
			Chat.SendChatMessage(autoJoin);

		if (Network.isServer && Settings.AutoPromoteIDs.Contains(playerState.steamId))
			Wait.Frames(() =>
				PlayerManager.Instance.PromoteThisPlayer(playerState.name)
			);
	}
}

