namespace Unleashed.Patches;

[HarmonyPatch]
static class Nickname
{
	static Settings.Setting<string> Entry => Settings.EntryNickname;

	[HarmonyPrefix]
	[HarmonyPatch(typeof(NetworkEvents), nameof(NetworkEvents.TriggerServerInitialized))]
	[HarmonyPatch(typeof(NetworkEvents), nameof(NetworkEvents.TriggerConnectingToServer))]
	static void TriggerServerInitializedPrefix()
	{
		if (Entry.Value is [_, ..] nickname)
			NetworkUI.Instance.SetPlayerName(nickname);
	}
}
