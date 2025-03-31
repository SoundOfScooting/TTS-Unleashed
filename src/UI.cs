using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using NewNet;
using UnityEngine;

namespace Unleashed;

[HarmonyPatch]
public static class MainUI
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(NetworkUI), nameof(NetworkUI.Start))]
	private static void StartDisconnected()
	{
		// relocate?
		PlayerStateX.StartDisconnected();

		var numberLabel = NetworkUI.Instance.GUIDisconnected.transform
			.Find("Version/Version Number")
			.GetComponent<UILabel>();
		numberLabel.text += $" {Main.PluginColour.RGBHex}+{Main.PLUGIN_ABBR} v{Main.PLUGIN_VERSION}[-]";
	}
	public static void StartConnected()
	{
		GUIEndTurnX.StartConnected();
		ToolVectorX.StartConnected();
		UZContextualStash.StartConnected();
	}

	public static void AssignLocalTRS(this GameObject @this, GameObject src)
	{
		@this.transform.parent = src.transform.parent;
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

	// AddComponent workarounds for inadequate GetCopyOf
	public static UISprite AddComponent(this GameObject go, UISprite toAdd)
	{
		var comp  = go.AddComponent<UISprite>(toAdd);
		comp.type = toAdd.type;
		comp.SetDimensions(toAdd.width, toAdd.height);
		return comp;
	}
	public static UIButton AddComponent(this GameObject go, UIButton toAdd)
	{
		var comp         = go.AddComponent<UIButton>(toAdd);
		comp.tweenTarget = go; // toAdd.tweenTarget;
		comp.OnInit();
		return comp;
	}
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
	private static void AwakePostfix(UIPointerRotationSnap __instance) =>
		__instance.gameObject.GetOrAddComponent<UIPointerRotationSnapX>();

	private UIPointerRotationSnap @base;
	private void Awake() =>
		@base = GetComponent<UIPointerRotationSnap>();

	private void OnAltClick()
	{
		@base.bMouse1 = true;
		UICamera.current.SpoofNotifyOnClick(gameObject);
	}
}
[HarmonyPatch]
public class UZCameraHome : MonoBehaviour
{
	public const string HomePref = $"{Main.PLUGIN_GUID}/{nameof(UZCameraHome)}";
	public enum Home
	{
		Hand,
		White, Brown, Red, Orange, Yellow, Green, Teal, Blue, Purple, Pink,
		Grey, Black,
	}

	private UILabel Label;

	public static UZCameraHome Instance { get; private set; }
	public static string GUIColorText { get; private set; }

	public static bool ForceResetCameraRotation;
	private Home mCurrent;
	public static Home Current
	{
		get => Instance ? Instance.mCurrent : default;
		set
		{
			if (!Instance)
				return;
			Instance.mCurrent = value;
			NeedToPickHome    = false;
//			PlayerPrefs.SetInt(HomePref, (int) value);
			CameraController.Instance.ResetCameraRotation();
		}
	}
	private bool mNeedToPickHome;
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
	private static void AwakePostfix() =>
		new GameObject("02 CameraHome", typeof(UZCameraHome));
	private void Awake()
	{
		CreateComponents();
		Instance = this;

//		Current = (Home) PlayerPrefs.GetInt(HomePref, (int) Value.Hand);
		Current = Home.Hand;
	}
	private void CreateComponents()
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

		var labelObject = new GameObject("Label");
		labelObject.AssignLocalTRS(@base.ThisLabelObject);
		labelObject.transform.parent        = transform;
		labelObject.transform.localPosition = new(0f, -2f, 0f);
		Label = labelObject.AddComponent(@base.ThisLabelObject.GetComponent<UILabel>());
	}

	private void OnClick()
	{
		if (NeedToPickHome)
		{
			Current = Home.Hand;
			return;
		}
		NeedToPickHome = true;
	}
	private void OnAltClick()
	{
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
	private static void DelayResetCameraRotationPrefix(CameraController __instance, ref string colourLabel, ref CameraState __state)
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
	private static void DelayResetCameraRotationPostfix(CameraController __instance, CameraState __state)
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
	private static void GUIEndTurnIL(ILContext il)
	{
		ILCursor c = new(il);
		c.GotoNext(MoveType.Before,
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(Network), nameof(Network.isServer)))
		);
		c.Next.Operand =     AccessTools.PropertyGetter(typeof(Network), nameof(Network.isAdmin));
	}

	private void OnAltClick()
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
	private static void AwakePostfix(UIStarTurn __instance) =>
		__instance.gameObject.GetOrAddComponent<UIStarTurnX>();

	[HarmonyPrefix]
	[HarmonyPatch(typeof(UIStarTurn), nameof(UIStarTurn.OnClick))]
	private static bool OnClickReplace()
	{
		if (Network.isAdmin)
			Turns.Instance.GUIEndTurn();
		return false;
	}

	private void Awake() =>
		EventManager.OnPlayerPromoted += OnPlayerPromoted;
	private void OnDestroy() =>
		EventManager.OnPlayerPromoted -= OnPlayerPromoted;

	private void OnPlayerPromoted(bool isPromoted, int id)
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
	private static void StartPostfix(UINameButton __instance) =>
		__instance.gameObject.GetOrAddComponent<UINameButtonX>();

	private UINameButton @base;
	private void Awake()
	{
		@base = GetComponent<UINameButton>();
		@base.DoNotConfirm.AddRange([ "Start Turns", "Reverse Turns", "Stop Turns" ]);
	}

	public bool Extra;
	private void OnAltClick()
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
	private static bool UpdateDropDownReplace(UINameButton __instance)
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
	private static void ShowIL(ILContext il)
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
			UICamera.Notify(__instance.gameObject, "PopupListShow", null)
		);
	}
	public void PopupListShow()
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
	private static void StartPostfix(UIColorSelection __instance) =>
		__instance.gameObject.GetOrAddComponent<UIColorSelectionX>();

	private bool   restartTooltip;
	private string originalTooltip;

	private UIColorSelection @base;
	private UITooltipScript  tooltip;
	private void Awake()
	{
		@base           = GetComponent<UIColorSelection>();
		tooltip         = GetComponent<UITooltipScript>();
		originalTooltip = tooltip.Tooltip;
	}

	private void OnEnable()
	{
		restartTooltip = true;

		// fix 1 frame of desync
		@base.Update();
		// fix Black LockObject not reappearing
		@base.LockObject.SendMessage("OnEnable");
	}
	private void OnAltClick()
	{
		if (UZCameraHome.NeedToPickHome)
		{
			UZCameraHome.NeedToPickHome           = false;
			UZCameraHome.ForceResetCameraRotation = true;
			CameraController.Instance.ResetCameraRotation(@base.label);
		}
	}

	private void Update()
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
	// @idea: would be nice to use deck import UI for cards to access extra options
	// @idea: edit each ui panel to add inaccesible parameters
	private const bool DEBUG_COMPAT = false;

	private class Comp : MonoBehaviour // want: Comp<T>
	{
		// want: List<Action<T>>
		public readonly List<Delegate> OnImportFakeQueue = [];
		// @todo? onCancel
	}
	private static Comp X<T>(this T @this) where T : UICustomObject<T> =>
		@this.gameObject.GetOrAddComponent<Comp>();
	// private static bool GetX<T>(this T @this, out Comp thisX) where T : UICustomObject<T> =>
	// 	@this.TryGetComponent(out thisX);
	// private static void ClearX<T>(this T @this) where T : UICustomObject<T>
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
	private static void QueueFake<T>(this T @this, Action<T> onImport) where T : UICustomObject<T>
	{
		if (@this.X().OnImportFakeQueue.Contains(onImport))
			return;
		@this.X().OnImportFakeQueue.Add(onImport);
		@this    .CustomObjectQueue.Add(null);
		@this.NumberInQueue      = @this.CustomObjectQueue.Count - 1;
		@this.TargetCustomObject = null;
		@this.gameObject.SetActive(true);
	}
	private static object Invoke(object @this, string method) =>
		AccessTools.Method(typeof(UICustomObjectX), method)
			.MakeGenericMethod([@this.GetType()])
			.Invoke(null, [@this]);

	private static bool BaseOnEnableFake<T>(this T @this) where T : UICustomObject<T>
	{
		if (@this.TargettingFake())
		{
			@this.TargetCustomObject = null;
			@this.GetComponent<UIHighlightTargets>().Reset();
			return true;
		}
		return false;
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomObject<MonoBehaviour>), nameof(UICustomObject<>.OnEnable))]
	private static bool BaseOnEnablePrefix(object __instance) =>
		!(bool) Invoke(__instance, nameof(BaseOnEnableFake));
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomImage), nameof(UICustomImage.OnEnable))]
	private static bool ImageOnEnablePrefix(UICustomImage __instance)
	{
		if (!__instance.BaseOnEnableFake())
			return true;
		__instance.TargetCustomImage = null;
		return false;
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomSky), nameof(UICustomSky.OnEnable))]
	private static bool SkyOnEnablePrefix(UICustomSky __instance)
	{
		if (!__instance.BaseOnEnableFake())
			return true;
		__instance.TargetCustomSky = null;
		return false;
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomObject<MonoBehaviour>), nameof(UICustomObject<>.Update))]
	[HarmonyPatch(typeof(UICustomObject<MonoBehaviour>), nameof(UICustomObject<>.CheckUpdateMatchingCustomObjects))]
	private static bool BaseUpdatePrefix(object __instance) =>
		!(bool) Invoke(__instance, nameof(TargettingFake));
	private static void BaseCloseFake<T>(this T @this) where T : UICustomObject<T>
	{
		if (@this.TargettingFake())
			@this.X().OnImportFakeQueue.RemoveAt(0);
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomObject<MonoBehaviour>), nameof(UICustomObject<>.Close))]
	private static void BaseClosePrefix(object __instance) =>
		Invoke(__instance, nameof(BaseCloseFake));

	private static bool ImportFake<T>(this T @this) where T : UICustomObject<T>
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
	private static bool ImportFakePrefix(object __instance) =>
		!(bool) Invoke(__instance, nameof(ImportFake));

	[HarmonyILManipulator]
	[HarmonyPatch(typeof(CustomAssetbundle),  nameof(CustomAssetbundle .bCustomUI), MethodType.Setter)]
	[HarmonyPatch(typeof(CustomCard),         nameof(CustomCard        .bCustomUI), MethodType.Setter)]
	[HarmonyPatch(typeof(CustomDeck),         nameof(CustomDeck        .bCustomUI), MethodType.Setter)]
	[HarmonyPatch(typeof(CustomDice),         nameof(CustomDice        .bCustomUI), MethodType.Setter)]
	[HarmonyPatch(typeof(CustomImage),        nameof(CustomImage       .bCustomUI), MethodType.Setter)]
	[HarmonyPatch(typeof(CustomJigsawPuzzle), nameof(CustomJigsawPuzzle.bCustomUI), MethodType.Setter)] // @todo: fully support
	[HarmonyPatch(typeof(CustomMesh),         nameof(CustomMesh        .bCustomUI), MethodType.Setter)]
	[HarmonyPatch(typeof(CustomPDF),          nameof(CustomPDF         .bCustomUI), MethodType.Setter)] // @todo: fully support
	[HarmonyPatch(typeof(CustomSky),          nameof(CustomSky         .bCustomUI), MethodType.Setter)]
	[HarmonyPatch(typeof(CustomTile),         nameof(CustomTile        .bCustomUI), MethodType.Setter)]
	[HarmonyPatch(typeof(CustomToken),        nameof(CustomToken       .bCustomUI), MethodType.Setter)]
	private static void AllowAdminIL(ILContext il)
	{
		ILCursor c = new(il);
		c.GotoNext(MoveType.Before,
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(Network), nameof(Network.isServer)))
		);
		c.Next.Operand     = AccessTools.PropertyGetter(typeof(Network), nameof(Network.isAdmin));
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomObject<MonoBehaviour>), nameof(UICustomObject<>.CheckUpdateMatchingCustomObjects))]
	private static bool CheckUpdateMatchingCustomObjectsPrefix() =>
		Network.isServer; // @idea: implement for client?

	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomAssetbundle), nameof(UICustomAssetbundle.Import))]
	private static bool AssetbundleImportPrefix(UICustomAssetbundle __instance, bool __runOriginal)
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
		Compat.ExecuteLuaScript(
			$$"""
			local obj = getObjectFromGUID("{{ __instance.TargetCustomObject.NPO.GUID }}")
			if obj then
				obj.setCustomObject({
					assetbundle           = {{ __instance.CustomAssetbundleURL         .LuaEncode() }},
					assetbundle_secondary = {{ __instance.CustomAssetbundleSecondaryURL.LuaEncode() }},
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
	private static bool CardImportPrefix(UICustomCard __instance, bool __runOriginal)
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
		Compat.ExecuteLuaScript(
			$$"""
			local obj = getObjectFromGUID("{{ __instance.TargetCustomObject.NPO.GUID }}")
			if obj then
				obj.setCustomObject({
					face     = {{ __instance.URLFace  .LuaEncode() }},
					back     = {{ __instance.URLBack  .LuaEncode() }},
					sideways = {{ __instance.bSideways.LuaEncode() }},
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
	private static bool DeckImportPrefix(UICustomDeck __instance, bool __runOriginal)
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
		Compat.ExecuteLuaScript(
			$$"""
			local obj = getObjectFromGUID("{{ __instance.TargetCustomObject.NPO.GUID }}")
			if obj then
				obj.setCustomObject({
					face           = {{ __instance.URLFace      .LuaEncode() }},
					unique_back    = {{ __instance.bUniqueBacks .LuaEncode() }},
					back           = {{ __instance.URLBack      .LuaEncode() }},
					width          = {{ __instance.WidthRange   .intValue }},
					height         = {{ __instance.HeightRange  .intValue }},
					number         = {{ __instance.NumberRange  .intValue }},
					sideways       = {{ __instance.bSideways    .LuaEncode() }},
					back_is_hidden = {{ __instance.bBackIsHidden.LuaEncode() }},
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
	private static bool DiceImportPrefix(UICustomDice __instance, bool __runOriginal)
	{
		if (!__runOriginal || !DEBUG_COMPAT && Network.isServer)
			return __runOriginal;
		__instance.CustomImageURL = __instance.CustomImageURL.Trim();
		if (string.IsNullOrEmpty(__instance.CustomImageURL))
		{
			Chat.LogError("You must supply a custom image URL.");
			return false;
		}
		Compat.ExecuteLuaScript(
			$$"""
			local obj = getObjectFromGUID("{{ __instance.TargetCustomObject.NPO.GUID }}")
			if obj then
				obj.setCustomObject({
					image = {{ __instance.CustomImageURL.LuaEncode() }},
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
	private static bool ImageImportPrefix(UICustomImage __instance, bool __runOriginal)
	{
		if (!__runOriginal || !DEBUG_COMPAT && Network.isServer)
			return __runOriginal;
		__instance.CustomImageURL = __instance.CustomImageURL.Trim();
		if (string.IsNullOrEmpty(__instance.CustomImageURL))
		{
			Chat.LogError("You must supply a custom image URL.");
			return false;
		}
		Compat.ExecuteLuaScript(
			__instance.TargetCustomObject.NPO.GUID is null // @todo: better table detection?
				? $"""
				Tables.setCustomURL({ __instance.CustomImageURL.LuaEncode() })
				"""
				: $$"""
				local obj = getObjectFromGUID("{{ __instance.TargetCustomObject.NPO.GUID }}")
				if obj then
					obj.setCustomObject({
						image = {{ __instance.CustomImageURL.LuaEncode() }},
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
	private static bool ImageDoubleImportPrefix(UICustomImageDouble __instance, bool __runOriginal)
	{
		if (!__runOriginal || !DEBUG_COMPAT && Network.isServer)
			return __runOriginal;
		__instance.CustomImageURL = __instance.CustomImageURL.Trim();
		// @bug: (base game) CustomImageSecondaryURL is not trimmed
		if (string.IsNullOrEmpty(__instance.CustomImageURL))
		{
			Chat.LogError("You must supply a custom image URL.");
			return false;
		}
		Compat.ExecuteLuaScript(
			$$"""
			local obj = getObjectFromGUID("{{ __instance.TargetCustomObject.NPO.GUID }}")
			if obj then
				obj.setCustomObject({
					image           = {{ __instance.CustomImageURL         .LuaEncode() }},
					image_secondary = {{ __instance.CustomImageSecondaryURL.LuaEncode() }},
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
	private static bool MeshImportPrefix(UICustomMesh __instance, bool __runOriginal)
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
		Compat.ExecuteLuaScript(
			$$"""
			local obj = getObjectFromGUID("{{ __instance.TargetCustomObject.NPO.GUID }}")
			if obj then
				obj.setCustomObject({
					mesh     = {{ __instance.MeshURL     .LuaEncode() }},
					diffuse  = {{ __instance.DiffuseURL  .LuaEncode() }},
					normal   = {{ __instance.NormalURL   .LuaEncode() }},
					collider = {{ __instance.ColliderURL .LuaEncode() }},
					convex   = {{ (!__instance.NonConvex).LuaEncode() }},
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
					cast_shadows = {{ __instance.CastShadows.LuaEncode() }},
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
	private static bool SkyImportPrefix(UICustomSky __instance, bool __runOriginal)
	{
		if (!__runOriginal || !DEBUG_COMPAT && Network.isServer)
			return __runOriginal;
		__instance.CustomImageURL = __instance.CustomImageURL.Trim();
		if (string.IsNullOrEmpty(__instance.CustomImageURL))
		{
			Chat.LogError("You must supply a custom image URL.");
			return false;
		}
		Compat.ExecuteLuaScript(
			$"""
			Backgrounds.setCustomURL({ __instance.CustomImageURL.LuaEncode() })
			"""
		);
		__instance.Close();
		return false;
	}
	[HarmonyPostfix]
	[HarmonyPatch(typeof(UICustomTile), nameof(UICustomTile.OnEnable))]
	private static void TileStartPostfix(UICustomTile __instance) =>
		__instance.StretchToggle.GetComponent<BoxCollider2D>().enabled = !DEBUG_COMPAT && PlayerStateX.Host.IsModded;
	// @todo: GetCustomObject parity
	// @todo: relocate to dedicated Lua fixes/additions
	[HarmonyPostfix]
	[HarmonyPatch(typeof(LuaGameObjectScript), nameof(LuaGameObjectScript.SetCustomObject))]
	private static void LuaGameObjectScriptSetCustomObjectPostfix(LuaGameObjectScript __instance, MoonSharp.Interpreter.Table Params)
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
	private static bool TileImportPrefix(UICustomTile __instance, bool __runOriginal)
	{
		if (!__runOriginal || !DEBUG_COMPAT && Network.isServer)
			return __runOriginal;
		__instance.CustomImageURL = __instance.CustomImageURL.Trim();
		if (string.IsNullOrEmpty(__instance.CustomImageURL))
		{
			Chat.LogError("You must supply a custom image URL.");
			return false;
		}
		Compat.ExecuteLuaScript(
			$$"""
			local obj = getObjectFromGUID("{{ __instance.TargetCustomObject.NPO.GUID }}")
			if obj then
				obj.setCustomObject({
					image        = {{ __instance.CustomImageURL         .LuaEncode() }},
					image_bottom = {{ __instance.CustomImageSecondaryURL.LuaEncode() }},
					type         = {{ __instance.TypeInt }},
					thickness    = {{ 0.9f * __instance.ThicknessSlider.value + 0.1f }},
					stackable    = {{ __instance.StackableToggle.value.LuaEncode() }},
					stretch      = {{ __instance.StretchToggle  .value.LuaEncode() }}, -- requires modded host
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
	private static bool TileImportPrefix(UICustomToken __instance, bool __runOriginal)
	{
		if (!__runOriginal || !DEBUG_COMPAT && Network.isServer)
			return __runOriginal;
		__instance.CustomImageURL = __instance.CustomImageURL.Trim();
		if (string.IsNullOrEmpty(__instance.CustomImageURL))
		{
			Chat.LogError("You must supply a custom image URL.");
			return false;
		}
		Compat.ExecuteLuaScript(
			$$"""
			local obj = getObjectFromGUID("{{ __instance.TargetCustomObject.NPO.GUID }}")
			if obj then
				obj.setCustomObject({
					image          = {{ __instance.CustomImageURL.LuaEncode() }},
					thickness      = {{ 0.9f * __instance.ThicknessSlider    .value + 0.1f }},
					merge_distance = {{ 20f  * __instance.MergeDistanceSlider.value + 5f }},
					stand_up       = {{ __instance.StandupToggle  .value.LuaEncode() }},
					stackable      = {{ __instance.StackableToggle.value.LuaEncode() }},
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
	public static List<VectorEraseData> EraseBuffer = [];
	public static bool EraseCancelled;

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
	private static void InitPostfix(UIGridMenuDecals __instance)
	{
		__instance.AddDecalButton.GetComponent<UIDisableIfNotServer>().enabled = false;
		__instance.AddDecalButton.gameObject.SetActive(true);
	}

	[HarmonyILManipulator]
	[HarmonyPatch(typeof(UIGridMenu.GridButtonDecal), MethodType.Constructor)]
	private static void UIGridMenuGridButtonDecalCtorIL(ILContext il)
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
	private static void DecalManagerRPCRemoveDecalPallet()
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
	private static void StartPostfix(UIFinder __instance)
	{
		var closeButton = __instance.finder.transform
			.Find("Close Button").gameObject;
		closeButton.GetComponent<UIButtonActivate>().target = __instance.finder;
		closeButton.SetActive(true);
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

	private UISprite Icon;
	private UILabel Label;

	// Constant state
	private Type type;
	// Temporary state
	private bool ctrlDown, shiftDown;
	private LuaPlayer target;

	private void CreateComponents(Type type)
	{
		this.type = type;

		var baseObject =
			NetworkUI.Instance.GUIContextualGlobalMenu.transform
				.Find("Table/04 Paste").gameObject;
		gameObject.AssignLocalTRS(baseObject);
		if ((type & FLAG_Global) == 0)
			transform.parent = NetworkUI.Instance.GUIContextualMenu.transform.Find("Table");

		(Label = gameObject.AddComponent(baseObject.GetComponent<UILabel>()))
			.text = "            ???";
		gameObject.AddComponent(baseObject.GetComponents<UIButton>()[0])
			.onClick = [new(OnClickContextual)];
		gameObject.AddComponent(baseObject.GetComponent<BoxCollider2D>());
		gameObject.AddComponent<TweenColor>();

		var baseImagesObject = baseObject.transform.Find("Images").gameObject;
		var imagesObject     = new GameObject("Images");
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

	public void OnStartContextual()
	{
		gameObject.SetActive(false);
		if (!Network.isAdmin) // @compat
			return;

		var player = LuaGlobalScriptManager.Instance.GlobalPlayer.GetPlayer(NetworkID.ID);
		target     = player;
		ctrlDown   = zInput.GetButton("Ctrl");
		shiftDown  = zInput.GetButton("Shift");
		switch (type)
		{
			case Type.GlobalUnlock:
			{
				if (ctrlDown)
				{
					foreach (var hand in HandZone.GetHandZones())
					if      (hand.Stash)
					{
						Icon.spriteName = "Icon-Toggle";
						Label.text = "            Unlock Stash [b](All)[/b]";
						PlayerScript.PointerScript.SetActive(gameObject, true);
						return;
					}
					return;
				}
				if      (player.GetPointerPosition() is {} pos)
				foreach (var hand in HandZone.GetHandZones())
				if      (PositionHoverOverStash(pos, hand))
				{
					target = LuaPlayer.GetHandPlayer(hand.TriggerLabel);
					Icon.spriteName = "Icon-Toggle";
					Label.text =
						$"            {(
							hand.Stash.IsGrabbable ? "Lock" : "Unlock"
						)} Stash {(
							hand.TriggerColour != Colour.White
								? hand.TriggerColour.Hex
								: "")
						}[b]({hand.TriggerLabel})[/b][-]";
					PlayerScript.PointerScript.SetActive(gameObject, true);
					return;
				}
				break;
			}
			case Type.GlobalDraw:
			{
				Icon.spriteName = "Icon-DrawCard6";
				Label.text = $"            {(shiftDown ? "Swap " : "Draw ")}Stash";
				if (ctrlDown)
				{
					foreach (var hand in HandZone.GetHandZones())
					if      (hand.Stash || (shiftDown && hand.GetHandObjects().Count > 0))
					{
						Label.text += " [b](All)[/b]";
						PlayerScript.PointerScript.SetActive(gameObject, true);
					}
					return;
				}
				if      (player.GetPointerPosition() is {} pos)
				foreach (var hand in HandZone.GetHandZones())
				if      (PositionHoverOverStash(pos, hand))
				{
					target = LuaPlayer.GetHandPlayer(hand.TriggerLabel);
					Label.text += $" {(hand.TriggerColour != Colour.White ? hand.TriggerColour.Hex : "")}[b]({hand.TriggerLabel})[/b][-]";
					PlayerScript.PointerScript.SetActive(gameObject, true);
					return;
				}
				if (player.GetHandStash() || (shiftDown && player.GetHandObjects().Count > 0))
				{
					PlayerScript.PointerScript.SetActive(gameObject, true);
					return;
				}
				break;
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
			// 			Label.text = $"            Draw Stash";
			// 			PlayerScript.PointerScript.SetActive(gameObject, true);
			// 			return;
			// 		}
			// 	}
			// 	return;
			// }
			case Type.ObjectStash:
			{
				bool anyInHand = false;
				// bool anyStash  = false; // @stash
				foreach (var obj in player.GetSelectedObjects())
				{
					if (anyInHand = obj.NPO.CurrentPlayerHand)
					// if (anyStash = obj.NPO.IsHandZoneStash) // @stash
						break;
				}
				if (anyInHand)
				{
					Icon.spriteName = "Icon-DrawCard6";
					Label.text = $"            {(
						shiftDown
							? "Swap "
						// @stash
						// : anyStash
						// 	? "Draw Stash & "
						: ""
					)}Stash";
					PlayerScript.PointerScript.SetActive(gameObject, true);
				}
				break;
			}
			default:
				throw new NotImplementedException("Unreachable!");
		}

	}

/*
local function LuaPrintObject(obj)
	if not obj then
		print("(nil)")
	else
		for _,key in ipairs({
			--[[ add keys here ]]--
		}) do
			print(key, ": ", stash[key])
		end
	end
end
print("---------------------")
LuaPrintObject(player.getHandStash())
*/
	public static readonly string LuaAllColors =
		$"local {nameof(LuaAllColors)} = {{ { string.Join(", ", Colour.AllPlayerLabels.Select(color => $"'{color}'")) } }}";
	public static string LuaHandZonePlayers =>
		$"local {nameof(LuaHandZonePlayers)} = {{ { string.Join(", ", HandZone.GetHandZones().Select(hand => $"Player.{hand.TriggerLabel}")) } }}";
	public const string LuaGetPlayerHandObjects =
		$$"""
		local function {{nameof(LuaGetPlayerHandObjects)}}(player)
			return (player.getHandCount() > 0) and player.getHandObjects() or {}
		end
		""";
	public const string LuaGetObjectHandZone =
		$"""
		local function {nameof(LuaGetObjectHandZone)}(obj)
			for _,target in ipairs({nameof(LuaHandZonePlayers)}) do
				for _,obj2 in ipairs({nameof(LuaGetPlayerHandObjects)}(target)) do
					if obj == obj2 then
						return target
					end
				end
			end
		end
		""";
	public const string LuaHideHandStash =
		$"""
		local function {nameof(LuaHideHandStash)}(stash)
			stash.attachHider("{Main.PLUGIN_GUID}/HideWhileStashing", true, {nameof(LuaAllColors)})
			local function removeHider()
				if stash then
					stash.attachHider("{Main.PLUGIN_GUID}/HideWhileStashing", false)
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
		""";
	public const string LuaMoveObjectToHandStash =
		$"""
		local function {nameof(LuaMoveObjectToHandStash)}(obj, target)
			target = target or {nameof(LuaGetObjectHandZone)}(obj)
			if target then
				local old_stash = target.getHandStash()
				if obj.moveToHandStash() and obj then
					local stash = target.getHandStash()
					if (obj == stash) or (stash ~= old_stash) or obj.isDestroyed() then
						for _,color in ipairs(obj.getSelectingPlayers()) do
							obj.removeFromPlayerSelection(color)
						end
						if (obj ~= stash) then
							obj.attachHider("{Main.PLUGIN_GUID}/HideWhileStashing", true, {nameof(LuaAllColors)})
						end
						--if (obj == stash) or (stash ~= old_stash) then
							--stash.interactable = true
							{nameof(LuaHideHandStash)}(stash)
						--end
						-- @todo: fix card reveal cases:
							-- STASH <- CARD (fixed???)
							-- nothing <- 1  CARD  (fixed)
							-- nothing <- 2  CARDs (fixed)
							-- nothing <- 3+ CARDs (broken)
					end
				end
			end
		end
		""";
	public void OnClickContextual()
	{
		if (PlayerScript.PointerScript)
			PlayerScript.PointerScript.ResetInfoObject();
		ActionStash(type, target, ctrlDown, shiftDown);
	}
	public static void ActionStash(Type type, LuaPlayer target, bool ctrlDown, bool shiftDown)
	{
		switch (type)
		{
			case Type.GlobalUnlock:
				Compat.ExecuteLuaScript(
					$$"""
					for _,target in ipairs({ {{(
						ctrlDown
							? string.Join(", ", HandZone.GetHandZones().Select(hand => $"Player.{hand}"))
							: $"Player.{target.color}"
					)}} }) do
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
				var isDraw = type == Type.GlobalDraw;
				Compat.ExecuteLuaScript(
					$"""
					{LuaAllColors}
					{LuaHandZonePlayers}
					{LuaGetPlayerHandObjects}
					{LuaGetObjectHandZone}
					{LuaHideHandStash}
					{LuaMoveObjectToHandStash}
					{(
						isDraw && ctrlDown
							? $"""
							for _,target in ipairs({nameof(LuaHandZonePlayers)}) do
							local player = target
							"""
							: $"""
							local target = nil
							local player = Player.{target.color}
							"""
					)}
					if player then
						{(
							isDraw
								? shiftDown
									? $"local objs = {nameof(LuaGetPlayerHandObjects)}(player)"
									: "----"
								: "local objs = player.getSelectedObjects()"
						)}
						{(
							isDraw || shiftDown
								? "player.drawHandStash()"
								: "----"
						)}
					{(
						!isDraw || shiftDown
							? $"""
								for i,obj in ipairs(objs) do
									{nameof(LuaMoveObjectToHandStash)}(obj, target)
								end
							"""
							: "\t----"
					)}
					{(
						isDraw && ctrlDown
							? "end"
							: "----"
					)}
					end
					"""
				);
				break;
			default:
				throw new NotImplementedException("Unreachable!");
		}
	}
}
