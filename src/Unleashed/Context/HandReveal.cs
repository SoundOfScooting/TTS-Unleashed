namespace Unleashed.Context;

[HarmonyPatch]
static class HandReveal
{
	[HarmonyILManipulator]
	[HarmonyPatch(typeof(Pointer), nameof(Pointer.StartContextual))]
	static void StartContextualIL(ILContext il)
	{
		var c = new ILCursor(il);
		c.GotoNext(MoveType.After,
			// SetActive(NetworkInstance.GUIContextualHandReveal, NetworkSingleton<NetworkUI>.Instance.handZoneToReveal != null && NetworkSingleton<NetworkUI>.Instance.handZoneToReveal.TriggerLabel == PointerColorLabel);
			x => x.MatchLdfld(() => NetworkUI.__instance().handZoneToReveal),
			x => x.MatchCallvirt(() => HandZone.__instance().TriggerLabel),
			x => x.MatchLdarg(0),
			x => x.MatchLdfld(() => Pointer.__instance().PointerColorLabel),
			x => x.MatchCall(typeof(string).Method("op_Equality"))
		);
		c.EmitDelegate(bool(bool eq) => true);
	}
}

