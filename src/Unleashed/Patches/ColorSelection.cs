namespace Unleashed.Patches;

static class UIColorSelectionExtensions
{
	extension(UIColorSelection)
	{
		public static void ShowDialog(int nplayerID = -1)
		{
			var playerID    = NetworkID.PlayerID(nplayerID);
			var playerState = PlayerManager.Instance.PlayerStateFromID(playerID);
			UIDialog.ShowDropDown(
				$"[b]{UZCameraHome.GUIColorText}[/b]" + (
					(playerID == NetworkID.ID) ? "" : $"\n{playerState.name}"
				),
				dropDownOptions: [.. Colour.AllPlayerLabels],
				drowDownValue:   playerState.stringColor,

				leftButtonText: "OK",
				leftButtonFunc: label =>
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
sealed class UIColorSelectionX : MonoBehaviour
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
		@base   = GetComponent<UIColorSelection>();
		tooltip = GetComponent<UITooltipScript>();
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
			zInput.GetButtonChanged("Ctrl") ||
			zInput.GetButtonChanged("Shift")
		){
			restartTooltip  = false;
			tooltip.ChangeTooltip(originalTooltip);
			if (!UZCameraHome.NeedToPickHome && Network.isAdmin)
			{
				if (zInput.GetButton("Shift"))
					tooltip.ChangeTooltip("[SWAP]\n"  + tooltip.Tooltip);
				else if (zInput.GetButton("Ctrl"))
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
		if (!Network.isAdmin ||
			!zInput.GetButton("Ctrl") && !zInput.GetButton("Shift") ||
			!PlayerManager.Instance.ColourInUse(__instance.label)
		) return true;

		var targetID = NetworkID.PlayerID(UIColorSelection.id);
		var seatedID = PlayerManager.Instance.IDFromColour(__instance.colour);
		if ((seatedID == -1) || (seatedID == targetID))
			return true;

		var target = PlayerManager.Instance.PlayersDictionary[targetID];
		var seated = PlayerManager.Instance.PlayersDictionary[seatedID];
		Lua.Execute(
			$"""
			local target = { Compat.LuaGetPlayerBySteamID }({ target.steamId })
			local seated = { Compat.LuaGetPlayerBySteamID }({ seated.steamId })
			local swap   = { zInput.GetButton("Shift") }
			{ Compat.LuaChangePlayerColorSeated }(target, seated, swap)
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
		static bool Permitted(string label) =>
			!DEBUG_TEST && (
				(label == "Grey") || Network.isAdmin ||
				((label != "Black") && PermissionsOptions.options.ChangeColor)
			);
		static bool Available(string label) =>
			DEBUG_TEST ||
			!PlayerManager.Instance.ColourInUse(label) || (
				Network.isAdmin &&
				(zInput.GetButton("Ctrl") || zInput.GetButton("Shift")) &&
				(label != PlayerManager.Instance.ColourLabelFromID(NetworkID.PlayerID(UIColorSelection.id)))
			);

		var permitted = UZCameraHome.NeedToPickHome || Permitted(__instance.label);
		var available = UZCameraHome.NeedToPickHome || Available(__instance.label);
		var visible   = available;
		var vector    = Vector3.zero;

		if     (visible)
		switch (__instance.label)
		{
			case "Grey":
			{
				if (VRHMD.isVR)
				{
					vector = new(Screen.width / 2, Screen.height / 2 - 60);
					break;
				}
				var position = new Vector3(0f, 3f, 0f);
				if (CameraController.Instance.bTopDown && !UZCameraHome.NeedToPickHome && Available("Black"))
					position.z -= 2f;

				vector = __instance.MainCamera.WorldToScreenPoint(position);
				break;
			}
			case "Black" when !UZCameraHome.NeedToPickHome:
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
					vector = new(
						x: 185f * Mathf.Cos(f) + Screen.width  / 2,
						y: 185f * Mathf.Sin(f) + Screen.height / 2
					);
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

