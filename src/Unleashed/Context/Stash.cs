using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Unleashed.Context;

sealed class UZContextualStash : UZMonoBehaviour
{
	enum Act
	{
		GlobalUnlock = 0 << 1 | 1,
		GlobalDraw   = 1 << 1 | 1,
		ObjectDraw   = 2 << 1 | 0,
		ObjectStash  = 3 << 1 | 0,
	}
	Act act;
	bool IsGlobal => (act & Act.GlobalUnlock) != 0;

	UISprite Icon;
	UILabel Label;

	[ModuleInitializer]
	internal static void ModuleInitializer()
		=> Events.OnStartConnected += OnStartConnected;
	static void OnStartConnected()
	{
		var parent = NetworkUI.Instance.GUIContextualMenu.transform.Find("Table");
		var @base  = NetworkUI.Instance.GUIContextualGlobalMenu.transform.Find("Table/04 Paste");

		// Global: After 04 Paste
		// InstantiateX(@base, parent: null, active: false, name: "04 Pb |SORT| Unlock Stash")
		// 	.GetOrAddComponent<UZContextualStash>()
		// 	.Initialize(Act.GlobalUnlock);
		InstantiateX(@base, parent: null, active: false, name: "04 Pc |SORT| Draw Stash")
			.GetOrAddComponent<UZContextualStash>()
			.Initialize(Act.GlobalDraw);

		// Object: After 06 Draw
		// InstantiateX(@base, parent: parent, active: false, name: "06 Ds |SORT| Draw Stash")
		// 	.GetOrAddComponent<UZContextualStash>()
		// 	.Initialize(Act.ObjectDraw);
		InstantiateX(@base, parent: parent, active: false, name: "06 Ds |SORT| Stash")
			.GetOrAddComponent<UZContextualStash>()
			.Initialize(Act.ObjectStash);
	}
	void Initialize(Act act)
	{
		this.act = act;

		Label = GetComponent<UILabel>();
		Label.text = Contextual.LABEL_PADDING + "???";

		GetComponent<UIButton>().onClick = [new(OnClickContextual)];
		Destroy(GetComponents<UIButton>()[1..]);
		GetComponent<I2.Loc.Localize>().enabled = false; // #loc

		Icon = transform.Find("Images").GetComponent<UISprite>();
		Icon.spriteName = "???";

		if (IsGlobal)
			Events.OnStartGlobalContextual += OnStartContextual;
		else
			Events.OnStartContextual += OnStartContextual;
	}
	void OnDestroy()
	{
		if (IsGlobal)
			Events.OnStartGlobalContextual -= OnStartContextual;
		else
			Events.OnStartContextual -= OnStartContextual;
	}

	public static bool PositionNearStash(Vector3 pos, HandZone hand)
	{
		if      (hand.Stash/*  && !hand.Stash.IsGrabbable */)
		foreach (var collider in hand.Stash.colliders)
		// if      (collider.bounds.Contains(pos with { y = collider.bounds.center.y }))
		// if      (collider.ClosestPoint(pos = pos with { y = collider.bounds.center.y }) == pos)
		if      ((collider.ClosestPoint(pos = pos with { y = collider.bounds.center.y }) - pos).magnitude < 1f /* 1e-5f */)
			return true;
		return false;
	}

	bool ctrlDown, shiftDown;
	LuaPlayer target;

	void OnStartContextual()
		=> Contextual.Check(gameObject, CheckContextual);
	bool CheckContextual()
	{
		if (!Network.IsAdmin) // #compat
			return false;

		var player = LuaGlobal.Instance.GlobalPlayer.GetPlayer(Network.ID);
		target     = player;
		ctrlDown   = zInput.GetButton(Inputs.Ctrl);
		shiftDown  = zInput.GetButton(Inputs.Shift);
		switch (act)
		{
			default: throw new UnreachableException();
			case Act.GlobalUnlock:
			{
				if (ctrlDown)
				{
					foreach (var hand in HandZone.GetHandZones())
					if      (hand.Stash)
					{
						Icon.spriteName = "Icon-Toggle";
						Label.text = Contextual.LABEL_PADDING +
							"Unlock Stash [b](All)[/b]";
						return true;
					}
					return false;
				}

				if      (player.GetPointerPosition() is {} pos)
				foreach (var hand in HandZone.GetHandZones())
				if      (PositionNearStash(pos, hand))
				{
					target = LuaPlayer.GetHandPlayer(hand.TriggerLabel);
					Icon.spriteName = "Icon-Toggle";
					Label.text = Contextual.LABEL_PADDING +
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
			case Act.GlobalDraw:
			{
				Icon.spriteName = "Icon-DrawCard6";
				Label.text = Contextual.LABEL_PADDING +
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
				if      (PositionNearStash(pos, hand))
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

				var hand2 = HandZone.GetHandZone(player.color, 0, true);
				if ((hand2 && hand2.Stash) || (shiftDown && player.GetHandObjects().Count > 0))
					return true;
				return false;
			}
			case Act.ObjectDraw:
			{
				if      (!shiftDown)
				foreach (var obj in player.GetSelectedObjects())
				if      (obj.NPO.IsHandZoneStash)
				{
					Icon.spriteName = "Icon-DrawCard6";
					Label.text = Contextual.LABEL_PADDING + "Draw Stash";
					return true;
				}
				return false;
			}
			case Act.ObjectStash:
			{
				foreach (var obj in player.GetSelectedObjects())
				if      (obj.NPO.CurrentPlayerHand && (shiftDown || !obj.NPO.IsHandZoneStash))
				{
					Icon.spriteName = "Icon-DrawCard6";
					Label.text = Contextual.LABEL_PADDING +
						$"{(shiftDown ? "Swap " : "")}Stash";
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
	public static readonly Lua.Variable LuaAllHandZonePlayers = new(
		nameof(LuaAllHandZonePlayers),
		$$"""
		{}
		for _,color in ipairs(Player.getAvailableColors()) do
			table.insert({{ (Lua) nameof(LuaAllHandZonePlayers) }}, Player[color])
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
			for _,target in ipairs({ LuaAllHandZonePlayers }) do
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
	public static readonly Lua.Variable LuaAttachTempHider = new(
		nameof(LuaAttachTempHider),
		$"""
		function (obj, temp)
			if not obj or obj.isDestroyed() then return end
			obj.attachHider({ LuaTempStashHiderID }, true, { LuaAllColors })
			if temp == false then return end

			local function removeHider()
				if obj then
					obj.attachHider({ LuaTempStashHiderID }, false)
				end
			end
			Wait.frames(function()
				Wait.condition(
					removeHider,
					function()
						return not obj or not obj.isSmoothMoving()
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
			{ LuaAttachTempHider }(obj)
			obj.moveToHandStash()
		end
		"""
		// $"""
		// function (obj, target)
		// 	target = target or { LuaGetObjectHandPlayer }(obj)
		// 	if target then
		// 		local old_stash = target.getHandStash()
		// 		if obj.moveToHandStash() and obj then
		// 			local stash = target.getHandStash()
		// 			if (obj == stash) or (stash ~= old_stash) or obj.isDestroyed() then
		// 				for _,color in ipairs(obj.getSelectingPlayers()) do
		// 					obj.removeFromPlayerSelection(color)
		// 				end
		// 				if (obj ~= stash) then
		// 					obj.attachHider({ LuaTempStashHiderID }, true, { LuaAllColors })
		// 				end
		// 				--if (obj == stash) or (stash ~= old_stash) then
		// 					--stash.interactable = true
		// 					{ LuaHideHandStash }(stash)
		// 				--end
		// 				-- // #todo: fix card reveal cases:
		// 					-- STASH <- CARD (fixed???)
		// 					-- nothing <- 1  CARD  (fixed)
		// 					-- nothing <- 2  CARDs (fixed)
		// 					-- nothing <- 3+ CARDs (broken)
		// 			end
		// 		end
		// 	end
		// end
		// """
	);
	public void OnClickContextual()
	{
		if (Pointer.MyPointer)
			Pointer.MyPointer.ResetInfoObject();
		InvokeAction();
	}
	void InvokeAction()
	{
		switch (act)
		{
			default: throw new UnreachableException();
			case Act.GlobalUnlock:
				throw new NotImplementedException();
			case Act.GlobalDraw:
				Lua.Execute(
					$$"""
					--[[ {{ (Lua) $"{act}" }} ]]--
					local all  = {{ ctrlDown }}
					local swap = {{ shiftDown }}

					local targets = {}
					for _,target in ipairs(all and {{ LuaAllHandZonePlayers }} or { Player[{{ target.color }}] }) do
						targets[target] = swap and {{ LuaGetPlayerHandObjects }}(target) or {}
					end

					for target,objs in pairs(targets) do
						target.drawHandStash()
						for _,obj in ipairs(objs) do
							{{ LuaMoveObjectToHandStash }}(obj, target)
						end
					end
					"""
				);
				break;
			case Act.ObjectDraw:
				throw new NotImplementedException();
			case Act.ObjectStash:
				Lua.Execute(
					$$"""
					--[[ {{ (Lua) $"{act}" }} ]]--
					local swap = {{ shiftDown }}

					local targets = {}
					for _,obj in ipairs(Player[{{ target.color }}].getSelectedObjects()) do
						local target = {{ LuaGetObjectHandPlayer }}(obj)
						if (target ~= nil) then
							targets[target] = targets[target] or {}
							-- if (target.getHandStash() ~= obj) then
								table.insert(targets[target], obj)
							-- end
						end
					end

					for target,objs in pairs(targets) do
						if swap then
							if #objs == 0 then
								objs = {{ LuaGetPlayerHandObjects }}(target)
							end
							target.drawHandStash()
						end
						for _,obj in ipairs(objs) do
							{{ LuaMoveObjectToHandStash }}(obj, target)
						end
					end
					"""
				);
				break;
		}
	}
}

