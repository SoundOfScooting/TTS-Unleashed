namespace Unleashed.Patches;

[HarmonyPatch]
static class Finder
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(UIFinder), nameof(UIFinder.Start))]
	static void StartPostfix(UIFinder __instance)
	{
		var closeButton = __instance.finder.transform.Find("Close Button").gameObject;
		closeButton.GetComponent<UIButtonActivate>().target = __instance.finder;
		closeButton.SetActive(true);
	}
}

