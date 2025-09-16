using Unleashed.Settings;

namespace Unleashed.Patches;

[HarmonyPatch]
static class FastCommands
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

