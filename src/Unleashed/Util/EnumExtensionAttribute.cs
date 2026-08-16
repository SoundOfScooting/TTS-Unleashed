using System.Diagnostics;
using System.Reflection;

namespace Unleashed.Util;

[HarmonyPatch]
file static class EnumExtensionPatches
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(Enum), nameof(Enum.GetNames))]
	static void GetNamesPostfix(Type enumType, ref string[] __result)
		=> __result = (string[])
			typeof(EnumExtensionAttribute<>)
			.MakeGenericType(enumType)
			.Method(nameof(EnumExtensionAttribute<>.AppendNames))
			.Invoke(null, [__result]);

	[HarmonyPostfix]
	[HarmonyPatch(typeof(Enum), nameof(Enum.GetValues))]
	static void GetValuesPostfix(Type enumType, ref Array __result)
		=> __result = (Array)
			typeof(EnumExtensionAttribute<>)
			.MakeGenericType(enumType)
			.Method(nameof(EnumExtensionAttribute<>.AppendValues))
			.Invoke(null, [__result]);
}
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
sealed class EnumExtensionAttribute<T> : Attribute where T : struct, Enum
{
	static List<(string Key, T Value)> Cache => field ??= FindAttributes();

	public static string[] AppendNames(string[] @base)
		=> Cache is [] ? @base : [.. @base, .. Cache.Select(x => x.Key)];
	public static T[] AppendValues(T[] @base)
		=> Cache is [] ? @base : [.. @base, .. Cache.Select(x => x.Value)];

	static List<(string Key, T Value)> FindAttributes()
	{
		List<(string Key, T Value)> items = [];
		foreach (var type in typeof(Main).Assembly.GetTypes())
		if      (type.GetCustomAttribute<EnumExtensionAttribute<T>>() is not null)
		{
			if (!type.IsSealed || !type.IsAbstract)
				throw new UnreachableException();
			foreach (var method in type.GetMethods())
			if      (method.IsPublic && method.IsStatic)
			if      (method.Name.StartsWith("get_", StringComparison.Ordinal))
			if      (method.GetParameters() is [] && method.ReturnType == typeof(T))
				items.Add((
					method.Name["get_".Length..],
					(T) method.Invoke(null, null)
				));
		}
		items.Sort((a,b) => a.Value.CompareTo(b.Value));
		return items;
	}
}

