namespace Unleashed.Contextual;

[HarmonyPatch]
static class Enable
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

		c.GotoNext(MoveType.After,
			// SetActive(NetworkInstance.GUIContextualPhysics, Network.isServer && flag);
			x => x.MatchLdfld(AccessTools.Field(typeof(NetworkUI), nameof(NetworkUI.GUIContextualPhysics))),
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(Network), nameof(Network.isServer)))
		);
		c.Previous.Operand = AccessTools.PropertyGetter(typeof(Network), nameof(Network.isAdmin));

		c.GotoNext(MoveType.After,
			// SetActive(NetworkInstance.GUIContextualCustom, Network.isServer && (bool)InfoObject.GetComponent<CustomObject>());
			x => x.MatchLdfld(AccessTools.Field(typeof(NetworkUI), nameof(NetworkUI.GUIContextualCustom))),
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(Network), nameof(Network.isServer)))
		);
		c.Previous.Operand = AccessTools.PropertyGetter(typeof(Network), nameof(Network.isAdmin));

#if TRUE_ULTIMATE_POWER
		// #todo: either support or remove Scripting but keep GUID
		c.GotoNext(MoveType.After,
			// SetActive(NetworkInstance.GUIContextualScripting, Network.isServer && !string.IsNullOrEmpty(component.GUID));
			x => x.MatchLdfld(AccessTools.Field(typeof(NetworkUI), nameof(NetworkUI.GUIContextualScripting))),
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(Network), nameof(Network.isServer)))
		);
		c.EmitDelegate(bool(bool isServer) => true);
#endif

		// #gold
		c.GotoNext(MoveType.Before,
			// SetActive(((Component)NetworkInstance.GUIContextualGoldBool.get_transform().get_parent()).get_gameObject(), Network.isServer && SteamManager.bKickstarterGold);
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(Network), nameof(Network.isServer))),
			x => x.MatchBrfalse(out _),
			x => x.MatchLdsfld(AccessTools.Field(typeof(SteamManager), nameof(SteamManager.bKickstarterGold))),
			x => x.MatchBr(out _),
			x => x.MatchLdcI4(0)
		);
		c.MoveAfterLabels();
		c.Remove();
		c.EmitDelegate(bool() =>
			PlayerManager.Instance.HostPlayerState().IsModded
		);
		// c.RemoveRange(2);
		// c.Index++;
		// c.RemoveRange(2);

		c.GotoNext(MoveType.After,
			// NetworkInstance.GUIContextualMenu.SetActive(value: true);
			x => x.MatchLdarg(0),
			x => x.MatchLdfld(AccessTools.Field(typeof(Pointer), nameof(Pointer.NetworkInstance))),
			x => x.MatchLdfld(AccessTools.Field(typeof(NetworkUI), nameof(NetworkUI.GUIContextualMenu))),
			x => x.MatchLdcI4(1),
			x => x.MatchCallvirt(AccessTools.Method(typeof(GameObject), nameof(GameObject.SetActive)))
		);
		// c.MoveAfterLabels();
		c.Index--;
		c.EmitDelegate(Events.TriggerStartContextual);
	}
}

