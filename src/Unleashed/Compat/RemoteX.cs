using System.Reflection;
using System.Runtime.CompilerServices;
using MethodRPCSort = NewNet.NetworkView.MethodRPCSort;

namespace Unleashed.Compat;

// #todo: add version control for both vanilla and modded MethodRPCSort ids
	// could also use implement string ids?

readonly record struct RPCMethods(List<MethodRPCSort> Base)
{
	static string FuncID(MethodInfo method) =>
		$"{Main.PLUGIN_GUID}/{method.FullDescription()}";
	public string AddFunc(MethodInfo method, Func<NetworkPlayer, bool> func)
	{
		var funcId = FuncID(method);
		BaseNetworkAttribute.validationFunctions.Add(funcId, func);
		return funcId;
	}

	[OverloadResolutionPriority(-1)]
	public void Set(MethodInfo method, Func<NetworkPlayer, bool> func) =>
		Set(method, new(validationFunction: AddFunc(method, func)));
	public void Set(MethodInfo method, RemoteX rpc)
	{
		var i = Base.FindIndex(x => x.method == method);
		var s = Base[i];
		s.rpc = (Remote) rpc;
		Base[i] = s;
	}
}

[HarmonyPatch]
[AttributeUsage(AttributeTargets.Method)]
sealed class RemoteX : BaseNetworkAttribute
{
	public static event Action<RPCMethods> RegisterOverrides;

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
	public static explicit operator Remote(RemoteX @this) =>
		new(@this.permission, @this.sendType, @this.validationFunction, @this.serializationMethod);

	[HarmonyILManipulator]
	[HarmonyPatch(typeof(NetworkView), nameof(NetworkView.FindAttributeAssemblies))]
	static void NetworkViewFindAttributeAssembliesIL(ILContext il)
	{
		var c = new ILCursor(il);
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
			RegisterOverrides?.Invoke(new(RPCMethods));

			List<MethodRPCSort> CustomRPCMethods = [];
			FindAttributesX(CustomRPCMethods);
			SortAttributesX(CustomRPCMethods);
			RPCMethods.AddRange(CustomRPCMethods);

			// DumpAttributes(RPCMethods);
		});
	}
	static void DumpAttributes(List<MethodRPCSort> RPCMethods)
	{
		foreach (var (i, entry) in RPCMethods.Index())
			Main.Log.LogWarning($"{i}: {entry.classType} / {entry.method}");
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
		CustomRPCMethods.Sort((a, b) =>
			CompareAssemblies(
				a.method.DeclaringType.Assembly,
				b.method.DeclaringType.Assembly
			)
			&& Comparison.Compare(a.uniqueName, b.uniqueName)
			&& CompareParameters(
				a.method.GetGenericArguments(),
				b.method.GetGenericArguments()
			)
			&& CompareParameters(
				a.method.GetParameters().Select(p => p.ParameterType),
				b.method.GetParameters().Select(p => p.ParameterType)
			)
		);
		static Comparison CompareAssemblies(Assembly a, Assembly b) =>
			Comparison.Compare(
				// -1 (non-plugin asm) < 0 (first plugin)
				PluginsOrdered.FindIndex(p => p?.GetType()?.Assembly == a),
				PluginsOrdered.FindIndex(p => p?.GetType()?.Assembly == b)
			);
		static Comparison CompareParameters(IEnumerable<Type> a, IEnumerable<Type> b)
		{
			using var enmA = a.GetEnumerator();
			using var enmB = b.GetEnumerator();
			while (true)
			{
				if (!enmA.MoveNext()) return Comparison.LT;
				if (!enmB.MoveNext()) return Comparison.GT;

				var cmp = Comparison.Compare(enmA.Current.Name, enmB.Current.Name);
				if (!cmp)
					return cmp;
			}
		}
	}
}

