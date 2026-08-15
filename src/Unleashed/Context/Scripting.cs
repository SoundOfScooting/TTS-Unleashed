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
			x => x.MatchLdfld(() => NetworkUI.__instance().GUIContextualScripting),
			x => x.MatchCall(() => Network.IsServer)
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
		NetworkUI.Instance.GUIContextualGUID
			.GetOrAddComponent<UZContextualGuid>();
	}
}
sealed class UZContextualGuid : MonoBehaviour
{
	void OnAltClick()
	{
		_ = this;
		if (!Network.IsServer)
			return;
		UIDialog.ShowInput(
			$"Enter GUID for {Pointer.MyPointer.InfoObject}",
			inputName: Pointer.MyPointer.InfoObject.GetNPO().GUID,

			leftButtonText: "OK",
			leftButtonFunc: input =>
			{
				if (input is not [_, ..])
					return;
				foreach (var npo in ManagerPhysicsObject.Instance.GrabbableNPOs)
				if      (npo.GUID == input)
				{
					Chat.LogError("GUID already in use!");
					return;
				}
				Pointer.MyPointer.InfoObject.GetNPO().GUID = input;
				Pointer.MyPointer.ResetInfoObject();
			},
			rightButtonText: "Cancel",
			rightButtonFunc: null
		);
	}
}

