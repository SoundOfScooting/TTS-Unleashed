using System.Runtime.CompilerServices;

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
	public static event Action<PlayerState> OnPlayersAddOther;
	public static event Action OnStartGlobalContextual;
	public static event Action OnStartContextual;

	[HarmonyPostfix]
	[HarmonyPatch(typeof(NetworkUI), nameof(NetworkUI.Start))]
	static void TriggerStartDisconnected()
	{
		if (OnStartDisconnected is {} action)
			Main.Try(action, ChatMessageType.System);
	}
	static void TriggerStartConnected()
	{
		if (OnStartConnected is {} action)
			Main.Try(action);
	}
	static void TriggerPlayersAddOther(PlayerState playerState)
	{
		if (OnPlayersAddOther is {} action)
			Main.Try(() =>
			{
				action.Invoke(playerState);
				return default(object);
			});
	}
	[HarmonyPostfix]
	[HarmonyPatch(typeof(Pointer), nameof(Pointer.StartGlobalContextual))]
	static void TriggerStartGlobalContextual()
	{
		if (OnStartGlobalContextual is {} action)
			Main.Try(action);
	}
	static void TriggerStartContextual()
	{
		if (OnStartContextual is {} action)
			Main.Try(action);
	}
	[HarmonyILManipulator]
	[HarmonyPatch(typeof(Pointer), nameof(Pointer.StartContextual))]
	static void StartContextualIL(ILContext il)
	{
		var c = new ILCursor(il);
		c.GotoNext(MoveType.After,
			// NetworkInstance.GUIContextualMenu.SetActive(value: true);
			x => x.MatchLdfld(AccessTools.Field(typeof(NetworkUI), nameof(NetworkUI.GUIContextualMenu))),
			x => x.MatchLdcI4(1),
			x => x.MatchCallvirt(AccessTools.Method(typeof(GameObject), nameof(GameObject.SetActive)))
		);
		c.Index--;
		c.EmitDelegate(TriggerStartContextual);
	}

	static bool addingAllPlayers; // annoying

	[ModuleInitializer]
	internal static void Initializer()
	{
		NetworkEvents.OnServerInitialized += OnServerInitialized;
		NetworkEvents.OnConnectedToServer += OnConnectedToServer;
		EventManager.OnPlayersAdd += OnPlayersAdd;
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
		if (!NetworkUI.Instance.bHotseat && !addingAllPlayers)
		{
			TriggerPlayersAddOther(playerState);
			return;
		}
	}
}

