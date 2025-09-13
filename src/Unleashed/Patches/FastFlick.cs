namespace Unleashed.Patches;

[HarmonyPatch]
static class FastFlick
{
	static Settings.Setting<bool> Enabled => Settings.EntryEnableFastFlick;

	[HarmonyILManipulator]
	[HarmonyPatch(typeof(Pointer), nameof(Pointer.StartLine))]
	static void PointerStartLineIL(ILContext il)
	{
		var c = new ILCursor(il);
		c.GotoNext(MoveType.After,
			// if (HighLightedObjects.Count == 0)
			x => x.MatchLdarg   (0),
			x => x.MatchCall    (AccessTools.PropertyGetter(typeof(Pointer), nameof(Pointer.HighLightedObjects))),
			x => x.MatchCallvirt(AccessTools.PropertyGetter(typeof(List<NetworkPhysicsObject>), nameof(List<>.Count)))
		);
		c.Emit(OpCodes.Ldarg_2);
		c.EmitDelegate(int(int count, NetworkPhysicsObject hoverObject) =>
			Network.isClient && Enabled.Value && (count == 0) && hoverObject
				? 1
				: count
		);
	}
}

