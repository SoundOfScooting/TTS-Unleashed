namespace Unleashed.Patches;

[HarmonyPatch]
static class TiltRotation
{
	[HarmonyPrefix]
	[HarmonyPatch(typeof(Pointer), nameof(Pointer.Update))]
	static void UpdatePrefix(Pointer __instance) // #hack
	{
		if      (zInput.GetButtonUp("Grab"))
		foreach (var grabbableNPO in ManagerPhysicsObject.Instance.GrabbableNPOs)
		if      (grabbableNPO.HeldByPlayerID == __instance.ID)
		if      (grabbableNPO.HasData())
			grabbableNPO.HeldTiltRotationIndex = 0;
	}
	[HarmonyILManipulator]
	[HarmonyPatch(typeof(Pointer), nameof(Pointer.Update))]
	static void UpdateIL(ILContext il)
	{
		var c = new ILCursor(il);
		for (var i = 0; i < 4; i++)
		{
			c.GotoNext(MoveType.After,
				// 0: ChangeHeldSpinRotationIndex(RotationSnap / 15);
				// 1: ChangeHeldSpinRotationIndex(24 - RotationSnap / 15);
				// 2: (skipped)
				// 3: ChangeHeldSpinRotationIndex(num4);
				x => x.MatchLdcI4(-1),
				x => x.MatchCall(AccessTools.Method(typeof(Pointer), nameof(Pointer.ChangeHeldSpinRotationIndex)))
			);
			if (i == 2)
				continue;
			c.Index--;
			c.MoveAfterLabels();
			c.Remove();
			c.EmitDelegate(void(Pointer __instance, int spinRotationDelta, int touchId) =>
			{
				if (zInput.GetButton("Ctrl"))
					__instance.ChangeHeldTiltRotationIndex(spinRotationDelta, touchId);
				else
					__instance.ChangeHeldSpinRotationIndex(spinRotationDelta, touchId);
			});
		}

		c.Index = 0;
		c.GotoNext(MoveType.After,
			// ChangeHeldFlipRotationIndex(12);
			x => x.MatchLdcI4(12),
			x => x.MatchLdcI4(-1),
			x => x.MatchCall(AccessTools.Method(typeof(Pointer), nameof(Pointer.ChangeHeldFlipRotationIndex)))
		);
		c.Index--;
		c.MoveAfterLabels();
		c.Remove();
		c.EmitDelegate(void(Pointer __instance, int flipRotationDelta, int touchId) =>
		{
			if (zInput.GetButton("Ctrl"))
				__instance.ChangeHeldTiltRotationIndex(flipRotationDelta, touchId);
			else
				__instance.ChangeHeldFlipRotationIndex(flipRotationDelta, touchId);
		});
	}

	[HarmonyILManipulator]
	[HarmonyPatch(typeof(ManagerPhysicsObject), nameof(ManagerPhysicsObject.UpdateGrabbedNPO))]
	static void UpdateGrabbedNPOIL(ILContext il)
	{
		// #todo: held tilt rotation offset
		var c = new ILCursor(il);
		for (var i = 1; i <= 4; i++)
			c.GotoNext(MoveType.After,
				// 0: identity = Quaternion.AngleAxis(grabbedNPO.HeldRotationOffset.x, Vector3.right) * identity;
				// 1: identity = Quaternion.AngleAxis(grabbedNPO.HeldRotationOffset.z, Vector3.forward) * identity;
				// 2: identity = Quaternion.AngleAxis(grabbedNPO.HeldFlipRotationIndex * 15, axis) * identity;
				// 3: identity = Quaternion.AngleAxis(grabbedNPO.HeldSpinRotationIndex * 15, Vector3.up) * identity;
				x => x.MatchCall(AccessTools.Method(typeof(Quaternion), "op_Multiply", [ typeof(Quaternion), typeof(Quaternion) ])),
				x => x.MatchStloc(13)
			);
		c.Emit(OpCodes.Ldarg_0);
		c.Emit(OpCodes.Ldarg_1);
		c.Emit(OpCodes.Ldloc, 13);
		c.EmitDelegate(Quaternion(ManagerPhysicsObject __instance, NetworkPhysicsObject grabbedNPO, Quaternion identity) =>
			!grabbedNPO.HasData()
				? identity
			: Quaternion.AngleAxis(
				grabbedNPO.HeldTiltRotationIndex * ManagerPhysicsObject.MIN_ROTATION_DEG,
				__instance.FlipsAroundZAxis(grabbedNPO.gameObject)
					? Vector3.right
					: Vector3.forward
			) * identity
		);
		c.Emit(OpCodes.Stloc, 13);
	}
}

