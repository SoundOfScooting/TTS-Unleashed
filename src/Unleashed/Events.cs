using System.Runtime.CompilerServices;
using UnityEngine.Events;

namespace Unleashed;

[HarmonyPatch]
static class Events
{
	// #generic
	[HarmonyILManipulator]
	[HarmonyPatch(typeof(EventUtil), nameof(EventUtil.RaiseSafe), [typeof(Action)])]
	// [HarmonyPatch(typeof(EventUtil), nameof(EventUtil.RaiseSafe), [typeof(Action<object>), typeof(object)])]
	// [HarmonyPatch(typeof(EventUtil), nameof(EventUtil.RaiseSafe), [typeof(Action<object,object>), typeof(object), typeof(object)])]
	static void RaiseSafeIL(ILContext il)
	{
		var c = new ILCursor(il);
		c.GotoNext(MoveType.After,
			// UnityEngine.Debug.LogException(exception);
			x => x.MatchCall((Action<Exception>) Debug.LogException)
		);
		// for some reason instructions can't be inserted before
		c.Prev.OpCode  = OpCodes.Dup;
		c.Prev.Operand = null;
		c.EmitDelegate(
			void(Exception exception) =>
			{
				Main.Log.LogError(exception);
				Debug.LogException(exception);
			}
		);
	}

	public static UnityEvent OnStartConnected = new();
	public static UnityEvent OnStartDisconnected = new();
	public static UnityEvent<PlayerState> OnPlayersAddOther = new();
	public static UnityEvent OnStartGlobalContextual = new();
	public static UnityEvent OnStartContextual = new();

	[HarmonyPostfix]
	[HarmonyPatch(typeof(NetworkUI), nameof(NetworkUI.Start))]
	static void TriggerStartDisconnected()
		=> Main.Catch(OnStartDisconnected.Invoke, ChatMessageType.System);
	static void TriggerStartConnected()
		=> Main.Catch(OnStartConnected.Invoke);
	static void TriggerPlayersAddOther(PlayerState playerState)
		=> Main.Catch(() => OnPlayersAddOther.Invoke(playerState));
	[HarmonyPostfix]
	[HarmonyPatch(typeof(Pointer), nameof(Pointer.StartGlobalContextual))]
	static void TriggerStartGlobalContextual()
		=> Main.Catch(OnStartGlobalContextual.Invoke);
	static void TriggerStartContextual()
		=> Main.Catch(OnStartContextual.Invoke);
	[HarmonyILManipulator]
	[HarmonyPatch(typeof(Pointer), nameof(Pointer.StartContextual))]
	static void StartContextualIL(ILContext il)
	{
		var c = new ILCursor(il);
		c.GotoNext(MoveType.After,
			// NetworkInstance.GUIContextualMenu.SetActive(value: true);
			x => x.MatchLdfld(() => NetworkUI.__instance().GUIContextualMenu),
			x => x.MatchLdcI4(1),
			x => x.MatchCallvirt(GameObject.__instance().SetActive)
		);
		c.Index--;
		c.EmitDelegate(TriggerStartContextual);
	}

	static bool addingAllPlayers; // annoying

	[ModuleInitializer]
	internal static void ModuleInitializer()
	{
		NetworkEvents.OnServerInitialized += OnServerInitialized;
		NetworkEvents.OnConnectedToServer += OnConnectedToServer;
		EventManager.OnPlayersAdd += OnPlayersAdd;
	}
	static void OnServerInitialized()
		=> addingAllPlayers = false;
	static void OnConnectedToServer()
	{
		addingAllPlayers = true;
		Wait.Time(() => addingAllPlayers = false, 5f); // failsafe
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UINotepad), nameof(UINotepad.SetNotepadRPC))]
	static void SetNotepadRPCRPCPrefix()
		=> addingAllPlayers = false; // rpc from server in NetworkUI.OnPlayerConnect after all players added

	static void OnPlayersAdd(PlayerState playerState)
	{
		// Chat.Log($"OnPlayersAdd {playerState.ID}", Main.PluginColour);
		if (playerState.ID == Network.ID)
		{
			TriggerStartConnected();
			return;
		}
		if (!NetworkUI.Instance.IsHotseat && !addingAllPlayers)
		{
			TriggerPlayersAddOther(playerState);
			return;
		}
	}
}

