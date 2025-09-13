using static UIGridMenu;

namespace Unleashed.GridMenu;

[HarmonyPatch]
static class Decals
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(UIGridMenuDecals), nameof(UIGridMenuDecals.Init))]
	static void InitPostfix(UIGridMenuDecals __instance)
	{
		__instance.AddDecalButton.GetComponent<UIDisableIfNotServer>().enabled = false;
		__instance.AddDecalButton.gameObject.SetActive(true);
	}

	[HarmonyILManipulator]
	[HarmonyPatch(typeof(GridButtonDecal), MethodType.Constructor)]
	static void GridButtonDecalCtorIL(ILContext il)
	{
		var c = new ILCursor(il);
		c.GotoNext(MoveType.Before,
			// if (Network.isServer)
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(Network), nameof(Network.isServer)))
		);
		c.Remove();
		c.Emit(OpCodes.Ldc_I4_1);
	}
	[HarmonyPostfix]
	[HarmonyPatch(typeof(DecalManager), nameof(DecalManager.RPCRemoveDecalPallet))]
	static void RPCRemoveDecalPalletPostfix()
	{
		if (Network.isClient && NetworkUI.Instance.GUIDecals.activeInHierarchy)
			NetworkUI.Instance.GUIDecals.GetComponent<UIGridMenuDecals>().Reload();
	}
}

