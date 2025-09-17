using System.Runtime.CompilerServices;
using Unleashed.Compat;
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
	static void StartPostfix(UINameButton __instance) =>
		__instance.gameObject.GetOrAddComponent<UINameButtonX>();

	UINameButton @base;
	UIButton button;
	void Awake()
	{
		@base  = GetComponent<UINameButton>();
		button = GetComponent<UIButton>();
		@base.DoNotConfirm.AddRange([ "Start Turns", "Reverse Turns", "Stop Turns" ]);
		@base.PopupList.OnPopupListShow += OnPopupListShow;
	}
	void OnDestroy() =>
		@base.PopupList.OnPopupListShow -= OnPopupListShow;

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
			__instance.NameLabel.text = playerState.name;
		return false;
	}
	void OnPopupListShow()
	{
		var items = @base.PopupList.items;
		items.Clear();

		(var isExtra, Extra) = (Extra, false);

		var isHotseat   = NetworkUI.Instance.bHotseat;
		var isOwnButton = @base.id == NetworkID.HotseatID;
		var buttonColor =
			Colour.ColourFromUIColour(button.defaultColor).Label;

		if (isExtra && Network.isAdmin)
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

		if (DebugChangeName.Value || (isHotseat && isOwnButton))
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

	[HarmonyPrefix]
	[HarmonyPatch(typeof(NetworkUI), nameof(NetworkUI.GUIPlayerSelection))]
	static bool GUIPlayerSelectionPrefix(NetworkUI __instance, string Value, int playerID)
	{
		var playerState = PlayerManager.Instance.PlayerStateFromID(playerID);
		if (!PlayerManager.Instance.NameInUse(playerState.name))
			return true;

		switch (Value)
		{
			default: return true;

			case "Start Turns":
				Turns.Instance.turnsState = new(
					Turns.Instance.turnsState,
					Enable:    true,
					TurnColor: playerState.stringColor
				);
				return false;
			case "Stop Turns":
				Turns.Instance.turnsState = new(
					Turns.Instance.turnsState,
					Enable:    false,
					TurnColor: ""
				);
				return false;
			case "Reverse Turns":
				Turns.Instance.turnsState = new(
					Turns.Instance.turnsState,
					Reverse: !Turns.Instance.turnsState.Reverse
				);
				return false;

			case "Change Color":
				if (UICamera.currentTouchID != UICameraTouch.LEFT)
				{
					UIColorSelection.ShowDialog(playerID);
					return false;
				}
				if (UZCameraHome.NeedToPickHome)
					UZCameraHome.NeedToPickHome = false;
				else if (__instance.bNeedToPickColour && playerID == NetworkID.PlayerID(UIColorSelection.id))
				{
					__instance.bNeedToPickColour = false;
					return false;
				}

				if (playerID != NetworkID.ID || zInput.GetButton("Ctrl") || zInput.GetButton("Shift"))
				{
					UIColorSelection.id = playerID;
					__instance.bNeedToPickColour = true;
					return false;
				}
				__instance.GUIChangeColor();
				return false;

			case "Blindfold":
			case "Unblindfold":
				PlayerManager.Instance.ChangeBlindfold(playerID, !playerState.blind);
				return false;

			case "Unmute":
			case "Mute":
				EventManager.TriggerPlayerMute(playerState.muted ^= true, playerID);
				return false;
			case "Server Unmute":
			case "Server Mute":
				PlayerManager.Instance.RPC(RPCTarget.All, PlayerManager.Instance.RPCMute, playerID, Value == "Server Mute");
				return false;
		}
	}

	[ModuleInitializer]
	internal static void Initializer() =>
		RemoteX.RegisterOverrides += (RPCMethods) =>
		{
			// bugfix
			// -[Remote(Permission.Admin)]
			// +[Remote("Turns/Turns.SetPlayerTurn")]
			RPCMethods.Set(
				AccessTools.Method(typeof(Turns), nameof(Turns.SetPlayerTurn)),
				player => player.isAdmin || (
					API.HostModded &&
					Turns.Instance.turnsState.PassTurns &&
					Turns.Instance.IsTurn(PlayerManager.Instance.PlayerStateFromID(player.id).stringColor)
				)
			);
		};
	[HarmonyPrefix]
	[HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.PromoteThisPlayer))]
	static bool PromoteThisPlayerPrefix(string name)
	{
		if (Network.isServer || !Network.isAdmin)
			return true;
		var steamId = PlayerManager.Instance.SteamIDFromName(name);
		Lua.Execute(
			$"""
			local player = { Lua.GetPlayerBySteamID }({ steamId })
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
		if (Network.isServer || !Network.isAdmin)
			return true;
		var steamId = PlayerManager.Instance.SteamIDFromName(name);
		Lua.Execute(
			$"""
			local player = { Lua.GetPlayerBySteamID }({ steamId })
			if player then
				player.kick()
			end
			"""
		);
		return false;
	}
}

