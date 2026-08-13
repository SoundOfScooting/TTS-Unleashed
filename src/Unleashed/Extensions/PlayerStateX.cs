using System.Runtime.CompilerServices;

namespace Unleashed.Extensions;

[HarmonyPatch]
static class PlayerStateX
{
	[ModuleInitializer]
	internal static void Initializer()
	{
		Events.OnStartConnected    += OnStartConnected;
		Events.OnStartDisconnected += OnStartDisconnected;
	}
	static void OnStartConnected()
		=> PlayerManager.Instance.MyPlayerState().IsModded = true;
	static void OnStartDisconnected()
		=> CWT = [];

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

	[HarmonyPostfix]
	[HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.Remove))]
	static void RemovePostfix(PlayerState playerState)
		=> CWT.Remove(playerState);
}

