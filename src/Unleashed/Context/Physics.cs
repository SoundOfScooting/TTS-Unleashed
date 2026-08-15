namespace Unleashed.Context;

[HarmonyPatch]
static class Physics
{
	[HarmonyILManipulator]
	[HarmonyPatch(typeof(Pointer), nameof(Pointer.StartContextual))]
	static void StartContextualIL(ILContext il)
	{
		var c = new ILCursor(il);
		c.GotoNext(MoveType.After,
			// SetActive(NetworkInstance.GUIContextualPhysics, Network.IsServer && flag);
			x => x.MatchLdfld(() => NetworkUI.__instance().GUIContextualPhysics),
			x => x.MatchCall(() => Network.IsServer)
		);
		// c.Prev.OperandAsGetter = () => Network.IsAdmin;
		c.EmitDelegate(bool(bool isServer) => Network.IsAdmin || PermissionsOptions.Options.Contextual);
	}
}

