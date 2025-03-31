using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using NewNet;
using UnityEngine;
using NPO_X = Unleashed.NetworkPhysicsObjectX;

namespace Unleashed;

[HarmonyPatch]
public class PlayerStateX
{
	public static ConditionalWeakTable<PlayerState, PlayerStateX> CWT = new();

	public bool IsModded;

	public static PlayerStateX Host =>
		PlayerManager.Instance.PlayerStateFromID(NetworkPlayer.SERVER_ID).X();

	// @todo: apparently CWT is broken in this Unity version?
	public static void StartDisconnected() =>
		CWT = new();
	[HarmonyPostfix]
	[HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.Remove))]
	private static void RemovePostfix(PlayerState playerState) =>
		CWT.Remove(playerState);
}
public class NetworkPhysicsObjectX : MonoBehaviour
{
	public int HeldTiltRotationIndex;
}
[HarmonyPatch]
public static class Compat
{
	public static PlayerStateX X(this PlayerState @this) =>
		PlayerStateX.CWT.GetOrCreateValue(@this);
	public static bool GetX(this PlayerState @this, out PlayerStateX thisX) =>
		PlayerStateX.CWT.TryGetValue(@this, out thisX);
	public static void ClearX(this PlayerState @this) =>
		PlayerStateX.CWT.Remove(@this);
	public static NPO_X X(this NetworkPhysicsObject @this) =>
		@this.gameObject.GetOrAddComponent<NPO_X>();
	public static bool GetX(this NetworkPhysicsObject @this, out NPO_X thisX) =>
		@this.TryGetComponent(out thisX);
	public static void ClearX(this NetworkPhysicsObject @this)
	{
		if (@this.GetX(out var thisX))
			UnityEngine.Object.Destroy(thisX);
	}

	public static int PlayerID(int id) =>
		(id == -1) ? NetworkID.ID : id;

	// [RemoteX(Permission.Server)]
	// public static void RPCSetPlayerStateX(AchievementManager _, ushort id, PlayerStateX playerX)
	// {
	// 	var player = PlayerManager.Instance.PlayerStateFromID(id);
	// 	PlayerStateX.CWT.Remove(player);
	// 	PlayerStateX.CWT.Add   (player, playerX);
	// }
	[RemoteX(Permission.Server)]
	public static void RPCSetIsModded(AchievementManager _, ushort id) =>
		PlayerManager.Instance.PlayerStateFromID(id).X().IsModded = true;

	[RemoteX(Permission.Owner, SendType.ReliableNoDelay, null, SerializationMethod.Default)]
	public static void ChangeHeldTiltRotationIndex(this Pointer @this, int tiltRotationDelta, int touchId = -1)
	{
		if (tiltRotationDelta == 0)
			return;
		if (Network.isClient)
		{
			if (PlayerStateX.Host.IsModded)
				@this.RPC_X(RPCTarget.Server, ChangeHeldTiltRotationIndex, tiltRotationDelta, touchId);
			return;
		}
		// @todo: wrong
		var action = tiltRotationDelta switch
		{
			>= 1 and <= 11 => PlayerAction.FlipIncrementalRight,
			12             => PlayerAction.FlipOver,
			_              => PlayerAction.FlipIncrementalLeft,
		};
		if (!EventManager.CheckPlayerAction(@this.PointerColorLabel, action, @this.GetGrabbedLuaObjects(touchId)))
			return;
		foreach (var grabbableNPO in ManagerPhysicsObject.Instance.GrabbableNPOs)
		if      (grabbableNPO.HeldByPlayerID == @this.ID && grabbableNPO.HeldByTouchID == touchId)
			ManagerPhysicsObject.Instance.SetHeldObjectTiltRotationIndex(grabbableNPO, tiltRotationDelta, @this.ID);
	}
	public static void SetHeldObjectTiltRotationIndex(this ManagerPhysicsObject @this, NetworkPhysicsObject npo, int tiltDelta, int id)
	{
		int heldFlipRotationIndex = npo    .HeldFlipRotationIndex;
		int heldSpinRotationIndex = npo    .HeldSpinRotationIndex;
		int heldTiltRotationIndex = npo.X().HeldTiltRotationIndex;
		int num = (heldTiltRotationIndex + tiltDelta) % 24;

		var luaGameObjectScript = npo.luaGameObjectScript;
		var playerColor = PlayerManager.Instance.PlayerStateFromID(id)?.stringColor;
		// @todo: wrong
		if (!luaGameObjectScript || luaGameObjectScript.CheckObjectRotate(heldSpinRotationIndex, num, playerColor, heldSpinRotationIndex, heldFlipRotationIndex))
		{
			npo.X().HeldTiltRotationIndex = num;
			npo.DisableFastDragWhileAnimating();
			// @todo: wrong
			EventManager.TriggerObjectRotate(npo, heldSpinRotationIndex, num, playerColor, heldSpinRotationIndex, heldFlipRotationIndex);
		}
	}

	public static string LastExecutedLuaScript = "";
	public static void ExecuteLuaScript(string lua)
	{
		LastExecutedLuaScript = lua;
		LuaGlobalScriptManager.Instance.RPCExecuteScript(lua);
	}

	// public static string LuaEscape(string aText) =>
	// 	I2.Loc.SimpleJSON.JSONNode.Escape(aText);
	public static string LuaEncode(this string @this)
	{
		var escape = "";
		while (@this.Contains($"]{ escape }]"))
			escape += "=";
		return $"[{ escape }[{ @this }]{ escape }]";
	}
	public static string LuaEncode(this bool @this) =>
		@this.ToString().ToLower();
	public const string LuaGetPlayerBySteamID =
		$"""
		local function {nameof(LuaGetPlayerBySteamID)}(steam_id)
			for _,player in ipairs(Player.getPlayers()) do
				if player.steam_id == steam_id then
					return player
				end
			end
		end
		""";

	[HarmonyPrefix]
	[HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.PromoteThisPlayer))]
	private static bool PromoteThisPlayerPrefix(string name)
	{
		if (Network.isServer || !Network.isAdmin)
			return true;
		var steamId = PlayerManager.Instance.SteamIDFromName(name);
		ExecuteLuaScript(
			$"""
			{LuaGetPlayerBySteamID}
			local player = {nameof(LuaGetPlayerBySteamID)}("{steamId}")
			if player then
				player.promote()
			end
			"""
		);
		return false;
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.KickThisPlayer))]
	private static bool KickThisPlayerPrefix(string name)
	{
		if (Network.isServer || !Network.isAdmin)
			return true;
		var steamId = PlayerManager.Instance.SteamIDFromName(name);
		ExecuteLuaScript(
			$"""
			{LuaGetPlayerBySteamID}
			local player = {nameof(LuaGetPlayerBySteamID)}("{steamId}")
			if player then
				player.kick()
			end
			"""
		);
		return false;
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(Pointer), nameof(Pointer.SetPhysics))]
	private static bool SetPhysicsPrefix(Pointer __instance, int HoverObjectId, RigidbodyState rigidbodyState, PhysicsMaterialState physicsMaterialState)
	{
		if (Network.isServer && PlayerStateX.Host.IsModded)
			return true;

		List<string> guids = [];
		foreach (var npo in __instance.GetSelectedNPOs(HoverObjectId, bAlwaysIncludeSelected: true, bIncludeHeld: true))
		if      (npo.gameObject)
		{
			guids.Add(npo.GUID);
			npo.HighlightNotify(__instance.PointerDarkColour);
		}
		ExecuteLuaScript(
			$$"""
			for _,guid in ipairs({ {{ guids.Select(x => $"'{x}'") }} }) do
				local obj = getObjectFromGUID(guid)
				if obj then
					obj.use_gravity      = {{ rigidbodyState.UseGravity.LuaEncode() }}
					obj.mass             = {{ rigidbodyState.Mass }}
					obj.drag             = {{ rigidbodyState.Drag }}
					obj.angular_drag     = {{ rigidbodyState.AngularDrag }}
					obj.static_friction  = {{ physicsMaterialState.StaticFriction }}
					obj.dynamic_friction = {{ physicsMaterialState.DynamicFriction }}
					obj.bounciness       = {{ physicsMaterialState.Bounciness }}
				end
			end
			"""
		);
		return false;
	}
}

