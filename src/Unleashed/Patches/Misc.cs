namespace Unleashed.Patches;

[HarmonyPatch]
public static class Misc
{
	[HarmonyPrefix]
	[HarmonyPatch(typeof(NetworkUI), nameof(NetworkUI.GUIPlayerSelection))]
	static bool GUIPlayerSelectionPrefix(NetworkUI __instance, string Value, int playerID)
	{
		var playerState = PlayerManager.Instance.PlayerStateFromID(playerID);
		if     (PlayerManager.Instance.NameInUse(playerState.name))
		switch (Value)
		{
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
					ChangeColorDialog(playerID);
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
				PlayerManager.Instance.networkView.RPC(RPCTarget.All, PlayerManager.Instance.RPCMute, playerID, false);
				return false;
			case "Server Mute":
				PlayerManager.Instance.networkView.RPC(RPCTarget.All, PlayerManager.Instance.RPCMute, playerID, true);
				return false;
		}
		return true;
	}
	public static void ChangeColorDialog(int nplayerID = -1)
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
	// [HarmonyPostfix]
	// [HarmonyPatch(typeof(NetworkUI), nameof(NetworkUI.SetSpecificPlayerName))]
	// static void NetworkUISetSpecificPlayerNamePostfix(NetworkUI __instance, string newName)
	// {
	// 	if (!Network.isServer || __instance.bHotseat || __instance.playerIDToSet == -1)
	// 		return;

	// 	var player = PlayerManager.Instance.PlayerStateFromID(__instance.playerIDToSet);
	// 	player.name = newName;
	// 	// update playerName which does nothing
	// 	__instance.networkView.RPC(player.networkPlayer, __instance.UpdateName, newName);

	// 	// would trigger Auto Join Message
	// 	var playerData = new PlayerManager.PlayerData(player);
	// 	PlayerManager.Instance.RemovePlayer(player.id);
	// 	PlayerManager.Instance.AddPlayer   (playerData);
	// 	// PlayerManager.Instance.networkView.RPC(RPCTarget.Others, PlayerManager.Instance.RPCRemovePlayer, player.id);
	// 	// PlayerManager.Instance.networkView.RPC(RPCTarget.Others, PlayerManager.Instance.RPCAddPlayer,    playerData);
	// }

	[HarmonyPrefix]
	[HarmonyPatch(typeof(UIColorSelection), nameof(UIColorSelection.Update))]
	static bool UIColorSelectionUpdateReplace(UIColorSelection __instance)
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
				(label != PlayerManager.Instance.ColourLabelFromID(NetworkID.PlayerID(UIColorSelection.id))) &&
				(zInput.GetButton("Ctrl") || zInput.GetButton("Shift"))
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
					vector = new(Screen.width / 2, Screen.height / 2 - 60, 0f);
					break;
				}
				Vector3 position = new(0f, 3f, 0f);
				if (CameraController.Instance.bTopDown && !UZCameraHome.NeedToPickHome && Available("Black"))
					position.z -= 2f;

				vector = __instance.MainCamera.WorldToScreenPoint(position);
				break;
			}
			case "Black" when !UZCameraHome.NeedToPickHome:
			{
				if (VRHMD.isVR)
				{
					vector = new(Screen.width / 2, Screen.height / 2 + 60, 0f);
					break;
				}
				Vector3 position = new(0f, 10f, 0f);
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
					float f = (float) Math.PI / 5f * Colour.IDFromColour(__instance.colour);
					float x = 185f * Mathf.Cos(f)  + Screen.width  / 2;
					float y = 185f * Mathf.Sin(f)  + Screen.height / 2;
					vector  = new(x, y, 0f);
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
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UIColorSelection), nameof(UIColorSelection.OnClick))]
	static bool UIColorSelectionOnClickPrefix(UIColorSelection __instance)
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

	[HarmonyILManipulator]
	[HarmonyPatch(typeof(Pointer), nameof(Pointer.Update))]
	static void PointerUpdateIL(ILContext il)
	{
		var c = new ILCursor(il);
		c.GotoNext(MoveType.Before,
			// else if (CurrentPointerMode == PointerMode.Paint)
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(Pointer), nameof(Pointer.CurrentPointerMode))),
			x => x.MatchLdcI4((int) PointerMode.Paint),
			x => x.MatchBneUn(out _)
		);
		c.Index++;
		c.EmitDelegate(PointerMode(PointerMode currentPointerMode) =>
			(currentPointerMode == PointerMode.VectorPixel)
				? PointerMode.Paint
				: currentPointerMode
		);
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(Pointer), nameof(Pointer.IsVectorTool))]
	static bool PointerIsVectorToolPrefix(PointerMode mode, ref bool __result)
	{
		// allow usage even if not in the toolbar
		if (mode != PointerMode.VectorPixel)
			return true;
		__result = true;
		return false;
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(ToolVector), nameof(ToolVector.UpdateVectorPixel))]
	static bool ToolVectorUpdateVectorPixelReplace(ToolVector __instance)
	{
		if (__instance.VectorActionDown() || (__instance.VectorAction() && __instance.drawing && __instance.CheckMove()))
			__instance.StartDrawing(2, loop: true);
		if (__instance.VectorActionUp())
			__instance.EndDrawing();
		return false;
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(ToolVector), nameof(ToolVector.UpdateVectorErase))]
	static bool ToolVectorUpdateVectorErasePrefix(ToolVector __instance)
	{
		// #todo: somehow preserve overlap order of lines?
			// redrawn lines are all on top, but at least in relative order to each other
		if (__instance.VectorActionDown())
		{
			ToolVectorX.EraseBuffer    = [];
			ToolVectorX.EraseCancelled = false;
		}
		if (__instance.VectorActionUp())
			ToolVectorX.EraseCancelled = false;

		return !ToolVectorX.EraseCancelled;
	}
	[HarmonyILManipulator]
	[HarmonyPatch(typeof(ToolVector), nameof(ToolVector.UpdateVectorErase))]
	static void ToolVectorUpdateVectorEraseIL(ILContext il)
	{
		var c = new ILCursor(il);
		int found = 0;
		while (c.TryGotoNext(MoveType.Before,
			// RPCRemoveLine(drawnLine.Key);
			x => x.MatchCall(AccessTools.Method(typeof(ToolVector), nameof(ToolVector.RPCRemoveLine)))
		)){
			found++;
			c.Emit(OpCodes.Ldloc_2);
			c.EmitDelegate(void(KeyValuePair<uint, ToolVector.VectorDrawData> drawnLine) =>
				ToolVectorX.EraseBuffer.Add(new(drawnLine.Value))
			);
			c.Index += 3; // annoying
		}
		if (found != 2)
			Main.Log.LogWarning($"{nameof(Misc)}.{nameof(ToolVectorUpdateVectorEraseIL)} expected 2, found {found}");
	}
	// [HarmonyTranspiler]
	// [HarmonyPatch(typeof(ToolVector), nameof(ToolVector.UpdateVectorErase))]
	// static IEnumerable<CodeInstruction> ToolVectorUpdateVectorEraseTranspiler(IEnumerable<CodeInstruction> instructions)
	// {
	// 	int found = 0;
	// 	foreach (var instruction in instructions)
	// 	{
	// 		if (instruction.Calls(AccessTools.Method(typeof(ToolVector), nameof(ToolVector.RPCRemoveLine))))
	// 		{
	// 			yield return new(System.Reflection.Emit.OpCodes.Ldloc_2);
	// 			found++;
	// 			yield return Transpilers.EmitDelegate(void(KeyValuePair<uint, ToolVector.VectorDrawData> drawnLine) =>
	// 				ToolVectorX.EraseBuffer.Add(new(drawnLine.Value))
	// 			);
	// 		}
	// 		yield return instruction;
	// 	}
	// 	if (found != 2)
	// 		Main.Log.LogWarning($"{nameof(Misc)}.{nameof(ToolVectorUpdateVectorEraseTranspiler)} expected 2, found {found}");
	// }
	[HarmonyILManipulator]
	[HarmonyPatch(typeof(ToolVector), nameof(ToolVector.LateUpdate))]
	static void ToolVectorLateUpdateIL(ILContext il)
	{
		var c = new ILCursor(il);
		c.GotoNext(MoveType.After,
			// if (zInput.GetButtonDown("Tap") && drawing)
			x => x.MatchLdstr("Tap"),
			x => x.MatchLdcI4(0),
			x => x.MatchCall(AccessTools.Method(typeof(zInput), nameof(zInput.GetButtonDown)))
		);
		c.Emit(OpCodes.Ldarg_0);
		c.EmitDelegate(bool(bool tapDown, ToolVector __instance) =>
		{
			if (tapDown && (__instance.pointerMode == PointerMode.VectorErase) && __instance.VectorAction() && !ToolVectorX.EraseCancelled)
			{
				ToolVectorX.EraseCancelled = true;
				ToolVectorX.EraseBuffer.Sort((a, b) =>
					Comparison.Compare(a.sortingOrder, b.sortingOrder)
				);
				List<ToolVector.LineNetworkData> lines = [];
				foreach (var erased in ToolVectorX.EraseBuffer)
				{
					var drawData = erased.drawData;
					if (drawData.attached != erased.wasAttached)
						continue; // attached was destroyed
					lines.Add(new(
						__instance.GetGUID(),
						drawData.attached,
						erased  .positions,
						drawData.color,
						drawData.thickness,
						drawData.rotation,
						drawData.loop,
						drawData.square
					));
//					__instance.networkView.RPC(RPCTarget.Others, __instance.RPCAddLine, lineNetworkData);
//					__instance.RPCAddLine(lineNetworkData);
				}
				if (lines.Count > 0)
				{
					__instance.networkView.RPC(RPCTarget.Others, __instance.RPCAddLines, lines);
					__instance.RPCAddLines(lines);
				}
			}
			return tapDown;
		});

		ILLabel skipLabel = null;
		c.GotoNext(MoveType.After,
			// if (zInput.GetButtonDown("Tap") && drawing)
			x => x.MatchLdarg(0),
			x => x.MatchLdfld(AccessTools.Field(typeof(ToolVector), nameof(ToolVector.drawing))),
			x => x.MatchBrfalse(out skipLabel)
		);
		c.Emit(OpCodes.Ldarg_0); // could instead emit before "Tap" check
		c.EmitDelegate(bool(ToolVector __instance) =>
			__instance.pointerMode == PointerMode.VectorPixel
		);
		c.Emit(OpCodes.Brtrue, skipLabel);

		c.GotoNext(MoveType.Before,
			// RPCRemoveLine(currentDrawingGuid);
			x => x.MatchLdarg(0),
			x => x.MatchLdarg(0),
			x => x.MatchLdfld(AccessTools.Field (typeof(ToolVector), nameof(ToolVector.currentDrawingGuid))),
			x => x.MatchCall (AccessTools.Method(typeof(ToolVector), nameof(ToolVector.RPCRemoveLine)))
		);
		c.MoveAfterLabels();
		c.RemoveRange(4);
		c.GotoNext(MoveType.Before,
			// EndDrawing();
			x => x.MatchLdarg(0),
			x => x.MatchCall(AccessTools.Method(typeof(ToolVector), nameof(ToolVector.EndDrawing)))
		);
		c.Index++;
		c.Remove();
		c.EmitDelegate(void(ToolVector __instance) =>
			__instance.RPCRemoveLine(__instance.currentDrawingGuid)
		);
	}
	[HarmonyILManipulator]
	[HarmonyPatch(typeof(Pointer), nameof(Pointer.StartLine))]
	static void PointerStartLineIL(ILContext il)
	{
		var c = new ILCursor(il);
		c.GotoNext(MoveType.After,
			// if (HighLightedObjects.Count == 0)
			x => x.MatchLdarg   (0),
			x => x.MatchCall    (AccessTools.PropertyGetter(typeof(Pointer),                    nameof(Pointer.HighLightedObjects))),
			x => x.MatchCallvirt(AccessTools.PropertyGetter(typeof(List<NetworkPhysicsObject>), nameof(List<>.Count)))
		);
		c.Emit(OpCodes.Ldarg_2);
		c.EmitDelegate(int(int count, NetworkPhysicsObject hoverObject) =>
			Network.isClient && Settings.EntryEnableFastFlick.Value && (count == 0) && hoverObject
				? 1
				: count
		);
	}

	[HarmonyILManipulator]
	[HarmonyPatch(typeof(Pointer), nameof(Pointer.StartContextual))]
	static void PointerStartContextualIL(ILContext il)
	{
		var c = new ILCursor(il);
		c.GotoNext(MoveType.After,
			// SetActive(NetworkInstance.GUIContextualHandReveal, NetworkSingleton<NetworkUI>.Instance.handZoneToReveal != null && NetworkSingleton<NetworkUI>.Instance.handZoneToReveal.TriggerLabel == PointerColorLabel);
			x => x.MatchLdfld(AccessTools.Field(typeof(NetworkUI), nameof(NetworkUI.handZoneToReveal))),
			x => x.MatchCallvirt(AccessTools.PropertyGetter(typeof(HandZone), nameof(HandZone.TriggerLabel))),
			x => x.MatchLdarg(0),
			x => x.MatchLdfld(AccessTools.Field(typeof(Pointer), nameof(Pointer.PointerColorLabel))),
			x => x.MatchCall(AccessTools.Method(typeof(string), "op_Equality"))
		);
		c.EmitDelegate(bool(bool eq) => true);

		c.GotoNext(MoveType.After,
			// SetActive(NetworkInstance.GUIContextualPhysics, Network.isServer && flag);
			x => x.MatchLdfld(AccessTools.Field(typeof(NetworkUI), nameof(NetworkUI.GUIContextualPhysics))),
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(Network), nameof(Network.isServer)))
		);
		c.Previous.Operand = AccessTools.PropertyGetter(typeof(Network), nameof(Network.isAdmin));

		c.GotoNext(MoveType.After,
			// SetActive(NetworkInstance.GUIContextualCustom, Network.isServer && (bool)InfoObject.GetComponent<CustomObject>());
			x => x.MatchLdfld(AccessTools.Field(typeof(NetworkUI), nameof(NetworkUI.GUIContextualCustom))),
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(Network), nameof(Network.isServer)))
		);
		c.Previous.Operand = AccessTools.PropertyGetter(typeof(Network), nameof(Network.isAdmin));

#if TRUE_ULTIMATE_POWER
		// #todo: either support or remove Scripting but keep GUID
		c.GotoNext(MoveType.After,
			// SetActive(NetworkInstance.GUIContextualScripting, Network.isServer && !string.IsNullOrEmpty(component.GUID));
			x => x.MatchLdfld(AccessTools.Field(typeof(NetworkUI), nameof(NetworkUI.GUIContextualScripting))),
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(Network), nameof(Network.isServer)))
		);
		c.EmitDelegate(bool(bool isServer) => true);
#endif

		// #gold
		c.GotoNext(MoveType.Before,
			// SetActive(((Component)NetworkInstance.GUIContextualGoldBool.get_transform().get_parent()).get_gameObject(), Network.isServer && SteamManager.bKickstarterGold);
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(Network), nameof(Network.isServer))),
			x => x.MatchBrfalse(out _),
			x => x.MatchLdsfld(AccessTools.Field(typeof(SteamManager), nameof(SteamManager.bKickstarterGold))),
			x => x.MatchBr(out _),
			x => x.MatchLdcI4(0)
		);
		c.MoveAfterLabels();
		c.Remove();
		c.EmitDelegate(bool() =>
			PlayerManager.Instance.HostPlayerState().IsModded
		);
		// c.RemoveRange(2);
		// c.Index++;
		// c.RemoveRange(2);

		c.GotoNext(MoveType.After,
			// NetworkInstance.GUIContextualMenu.SetActive(value: true);
			x => x.MatchLdarg(0),
			x => x.MatchLdfld(AccessTools.Field(typeof(Pointer), nameof(Pointer.NetworkInstance))),
			x => x.MatchLdfld(AccessTools.Field(typeof(NetworkUI), nameof(NetworkUI.GUIContextualMenu))),
			x => x.MatchLdcI4(1),
			x => x.MatchCallvirt(AccessTools.Method(typeof(GameObject), nameof(GameObject.SetActive)))
		);
		// c.MoveAfterLabels();
		c.Index--;
		c.Emit(OpCodes.Ldarg_1);
		c.EmitDelegate(void(GameObject InfoObject) =>
		{
			// slow?
			foreach (var item in NetworkUI.Instance.GUIContextualMenu.transform.GetChild(0).GetComponentsInChildren<IUZContextual>(true))
				item.OnStartContextual();
		});
	}
	[HarmonyPostfix]
	[HarmonyPatch(typeof(Pointer), nameof(Pointer.StartGlobalContextual))]
	static void PointerStartGlobalContextualPostfix()
	{
		// slow?
		foreach (var item in NetworkUI.Instance.GUIContextualGlobalMenu.transform.GetChild(0).GetComponentsInChildren<IUZContextual>(true))
			item.OnStartContextual();
	}
}

