using System.Runtime.CompilerServices;

namespace Unleashed.Extensions;

[HarmonyPatch]
static class PlayerStateX
{
	[ModuleInitializer]
	internal static void Initializer()
	{
		Events.OnStartDisconnected += OnStartDisconnected;
		Events.OnStartConnected    += OnStartConnected;
	}
	static void OnStartDisconnected() =>
		CWT = new();
	static void OnStartConnected() =>
		PlayerManager.Instance.MyPlayerState().IsModded = true;

	static ConditionalWeakTable<PlayerState, Data> CWT;
	sealed class Data
	{
		public bool IsModded;
	}
	extension(PlayerState @this)
	{
		Data Data => CWT.GetOrCreateValue(@this);
		public ref bool IsModded => ref @this.Data.IsModded;
	}

	// #todo: apparently CWT is broken in this Unity version?
	[HarmonyPostfix]
	[HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.Remove))]
	static void RemovePostfix(PlayerState playerState) =>
		CWT.Remove(playerState);
}

