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
			x => x.MatchLdfld(AccessTools.Field(typeof(NetworkUI), nameof(NetworkUI.GUIContextualPhysics))),
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(Network), nameof(Network.IsServer)))
		);
		// c.Previous.Operand = AccessTools.PropertyGetter(typeof(Network), nameof(Network.IsAdmin));
		c.EmitDelegate(bool(bool isServer) => Network.IsAdmin || PermissionsOptions.Options.Contextual);
	}
}

