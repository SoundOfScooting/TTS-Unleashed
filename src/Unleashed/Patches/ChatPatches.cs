using Unleashed.Settings;

namespace Unleashed.Patches;

[HarmonyPatch]
static class ChatPatches
{
	[Setting]
	static readonly Setting<bool> Enabled = new()
	{
		MigrateFrom = [
			("General", "Enable Fast Commands"), // 0.1.0
		],
		Section     = Section.Controls,
		Key         = "Enable Fast Commands",
		Default     = true,
		Description =
			"""
			When shift is not held down, the 'Help' control instead starts typing a command in chat.
			Best used when 'Help' is bound to /.
			""",
	};
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

	[HarmonyILManipulator]
	[HarmonyPatch(typeof(NetworkUI), nameof(NetworkUI.Update))]
	static void UpdateIL(ILContext il)
	{
		var c = new ILCursor(il);
		c.GotoNext(MoveType.After,
			x => x.MatchLdstr("Help"),
			x => x.MatchLdcI4((int) ControlType.Keyboard),
			x => x.MatchCall(AccessTools.Method(typeof(zInput), nameof(zInput.GetButtonDown)))
		);
		c.EmitDelegate(bool(bool helpDown) =>
		{
			if (!helpDown || !Enabled.Value || zInput.GetButton("Shift"))
				return helpDown;
			if (!UICamera.SelectIsInput())
			{
				UIChatInput.Instance.ChatButtonOnClick();
				UIChatInput.Instance.mInput.value = "/";
			}
			return false;
		});
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(UIChatInput), nameof(UIChatInput.Start))]
	static void StartPostfix(UIChatInput __instance)
	{
		// __instance.gameObject.GetOrAddComponent<UIInputBBCode>().Awake();

		// #todo: make input box grow and log shrink like MultilineName
		__instance.transform.Find("Label").GetComponent<UILabel>().multiLine = true;
	}

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

