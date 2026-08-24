using System.Runtime.CompilerServices;

namespace Unleashed.Context;

[HarmonyPatch]
sealed class UZContextualEphemeral : UZMonoBehaviour
{
	[ModuleInitializer]
	internal static void ModuleInitializer()
		=> Events.OnStartConnected += OnStartConnected;
	static void OnStartConnected()
		=> InstantiateX(NetworkUI.Instance.GUIContextualDestroyableBool.transform.parent)
			.Find("Persistent Toggle")
			.GetOrAddComponent<UZContextualEphemeral>()
			.Initialize();
	void Initialize()
	{
		var parent = transform.parent;
		parent.SetSiblingIndex(parent.GetSiblingIndex()-1);
		parent.name = "Ephemeral";
		parent.GetComponent<UILabel>().text = Contextual.LABEL_PADDING + parent.name;

		name = $"{parent.name} Toggle";
		GetComponent<UIButton>().onClick = [new(this, nameof(OnClickContextual))];
		GetComponent<UITooltipObject>().Tooltip = "Should the object be excluded from saved games?";
		GetComponent<I2.Loc.Localize>().enabled = false; // #loc

		NetworkUI.Instance.GUIContextualToggles.GetComponent<UIHoverEnableObjects>().HoverEnableObjects.Add(gameObject);

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
	// #idea: copy-paste? (Persistent doesn't transfer either)

	[HarmonyILManipulator]
	[HarmonyPatch(typeof(ManagerPhysicsObject), nameof(ManagerPhysicsObject.CurrentState))]
	static void CurrentStateIL(ILContext il)
	{
		var c = new ILCursor(il);
		c.GotoNext(MoveType.After,
			// if (netPhysObject.IsSaved && !netPhysObject.IsDestroyed)
			x => x.MatchLdloc(10),
			x => x.MatchCallvirt(() => NetPhysObject.__instance().IsSaved)
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
			x => x.MatchCall    (() => ContainerObject.__instance().NPO),
			x => x.MatchCallvirt(() => NetPhysObject.__instance().DoesNotPersist),
			x => x.MatchCallvirt(() => NetPhysObject.__instance().DoesNotPersist, MethodType.Setter)
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

