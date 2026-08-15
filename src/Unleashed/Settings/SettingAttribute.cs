using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Unleashed.Settings;

[Conditional(nameof(API.TRUE_ULTIMATE_POWER))]
sealed class PowerSetting([CallerFilePath] string sourceFile = default, [CallerLineNumber] int sourceLine = default)
	: SettingAttribute(sourceFile, sourceLine);

[AttributeUsage(AttributeTargets.Field)]
class SettingAttribute([CallerFilePath] string sourceFile = default, [CallerLineNumber] int sourceLine = default) : Attribute, IComparable<SettingAttribute>
{
	public string SourceFile { get; } = sourceFile;
	public int    SourceLine { get; } = sourceLine;
	public int CompareTo(SettingAttribute other)
		=> Comparison.Default(other)
		?? Comparison.CompareOrdinal(SourceFile, other.SourceFile)
		&& Comparison.Compare       (SourceLine, other.SourceLine);

	static (SettingAttribute, Setting)[] AllSettings;

	static bool loaded;
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

