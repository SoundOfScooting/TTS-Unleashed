using Unleashed.Settings;

namespace Unleashed.Patches;

[HarmonyPatch]
static class CameraControls
{
	[Setting]
	static readonly Setting<bool> BlockMousePanningOverUI = new()
	{
		MigrateFrom = [
			("General", "Block Mouse Panning Over UI"), // 0.1.0
		],
		Section     = Section.Controls,
		Key         = "Block Mouse Panning Over UI",
		Default     = true,
		Description =
			"""
			Blocks 'Camera Hold Rotate' control while hovering over UI.
			This eases right clicking UI elements, but prevents mouse panning over large panels.
			""",
	};
	[Setting]
	static readonly Setting<bool> InvertHorizontalAxis = new()
	{
		MigrateFrom = [
			("General", "Invert Horizontal 3P Controls"), // 0.1.0
		],
		Section     = Section.Controls,
		Key         = "Invert Third-Person Horizontal Axis",
		Default     = false,
		Description =
			"""
			Swaps controls 'Camera Left' with 'Camera Right' in third-person and top-down view.
			This aligns them with first-person view and mouse panning.
			""",
	};
	[Setting]
	static readonly Setting<bool> InvertVerticalAxis = new()
	{
		MigrateFrom = [
			("General", "Invert Vertical 3P Controls"), // 0.1.0
		],
		Section     = Section.Controls,
		Key         = "Invert Third-Person Vertical Axis",
		Default     = false,
		Description =
			"""
			Swaps controls 'Camera Down' with 'Camera Up' in third-person view.
			This aligns them with first-person view and mouse panning.
			""",
	};

	[HarmonyILManipulator]
	[HarmonyPatch(typeof(CameraController), nameof(CameraController.Update))]
	static void UpdateIL(ILContext il)
	{
		var c = new ILCursor(il);
		c.GotoNext(MoveType.After,
			// x += zInput.GetAxis("Camera Horizontal") * 100f * -1f * Time.deltaTime * xLookMulti;
			x => x.MatchCall(() => CameraController.__instance().xLookMulti),
			x => x.MatchMul(),
			x => x.MatchAdd(),
			x => x.MatchStfld(() => CameraController.__instance().x)
		);
		c.Index -= 2;
		c.Remove();
		c.EmitDelegate(
			float(float x, float dx)
				=> InvertHorizontalAxis.Value
					? x - dx
					: x + dx
		);

		c.GotoNext(MoveType.After,
			// y -= zInput.GetAxis("Camera Vertical") * 100f * -1f * Time.deltaTime * yLookMulti;
			x => x.MatchCall(() => CameraController.__instance().yLookMulti),
			x => x.MatchMul(),
			x => x.MatchSub(),
			x => x.MatchStfld(() => CameraController.__instance().y)
		);
		c.Index -= 2;
		c.Remove();
		c.EmitDelegate(
			float(float y, float dy)
				=> InvertVerticalAxis.Value
					? y + dy
					: y - dy
		);

		c.Index   = 0;
		var found = 0;
		while (c.TryGotoNext(MoveType.After,
			// zInput.GetButton("Camera Hold Rotate")
			x => x.MatchLdstr("Camera Hold Rotate"),
			x => x.MatchLdcI4(0),
			x => x.MatchCall(zInput.GetButton)
		)){
			found++;
			c.EmitDelegate(
				bool(bool cameraHoldRotateDown)
					=> cameraHoldRotateDown && !(
						BlockMousePanningOverUI.Value &&
						UICamera.HoverOverUI()
					)
			);
		}
		if (found != 2)
			Main.Log.LogWarning($"{nameof(CameraControls)}.{nameof(UpdateIL)} expected 2, found {found}");
	}
}

