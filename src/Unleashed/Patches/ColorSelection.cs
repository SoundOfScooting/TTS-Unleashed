namespace Unleashed.Patches;

static class UIColorSelectionExtensions
{
	extension(UIColorSelection)
	{
		public static void ShowDialog(int nplayerID = -1)
		{
			var playerID    = Network.ToID(nplayerID);
			var playerState = PlayerManager.Instance.PlayerStateFromID(playerID);
			UIDialog.ShowDropDown(
				$"[b]{UZCameraHome.GUIColorText}[/b]" + (
					(playerID == Network.ID) ? "" : $"\n{playerState.Name}"
				),
				dropDownOptions: [.. Colour.AllPlayerLabels],
				drowDownValue:   playerState.ColorLabel,

				leftButtonText: "OK",
				leftButtonFunc: (label, _) =>
				{
					if (UZCameraHome.NeedToPickHome)
						UZCameraHome.NeedToPickHome = false;
					NetworkUI.Instance.CheckColor(label, playerID);
				},

				rightButtonText: "Cancel",
				rightButtonFunc: null
			);
		}
	}
}
[HarmonyPatch]
sealed class UIColorSelectionX : UZMonoBehaviour
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(UIColorSelection), nameof(UIColorSelection.Start))]
	static void StartPostfix(UIColorSelection __instance)
	{
		__instance.gameObject.SetActive(true);
		__instance.GetOrAddComponent<UIColorSelectionX>();
	}

	bool   restartTooltip;
	string originalTooltip;

	UIColorSelection @base;
	UITooltipObject  tooltip;
	void Awake()
	{
		@base   = GetComponent<UIColorSelection>();
		tooltip = GetComponent<UITooltipObject>();
		originalTooltip = tooltip.Tooltip;
	}

	void OnEnable()
	{
		restartTooltip = true;

		// fix 1 frame of desync
		@base.Update();
		// fix Black LockObject not reappearing
		@base.LockObject.SendMessage(nameof(OnEnable));
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
		if (restartTooltip ||
			zInput.GetButtonChanged(Inputs.Ctrl) ||
			zInput.GetButtonChanged(Inputs.Shift)
		){
			restartTooltip = false;
			tooltip.ChangeTooltip(originalTooltip);
			if (!UZCameraHome.NeedToPickHome && Network.IsAdmin)
			{
				if (zInput.GetButton(Inputs.Shift))
					tooltip.ChangeTooltip("[SWAP]\n"  + tooltip.Tooltip);
				else if (zInput.GetButton(Inputs.Ctrl))
					tooltip.ChangeTooltip("[FORCE]\n" + tooltip.Tooltip);
			}
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(UIColorSelection), nameof(UIColorSelection.OnClick))]
	static bool OnClickPrefix(UIColorSelection __instance)
	{
		if (UZCameraHome.NeedToPickHome)
		{
			UZCameraHome.Current = Enum.Parse<UZCameraHome.Home>(__instance.label);
			return false;
		}
		if (!Network.IsAdmin ||
			!zInput.GetButton(Inputs.Ctrl) && !zInput.GetButton(Inputs.Shift) ||
			!PlayerManager.Instance.ColourInUse(__instance.label)
		) return true;

		var targetID = Network.ToID(UIColorSelection.id);
		var seatedID = PlayerManager.Instance.IDFromColour(__instance.colour);
		if ((seatedID == -1) || (seatedID == targetID))
			return true;

		Lua.Execute(
			$"""
			local target = { PlayerManager.Instance.PlayersDictionary[targetID] }
			local seated = { PlayerManager.Instance.PlayersDictionary[seatedID] }
			local swap   = { zInput.GetButton(Inputs.Shift) }
			{ Lua.ChangePlayerColorSeated }(target, seated, swap)
			"""
		);
		NetworkUI.Instance.bNeedToPickColour = false;
		return false;
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(UIColorSelection), nameof(UIColorSelection.Update))]
	static bool UpdateReplace(UIColorSelection __instance)
	{
		const bool DEBUG_TEST = false;
		static bool Permitted(string label)
			=> !DEBUG_TEST && (
				(label == Colour.GreyLabel) || Network.IsAdmin ||
				((label != Colour.BlackLabel) && PermissionsOptions.Options.ChangeColor)
			);
		static bool Available(string label)
		{
			if (DEBUG_TEST)
				return true;
			if (NetworkUI.Instance.IsHotseat && label == Colour.GreyLabel)
				return false;
			return !PlayerManager.Instance.ColourInUse(label) || (
				Network.IsAdmin &&
				(zInput.GetButton(Inputs.Ctrl) || zInput.GetButton(Inputs.Shift)) &&
				(label != PlayerManager.Instance.ColourLabelFromID(Network.ToID(UIColorSelection.id)))
			);
		}

		var permitted = UZCameraHome.NeedToPickHome || Permitted(__instance.label);
		var available = UZCameraHome.NeedToPickHome || Available(__instance.label);
		var visible   = available;
		var vector    = Vector3.zero;

		if     (visible)
		switch (__instance.label)
		{
			case Colour.GreyLabel:
			{
				if (VRHMD.isVR)
				{
					vector = new(Screen.width / 2, Screen.height / 2 - 60);
					break;
				}
				var position = new Vector3(0f, 3f, 0f);
				if (CameraController.Instance.bTopDown && !UZCameraHome.NeedToPickHome && Available(Colour.BlackLabel))
					position.z -= 2f;

				vector = __instance.MainCamera.WorldToScreenPoint(position);
				break;
			}
			case Colour.BlackLabel when !UZCameraHome.NeedToPickHome:
			{
				if (VRHMD.isVR)
				{
					vector = new(Screen.width / 2, Screen.height / 2 + 60);
					break;
				}
				var position = new Vector3(0f, 10f, 0f);
				if (CameraController.Instance.bTopDown)
					position.z += 2f;

				vector = __instance.MainCamera.WorldToScreenPoint(position);
				break;
			}
			default:
			{
				var hand = HandZone.GetHand(__instance.label);
				if (!(visible = hand))
					break;
				if (VRHMD.isVR)
				{
					var f = (float) Math.PI / 5f * Colour.IDFromColour(__instance.colour);
					vector = new()
					{
						x = 185f * Mathf.Cos(f) + Screen.width  / 2,
						y = 185f * Mathf.Sin(f) + Screen.height / 2,
					};
					break;
				}
				vector = __instance.MainCamera.WorldToScreenPoint(hand.transform.position);
				break;
			}
		}

		// every frame :)
		if (__instance.LockObject)
			__instance.LockObject.SetActive(visible && !permitted);
		__instance.ColorUIButton.enabled = visible;
		__instance.ColorUISprite.enabled = visible;
		__instance.GetComponent<BoxCollider2D>().enabled = visible && permitted;
		if (__instance.uiPulse)
			__instance.uiPulse.enabled = visible && permitted;

		if (vector.z >= 0f)
			vector.z  = 0f;
		if (vector != Vector3.zero)
			__instance.transform.position = __instance.CameraUI.ScreenToWorldPoint(vector);
		return false;
	}
}

