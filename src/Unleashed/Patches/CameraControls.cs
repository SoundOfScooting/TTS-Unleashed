namespace Unleashed.Patches;

[HarmonyPatch]
static class CameraControls
{
	static Settings.Setting<bool> InvertHorizontalAxis    => Settings.EntryInvertHorizontal3PControls;
	static Settings.Setting<bool> InvertVerticalAxis      => Settings.EntryInvertVertical3PControls;
	static Settings.Setting<bool> BlockMousePanningOverUI => Settings.EntryBlockMousePanningOverUI;

	[HarmonyILManipulator]
	[HarmonyPatch(typeof(CameraController), nameof(CameraController.Update))]
	static void UpdateIL(ILContext il)
	{
		var c = new ILCursor(il);
		c.GotoNext(MoveType.After,
			// x += zInput.GetAxis("Camera Horizontal") * 100f * -1f * Time.deltaTime * xLookMulti;
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(CameraController), nameof(CameraController.xLookMulti))),
			x => x.MatchMul(),
			x => x.MatchAdd(),
			x => x.MatchStfld(AccessTools.Field(typeof(CameraController), nameof(CameraController.x)))
		);
		c.Index -= 2;
		c.Remove();
		c.EmitDelegate(float(float x, float dx) =>
			InvertHorizontalAxis.Value
				? x - dx
				: x + dx
		);

		c.GotoNext(MoveType.After,
			// y -= zInput.GetAxis("Camera Vertical") * 100f * -1f * Time.deltaTime * yLookMulti;
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(CameraController), nameof(CameraController.yLookMulti))),
			x => x.MatchMul(),
			x => x.MatchSub(),
			x => x.MatchStfld(AccessTools.Field(typeof(CameraController), nameof(CameraController.y)))
		);
		c.Index -= 2;
		c.Remove();
		c.EmitDelegate(float(float y, float dy) =>
			InvertVerticalAxis.Value
				? y + dy
				: y - dy
		);

		c.Index   = 0;
		int found = 0;
		while (c.TryGotoNext(MoveType.After,
			// zInput.GetButton("Camera Hold Rotate")
			x => x.MatchLdstr("Camera Hold Rotate"),
			x => x.MatchLdcI4(0),
			x => x.MatchCall(AccessTools.Method(typeof(zInput), nameof(zInput.GetButton)))
		)){
			found++;
			c.EmitDelegate(bool(bool cameraHoldRotateDown) =>
				cameraHoldRotateDown && !(
					BlockMousePanningOverUI.Value &&
					UICamera.HoverOverUI()
				)
			);
		}
		if (found != 2)
			Main.Log.LogWarning($"{nameof(CameraControls)}.{nameof(UpdateIL)} expected 2, found {found}");
	}
}

