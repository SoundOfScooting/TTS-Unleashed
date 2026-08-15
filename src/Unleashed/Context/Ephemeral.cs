using System.Runtime.CompilerServices;

namespace Unleashed.Context;

[HarmonyPatch]
sealed class UZContextualEphemeral : MonoBehaviour
{
	static Transform InstantiateSibling(Transform original)
		=> Instantiate(original, original.parent);

	[ModuleInitializer]
	internal static void Initializer()
		=> Events.OnStartConnected += OnStartConnected;
	static void OnStartConnected()
		=> InstantiateSibling(NetworkUI.Instance.GUIContextualDestroyableBool.transform.parent)
			.Find("Persistent Toggle").gameObject
			.GetOrAddComponent<UZContextualEphemeral>()
			.Initialize();
	void Initialize()
	{
		NetworkUI.Instance.GUIContextualToggles.GetComponent<UIHoverEnableObjects>().HoverEnableObjects.Add(gameObject);

		var parent = transform.parent;
		parent.SetSiblingIndex(parent.GetSiblingIndex()-1);
		parent.name = "Ephemeral";
		parent.GetComponent<UILabel>().text = Contextual.LABEL_PADDING + parent.name;

		name = $"{parent.name} Toggle";
		GetComponent<UIButton>().onClick = [new(this, nameof(OnClickContextual))];
		GetComponent<I2.Loc.Localize>().enabled = false;
		GetComponent<UITooltipObject>().Tooltip = "Should the object be excluded from saved games?";

		Events.OnStartContextual += OnStartContextual;
	}
	void OnDestroy()
		=> Events.OnStartContextual -= OnStartContextual;

	void OnStartContextual()
		=> Contextual.Check(gameObject, CheckContextual);
	bool CheckContextual()
	{
		if (!Network.IsServer)
			return false;
		GetComponent<UIToggle>().value = Pointer.MyPointer.InfoObject.GetNPO().Ephemeral;
		return true;
	}
	void OnClickContextual(bool _)
	{
		_ = this;
		if (Pointer.MyPointer)
		{
			// Pointer.MyPointer.ResetInfoObject();
			Pointer.MyPointer.Ephemeral(GetComponent<UIToggle>().value);
		}
	}

	// #todo: copy-paste

	[HarmonyILManipulator]
	[HarmonyPatch(typeof(ManagerPhysicsObject), nameof(ManagerPhysicsObject.CurrentState))]
	static void CurrentStateIL(ILContext il)
	{
		var c = new ILCursor(il);
		c.GotoNext(MoveType.After,
			// if (netPhysObject.IsSaved && !netPhysObject.IsDestroyed)
			x => x.MatchLdloc(10),
			x => x.MatchCallvirt(AccessTools.PropertyGetter(typeof(NetPhysObject), nameof(NetPhysObject.IsSaved)))
		);
		c.Emit(OpCodes.Ldloc, 10);
		c.EmitDelegate(
			bool(bool isSaved, NetPhysObject npo)
				=> isSaved && !npo.Ephemeral
		);
	}

	readonly ref struct InvisibleCache()
	{
		public sealed class Empty { }
		public readonly ConditionalWeakTable<NetPhysObject, Empty> Invisible = [];
		public readonly ConditionalWeakTable<NetPhysObject, Empty> Obscured  = [];
	}
	[HarmonyWrapSafe]
	[HarmonyPrefix]
	[HarmonyPatch(typeof(ScreenshotUtility), nameof(ScreenshotUtility.SaveThumbnail), [typeof(string)])]
	// #audit: attached objects, mp3 players being weird
	static void SaveThumbnailPrefix(ref InvisibleCache __state)
	{
		__state = new();
		foreach (var npo in ManagerPhysicsObject.Instance.GrabbableNPOs)
		if      (npo.Ephemeral)
		{
			if (!npo.OverrideIsInvisible)
			{
				__state.Invisible.GetOrCreateValue(npo);
				npo.ForceInvisible(true);
			}
			if (!npo.OverrideIsObscured)
			{
				__state.Obscured.GetOrCreateValue(npo);
				npo.ForceObscured(true);
			}
		}
	}
	[HarmonyWrapSafe]
	[HarmonyPostfix]
	[HarmonyPatch(typeof(ScreenshotUtility), nameof(ScreenshotUtility.SaveThumbnail), [typeof(string)])]
	static void SaveThumbnailPostfix(ref InvisibleCache __state)
	{
		foreach (var (npo, _) in __state.Invisible)
			npo.ForceInvisible(false);
		foreach (var (npo, _) in __state.Obscured)
			npo.ForceObscured(false);
	}

	[HarmonyILManipulator]
	[HarmonyPatch(typeof(DeckObject), nameof(DeckObject.RemoveCard))]
	[HarmonyPatch(typeof(DeckObject), nameof(DeckObject.TakeCard))]
	[HarmonyPatch(typeof(DeckObject), nameof(DeckObject.Remove))]
	static void SetEphemeralIL(ILContext il)
	{
		var c = new ILCursor(il);
		var local = -1;
		c.GotoNext(MoveType.After,
			// nPO.DoesNotPersist = base.NPO.DoesNotPersist;
			x => x.MatchLdloc(out local),
			x => x.MatchLdarg(0),
			x => x.MatchCall    (AccessTools.PropertyGetter(typeof(ContainerObject), nameof(ContainerObject.NPO))),
			x => x.MatchCallvirt(AccessTools.PropertyGetter(typeof(NetPhysObject),   nameof(NetPhysObject.DoesNotPersist))),
			x => x.MatchCallvirt(AccessTools.PropertySetter(typeof(NetPhysObject),   nameof(NetPhysObject.DoesNotPersist)))
		);
		c.Emit(OpCodes.Ldarg_0);
		c.Emit(OpCodes.Ldloc, local);
		c.EmitDelegate(
			void(DeckObject @this, NetPhysObject nPO) =>
			{
				if (@this.gameObject.GetNPO().Ephemeral)
					nPO.Ephemeral = true;
			}
		);
	}
}

