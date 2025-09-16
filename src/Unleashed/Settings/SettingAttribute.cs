using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Unleashed.Settings;

[Conditional("TRUE_ULTIMATE_POWER")]
sealed class PowerSetting([CallerFilePath] string srcFile = default, [CallerLineNumber] int srcLine = default)
	: SettingAttribute(srcFile, srcLine);

[AttributeUsage(AttributeTargets.Field)]
class SettingAttribute([CallerFilePath] string srcFile = default, [CallerLineNumber] int srcLine = default) : Attribute, IComparable<SettingAttribute>
{
	public string SrcFile { get; } = srcFile;
	public int    SrcLine { get; } = srcLine;

	int IComparable<SettingAttribute>.CompareTo(SettingAttribute other) => CompareTo(other);
	public Comparison CompareTo(SettingAttribute other) =>
		other is null ? Comparison.GT :
		Comparison.Compare(SrcFile, other.SrcFile) &&
		Comparison.Compare(SrcLine, other.SrcLine);

	static (SettingAttribute, Setting)[] AllSettings;

	private static bool loaded;
	public static void Load()
	{
		if (!loaded)
			ConfigManagerPatches.Apply();
		else
			Main.Config.Reload();

		loaded = true;
		Main.Config.SaveOnConfigSet = false;

		AllSettings ??= [..
			from  type  in typeof(Main).Assembly.GetTypes()
			from  field in type.GetFields(AccessTools.all)
			where field.IsStatic
			let   attr = field.GetCustomAttribute<SettingAttribute>()
			where attr is {}
			let   setting = field.GetValue(null) as Setting
			orderby setting.Section, attr
			select (attr, setting)
		];
		foreach (var (index, (attr, setting)) in AllSettings.Index())
		{
			// Main.Log.LogWarning($"{attr.SrcFile}:{attr.SrcLine} {setting.Section}/{setting.Key}");
			setting.Bind(Main.Config, index);
		}

		Main.Config.SaveOnConfigSet = true;
		Main.Config.Save(); // #todo: save iff changed
	}
}

