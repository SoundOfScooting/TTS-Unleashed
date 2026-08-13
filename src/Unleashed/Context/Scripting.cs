using System.Runtime.CompilerServices;

namespace Unleashed.Context;

[HarmonyPatch]
static class Scripting
{
	[HarmonyILManipulator]
	[HarmonyPatch(typeof(Pointer), nameof(Pointer.StartContextual))]
	static void StartContextualIL(ILContext il)
	{
		var c = new ILCursor(il);
		c.GotoNext(MoveType.After,
			// SetActive(NetworkInstance.GUIContextualScripting, Network.IsServer && !string.IsNullOrEmpty(component.GUID));
			x => x.MatchLdfld(AccessTools.Field(typeof(NetworkUI), nameof(NetworkUI.GUIContextualScripting))),
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(Network), nameof(Network.IsServer)))
		);
		c.EmitDelegate(bool(bool isServer) => true);
	}

	[ModuleInitializer]
	internal static void Initializer()
		=> Events.OnStartContextual += OnStartContextual;
	static void OnStartContextual()
	{
		// #idea: support editor (possibly read-only)
		NetworkUI.Instance.GUIContextualGUID
			.transform.parent.Find("01 Editor")
			.gameObject.GetOrAddComponent<UIDisableIfNotServer>();
	}
}

