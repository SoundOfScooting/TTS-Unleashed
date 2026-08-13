using System.Reflection;
using Unleashed.Util;
using MethodRPCSort = NewNet.NetworkView.MethodRPCSort;

namespace Unleashed.Compat;

// #todo: add version control for both vanilla and modded MethodRPCSort ids
	// could also use implement string ids?

[HarmonyPatch]
[AttributeUsage(AttributeTargets.Method)]
sealed class RemoteX : BaseNetworkAttribute
{
	public SendType SendType = SendType.ReliableBuffered;
	public RemoteX(
		Permission permission = Permission.Client,
		SendType sendType = SendType.ReliableBuffered,
		string validationFunction = null,
		SerializationMethod serializationMethod = SerializationMethod.Default,
		bool useGlobalValidationFunction = true
	){
		Permission = permission;
		SendType = sendType;
		ValidationFunction = validationFunction;
		SerializationMethod = serializationMethod;
		UseGlobalValidationFunction = useGlobalValidationFunction;
	}
	public static explicit operator Remote(RemoteX @this)
		=> new(@this.Permission, @this.SendType, @this.ValidationFunction, @this.SerializationMethod, @this.UseGlobalValidationFunction);

	[HarmonyILManipulator]
	[HarmonyPatch(typeof(NetworkView), nameof(NetworkView.FindAttributeAssemblies))]
	static void NetworkViewFindAttributeAssembliesIL(ILContext il)
	{
		var c = new ILCursor(il);
		c.GotoNext(MoveType.Before,
			// RPCMethods.Sort((MethodRPCSort x, MethodRPCSort y) => string.Compare(x.uniqueName, y.uniqueName, StringComparison.Ordinal));
			x => x.MatchCallvirt(AccessTools.Method(typeof(List<MethodRPCSort>), nameof(List<>.Sort), [ typeof(Comparison<MethodRPCSort>) ]))
		);
		c.MoveAfterLabels();
		c.Remove();
		c.MoveAfterLabels();
		c.EmitDelegate(
			void(List<MethodRPCSort> RPCMethods, Comparison<MethodRPCSort> comp) =>
			{
				// WARNING: DO NOT REMOVE //
				RPCMethods.Sort(comp);
				// ////////////////////// //

				List<MethodRPCSort> CustomRPCMethods = [];
				FindAttributesX(CustomRPCMethods);
				SortAttributesX(CustomRPCMethods);
				RPCMethods.AddRange(CustomRPCMethods);

				// DumpAttributes(RPCMethods);
			}
		);
	}
	static void DumpAttributes(List<MethodRPCSort> RPCMethods)
	{
		foreach (var (i, entry) in RPCMethods.Index())
			Main.Log.LogWarning($"{i}: {entry.ClassType} / {entry.Method}");
	}

	static List<MethodRPCSort> FindAttributesX(List<MethodRPCSort> CustomRPCMethods)
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
			// // instance
			// if (type.IsSubclassOf(typeof(NetworkBehavior)))
			// {
			// 	methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			// 	foreach (var method in methods)
			// 	if      (IsDefined(method, typeof(RemoteX)))
			// 	{
			// 		var attr = method.GetCustomAttributes<RemoteX>(true).First();
			// 		CustomRPCMethods.Add(new(type, method, (Remote) attr));
			// 	}
			// }
		}
		return CustomRPCMethods;
	}

	static readonly List<BepInEx.BaseUnityPlugin> PluginsOrdered =
		Traverse.Create(typeof(BepInEx.Bootstrap.Chainloader)).Field("_plugins").GetValue<List<BepInEx.BaseUnityPlugin>>();
	static void SortAttributesX(List<MethodRPCSort> CustomRPCMethods)
	{
		CustomRPCMethods.Sort((lhs, rhs)
			=> Comparison.Compare(
				lhs.Method.DeclaringType.Assembly,
				rhs.Method.DeclaringType.Assembly,
				// -1 (non-plugin asm) < 0 (first plugin)
				a => PluginsOrdered.FindIndex(b => a == b?.GetType()?.Assembly)
			)
			&& Comparison.CompareOrdinal(lhs.UniqueName, rhs.UniqueName)
			&& Comparison.Compare(
				lhs.Method.GetGenericArguments(),
				rhs.Method.GetGenericArguments(),
				x => x.Name,
				Comparison.CompareOrdinal
			)
			&& Comparison.Compare(
				lhs.Method.GetParameters(),
				rhs.Method.GetParameters(),
				x => x.ParameterType.Name,
				Comparison.CompareOrdinal
			)
		);
	}
}

