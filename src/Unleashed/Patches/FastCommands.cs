namespace Unleashed.Patches;

[HarmonyPatch]
static class FastCommands
{
	static Settings.Setting<bool> Enabled => Settings.EntryEnableFastCommands;

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
}

