using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unleashed.Compat;

namespace Unleashed.Context;

sealed class UZContextualStash : MonoBehaviour
{
	enum Act
	{
		GlobalUnlock = 0 << 1 | 1,
		GlobalDraw   = 1 << 1 | 1,
		ObjectDraw   = 2 << 1 | 0,
		ObjectStash  = 3 << 1 | 0,
	}
	Act act;
	bool IsGlobal => act switch
	{
		Act.GlobalDraw   => true,
		Act.GlobalUnlock => true,
		Act.ObjectDraw   => false,
		Act.ObjectStash  => false,
		_ => throw new UnreachableException(),
	};

	UISprite Icon;
	UILabel Label;

	[ModuleInitializer]
	internal static void Initializer() =>
		Events.OnStartConnected += OnStartConnected;
	static void OnStartConnected()
	{
		// after 04 Paste
		if (API.TRUE_ULTIMATE_POWER)
			new GameObject("04 Pb |SORT| Unlock Stash")
				.AddComponent<UZContextualStash>()
				.CreateComponents(Act.GlobalUnlock);
		new GameObject("04 Pc |SORT| Draw Stash")
			.AddComponent<UZContextualStash>()
			.CreateComponents(Act.GlobalDraw);

		// after 06 Draw
		// new GameObject("06 Ds |SORT| Draw Stash")
		// 	.AddComponent<UZContextualStash>()
		// 	.CreateComponents(Type.ObjectDraw);
		new GameObject("06 Ds |SORT| Stash")
			.AddComponent<UZContextualStash>()
			.CreateComponents(Act.ObjectStash);
	}

	void CreateComponents(Act act)
	{
		this.act = act;

		var @base = NetworkUI.Instance.GUIContextualGlobalMenu.transform.Find("Table/04 Paste");
		transform.CopyParent(@base);
		if (!IsGlobal)
			transform.parent = NetworkUI.Instance.GUIContextualMenu.transform.Find("Table");

		(Label = gameObject.CopyComponent<UILabel>(@base))
			.text = Contextual.LABEL_PADDING + "???";
		gameObject.CopyComponent(@base.GetComponents<UIButton>()[0])
			.onClick = [new(OnClickContextual)];
		gameObject.CopyComponent<BoxCollider2D>(@base);
		gameObject.AddComponent <TweenColor>();

		var baseImages   = @base.Find("Images");
		var imagesObject = new GameObject(baseImages.name);
		imagesObject.CopyParent(baseImages).parent = transform;

		(Icon = imagesObject.CopyComponent<UISprite>(baseImages))
			.spriteName = "???";

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

	bool ctrlDown, shiftDown;
	LuaPlayer target;

	void OnStartContextual() =>
		Contextual.Check(gameObject, CheckContextual);
	bool CheckContextual()
	{
		if (!Network.isAdmin) // #compat
			return false;

		var player = LuaGlobalScriptManager.Instance.GlobalPlayer.GetPlayer(NetworkID.ID);
		target     = player;
		ctrlDown   = zInput.GetButton("Ctrl");
		shiftDown  = zInput.GetButton("Shift");
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
				if      (PositionHoverOverStash(pos, hand))
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
			case Act.ObjectStash:
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
					Label.text = Contextual.LABEL_PADDING +
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
		switch (act)
		{
			default: throw new UnreachableException();
			case Act.GlobalUnlock:
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
			case Act.GlobalDraw:
			case Act.ObjectStash:
				Lua.Execute(
					$"""
					local global  = { act == Act.GlobalDraw }
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

