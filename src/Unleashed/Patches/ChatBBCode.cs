using Unleashed.Settings;

namespace Unleashed.Patches;

[HarmonyPatch]
static class ChatBBCode
{
	// #idea: multiline chat box

	[Setting]
	static readonly Setting<bool> BBCode = new()
	{
		Section = Section.Misc,
		Key     = "Chat BBCode",
		Default = true,
		Description =
			"""
			Prevents stripping of BBCode in chat messages (only for modded recipients).
			For example, you can say `[ff0000]this is a [b]red[/b] message[-]`.
			"""
	};

	// [HarmonyPostfix]
	// [HarmonyPatch(typeof(UIChatInput), nameof(UIChatInput.Start))]
	// static void StartPostfix(UIChatInput __instance)
	// 	=> __instance.gameObject.GetOrAddComponent<UIInputBBCode>().Awake();

	[HarmonyILManipulator]
	[HarmonyPatch(typeof(UIChatInput), nameof(UIChatInput.OnSubmit))]
	[HarmonyPatch(typeof(Chat), nameof(Chat.RPC_PlayerChatMessage))]
	static void OnSubmitIL(ILContext il)
	{
		var c = new ILCursor(il);
		c.GotoNext(MoveType.Before,
			x => x.MatchCall(AccessTools.Method(typeof(NGUIText), nameof(NGUIText.StripSymbols)))
		);
		c.MoveAfterLabels();
		c.Remove();
		c.EmitDelegate(string(string text)
			=> BBCode.Value ? text : NGUIText.StripSymbols(text)
		);
	}
}

