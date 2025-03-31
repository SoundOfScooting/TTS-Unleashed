using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MonoMod.Cil;
using NewNet;
using MethodRPCSort = NewNet.NetworkView.MethodRPCSort;

namespace Unleashed;

[HarmonyPatch]
[AttributeUsage(AttributeTargets.Method)]
public class RemoteX : BaseNetworkAttribute
{
	public SendType sendType = SendType.ReliableBuffered;
	public RemoteX(
		Permission permission = Permission.Client,
		SendType sendType = SendType.ReliableBuffered,
		string validationFunction = null,
		SerializationMethod serializationMethod = SerializationMethod.Default
	){
		this.permission = permission;
		this.sendType = sendType;
		this.validationFunction = validationFunction;
		this.serializationMethod = serializationMethod;
	}
	public RemoteX(Permission permission)
	{
		this.permission = permission;
		sendType = SendType.ReliableBuffered; // ???
	}
	public RemoteX(SendType sendType)
	{
		permission = Permission.Client;
		this.sendType = sendType;
	}
	public RemoteX(string validationFunction)
	{
		permission = Permission.Client;
		this.validationFunction = validationFunction;
	}
	public RemoteX(SerializationMethod serializationMethod)
	{
		permission = Permission.Client;
		this.serializationMethod = serializationMethod;
	}
	public static explicit operator Remote(RemoteX r) =>
		new(r.permission, r.sendType, r.validationFunction, r.serializationMethod);

	[HarmonyILManipulator]
	[HarmonyPatch(typeof(NetworkView), nameof(NetworkView.FindAttributeAssemblies))]
	private static void NetworkViewFindAttributeAssembliesIL(ILContext il)
	{
		ILCursor c = new(il);
		c.GotoNext(MoveType.Before,
			// RPCMethods.Sort((MethodRPCSort x, MethodRPCSort y) => x.uniqueName.CompareTo(y.uniqueName));
			x => x.MatchCallvirt(AccessTools.Method(typeof(List<MethodRPCSort>), nameof(List<>.Sort), [ typeof(Comparison<MethodRPCSort>) ]))
		);
		c.MoveAfterLabels();
		c.Remove();
		c.MoveAfterLabels();
		c.EmitDelegate(void(List<MethodRPCSort> RPCMethods, Comparison<MethodRPCSort> comp) =>
		{
			// WARNING: DO NOT REMOVE //
			RPCMethods.Sort(comp);
			// ////////////////////// //
			ChangeAttributes(RPCMethods);

			List<MethodRPCSort> CustomRPCMethods = [];
			AddAttributesX (CustomRPCMethods);
			FindAttributesX(CustomRPCMethods);
			SortAttributesX(CustomRPCMethods);
			RPCMethods.AddRange(CustomRPCMethods);

			// DumpAttributes(RPCMethods);
		});
	}
	private static void DumpAttributes(List<MethodRPCSort> RPCMethods)
	{
		for (int i = 0; i < RPCMethods.Count; i++)
		{
			var entry = RPCMethods[i];
			Main.Log.LogWarning($"{i}: {entry.classType} / {entry.method}");
		}
	}
	public static void ChangeAttributes(List<MethodRPCSort> RPCMethods)
	{
		void SetRemote(MethodInfo method, Remote rpc)
		{
			var i = RPCMethods.FindIndex(x => x.method == method);
			var s = RPCMethods[i];
			s.rpc = rpc;
			RPCMethods[i] = s;
		}

		// BUGFIX
		// [Remote(Permission.Admin)]
		// =>
		// [Remote("PLUGIN_GUID/Turns.SetPlayerTurn")]
		SetRemote(AccessTools.Method(typeof(Turns), nameof(Turns.SetPlayerTurn)),
			new($"{Main.PLUGIN_GUID}/Turns.SetPlayerTurn")
		);
		validationFunctions.Add(
			$"{Main.PLUGIN_GUID}/Turns.SetPlayerTurn",
			player => PlayerStateX.Host.IsModded
				? player.isAdmin || (
					Turns.Instance.turnsState.PassTurns &&
					Turns.Instance.IsTurn(PlayerManager.Instance.PlayerStateFromID(player.id).stringColor)
				)
				: player.isAdmin
		);

		// [Remote(Permission.Server)]
		// =>
		// [Remote(Permission.Owner, SendType.ReliableBuffered, "Permissions/Contextual", SerializationMethod.Default)]
		SetRemote(AccessTools.Method(typeof(Pointer), nameof(Pointer.SetPhysics)),
			new(Permission.Owner, SendType.ReliableBuffered, $"{Main.PLUGIN_GUID}/Pointer.SetPhysics", SerializationMethod.Default)
		);
		validationFunctions.Add(
			$"{Main.PLUGIN_GUID}/Pointer.SetPhysics",
			player => PlayerStateX.Host.IsModded
				? validationFunctions["Permissions/Contextual"](player)
				: player.isServer
		);
	}

	public static void AddAttributesX(List<MethodRPCSort> CustomRPCMethods)
	{
		//
	}
	public static List<MethodRPCSort> FindAttributesX(List<MethodRPCSort> CustomRPCMethods)
	{
		foreach (var asm  in AppDomain.CurrentDomain.GetAssemblies())
		foreach (var type in asm.GetTypes())
		{
			// static
			var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (var method in methods)
			if      (IsDefined(method, typeof(RemoteX)))
			{
				var parameters = method.GetParameters();
				if (parameters is not [ { ParameterType: var behaviorType }, .. ] || !behaviorType.IsSubclassOf(typeof(NetworkBehavior)))
					throw new InvalidOperationException($"Invalid method for {nameof(RemoteX)} attribute {method.DeclaringType}.{method.Name}");

				var attr = method.GetCustomAttributes<RemoteX>(true).First();
				CustomRPCMethods.Add(new(behaviorType, method, (Remote) attr));
			}
//			// instance
//			if (type.IsSubclassOf(typeof(NetworkBehavior)))
//			{
//				methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
//				foreach (var method in methods)
//				if      (IsDefined(method, typeof(RemoteX)))
//				{
//					var attr = method.GetCustomAttributes<RemoteX>(true).First();
//					CustomRPCMethods.Add(new(type, method, (Remote) attr));
//				}
//			}
		}
		return CustomRPCMethods;
	}
	public static void SortAttributesX(List<MethodRPCSort> CustomRPCMethods)
	{
		CustomRPCMethods.Sort((a, b) =>
		{
			var Chainloader_T = Traverse.Create(typeof(BepInEx.Bootstrap.Chainloader));
			var plugins       = Chainloader_T.Field("_plugins").GetValue<List<BepInEx.BaseUnityPlugin>>();

			int asmA  = plugins.FindIndex(p => p?.GetType()?.Assembly == a.method.DeclaringType.Assembly);
			int asmB  = plugins.FindIndex(p => p?.GetType()?.Assembly == b.method.DeclaringType.Assembly);
			var comp  = asmA.CompareTo(asmB); // -1 (non-plugin asm) < 0 (first plugin)
			if (comp != Comparison.EQ)
				return comp;

			comp = a.uniqueName.CompareTo(b.uniqueName);
			if (comp != Comparison.EQ)
				return comp;

			static int compareTypes(IEnumerable<Type> typesA, IEnumerable<Type> typesB)
			{
				using var enmA = typesA.GetEnumerator();
				using var enmB = typesB.GetEnumerator();
				while (true)
				{
					if (!enmA.MoveNext())
						return Comparison.LT;
					if (!enmB.MoveNext())
						return Comparison.GT;

					var comp  = enmA.Current.Name.CompareTo(enmB.Current.Name);
					if (comp != Comparison.EQ)
						return comp;
				}
			}
			comp = compareTypes(
				a.method.GetGenericArguments(),
				b.method.GetGenericArguments()
			);
			if (comp != Comparison.EQ)
				return comp;

			comp = compareTypes(
				a.method.GetParameters().Select(p => p.ParameterType),
				b.method.GetParameters().Select(p => p.ParameterType)
			);
			if (comp != Comparison.EQ)
				return comp;

			return Comparison.EQ;
		});
	}
}

public static class NetworkBehaviorExtensions // awful
{
	// static
	public static void RPC_X<T>(this T @this, RPCTarget target, Action<T> action) where T : NetworkBehavior =>
		@this.networkView.RPC(target, action, @this);
	public static void RPC_X<T, T1>(this T @this, RPCTarget target, Action<T, T1> action, T1 arg1) where T : NetworkBehavior =>
		@this.networkView.RPC(target, action, @this, arg1);
	public static void RPC_X<T, T1, T2>(this T @this, RPCTarget target, Action<T, T1, T2> action, T1 arg1, T2 arg2) where T : NetworkBehavior =>
		@this.networkView.RPC(target, action, @this, arg1, arg2);
	public static void RPC_X<T, T1, T2, T3>(this T @this, RPCTarget target, Action<T, T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3) where T : NetworkBehavior =>
		@this.networkView.RPC(target, action, @this, arg1, arg2, arg3);
	public static void RPC_X<T, T1, T2, T3, T4>(this T @this, RPCTarget target, NetworkView.Action<T, T1, T2, T3, T4> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4) where T : NetworkBehavior =>
		@this.networkView.RPC(target, action, @this, arg1, arg2, arg3, arg4);
	public static void RPC_X<T, T1, T2, T3, T4, T5>(this T @this, RPCTarget target, NetworkView.Action<T, T1, T2, T3, T4, T5> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) where T : NetworkBehavior =>
		@this.networkView.RPC(target, action, @this, arg1, arg2, arg3, arg4, arg5);

	public static void RPC_X<T, TResult>(this T @this, RPCTarget target, Func<T, TResult> action) where T : NetworkBehavior =>
		@this.networkView.RPC(target, action, @this);
	public static void RPC_X<T, T1, TResult>(this T @this, RPCTarget target, Func<T, T1, TResult> action, T1 arg1) where T : NetworkBehavior =>
		@this.networkView.RPC(target, action, @this, arg1);
	public static void RPC_X<T, T1, T2, TResult>(this T @this, RPCTarget target, Func<T, T1, T2, TResult> action, T1 arg1, T2 arg2) where T : NetworkBehavior =>
		@this.networkView.RPC(target, action, @this, arg1, arg2);
	public static void RPC_X<T, T1, T2, T3, TResult>(this T @this, RPCTarget target, Func<T, T1, T2, T3, TResult> action, T1 arg1, T2 arg2, T3 arg3) where T : NetworkBehavior =>
		@this.networkView.RPC(target, action, @this, arg1, arg2, arg3);
	public static void RPC_X<T, T1, T2, T3, T4, TResult>(this T @this, RPCTarget target, Func<T, T1, T2, T3, T4, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4) where T : NetworkBehavior =>
		@this.networkView.RPC(target, action, @this, arg1, arg2, arg3, arg4);
	public static void RPC_X<T, T1, T2, T3, T4, T5, TResult>(this T @this, RPCTarget target, Func<T, T1, T2, T3, T4, T5, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) where T : NetworkBehavior =>
		@this.networkView.RPC(target, action, @this, arg1, arg2, arg3, arg4, arg5);

	public static void RPC_X<T>(this T @this, NetworkPlayer receiver, Action<T> action) where T : NetworkBehavior =>
		@this.networkView.RPC(receiver, action, @this);
	public static void RPC_X<T, T1>(this T @this, NetworkPlayer receiver, Action<T, T1> action, T1 arg1) where T : NetworkBehavior =>
		@this.networkView.RPC(receiver, action, @this, arg1);
	public static void RPC_X<T, T1, T2>(this T @this, NetworkPlayer receiver, Action<T, T1, T2> action, T1 arg1, T2 arg2) where T : NetworkBehavior =>
		@this.networkView.RPC(receiver, action, @this, arg1, arg2);
	public static void RPC_X<T, T1, T2, T3>(this T @this, NetworkPlayer receiver, Action<T, T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3) where T : NetworkBehavior =>
		@this.networkView.RPC(receiver, action, @this, arg1, arg2, arg3);
	public static void RPC_X<T, T1, T2, T3, T4>(this T @this, NetworkPlayer receiver, NetworkView.Action<T, T1, T2, T3, T4> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4) where T : NetworkBehavior =>
		@this.networkView.RPC(receiver, action, @this, arg1, arg2, arg3, arg4);
	public static void RPC_X<T, T1, T2, T3, T4, T5>(this T @this, NetworkPlayer receiver, NetworkView.Action<T, T1, T2, T3, T4, T5> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) where T : NetworkBehavior =>
		@this.networkView.RPC(receiver, action, @this, arg1, arg2, arg3, arg4, arg5);

	public static void RPC_X<T, TResult>(this T @this, NetworkPlayer receiver, Func<T, TResult> action) where T : NetworkBehavior =>
		@this.networkView.RPC(receiver, action, @this);
	public static void RPC_X<T, T1, TResult>(this T @this, NetworkPlayer receiver, Func<T, T1, TResult> action, T1 arg1) where T : NetworkBehavior =>
		@this.networkView.RPC(receiver, action, @this, arg1);
	public static void RPC_X<T, T1, T2, TResult>(this T @this, NetworkPlayer receiver, Func<T, T1, T2, TResult> action, T1 arg1, T2 arg2) where T : NetworkBehavior =>
		@this.networkView.RPC(receiver, action, @this, arg1, arg2);
	public static void RPC_X<T, T1, T2, T3, TResult>(this T @this, NetworkPlayer receiver, Func<T, T1, T2, T3, TResult> action, T1 arg1, T2 arg2, T3 arg3) where T : NetworkBehavior =>
		@this.networkView.RPC(receiver, action, @this, arg1, arg2, arg3);
	public static void RPC_X<T, T1, T2, T3, T4, TResult>(this T @this, NetworkPlayer receiver, Func<T, T1, T2, T3, T4, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4) where T : NetworkBehavior =>
		@this.networkView.RPC(receiver, action, @this, arg1, arg2, arg3, arg4);
	public static void RPC_X<T, T1, T2, T3, T4, T5, TResult>(this T @this, NetworkPlayer receiver, Func<T, T1, T2, T3, T4, T5, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) where T : NetworkBehavior =>
		@this.networkView.RPC(receiver, action, @this, arg1, arg2, arg3, arg4, arg5);

//	// instance
//	public static void RPCX(this NetworkBehavior @this, RPCTarget target, Action action) =>
//		@this.networkView.RPC(target, action);
//	public static void RPCX<T1>(this NetworkBehavior @this, RPCTarget target, Action<T1> action, T1 arg1) =>
//		@this.networkView.RPC(target, action, arg1);
//	public static void RPCX<T1, T2>(this NetworkBehavior @this, RPCTarget target, Action<T1, T2> action, T1 arg1, T2 arg2) =>
//		@this.networkView.RPC(target, action, arg1, arg2);
//	public static void RPCX<T1, T2, T3>(this NetworkBehavior @this, RPCTarget target, Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3) =>
//		@this.networkView.RPC(target, action, arg1, arg2, arg3);
//	public static void RPCX<T1, T2, T3, T4>(this NetworkBehavior @this, RPCTarget target, Action<T1, T2, T3, T4> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4) =>
//		@this.networkView.RPC(target, action, arg1, arg2, arg3, arg4);
//	public static void RPCX<T1, T2, T3, T4, T5>(this NetworkBehavior @this, RPCTarget target, NetworkView.Action<T1, T2, T3, T4, T5> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) =>
//		@this.networkView.RPC(target, action, arg1, arg2, arg3, arg4, arg5);
//	public static void RPCX<T1, T2, T3, T4, T5, T6>(this NetworkBehavior @this, RPCTarget target, NetworkView.Action<T1, T2, T3, T4, T5, T6> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) =>
//		@this.networkView.RPC(target, action, arg1, arg2, arg3, arg4, arg5, arg6);
//
//	public static void RPCX<TResult>(this NetworkBehavior @this, RPCTarget target, Func<TResult> action) =>
//		@this.networkView.RPC(target, action);
//	public static void RPCX<T1, TResult>(this NetworkBehavior @this, RPCTarget target, Func<T1, TResult> action, T1 arg1) =>
//		@this.networkView.RPC(target, action, arg1);
//	public static void RPCX<T1, T2, TResult>(this NetworkBehavior @this, RPCTarget target, Func<T1, T2, TResult> action, T1 arg1, T2 arg2) =>
//		@this.networkView.RPC(target, action, arg1, arg2);
//	public static void RPCX<T1, T2, T3, TResult>(this NetworkBehavior @this, RPCTarget target, Func<T1, T2, T3, TResult> action, T1 arg1, T2 arg2, T3 arg3) =>
//		@this.networkView.RPC(target, action, arg1, arg2, arg3);
//	public static void RPCX<T1, T2, T3, T4, TResult>(this NetworkBehavior @this, RPCTarget target, Func<T1, T2, T3, T4, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4) =>
//		@this.networkView.RPC(target, action, arg1, arg2, arg3, arg4);
//	public static void RPCX<T1, T2, T3, T4, T5, TResult>(this NetworkBehavior @this, RPCTarget target, Func<T1, T2, T3, T4, T5, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) =>
//		@this.networkView.RPC(target, action, arg1, arg2, arg3, arg4, arg5);
//	public static void RPCX<T1, T2, T3, T4, T5, T6, TResult>(this NetworkBehavior @this, RPCTarget target, Func<T1, T2, T3, T4, T5, T6, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) =>
//		@this.networkView.RPC(target, action, arg1, arg2, arg3, arg4, arg5, arg6);
//
//	public static void RPCX(this NetworkBehavior @this, NetworkPlayer receiver, Action action) =>
//		@this.networkView.RPC(receiver, action);
//	public static void RPCX<T1>(this NetworkBehavior @this, NetworkPlayer receiver, Action<T1> action, T1 arg1) =>
//		@this.networkView.RPC(receiver, action, arg1);
//	public static void RPCX<T1, T2>(this NetworkBehavior @this, NetworkPlayer receiver, Action<T1, T2> action, T1 arg1, T2 arg2) =>
//		@this.networkView.RPC(receiver, action, arg1, arg2);
//	public static void RPCX<T1, T2, T3>(this NetworkBehavior @this, NetworkPlayer receiver, Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3) =>
//		@this.networkView.RPC(receiver, action, arg1, arg2, arg3);
//	public static void RPCX<T1, T2, T3, T4>(this NetworkBehavior @this, NetworkPlayer receiver, Action<T1, T2, T3, T4> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4) =>
//		@this.networkView.RPC(receiver, action, arg1, arg2, arg3, arg4);
//	public static void RPCX<T1, T2, T3, T4, T5>(this NetworkBehavior @this, NetworkPlayer receiver, NetworkView.Action<T1, T2, T3, T4, T5> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) =>
//		@this.networkView.RPC(receiver, action, arg1, arg2, arg3, arg4, arg5);
//	public static void RPCX<T1, T2, T3, T4, T5, T6>(this NetworkBehavior @this, NetworkPlayer receiver, NetworkView.Action<T1, T2, T3, T4, T5, T6> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) =>
//		@this.networkView.RPC(receiver, action, arg1, arg2, arg3, arg4, arg5, arg6);
//
//	public static void RPCX<TResult>(this NetworkBehavior @this, NetworkPlayer receiver, Func<TResult> action) =>
//		@this.networkView.RPC(receiver, action);
//	public static void RPCX<T1, TResult>(this NetworkBehavior @this, NetworkPlayer receiver, Func<T1, TResult> action, T1 arg1) =>
//		@this.networkView.RPC(receiver, action, arg1);
//	public static void RPCX<T1, T2, TResult>(this NetworkBehavior @this, NetworkPlayer receiver, Func<T1, T2, TResult> action, T1 arg1, T2 arg2) =>
//		@this.networkView.RPC(receiver, action, arg1, arg2);
//	public static void RPCX<T1, T2, T3, TResult>(this NetworkBehavior @this, NetworkPlayer receiver, Func<T1, T2, T3, TResult> action, T1 arg1, T2 arg2, T3 arg3) =>
//		@this.networkView.RPC(receiver, action, arg1, arg2, arg3);
//	public static void RPCX<T1, T2, T3, T4, TResult>(this NetworkBehavior @this, NetworkPlayer receiver, Func<T1, T2, T3, T4, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4) =>
//		@this.networkView.RPC(receiver, action, arg1, arg2, arg3, arg4);
//	public static void RPCX<T1, T2, T3, T4, T5, TResult>(this NetworkBehavior @this, NetworkPlayer receiver, Func<T1, T2, T3, T4, T5, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) =>
//		@this.networkView.RPC(receiver, action, arg1, arg2, arg3, arg4, arg5);
//	public static void RPCX<T1, T2, T3, T4, T5, T6, TResult>(this NetworkBehavior @this, NetworkPlayer receiver, Func<T1, T2, T3, T4, T5, T6, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) =>
//		@this.networkView.RPC(receiver, action, arg1, arg2, arg3, arg4, arg5, arg6);
}

