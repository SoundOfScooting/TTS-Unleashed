using System.Diagnostics;

namespace Unleashed;

[HarmonyPatch]
public static class MainUI
{
	public static void StartConnected()
	{
		GUIEndTurnX.StartConnected();
		ToolVectorX.StartConnected();
		UIContextualX.StartConnected();
	}

	public static void AssignLocalTRS(this GameObject @this, GameObject src)
	{
		@this.transform.parent        = src.transform.parent;
		@this.transform.localPosition = src.transform.localPosition;
		@this.transform.localRotation = src.transform.localRotation;
		@this.transform.localScale    = src.transform.localScale;
		@this.layer = src.layer;
	}

	public static Vector3[] GetPositions(this LineRenderer @this)
	{
		var positions = new Vector3[@this.positionCount];
		@this.GetPositions(positions);
		return positions;
	}

	/// <summary>
	/// Wrapper around <see cref="global::ExtensionMethods.AddComponent{T}(GameObject, T)"/> due to inadequate <see cref="global::ExtensionMethods.GetCopyOf{T}(Component, T)"/>
	/// </summary>
	public static UISprite AddComponent(this GameObject go, UISprite toAdd)
	{
		var comp  = go.AddComponent<UISprite>(toAdd);
		comp.type = toAdd.type;
		comp.SetDimensions(toAdd.width, toAdd.height);
		return comp;
	}
	/// <summary>
	/// Wrapper around <see cref="global::ExtensionMethods.AddComponent{T}(GameObject, T)"/> due to inadequate <see cref="global::ExtensionMethods.GetCopyOf{T}(Component, T)"/>
	/// </summary>
	public static UIButton AddComponent(this GameObject go, UIButton toAdd)
	{
		var comp = go.AddComponent<UIButton>(toAdd);
		if (toAdd.tweenTarget)
			comp.tweenTarget = go;
		comp.OnInit();
		comp.hover   = toAdd.hover;
		comp.pressed = toAdd.pressed;
		return comp;
	}
	/// <summary>
	/// Wrapper around <see cref="global::ExtensionMethods.AddComponent{T}(GameObject, T)"/> due to inadequate <see cref="global::ExtensionMethods.GetCopyOf{T}(Component, T)"/>
	/// </summary>
	public static UILabel AddComponent(this GameObject go, UILabel toAdd)
	{
		var comp      = go.AddComponent<UILabel>(toAdd);
		comp.color    = toAdd.color;
		comp.fontSize = toAdd.fontSize;
		return comp;
	}

	public static void RestartTooltip(this UITooltipScript @this)
	{
		if (UIHoverText.text == @this.translatedTooltip)
		{
			@this.CancelTooltip();
			@this.OnHover(true);
		}
		else if (
			UIHoverText.text == @this.translatedDelayTooltip && @this.translatedTooltip == "" ||
			UIHoverText.text == @this.translatedTooltip + UIHoverText.DelayTooltipSpacer + @this.translatedDelayTooltip
		){
			@this.CancelTooltip();
			@this.OnHover(true);
			@this.CancelInvoke(nameof(@this.InvokeDelayTooltip));
			@this.InvokeDelayTooltip();
		}
	}

	public static void SpoofNotifyOnClick(this UICamera _, GameObject go, int touchID = UICameraTouch.LEFT)
	{
		var      currentTouchID = UICamera.currentTouchID;
		UICamera.currentTouchID = touchID;
		{
			UICamera.Notify(go, "OnClick", null);
		}
		UICamera.currentTouchID = currentTouchID;
	}
}

[HarmonyPatch]
public class UIPointerRotationSnapX : MonoBehaviour
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
		UICamera.current.SpoofNotifyOnClick(gameObject);
	}
}
[HarmonyPatch]
public class UZCameraHome : MonoBehaviour
{
	// public const string HomePref = $"{Main.PLUGIN_GUID}/{nameof(UZCameraHome)}";
	public enum Home
	{
		Hand,
		White, Brown, Red, Orange, Yellow, Green, Teal, Blue, Purple, Pink,
		Grey, Black,
	}

	UILabel Label;

	public static UZCameraHome Instance { get; private set; }
	public static string GUIColorText { get; private set; }

	public static bool ForceResetCameraRotation { get; set; }
	Home mCurrent;
	public static Home Current
	{
		get => Instance ? Instance.mCurrent : default;
		set
		{
			if (!Instance)
				return;
			Instance.mCurrent = value;
			NeedToPickHome    = false;
			// PlayerPrefs.SetInt(HomePref, (int) value);
			CameraController.Instance.ResetCameraRotation();
		}
	}
	bool mNeedToPickHome;
	public static bool NeedToPickHome
	{
		get => Instance && Instance.mNeedToPickHome;
		set
		{
			if (!Instance)
				return;
			Instance.mNeedToPickHome = value;
			Instance.Label.text = value
				? $"({nameof(Home.Hand)})"
				: $"{Current}";

			NetworkUI.Instance.bNeedToPickColour = value;
			var label = NetworkUI.Instance.GUIColor
				.transform.Find("!Label").GetComponent<UILabel>();
			GUIColorText ??= label.text;
			label.text = value
				? "Choose Camera Home"
				: GUIColorText;
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(UITopBar), nameof(UITopBar.Awake))]
	static void AwakePostfix() =>
		_ = new GameObject("02 CameraHome", typeof(UZCameraHome));
	void Awake()
	{
		CreateComponents();
		Instance = this;

		// Current = (Home) PlayerPrefs.GetInt(HomePref, (int) Value.Hand);
		Current = Home.Hand;
	}
	void CreateComponents()
	{
		var @base = Resources
			.FindObjectsOfTypeAll<UIPointerRotationSnap>()
			.FirstOrDefault();

		gameObject.AssignLocalTRS(@base.gameObject);
		transform .localPosition += new Vector3(56f, 0f, 0f);
		gameObject.AddComponent(@base.GetComponent<BoxCollider2D>());
		gameObject.AddComponent(@base.GetComponent<UISprite>());
		gameObject.AddComponent(@base.GetComponent<UIButton>());
		gameObject.AddComponent<UITooltipScript>().Tooltip = "Camera Home";
		gameObject.AddComponent<TweenColor>();
		// I2.Loc.Localize

		var labelObject = new GameObject(@base.ThisLabelObject.name);
		labelObject.AssignLocalTRS(@base.ThisLabelObject);
		labelObject.transform.parent        = transform;
		labelObject.transform.localPosition = new(0f, -2f, 0f);
		Label = labelObject.AddComponent(@base.ThisLabelObject.GetComponent<UILabel>());
	}

	void OnClick()
	{
		_ = this;
		if (NeedToPickHome)
		{
			Current = Home.Hand;
			return;
		}
		NeedToPickHome = true;
	}
	void OnAltClick()
	{
		_ = this;
		if (NeedToPickHome)
		{
			NeedToPickHome           = false;
			ForceResetCameraRotation = true;
			CameraController.Instance.ResetCameraRotation();
			return;
		}
		UIDialog.ShowDropDown(
			"[b]Choose Camera Home[/b]",
			dropDownOptions: [.. EnumX.GetNames<Home>()],
			drowDownValue:   Current.ToString(),

			leftButtonText: "OK",
			leftButtonFunc: label =>
				Current = EnumX.Parse<Home>(label),

			rightButtonText: "Cancel",
			rightButtonFunc: null
		);
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(CameraController), nameof(CameraController.DelayResetCameraRotation))]
	static void DelayResetCameraRotationPrefix(CameraController __instance, ref string colourLabel, ref CameraState __state)
	{
		__state = null;
		if (ForceResetCameraRotation)
		{
			ForceResetCameraRotation = false;
			__state = __instance.CameraStates[0];
			__instance.CameraStates[0] = null;
			return;
		}
		if (NeedToPickHome)
			colourLabel = "Grey";
		else switch (Current)
		{
			case Home.Hand:
				break;
			default:
				colourLabel = Current.ToString();
				break;
		}
	}
	[HarmonyPostfix]
	[HarmonyPatch(typeof(CameraController), nameof(CameraController.DelayResetCameraRotation))]
	static void DelayResetCameraRotationPostfix(CameraController __instance, CameraState __state)
	{
		if (__state != null)
			__instance.CameraStates[0] = __state;
	}
}

[HarmonyPatch]
public class GUIEndTurnX : MonoBehaviour
{
	public static void StartConnected() =>
		NetworkUI.Instance.GUIEndTurn.GetOrAddComponent<GUIEndTurnX>();

	[HarmonyILManipulator]
	[HarmonyPatch(typeof(Turns), nameof(Turns.GUIEndTurn))]
	static void GUIEndTurnIL(ILContext il)
	{
		ILCursor c = new(il);
		c.GotoNext(MoveType.Before,
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(Network), nameof(Network.isServer)))
		);
		c.Next.Operand =     AccessTools.PropertyGetter(typeof(Network), nameof(Network.isAdmin));
	}

	void OnAltClick()
	{
		if (!Network.isAdmin/* && !Turns.Instance.turnsState.PassTurns*/)
			return;
		Turns.Instance.turnsState.Reverse ^= true;
		{
			UICamera.current.SpoofNotifyOnClick(gameObject);
		}
		Turns.Instance.turnsState.Reverse ^= true;
	}
}
[HarmonyPatch]
public class UIStarTurnX : GUIEndTurnX
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(UIStarTurn), nameof(UIStarTurn.Awake))]
	static void AwakePostfix(UIStarTurn __instance) =>
		__instance.gameObject.GetOrAddComponent<UIStarTurnX>();

	[HarmonyPrefix]
	[HarmonyPatch(typeof(UIStarTurn), nameof(UIStarTurn.OnClick))]
	static bool OnClickReplace()
	{
		if (Network.isAdmin)
			Turns.Instance.GUIEndTurn();
		return false;
	}

	void Awake() =>
		EventManager.OnPlayerPromoted += OnPlayerPromoted;
	void OnDestroy() =>
		EventManager.OnPlayerPromoted -= OnPlayerPromoted;

	void OnPlayerPromoted(bool isPromoted, int id)
	{
		if (id == NetworkID.ID)
			GetComponent<UITooltipScript>().Tooltip = isPromoted
				? "Turn (Click to skip)"
				: "Turn";
	}
}

[HarmonyPatch]
public class UINameButtonX : MonoBehaviour
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(UINameButton), nameof(UINameButton.Start))]
	static void StartPostfix(UINameButton __instance) =>
		__instance.gameObject.GetOrAddComponent<UINameButtonX>();

	UINameButton @base;
	void Awake()
	{
		@base = GetComponent<UINameButton>();
		@base.DoNotConfirm.AddRange([ "Start Turns", "Reverse Turns", "Stop Turns" ]);
	}

	public bool Extra;
	void OnAltClick()
	{
		if (!UIPopupList.isOpen)
		{
			Extra = true;
			// @base.UpdateDropDown();
		}
		UICamera.Notify(gameObject, "OnClick", null);
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(UINameButton), nameof(UINameButton.UpdateDropDown))]
	static bool UpdateDropDownReplace(UINameButton __instance)
	{
		// ???
		if (PlayerManager.Instance.PlayersDictionary.TryGetValue(__instance.id, out var playerState))
			__instance.NameLabel.text = playerState.name;

		// old behavior
		// if (__instance.TryGetComponent<UINameButtonX>(out var instanceX))
		// 	instanceX.PopupListShow();
		return false;
	}
	[HarmonyILManipulator]
	[HarmonyPatch(typeof(UIPopupList), nameof(UIPopupList.Show))]
	static void ShowIL(ILContext il)
	{
		ILCursor c = new(il);
		c.GotoNext(MoveType.Before,
			// Singleton<UIPalette>.Instance.InitTheme(this);
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(Singleton<UIPalette>), nameof(Singleton<>.Instance))),
			x => x.MatchLdarg(0),
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(Singleton<UIPalette>), nameof(Singleton<>.Instance))),
			x => x.MatchLdfld(AccessTools.Field(typeof(UIPalette), nameof(UIPalette.CurrentThemeColours)))
		);
		c.MoveAfterLabels();
		c.Emit(OpCodes.Ldarg_0);
		c.EmitDelegate(void(UIPopupList __instance) =>
			UICamera.Notify(__instance.gameObject, nameof(UZOnPopupListShow), null)
		);
	}
	public void UZOnPopupListShow()
	{
		var isHotseat   = NetworkUI.Instance.bHotseat;
		var isOwnButton = @base.id == (
			isHotseat
				? NetworkUI.Instance.CurrentHotseat
				: NetworkID.ID
		);
		var buttonColor =
			Colour.ColourFromUIColour(
				@base.GetComponent<UIButton>().defaultColor
			).Label;

		List<string> items = [];
		if (isHotseat)
		{
			if (isOwnButton)
			{
				items.Add("Change Color");
				items.Add("Change Team");
				items.Add("Change Name");
			}
			else if (Turns.Instance.turnsState.PassTurns)
				items.Add("Pass Turn");
		}
		else
		{
			if (Extra && Network.isAdmin)
			{
				if (Turns.Instance.turnsState.Enable)
				{
					items.Add("Stop Turns");
					items.Add("Reverse Turns");
				}
				else
					items.Add("Start Turns");
			}
			if (Turns.Instance.turnsState.Enable)
			{
				if (!Turns.Instance.IsTurn(buttonColor))
				{
					if (Turns.Instance.turnsState.PassTurns && Turns.Instance.IsTurn())
						items.Add("Pass Turn");
					else if (Network.isAdmin)
						items.Add("Set Turn");
				}
			}

			if (Network.isAdmin || isOwnButton)
				items.Add("Change Color");
			items.Add("Change Team");

			if (Settings.DebugChangeNameButton.Value)
				items.Add("Change Name");

			items.Add(PlayerManager.Instance.IsBlinded(@base.id)
				? "Unblindfold"
				: "Blindfold"
			);
			items.Add(PlayerManager.Instance.IsMuted(@base.id)
				? "Unmute"
				: "Mute"
			);
			if (Network.isAdmin)
			{
				items.Add("Server Mute");
				items.Add("Server Unmute");
			}
			if (Network.isAdmin && !PlayerManager.Instance.IsHost(@base.id))
			{
				items.Add(PlayerManager.Instance.IsPromoted(@base.id)
					? "Demote"
					: "Promote"
				);
				items.Add("Kick");
			}
			if (Network.isServer && !isOwnButton)
			{
				items.Add("Ban");
				items.Add("Give Host");
			}
		}
		Extra = false;
		// GetComponent<BoxCollider2D>().enabled = items.Count > 0; // fuck you
		@base.PopupList.items = items;
	}
}
[HarmonyPatch]
public class UIColorSelectionX : MonoBehaviour
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(UIColorSelection), nameof(UIColorSelection.Start))]
	static void StartPostfix(UIColorSelection __instance) =>
		__instance.gameObject.GetOrAddComponent<UIColorSelectionX>();

	bool   restartTooltip;
	string originalTooltip;

	UIColorSelection @base;
	UITooltipScript  tooltip;
	void Awake()
	{
		@base           = GetComponent<UIColorSelection>();
		tooltip         = GetComponent<UITooltipScript>();
		originalTooltip = tooltip.Tooltip;
	}

	void OnEnable()
	{
		restartTooltip = true;

		// fix 1 frame of desync
		@base.Update();
		// fix Black LockObject not reappearing
		@base.LockObject.SendMessage("OnEnable");
	}
	void OnAltClick()
	{
		if (UZCameraHome.NeedToPickHome)
		{
			UZCameraHome.NeedToPickHome           = false;
			UZCameraHome.ForceResetCameraRotation = true;
			CameraController.Instance.ResetCameraRotation(@base.label);
		}
	}

	void Update()
	{
		if (restartTooltip || (
			zInput.GetButtonDown("Ctrl")  || zInput.GetButtonUp("Ctrl") ||
			zInput.GetButtonDown("Shift") || zInput.GetButtonUp("Shift")
		)){
			restartTooltip  = false;
			tooltip.Tooltip = originalTooltip;
			if (!UZCameraHome.NeedToPickHome && Network.isAdmin)
			{
				if (zInput.GetButton("Shift"))
					tooltip.Tooltip = "[SWAP]\n"  + tooltip.Tooltip;
				else if (zInput.GetButton("Ctrl"))
					tooltip.Tooltip = "[FORCE]\n" + tooltip.Tooltip;
			}
			tooltip.RestartTooltip();
		}
	}
}

[HarmonyPatch]
public static class UICustomObjectX
{
	// #idea: would be nice to use deck import UI for cards to access extra options
	// #idea: edit each ui panel to add inaccesible parameters
	const bool DEBUG_COMPAT = false;

	class Data : MonoBehaviour // #want: Data<T>, List<Action<T>>
	{
		public readonly List<Delegate> OnImportFakeQueue = [];
		// #todo? onCancel
	}
	static Data X<T>(this T @this) where T : UICustomObject<T> =>
		@this.gameObject.GetOrAddComponent<Data>();
	// static bool GetX<T>(this T @this, out Data thisX) where T : UICustomObject<T> =>
	// 	@this.TryGetComponent(out thisX);
	// static void ClearX<T>(this T @this) where T : UICustomObject<T>
	// {
	// 	if (@this.GetX(out var thisX))
	// 		UnityEngine.Object.Destroy(thisX);
	// }

	public static bool TargettingFake<T>(this T @this) where T : UICustomObject<T> =>
		@this.CustomObjectQueue is [null, ..];

	public static void QueueFake(this UICustomImage @this, Action<UICustomImage> onImport) =>
		QueueFake<UICustomImage>(@this, onImport);
	public static void QueueFake(this UICustomSky @this, Action<UICustomSky> onImport) =>
		QueueFake<UICustomSky>(@this, onImport);
	static void QueueFake<T>(this T @this, Action<T> onImport) where T : UICustomObject<T>
	{
		if (@this.X().OnImportFakeQueue.Contains(onImport))
			return;
		@this.X().OnImportFakeQueue.Add(onImport);
		@this    .CustomObjectQueue.Add(null);
		@this.NumberInQueue      = @this.CustomObjectQueue.Count - 1;
		@this.TargetCustomObject = null;
		@this.gameObject.SetActive(true);
	}
	static object Invoke(object @this, string method) =>
		AccessTools.Method(typeof(UICustomObjectX), method)
			.MakeGenericMethod([@this.GetType()])
			.Invoke(null, [@this]);

	static bool BaseOnEnableFake<T>(this T @this) where T : UICustomObject<T>
	{
		if (@this.TargettingFake())
		{
			@this.TargetCustomObject = null;
			@this.GetComponent<UIHighlightTargets>().Reset();
			return true;
		}
		return false;
	}
	// #generic
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomObject<MonoBehaviour>), nameof(UICustomObject<>.OnEnable))]
	static bool BaseOnEnablePrefix(object __instance) =>
		!(bool) Invoke(__instance, nameof(BaseOnEnableFake));
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomImage), nameof(UICustomImage.OnEnable))]
	static bool ImageOnEnablePrefix(UICustomImage __instance)
	{
		if (!__instance.BaseOnEnableFake())
			return true;
		__instance.TargetCustomImage = null;
		return false;
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomSky), nameof(UICustomSky.OnEnable))]
	static bool SkyOnEnablePrefix(UICustomSky __instance)
	{
		if (!__instance.BaseOnEnableFake())
			return true;
		__instance.TargetCustomSky = null;
		return false;
	}

	// #generic
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomObject<MonoBehaviour>), nameof(UICustomObject<>.Update))]
	[HarmonyPatch(typeof(UICustomObject<MonoBehaviour>), nameof(UICustomObject<>.CheckUpdateMatchingCustomObjects))]
	static bool BaseUpdatePrefix(object __instance) =>
		!(bool) Invoke(__instance, nameof(TargettingFake));
	static void BaseCloseFake<T>(this T @this) where T : UICustomObject<T>
	{
		if (@this.TargettingFake())
			@this.X().OnImportFakeQueue.RemoveAt(0);
	}
	// #generic
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomObject<MonoBehaviour>), nameof(UICustomObject<>.Close))]
	static void BaseClosePrefix(object __instance) =>
		Invoke(__instance, nameof(BaseCloseFake));

	static bool ImportFake<T>(this T @this) where T : UICustomObject<T>
	{
		if (@this.TargettingFake() && @this.X().OnImportFakeQueue is [{} onImport, ..])
		{
			onImport.DynamicInvoke(@this);
			return true;
		}
		return false;
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomImage), nameof(UICustomImage.Import))]
	[HarmonyPatch(typeof(UICustomSky),   nameof(UICustomSky  .Import))]
	[HarmonyPriority(Priority.HigherThanNormal)]
	static bool ImportFakePrefix(object __instance) =>
		!(bool) Invoke(__instance, nameof(ImportFake));

	[HarmonyILManipulator]
	[HarmonyPatch(typeof(CustomAssetbundle),  nameof(CustomAssetbundle .bCustomUI), MethodType.Setter)]
	[HarmonyPatch(typeof(CustomCard),         nameof(CustomCard        .bCustomUI), MethodType.Setter)]
	[HarmonyPatch(typeof(CustomDeck),         nameof(CustomDeck        .bCustomUI), MethodType.Setter)]
	[HarmonyPatch(typeof(CustomDice),         nameof(CustomDice        .bCustomUI), MethodType.Setter)]
	[HarmonyPatch(typeof(CustomImage),        nameof(CustomImage       .bCustomUI), MethodType.Setter)]
	[HarmonyPatch(typeof(CustomJigsawPuzzle), nameof(CustomJigsawPuzzle.bCustomUI), MethodType.Setter)] // #todo: fully support
	[HarmonyPatch(typeof(CustomMesh),         nameof(CustomMesh        .bCustomUI), MethodType.Setter)]
	[HarmonyPatch(typeof(CustomPDF),          nameof(CustomPDF         .bCustomUI), MethodType.Setter)] // #todo: fully support
	[HarmonyPatch(typeof(CustomSky),          nameof(CustomSky         .bCustomUI), MethodType.Setter)]
	[HarmonyPatch(typeof(CustomTile),         nameof(CustomTile        .bCustomUI), MethodType.Setter)]
	[HarmonyPatch(typeof(CustomToken),        nameof(CustomToken       .bCustomUI), MethodType.Setter)]
	static void AllowAdminIL(ILContext il)
	{
		ILCursor c = new(il);
		c.GotoNext(MoveType.Before,
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(Network), nameof(Network.isServer)))
		);
		c.Next.Operand     = AccessTools.PropertyGetter(typeof(Network), nameof(Network.isAdmin));
	}
	// #generic
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomObject<MonoBehaviour>), nameof(UICustomObject<>.CheckUpdateMatchingCustomObjects))]
	static bool CheckUpdateMatchingCustomObjectsPrefix() =>
		Network.isServer; // #idea: implement for client?

	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomAssetbundle), nameof(UICustomAssetbundle.Import))]
	static bool AssetbundleImportPrefix(UICustomAssetbundle __instance, bool __runOriginal)
	{
		if (!__runOriginal || !DEBUG_COMPAT && Network.isServer)
			return __runOriginal;
		__instance.CustomAssetbundleURL          = __instance.CustomAssetbundleURL         .Trim();
		__instance.CustomAssetbundleSecondaryURL = __instance.CustomAssetbundleSecondaryURL.Trim();
		if (string.IsNullOrEmpty(__instance.CustomAssetbundleURL))
		{
			Chat.LogError("You must supply a custom assetbundle URL.");
			return false;
		}
		Lua.Execute(
			$$"""
			local obj = getObjectFromGUID({{ __instance.TargetCustomObject.NPO.GUID }})
			if obj then
				obj.setCustomObject({
					assetbundle           = {{ __instance.CustomAssetbundleURL }},
					assetbundle_secondary = {{ __instance.CustomAssetbundleSecondaryURL }},
					type                  = {{ __instance.TypeInt }},
					material              = {{ __instance.MaterialInt }},
				})
				obj.reload()
			end
			"""
		);
		__instance.Close();
		return false;
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomCard), nameof(UICustomCard.Import))]
	static bool CardImportPrefix(UICustomCard __instance, bool __runOriginal)
	{
		if (!__runOriginal || !DEBUG_COMPAT && Network.isServer)
			return __runOriginal;
		__instance.URLFace = __instance.URLFace.Trim();
		__instance.URLBack = __instance.URLBack.Trim();
		if (string.IsNullOrEmpty(__instance.URLFace))
		{
			Chat.LogError("You must supply a face image URL.");
			return false;
		}
		if (string.IsNullOrEmpty(__instance.URLBack))
		{
			Chat.LogError("You must supply a back image URL.");
			return false;
		}
		Lua.Execute(
			$$"""
			local obj = getObjectFromGUID({{ __instance.TargetCustomObject.NPO.GUID }})
			if obj then
				obj.setCustomObject({
					face     = {{ __instance.URLFace }},
					back     = {{ __instance.URLBack }},
					sideways = {{ __instance.bSideways }},
					type     = {{ __instance.TypePopupList.items.IndexOf(__instance.TypePopupList.value) }},
				})
				obj.reload()
			end
			"""
		);
		__instance.Close();
		return false;
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomDeck), nameof(UICustomDeck.Import))]
	static bool DeckImportPrefix(UICustomDeck __instance, bool __runOriginal)
	{
		if (!__runOriginal || !DEBUG_COMPAT && Network.isServer)
			return __runOriginal;
		__instance.URLFace = __instance.URLFace.Trim();
		__instance.URLBack = __instance.URLBack.Trim();
		if (string.IsNullOrEmpty(__instance.URLFace))
		{
			Chat.LogError("You must supply a face image URL.");
			return false;
		}
		if (string.IsNullOrEmpty(__instance.URLBack))
		{
			Chat.LogError("You must supply a back image URL.");
			return false;
		}
		Lua.Execute(
			$$"""
			local obj = getObjectFromGUID({{ __instance.TargetCustomObject.NPO.GUID }})
			if obj then
				obj.setCustomObject({
					face           = {{ __instance.URLFace }},
					unique_back    = {{ __instance.bUniqueBacks }},
					back           = {{ __instance.URLBack }},
					width          = {{ __instance.WidthRange .intValue }},
					height         = {{ __instance.HeightRange.intValue }},
					number         = {{ __instance.NumberRange.intValue }},
					sideways       = {{ __instance.bSideways }},
					back_is_hidden = {{ __instance.bBackIsHidden }},
					type           = {{ __instance.TypePopupList.items.IndexOf(__instance.TypePopupList.value) }},
				})
				obj.reload()
			end
			"""
		);
		__instance.Close();
		return false;
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomDice), nameof(UICustomDice.Import))]
	static bool DiceImportPrefix(UICustomDice __instance, bool __runOriginal)
	{
		if (!__runOriginal || !DEBUG_COMPAT && Network.isServer)
			return __runOriginal;
		__instance.CustomImageURL = __instance.CustomImageURL.Trim();
		if (string.IsNullOrEmpty(__instance.CustomImageURL))
		{
			Chat.LogError("You must supply a custom image URL.");
			return false;
		}
		Lua.Execute(
			$$"""
			local obj = getObjectFromGUID({{ __instance.TargetCustomObject.NPO.GUID }})
			if obj then
				obj.setCustomObject({
					image = {{ __instance.CustomImageURL }},
					type  = {{ __instance.TypeInt }},
				})
				obj.reload()
			end
			"""
		);
		__instance.Close();
		return false;
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomImage), nameof(UICustomImage.Import))]
	static bool ImageImportPrefix(UICustomImage __instance, bool __runOriginal)
	{
		if (!__runOriginal || !DEBUG_COMPAT && Network.isServer)
			return __runOriginal;
		__instance.CustomImageURL = __instance.CustomImageURL.Trim();
		if (string.IsNullOrEmpty(__instance.CustomImageURL))
		{
			Chat.LogError("You must supply a custom image URL.");
			return false;
		}
		Lua.Execute(
			__instance.TargetCustomObject.gameObject == ManagerPhysicsObject.Instance.Table
				? (Lua.Template) $"""
				Tables.setCustomURL({ __instance.CustomImageURL })
				"""
				: (Lua.Template) $$"""
				local obj = getObjectFromGUID({{ __instance.TargetCustomObject.NPO.GUID }})
				if obj then
					obj.setCustomObject({
						image = {{ __instance.CustomImageURL }},
					})
					obj.reload()
				end
				"""
		);
		__instance.Close();
		return false;
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomImageDouble), nameof(UICustomImageDouble.Import))]
	static bool ImageDoubleImportPrefix(UICustomImageDouble __instance, bool __runOriginal)
	{
		if (!__runOriginal || !DEBUG_COMPAT && Network.isServer)
			return __runOriginal;
		__instance.CustomImageURL = __instance.CustomImageURL.Trim();
		// #bug: (base game) CustomImageSecondaryURL is not trimmed
		if (string.IsNullOrEmpty(__instance.CustomImageURL))
		{
			Chat.LogError("You must supply a custom image URL.");
			return false;
		}
		Lua.Execute(
			$$"""
			local obj = getObjectFromGUID({{ __instance.TargetCustomObject.NPO.GUID }})
			if obj then
				obj.setCustomObject({
					image           = {{ __instance.CustomImageURL }},
					image_secondary = {{ __instance.CustomImageSecondaryURL }},
					image_scalar    = {{ __instance.CustomImageScalar }},
				})
				obj.reload()
			end
			"""
		);
		__instance.Close();
		return false;
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomMesh), nameof(UICustomMesh.Import))]
	static bool MeshImportPrefix(UICustomMesh __instance, bool __runOriginal)
	{
		if (!__runOriginal || !DEBUG_COMPAT && Network.isServer)
			return __runOriginal;
		__instance.MeshURL     = __instance.MeshURL    .Trim();
		__instance.DiffuseURL  = __instance.DiffuseURL .Trim();
		__instance.NormalURL   = __instance.NormalURL  .Trim();
		__instance.ColliderURL = __instance.ColliderURL.Trim();
		if (string.IsNullOrEmpty(__instance.MeshURL))
		{
			Chat.LogError("You must supply a model URL to create a custom model.");
			return false;
		}
		Lua.Execute(
			$$"""
			local obj = getObjectFromGUID({{ __instance.TargetCustomObject.NPO.GUID }})
			if obj then
				obj.setCustomObject({
					mesh     = {{ __instance.MeshURL }},
					diffuse  = {{ __instance.DiffuseURL }},
					normal   = {{ __instance.NormalURL }},
					collider = {{ __instance.ColliderURL }},
					convex   = {{ !__instance.NonConvex }},
					type     = {{ __instance.TypeIndex }},
					material = {{ __instance.MaterialIndex }},
					specular_intensity = {{ __instance.CustomShader.SpecularIntensity }},
					specular_color     = {
						r = {{ __instance.CustomShader.SpecularColor.r }},
						g = {{ __instance.CustomShader.SpecularColor.g }},
						b = {{ __instance.CustomShader.SpecularColor.b }},
						a = {{ __instance.CustomShader.SpecularColor.a ?? 1 }},
					},
					specular_sharpness = {{ __instance.CustomShader.SpecularSharpness }},
					fresnel_strength   = {{ __instance.CustomShader.FresnelStrength   }},
					cast_shadows = {{ __instance.CastShadows }},
				})
				obj.reload()
			end
			"""
		);
		__instance.Close();
		return false;
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomSky), nameof(UICustomSky.Import))]
	static bool SkyImportPrefix(UICustomSky __instance, bool __runOriginal)
	{
		if (!__runOriginal || !DEBUG_COMPAT && Network.isServer)
			return __runOriginal;
		__instance.CustomImageURL = __instance.CustomImageURL.Trim();
		if (string.IsNullOrEmpty(__instance.CustomImageURL))
		{
			Chat.LogError("You must supply a custom image URL.");
			return false;
		}
		Lua.Execute(
			$"""
			Backgrounds.setCustomURL({ __instance.CustomImageURL })
			"""
		);
		__instance.Close();
		return false;
	}
	[HarmonyPostfix]
	[HarmonyPatch(typeof(UICustomTile), nameof(UICustomTile.OnEnable))]
	static void TileStartPostfix(UICustomTile __instance) =>
		__instance.StretchToggle.GetComponent<BoxCollider2D>().enabled = !DEBUG_COMPAT && PlayerStateX.Host.IsModded;
	// #todo: GetCustomObject parity
	// #todo: relocate to dedicated Lua fixes/additions
	[HarmonyPostfix]
	[HarmonyPatch(typeof(LuaGameObjectScript), nameof(LuaGameObjectScript.SetCustomObject))]
	static void LuaGameObjectScriptSetCustomObjectPostfix(LuaGameObjectScript __instance, MoonSharp.Interpreter.Table Params)
	{
		if (Params == null)
			return;
		if (__instance.NPO.customImage && __instance.NPO.customTile)
		{
			if (Params["stretch"] != null && bool.TryParse(Params["stretch"].ToString(), out var bStretch))
				__instance.NPO.customTile.bStretch = bStretch;
		}
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomTile), nameof(UICustomTile.Import))]
	static bool TileImportPrefix(UICustomTile __instance, bool __runOriginal)
	{
		if (!__runOriginal || !DEBUG_COMPAT && Network.isServer)
			return __runOriginal;
		__instance.CustomImageURL = __instance.CustomImageURL.Trim();
		if (string.IsNullOrEmpty(__instance.CustomImageURL))
		{
			Chat.LogError("You must supply a custom image URL.");
			return false;
		}
		Lua.Execute(
			$$"""
			local obj = getObjectFromGUID({{ __instance.TargetCustomObject.NPO.GUID }})
			if obj then
				obj.setCustomObject({
					image        = {{ __instance.CustomImageURL }},
					image_bottom = {{ __instance.CustomImageSecondaryURL }},
					type         = {{ __instance.TypeInt }},
					thickness    = {{ __instance.ThicknessSlider.value * 0.9f + 0.1f }},
					stackable    = {{ __instance.StackableToggle.value }},
					stretch      = {{ __instance.StretchToggle  .value }}, -- modded
				})
				obj.reload()
			end
			"""
		);
		__instance.Close();
		return false;
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomToken), nameof(UICustomToken.Import))]
	static bool TileImportPrefix(UICustomToken __instance, bool __runOriginal)
	{
		if (!__runOriginal || !DEBUG_COMPAT && Network.isServer)
			return __runOriginal;
		__instance.CustomImageURL = __instance.CustomImageURL.Trim();
		if (string.IsNullOrEmpty(__instance.CustomImageURL))
		{
			Chat.LogError("You must supply a custom image URL.");
			return false;
		}
		Lua.Execute(
			$$"""
			local obj = getObjectFromGUID({{ __instance.TargetCustomObject.NPO.GUID }})
			if obj then
				obj.setCustomObject({
					image          = {{ __instance.CustomImageURL }},
					thickness      = {{ __instance.ThicknessSlider    .value * 0.9f + 0.1f }},
					merge_distance = {{ __instance.MergeDistanceSlider.value * 20f  + 5f }},
					stand_up       = {{ __instance.StandupToggle  .value }},
					stackable      = {{ __instance.StackableToggle.value }},
				})
				obj.reload()
			end
			"""
		);
		__instance.Close();
		return false;
	}
}

[HarmonyPatch]
public static class ToolVectorX
{
	public readonly struct VectorEraseData(ToolVector.VectorDrawData drawData)
	{
		public readonly ToolVector.VectorDrawData drawData = drawData;
		public readonly bool      wasAttached   = drawData.attached;
		public readonly int       sortingOrder  = drawData.line.sortingOrder;
		public readonly Vector3[] positions     = drawData.line.GetPositions();
//			for (int i = 0; i < positions.Length; i++)
//				positions[i] = drawData.line.transform.TransformPoint(positions[i]);
	}
	public static List<VectorEraseData> EraseBuffer { get; internal set; } = [];
	public static bool EraseCancelled { get; internal set; }

	public static void StartConnected() =>
		Wait.Frames(UpdateUI);
	public static void UpdateUI()
	{
		if (!NetworkUI._Instance || !NetworkUI.Instance.GUIConnected.activeInHierarchy)
			return;

		var DrawT = NetworkUI.Instance.GUIConnected.transform
			.Find("# Pointer Mode/Anchor/Grid/02 Draw");
		var ui = DrawT.GetComponent<UIPointerMode>();

		int GetIndex(GameObject go) =>
			ui.ExpandButtonStructs.FindIndex(expand => expand.Button == go);
		void AddX(GameObject go, float dx)
		{
			var i = GetIndex(go);
			var expand = ui.ExpandButtonStructs[i];
			expand.StartlocalPosition.x += dx;
			ui.ExpandButtonStructs[i] = expand;
		}

//		var DrawPen    = DrawT.Find("Scroll View/01 Pen"   ).gameObject; // +56
//		var DrawLine   = DrawT.Find("Scroll View/02 Line"  ).gameObject; // +56
//		var DrawBox    = DrawT.Find("Scroll View/03 Box"   ).gameObject; // +56
//		var DrawCircle = DrawT.Find("Scroll View/04 Circle").gameObject; // +56
		var DrawPixel  = DrawT.Find("Scroll View/04 Pixel" ).gameObject; // +56
		var DrawErase  = DrawT.Find("Scroll View/05 Erase" ).gameObject; // +56
		var DrawColor  = DrawT.Find("Scroll View/06 Color" ).gameObject; // +56
		var DrawDelete = DrawT.Find("Scroll View/Delete"   ).gameObject; // +44

		var enabled = Settings.EntryEnableVectorPixel.Value;
		if (!enabled)
			Pointer.VectorTools.Remove(PointerMode.VectorPixel);
		else if (!Pointer.VectorTools.Contains(PointerMode.VectorPixel))
			Pointer.VectorTools.Insert(Pointer.VectorTools.IndexOf(PointerMode.VectorErase), PointerMode.VectorPixel);

		var index = GetIndex(DrawPixel);
		if (enabled && (index < 0))
		{
			ui.ExpandButtonStructs.Add(new()
			{
				Button             = DrawPixel,
				ButtonTransform    = DrawPixel.transform,
				StartlocalPosition = ui.ExpandButtonStructs[GetIndex(DrawErase)].StartlocalPosition,
			});
			AddX(DrawErase,  56f);
			AddX(DrawColor,  56f);
			AddX(DrawDelete, 56f);
		}
		if (!enabled && (index >= 0))
		{
			ui.ExpandButtonStructs.RemoveAt(index);
			AddX(DrawErase,  -56f);
			AddX(DrawColor,  -56f);
			AddX(DrawDelete, -56f);
		}
		DrawPixel.SetActive(enabled);
		if (enabled)
			DrawPixel.transform.Find("Sprite").GetComponent<UISprite>().color = Color.white;
	}
}

[HarmonyPatch]
public static class UIGridMenuDecalsX
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(UIGridMenuDecals), nameof(UIGridMenuDecals.Init))]
	static void InitPostfix(UIGridMenuDecals __instance)
	{
		__instance.AddDecalButton.GetComponent<UIDisableIfNotServer>().enabled = false;
		__instance.AddDecalButton.gameObject.SetActive(true);
	}

	[HarmonyILManipulator]
	[HarmonyPatch(typeof(UIGridMenu.GridButtonDecal), MethodType.Constructor)]
	static void UIGridMenuGridButtonDecalCtorIL(ILContext il)
	{
		ILCursor c = new(il);
		c.GotoNext(MoveType.Before,
			// if (Network.isServer)
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(Network), nameof(Network.isServer)))
		);
		c.Remove();
		c.Emit(OpCodes.Ldc_I4_1);
	}
	[HarmonyPostfix]
	[HarmonyPatch(typeof(DecalManager), nameof(DecalManager.RPCRemoveDecalPallet))]
	static void DecalManagerRPCRemoveDecalPalletPostfix()
	{
		if (Network.isClient && NetworkUI.Instance.GUIDecals.activeInHierarchy)
			NetworkUI.Instance.GUIDecals.GetComponent<UIGridMenuDecals>().Reload();
	}
}

[HarmonyPatch]
public static class UIFinderX
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(UIFinder), nameof(UIFinder.Start))]
	static void StartPostfix(UIFinder __instance)
	{
		var closeButton = __instance.finder.transform
			.Find("Close Button").gameObject;
		closeButton.GetComponent<UIButtonActivate>().target = __instance.finder;
		closeButton.SetActive(true);
	}
}

[HarmonyPatch]
public static class UIContextualX
{
	public static void StartConnected() =>
		UZContextualStash.StartConnected();

	[HarmonyPostfix]
	[HarmonyPatch(typeof(UIContextual), nameof(UIContextual.Awake))]
	static void AwakePostfix(UIContextual __instance)
	{
		if (__instance.nameInput)
		{
			EventDelegate.Add(__instance.nameInput.onChange, __instance.DelayReposition);

			__instance.nameInput.onReturnKey    = UIInput.OnReturnKey.NewLine; // Submit
			__instance.nameInput.characterLimit = 1024; // 2048 == descriptionInput.characterLimit

			var descLabel = __instance.descriptionInput.label;
			var nameLabel = __instance.nameInput       .label;
			nameLabel.maxLineCount   = 8; // 16 == descLabel.maxLineCount
			nameLabel.overflowMethod = UILabel.Overflow.ResizeHeight;
			nameLabel.pivot          = UIWidget.Pivot.Top;
			nameLabel.leftAnchor  .Set(null, descLabel.leftAnchor  .relative, descLabel.leftAnchor  .absolute);
			nameLabel.bottomAnchor.Set(null, descLabel.bottomAnchor.relative, descLabel.bottomAnchor.absolute);
			nameLabel.rightAnchor .Set(null, descLabel.rightAnchor .relative, descLabel.rightAnchor .absolute);
			nameLabel.topAnchor   .Set(nameLabel.transform.parent,         1, descLabel.topAnchor   .absolute);
			nameLabel.ResetAndUpdateAnchors();

			var descSprite = __instance.descriptionInput.GetComponent<UISprite>();
			var nameSprite = __instance.nameInput       .GetComponent<UISprite>();
			nameSprite.SetAnchor(nameLabel.gameObject,
				left:   descSprite.leftAnchor .absolute,
				bottom: descSprite.topAnchor  .absolute * -1,
				right:  descSprite.rightAnchor.absolute,
				top:    descSprite.topAnchor  .absolute
			);
		}
	}
	[HarmonyPostfix]
	[HarmonyPatch(typeof(UIContextual), nameof(UIContextual.OnDestroy))]
	static void OnDestroyPostfix(UIContextual __instance)
	{
		if (__instance.nameInput)
			EventDelegate.Remove(__instance.nameInput.onChange, __instance.DelayReposition);
	}
}
public interface IUZContextual
{
	void OnStartContextual();
}
public class UZContextualStash : MonoBehaviour, IUZContextual
{
	public const Type FLAG_Global = (Type) 0b1;
	public enum Type
	{
		GlobalUnlock = 0 << 1 | FLAG_Global,
		GlobalDraw   = 1 << 1 | FLAG_Global,
		ObjectDraw   = 2 << 1 | 0,
		ObjectStash  = 3 << 1 | 0,
	}

	public static void StartConnected()
	{
		// after 04 Paste
#if TRUE_ULTIMATE_POWER
		new GameObject("04 Pb |SORT| Unlock Stash")
			.AddComponent<UZContextualStash>()
			.CreateComponents(Type.GlobalUnlock);
#endif
		new GameObject("04 Pc |SORT| Draw Stash")
			.AddComponent<UZContextualStash>()
			.CreateComponents(Type.GlobalDraw);

		// after 06 Draw
		// new GameObject("06 Ds |SORT| Draw Stash")
		// 	.AddComponent<UZContextualStash>()
		// 	.CreateComponents(Type.ObjectDraw);
		new GameObject("06 Ds |SORT| Stash")
			.AddComponent<UZContextualStash>()
			.CreateComponents(Type.ObjectStash);
	}

	const string LABEL_PADDING = "            ";

	UISprite Icon;
	UILabel Label;

	// Constant state
	Type type;
	// Temporary state
	bool ctrlDown, shiftDown;
	LuaPlayer target;

	void CreateComponents(Type type)
	{
		this.type = type;

		var baseObject =
			NetworkUI.Instance.GUIContextualGlobalMenu.transform
				.Find("Table/04 Paste").gameObject;
		gameObject.AssignLocalTRS(baseObject);
		if ((type & FLAG_Global) == 0)
			transform.parent = NetworkUI.Instance.GUIContextualMenu.transform.Find("Table");

		(Label = gameObject.AddComponent(baseObject.GetComponent<UILabel>()))
			.text = LABEL_PADDING + "???";
		gameObject.AddComponent(baseObject.GetComponents<UIButton>()[0])
			.onClick = [new(OnClickContextual)];
		gameObject.AddComponent(baseObject.GetComponent<BoxCollider2D>());
		gameObject.AddComponent<TweenColor>();

		var baseImagesObject = baseObject.transform.Find("Images").gameObject;
		var imagesObject     = new GameObject(baseImagesObject.name);
		imagesObject.AssignLocalTRS(baseImagesObject);
		imagesObject.transform.parent = transform;

		(Icon = imagesObject.AddComponent(baseImagesObject.GetComponent<UISprite>()))
			.spriteName = "???";
	}

	public static bool PositionHoverOverStash(Vector3 pos, HandZone hand)
	{
		if      (hand.Stash/*  && !hand.Stash.IsGrabbable */)
		foreach (var collider in hand.Stash.Colliders)
		// if      (collider.bounds.Contains(pos with { y = collider.bounds.center.y }))
		// if      (collider.ClosestPoint(pos = pos with { y = collider.bounds.center.y }) == pos)
		if      ((collider.ClosestPoint(pos = pos with { y = collider.bounds.center.y }) - pos).magnitude < 1f /* 1e-5f */)
			return true;
		return false;
	}

	void IUZContextual.OnStartContextual()
	{
		gameObject.SetActive(false);
		if (OnStartContextual())
			PlayerScript.PointerScript.SetActive(gameObject, true);
	}
	bool OnStartContextual()
	{
		if (!Network.isAdmin) // #compat
			return false;

		var player = LuaGlobalScriptManager.Instance.GlobalPlayer.GetPlayer(NetworkID.ID);
		target     = player;
		ctrlDown   = zInput.GetButton("Ctrl");
		shiftDown  = zInput.GetButton("Shift");
		switch (type)
		{
			default: throw new UnreachableException();
			case Type.GlobalUnlock:
			{
				if (ctrlDown)
				{
					foreach (var hand in HandZone.GetHandZones())
					if      (hand.Stash)
					{
						Icon.spriteName = "Icon-Toggle";
						Label.text = LABEL_PADDING +
							"Unlock Stash [b](All)[/b]";
						return true;
					}
					return false;
				}
				if      (player.GetPointerPosition() is {} pos)
				foreach (var hand in HandZone.GetHandZones())
				if      (PositionHoverOverStash(pos, hand))
				{
					target = LuaPlayer.GetHandPlayer(hand.TriggerLabel);
					Icon.spriteName = "Icon-Toggle";
					Label.text = LABEL_PADDING +
						$"{(
							hand.Stash.IsGrabbable ? "Lock" : "Unlock"
						)} Stash {(
							hand.TriggerColour == Colour.White
								? ""
							: hand.TriggerColour.Hex
						)}[b]({hand.TriggerLabel})[/b][-]";
					return true;
				}
				return false;
			}
			case Type.GlobalDraw:
			{
				Icon.spriteName = "Icon-DrawCard6";
				Label.text = LABEL_PADDING +
					$"{(shiftDown ? "Swap " : "Draw ")}Stash";
				if (ctrlDown)
				{
					foreach (var hand in HandZone.GetHandZones())
					if      (hand.Stash || (shiftDown && hand.GetHandObjects().Count > 0))
					{
						Label.text += " [b](All)[/b]";
						return true;
					}
					return false;
				}
				if      (player.GetPointerPosition() is {} pos)
				foreach (var hand in HandZone.GetHandZones())
				if      (PositionHoverOverStash(pos, hand))
				{
					target = LuaPlayer.GetHandPlayer(hand.TriggerLabel);
					Label.text +=
						$" {(
							hand.TriggerColour == Colour.White
								? ""
							: hand.TriggerColour.Hex
						)}[b]({hand.TriggerLabel})[/b][-]";
					return true;
				}
				if (player.GetHandStash() || (shiftDown && player.GetHandObjects().Count > 0))
					return true;
				return false;
			}
			// case Type.ObjectDraw:
			// {
			// 	if      (!shiftDown) // see ObjectStash
			// 	foreach (var obj in player.GetSelectedObjects())
			// 	{
			// 		var hand = obj.NPO.CurrentPlayerHand;
			// 		if (hand && hand.NPO.IsHandZoneStash)
			// 		{
			// 			Icon.spriteName = "Icon-DrawCard6";
			// 			Label.text = LABEL_PADDING + "Draw Stash";
			// 			return true;
			// 		}
			// 	}
			// 	return false;
			// }
			case Type.ObjectStash:
			{
				bool anyInHand = false;
				// bool anyStash  = false; // #stash
				foreach (var obj in player.GetSelectedObjects())
				{
					// if (anyStash = obj.NPO.IsHandZoneStash) // #stash
					if (anyInHand = obj.NPO.CurrentPlayerHand)
						break;
				}
				if (anyInHand)
				{
					Icon.spriteName = "Icon-DrawCard6";
					Label.text = LABEL_PADDING +
						$"{(
							shiftDown
								? "Swap "
							// #stash
							// : anyStash
							// 	? "Draw Stash & "
							: ""
						)}Stash";
					return true;
				}
				return false;
			}
		}
	}

	public static readonly Lua.Variable LuaAllColors = new(
		nameof(LuaAllColors),
		$"{ Lua.Table(Colour.AllPlayerLabels) }"
	);
	public static readonly Lua.Variable LuaHandZonePlayers = new(
		nameof(LuaHandZonePlayers),
		$$"""
		{}
		for _,color in ipairs(Player.getAvailableColors()) do
			table.insert({{ (Lua) nameof(LuaHandZonePlayers) }}, Player[color])
		end
		"""
	);
	public static readonly Lua.Variable LuaGetPlayerHandObjects = new(
		nameof(LuaGetPlayerHandObjects),
		$$"""
		function (player)
			return (player.getHandCount() > 0) and player.getHandObjects() or {}
		end
		"""
	);
	public static readonly Lua.Variable LuaGetObjectHandPlayer = new(
		nameof(LuaGetObjectHandPlayer),
		$"""
		function (obj)
			for _,target in ipairs({ LuaHandZonePlayers }) do
				for _,obj_ in ipairs({ LuaGetPlayerHandObjects }(target)) do
					if obj == obj_ then
						return target
					end
				end
			end
		end
		"""
	);
	public static readonly Lua LuaTempStashHiderID =
		(Lua.Literal) $"{ Main.PLUGIN_GUID }/{ nameof(LuaTempStashHiderID) }";
	public static readonly Lua.Variable LuaHideHandStash = new(
		nameof(LuaHideHandStash),
		$"""
		function (stash)
			stash.attachHider({ LuaTempStashHiderID }, true, { LuaAllColors })
			local function removeHider()
				if stash then
					stash.attachHider({ LuaTempStashHiderID }, false)
				end
			end
			Wait.frames(function()
				Wait.condition(
					removeHider,
					function()
						return not stash or not stash.isSmoothMoving()
					end,
					2, removeHider
				)
			end)
		end
		"""
	);
	public static readonly Lua.Variable LuaMoveObjectToHandStash = new(
		nameof(LuaMoveObjectToHandStash),
		$"""
		function (obj, target)
			target = target or { LuaGetObjectHandPlayer }(obj)
			if target then
				local old_stash = target.getHandStash()
				if obj.moveToHandStash() and obj then
					local stash = target.getHandStash()
					if (obj == stash) or (stash ~= old_stash) or obj.isDestroyed() then
						for _,color in ipairs(obj.getSelectingPlayers()) do
							obj.removeFromPlayerSelection(color)
						end
						if (obj ~= stash) then
							obj.attachHider({ LuaTempStashHiderID }, true, { LuaAllColors })
						end
						--if (obj == stash) or (stash ~= old_stash) then
							--stash.interactable = true
							{ LuaHideHandStash }(stash)
						--end
						-- // #todo: fix card reveal cases:
							-- STASH <- CARD (fixed???)
							-- nothing <- 1  CARD  (fixed)
							-- nothing <- 2  CARDs (fixed)
							-- nothing <- 3+ CARDs (broken)
					end
				end
			end
		end
		"""
	);
	public void OnClickContextual()
	{
		if (PlayerScript.PointerScript)
			PlayerScript.PointerScript.ResetInfoObject();
		InvokeAction();
	}
	void InvokeAction()
	{
		switch (type)
		{
			default: throw new UnreachableException();
			case Type.GlobalUnlock:
				Lua.Execute(
					$"""
					local global  = { true }
					local all     = global and { ctrlDown }
					local targets =
						all and { LuaHandZonePlayers }
						or      { Lua.Table((Lua) $"Player.{ target.color }") }
					for _,target in ipairs(targets) do
						if target then
							local stash = target.getHandStash()
							if stash then
								stash.interactable = not stash.interactable
							end
						end
					end
					"""
				);
				break;
			case Type.GlobalDraw:
			case Type.ObjectStash:
				Lua.Execute(
					$"""
					local global  = { type == Type.GlobalDraw }
					local all     = global and { ctrlDown }
					local swap    = { shiftDown }
					local targets =
						all and { LuaHandZonePlayers }
						or      { Lua.Table((Lua) $"Player.{ target.color }") }
					for _,target in ipairs(targets) do
						if target then
							local objs =
								not global and target.getSelectedObjects()
								or swap    and { LuaGetPlayerHandObjects }(target)
								or { Lua.Table() }
							if global or swap then
								target.drawHandStash()
							end
							for _,obj in ipairs(objs) do
								{ LuaMoveObjectToHandStash }(obj, global and target)
							end
						end
					end
					"""
				);
				break;
		}
	}
}
