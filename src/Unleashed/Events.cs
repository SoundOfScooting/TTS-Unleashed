namespace Unleashed;

// terrible
interface IEvent<T> where T : Delegate
{
	T Trigger { get; }
	void operator +=(T listener);
	void operator -=(T listener);
}
record struct Event<T>(T Trigger = null) : IEvent<T> where T : Delegate
{
	public T Trigger { get; private set; } = Trigger;
	public void operator +=(T listener) =>
		Trigger = (T) Delegate.Combine(Trigger, listener);
	public void operator -=(T listener) =>
		Trigger = (T) Delegate.Remove(Trigger, listener);
}

[HarmonyPatch]
static class Events
{
	public static event Action OnStartConnected;
	public static event Action OnStartDisconnected;
	// public static event Action OnPlayersAddOther;
	public static event Action OnStartGlobalContextual;
	public static event Action OnStartContextual;

	[HarmonyPostfix]
	[HarmonyPatch(typeof(NetworkUI), nameof(NetworkUI.Start))]
	static void TriggerStartDisconnected() =>
		OnStartDisconnected?.Invoke();
	static void TriggerStartConnected() =>
		OnStartConnected?.Invoke();
	// static void TriggerPlayersAddOther() =>
	// 	OnPlayersAddOther?.Invoke();
	[HarmonyPostfix]
	[HarmonyPatch(typeof(Pointer), nameof(Pointer.StartGlobalContextual))]
	static void TriggerStartGlobalContextual() =>
		OnStartGlobalContextual?.Invoke();
	internal static void TriggerStartContextual() =>
		OnStartContextual?.Invoke();

	static bool addingAllPlayers; // annoying

	public static void Load()
	{
		NetworkEvents.OnServerInitialized += OnServerInitialized;
		NetworkEvents.OnConnectedToServer += OnConnectedToServer;
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
			TriggerStartConnected();
			return;
		}
		if (NetworkUI.Instance.bHotseat)
			return;

		// TriggerPlayersAddOther();

		if (!addingAllPlayers && Settings.EntryAutoJoinMessage.Value is [_, ..] autoJoin)
			Chat.SendChatMessage(autoJoin);

		if (Network.isServer && Settings.AutoPromoteIDs.Contains(playerState.steamId))
			Wait.Frames(() =>
				PlayerManager.Instance.PromoteThisPlayer(playerState.name)
			);
	}
}

