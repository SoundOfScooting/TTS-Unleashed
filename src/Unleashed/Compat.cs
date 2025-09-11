using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MonoMod.Cil;
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

	public static void StartDisconnected() =>
		CWT = new();
	public static void StartConnected() =>
		PlayerManager.Instance.MyPlayerState().X().IsModded = true;

	// #todo: apparently CWT is broken in this Unity version?
	[HarmonyPostfix]
	[HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.Remove))]
	static void RemovePostfix(PlayerState playerState) =>
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

	public static void StartDisconnected() =>
		PlayerStateX.StartDisconnected();
	public static void StartConnected() =>
		PlayerStateX.StartConnected();

	const string VERSION_HEADER = $"\n{Main.PLUGIN_GUID} V";

	[HarmonyILManipulator]
	[HarmonyPatch(typeof(NetworkUI), nameof(NetworkUI.ConnectedToServer))]
	static void ConnectedToServerIL(ILContext il)
	{
		ILCursor c = new(il);
		c.GotoNext(MoveType.After,
			// base.networkView.RPC(RPCTarget.Server, Register, playerName, VersionNumber, SystemInfo.deviceUniqueIdentifier, VRHMD.isVR);
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(NetworkUI), nameof(NetworkUI.VersionNumber)))
		);
		c.EmitDelegate(string(string VersionNumber) =>
			VersionNumber + VERSION_HEADER + Main.PLUGIN_VERSION
		);
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(NetworkUI), nameof(NetworkUI.Register))]
	static void RegisterPrefix(ref NetworkPlayer __state) =>
		__state = Network.sender;
	[HarmonyPostfix]
	[HarmonyPatch(typeof(NetworkUI), nameof(NetworkUI.Register))]
	static void RegisterPostfix(string name, string versionnum, NetworkPlayer __state)
	{
		var sender = __state;
		int s = versionnum.IndexOf(VERSION_HEADER);
		if (s < 0)
			return;

		s += VERSION_HEADER.Length;
		int e = versionnum.IndexOf("\n", s);
		if (e < 0)
			e = versionnum.Length;

		var clientVersion = new Version(versionnum[s..e]);
		var hostVersion   = Main.Instance.Info.Metadata.Version;
		if (clientVersion.Major != hostVersion.Major || clientVersion.Minor != hostVersion.Minor)
		{
			Chat.SendChat($"{Colour.YellowHex}{name} is running incompatible {Main.PluginColour.RGBHex}{Main.PLUGIN_NAME}[-] version V{clientVersion}.");
			return;
		}
		// Chat.SendChat($"{Colour.GreenHex}{name} is running compatible {Main.PluginColour.RGBHex}{Main.PLUGIN_NAME}[-] version V{clientVersion}.");
		Wait.Frames(() =>
		{
			try
			{
				PlayerManager.Instance.PlayerStateFromID(sender.id).X().IsModded = true;
				AchievementManager.Instance.RPC_X(sender, RPCSetIsModded, NetworkPlayer.SERVER_ID);
			}
			catch (Exception e)
			{
				Chat.Log(e.ToString(), Colour.Red);
			}
		});
	}

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
		// #todo: wrong
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
		// #todo: wrong
		if (!luaGameObjectScript || luaGameObjectScript.CheckObjectRotate(heldSpinRotationIndex, num, playerColor, heldSpinRotationIndex, heldFlipRotationIndex))
		{
			npo.X().HeldTiltRotationIndex = num;
			npo.DisableFastDragWhileAnimating();
			// #todo: wrong
			EventManager.TriggerObjectRotate(npo, heldSpinRotationIndex, num, playerColor, heldSpinRotationIndex, heldFlipRotationIndex);
		}
	}

	// #todo: not usable in hotseat
	public static readonly Lua.Variable LuaGetPlayerBySteamID = new(
		nameof(LuaGetPlayerBySteamID),
		$"""
		function (steam_id)
			for _,player in ipairs(Player.getPlayers()) do
				if player.steam_id == steam_id then
					return player
				end
			end
			return nil
		end
		"""
	);
	public static readonly Lua.Variable LuaChangePlayerColorSeated = new(
		nameof(LuaChangePlayerColorSeated),
		$"""
		function (target, seated, swap)
			if target and seated then
				local target_color = target.color
				local seated_color = seated.color

				seated.changeColor("Grey")
				target.changeColor(seated_color)

				if swap and (target_color ~= "Grey") then
					seated.changeColor(target_color)
				end
			end
		end
		"""
	);

	[HarmonyPrefix]
	[HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.PromoteThisPlayer))]
	static bool PromoteThisPlayerPrefix(string name)
	{
		if (Network.isServer || !Network.isAdmin)
			return true;
		var steamId = PlayerManager.Instance.SteamIDFromName(name);
		Lua.Execute(
			$"""
			local player = { LuaGetPlayerBySteamID }({ steamId })
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
			local player = { LuaGetPlayerBySteamID }({ steamId })
			if player then
				player.kick()
			end
			"""
		);
		return false;
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(Pointer), nameof(Pointer.SetPhysics))]
	static bool SetPhysicsPrefix(Pointer __instance, int HoverObjectId, RigidbodyState rigidbodyState, PhysicsMaterialState physicsMaterialState)
	{
		if (Network.isServer || PlayerStateX.Host.IsModded)
			return true;

		List<string> guids = [];
		foreach (var npo in __instance.GetSelectedNPOs(HoverObjectId, bAlwaysIncludeSelected: true, bIncludeHeld: true))
		if      (npo.gameObject)
		{
			guids.Add(npo.GUID);
			npo.HighlightNotify(__instance.PointerDarkColour);
		}
		Lua.Execute(
			$"""
			for _,guid in ipairs({ Lua.Table(guids) }) do
				local obj = getObjectFromGUID(guid)
				if obj then
					obj.use_gravity      = { rigidbodyState.UseGravity }
					obj.mass             = { rigidbodyState.Mass }
					obj.drag             = { rigidbodyState.Drag }
					obj.angular_drag     = { rigidbodyState.AngularDrag }
					obj.static_friction  = { physicsMaterialState.StaticFriction }
					obj.dynamic_friction = { physicsMaterialState.DynamicFriction }
					obj.bounciness       = { physicsMaterialState.Bounciness }
				end
			end
			"""
		);
		return false;
	}
}

