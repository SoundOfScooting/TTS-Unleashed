namespace Unleashed.Patches;

[HarmonyPatch]
sealed class UIPointerRotationSnapX : MonoBehaviour
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(UIPointerRotationSnap), nameof(UIPointerRotationSnap.Awake))]
	static void AwakePostfix(UIPointerRotationSnap __instance) =>
		__instance.gameObject.GetOrAddComponent<UIPointerRotationSnapX>();

	UIPointerRotationSnap @base;
	void Awake() =>
		@base = GetComponent<UIPointerRotationSnap>();

	void OnAltClick()
	{
		@base.bMouse1 = true;
		UICamera.SpoofOnClick(gameObject);
	}
}

