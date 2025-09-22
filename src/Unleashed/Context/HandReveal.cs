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
			x => x.MatchLdfld(AccessTools.Field(typeof(NetworkUI), nameof(NetworkUI.handZoneToReveal))),
			x => x.MatchCallvirt(AccessTools.PropertyGetter(typeof(HandZone), nameof(HandZone.TriggerLabel))),
			x => x.MatchLdarg(0),
			x => x.MatchLdfld(AccessTools.Field(typeof(Pointer), nameof(Pointer.PointerColorLabel))),
			x => x.MatchCall(AccessTools.Method(typeof(string), "op_Equality"))
		);
		c.EmitDelegate(bool(bool eq) => true);
	}
}

