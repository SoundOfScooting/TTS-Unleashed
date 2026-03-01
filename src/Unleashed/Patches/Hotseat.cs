using System.Reflection;

namespace Unleashed.Patches;

[HarmonyPatch]
static class Hotseat
{
	[HarmonyPatch]
	static class PatchNetworkSender
	{
		[HarmonyPrepare]
		static bool Prepare() => API.TRUE_ULTIMATE_POWER;
		[HarmonyTargetMethods]
		static IEnumerable<MethodBase> TargetMethods() => [
			AccessTools.Method(typeof(PlayerManager), nameof(PlayerManager.RPCTyping)),
			AccessTools.Method(typeof(PlayerManager), nameof(PlayerManager.RequestBlindfoldRPC)), // buggy
			AccessTools.Method(typeof(Chat), nameof(Chat.RPC_ChatMessage)),
			AccessTools.Method(typeof(Chat), nameof(Chat.RPC_ChatWhisperMessage)), // useless
			AccessTools.Method(typeof(Chat), nameof(Chat.RPC_ChatTeamMessage)),    // useless
		];
		[HarmonyPrefix]
		static void Prefix()
		{
			if (NetworkUI.Instance.IsHotseat)
				Network.Sender = new(Network.ID);
		}
		[HarmonyPostfix]
		static void Postfix()
		{
			if (NetworkUI.Instance.IsHotseat)
				Network.Sender = NetworkPlayer.GetServerPlayer();
		}
	}
}

