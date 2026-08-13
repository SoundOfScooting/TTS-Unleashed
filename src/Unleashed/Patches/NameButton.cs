using Unleashed.Settings;

namespace Unleashed.Patches;

[HarmonyPatch]
sealed class UINameButtonX : MonoBehaviour
{
	[Setting]
	static readonly DebugSetting<bool> DebugChangeName = new()
	{
		Key     = "Change Name Button",
		Default = false,
	};

	[HarmonyPostfix]
	[HarmonyPatch(typeof(UINameButton), nameof(UINameButton.Start))]
	static void StartPostfix(UINameButton __instance)
		=> __instance.gameObject.GetOrAddComponent<UINameButtonX>();

	UINameButton @base;
	UIButton button;
	void Awake()
	{
		@base  = GetComponent<UINameButton>();
		button = GetComponent<UIButton>();
		@base.DoNotConfirm.AddRange([
			"Start Turns", "Reverse Turns", "Stop Turns",
			"Mute", "Unmute",
		]);
		@base.PopupList.OnPopupListShow += OnPopupListShow;
	}
	void OnDestroy()
		=> @base.PopupList.OnPopupListShow -= OnPopupListShow;

	bool Extra;
	void OnAltClick()
	{
		if (!UIPopupList.isOpen)
			Extra = true;
		UICamera.SpoofOnClick(gameObject);
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(UINameButton), nameof(UINameButton.UpdateDropDown))]
	static bool UpdateDropDownReplace(UINameButton __instance)
	{
		if (PlayerManager.Instance.PlayersDictionary.TryGetValue(__instance.id, out var playerState))
			__instance.NameLabel.text = playerState.Name;
		return false;
	}
	void OnPopupListShow()
	{
		(var isExtra, Extra) = (Extra, false);

		var isHotseat   = NetworkUI.Instance.IsHotseat;
		var isOwnButton = @base.id == Network.ID;
		var buttonColor =
			Colour.ColourFromUIColour(button.defaultColor).Label;

		var items = @base.PopupList.items;
		items.Clear();

		if (isExtra && Network.IsAdmin)
		{
			if (Turns.Instance.TurnsState.Enable)
			{
				if (!isHotseat)
					items.Add("Stop Turns");
				items.Add("Reverse Turns");
			}
			else if (!isHotseat)
				items.Add("Start Turns");
		}
		if ((isHotseat || Turns.Instance.TurnsState.Enable) && (Turns.Instance.TurnsState.TurnColor != buttonColor)) // IsTurn is stupid
		{
			if (Turns.Instance.TurnsState.PassTurns && (isHotseat || Turns.Instance.IsTurn()))
				items.Add("Pass Turn");
			else if (!isHotseat && Network.IsAdmin)
				items.Add("Set Turn");
		}

		if (Network.IsAdmin || isOwnButton)
			items.Add("Change Color");
		if (Network.IsAdmin || PermissionsOptions.Options.ChangeTeam)
			items.Add("Change Team");

		if (DebugChangeName.Value || (isHotseat && isOwnButton))
			items.Add("Change Name");

		if (isExtra && (Network.IsAdmin || isOwnButton))
			items.Add(PlayerManager.Instance.IsBlinded(@base.id)
				? "Unblindfold"
				: "Blindfold"
			);
		items.Add(PlayerManager.Instance.IsMuted(@base.id)
			? "Unmute"
			: "Mute"
		);
		if (Network.IsAdmin && !isHotseat)
		{
			items.Add("Server Mute");
			items.Add("Server Unmute");
		}
		if (Network.IsAdmin && !PlayerManager.Instance.IsHost(@base.id) && !isHotseat)
		{
			items.Add(PlayerManager.Instance.IsPromoted(@base.id)
				? "Demote"
				: "Promote"
			);
			items.Add("Kick");
		}
		if (Network.IsServer && !isHotseat && !isOwnButton)
		{
			items.Add("Ban");
			items.Add("Give Host");
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(NetworkUI), nameof(NetworkUI.GUIPlayerSelection))]
	static bool GUIPlayerSelectionPrefix(NetworkUI __instance, string Value, int playerID)
	{
		var playerState = PlayerManager.Instance.PlayerStateFromID(playerID);
		if (!PlayerManager.Instance.NameInUse(playerState.Name))
			return true;

		switch (Value)
		{
			default: return true;

			case "Start Turns":
				Turns.Instance.TurnsState = new(
					Turns.Instance.TurnsState,
					Enable:    true,
					TurnColor: playerState.ColorLabel
				);
				return false;
			case "Stop Turns":
				Turns.Instance.TurnsState = new(
					Turns.Instance.TurnsState,
					Enable:    false,
					TurnColor: ""
				);
				return false;
			case "Reverse Turns":
				Turns.Instance.TurnsState = new(
					Turns.Instance.TurnsState,
					Reverse: !Turns.Instance.TurnsState.Reverse
				);
				return false;

			case "Change Color":
				// #cut
				if (UICamera.currentTouchID != UICameraTouch.LEFT)
				{
					UIColorSelection.ShowDialog(playerID);
					return false;
				}
				if (UZCameraHome.NeedToPickHome)
					UZCameraHome.NeedToPickHome = false;
				else if (__instance.bNeedToPickColour && playerID == Network.ToID(UIColorSelection.id))
				{
					__instance.bNeedToPickColour = false;
					return false;
				}

				if (playerID != Network.ID || zInput.GetButton("Ctrl") || zInput.GetButton("Shift"))
				{
					UIColorSelection.id = playerID;
					__instance.bNeedToPickColour = true;
					return false;
				}
				__instance.GUIChangeColor();
				return false;

			case "Unblindfold":
			case "Blindfold":
				if (playerID == Network.ID)
				{
					PlayerManager.Instance.ToggleBlindfold();
					return false;
				}
				PlayerManager.Instance.SetBlindfoldForPlayer(playerID, !playerState.Blind);
				return false;

			case "Unmute":
			case "Mute":
				EventManager.TriggerPlayerMute(playerState.Muted ^= true, playerID);
				return false;
			case "Server Unmute":
			case "Server Mute":
				PlayerManager.Instance.RPC(RPCTarget.All, PlayerManager.Instance.RPCMute, playerID, Value == "Server Mute");
				return false;
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.SetBlindfoldForPlayer))]
	static bool SetBlindfoldForPlayerPrefix(int id, bool blind)
	{
		if (API.IsServer || !Network.IsAdmin)
			return true;
		Lua.Execute(
			$"""
			local player = { PlayerManager.Instance.PlayerStateFromID(id) }
			if player then
				player.blindfolded = { blind }
			end
			"""
		);
		return false;
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.PromoteThisPlayer))]
	static bool PromoteThisPlayerPrefix(string name)
	{
		if (API.IsServer || !Network.IsAdmin)
			return true;
		var id = PlayerManager.Instance.IDFromName(name);
		Lua.Execute(
			$"""
			local player = { PlayerManager.Instance.PlayerStateFromID(id) }
			if player then
				player.promote()
			end
			"""
		);
		return false;
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.KickThisPlayer))]
	static bool KickThisPlayerPrefix(string name)
	{
		if (API.IsServer || !Network.IsAdmin)
			return true;
		var id = PlayerManager.Instance.IDFromName(name);
		Lua.Execute(
			$"""
			local player = { PlayerManager.Instance.PlayerStateFromID(id) }
			if player then
				player.kick()
			end
			"""
		);
		return false;
	}
}

