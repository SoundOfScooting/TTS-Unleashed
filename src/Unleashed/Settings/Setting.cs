using System.Diagnostics.CodeAnalysis;
using BepInEx.Configuration;

namespace Unleashed.Settings;

enum Section
{
	Menu = 1,
	Setup,
	Controls,
	Misc,
	Debug,
}

abstract class Setting
{
	public static string IdentityObjectConverter(object @object) => $"{@object}";
	public static object IdentityStringConverter(string @string) => $"{@string}";

	public required Section Section { get; init; }
	public required string Key { get; init; }
	public (string Section, string Key)[] MigrateFrom { get; init; } = [];
	public string Description { get; init; }

	public abstract void Bind(ConfigFile config, int index);
}
class Setting<T> : Setting
{
	public required T Default { get; init; }
	public AcceptableValueBase Acceptable { get; init; }

	internal ConfigurationManagerAttributes Attributes { get; set; }
	public object[] Tags
	{
		get => [ Attributes, .. field ??= [] ];
		init;
	}

	public Action<T> OnLoaded  { get; init; }
	public Action<T> OnChanged { get; init; }

	ConfigEntry<T> Entry { get; set; }
	public T Value
	{
		get => Entry is null ? Default : Entry.Value;
		set => Entry.Value = value;
	}

	public sealed override void Bind(ConfigFile config, int index)
	{
		Attributes ??= new();
		Attributes.Order = -index;

		Entry = config.BindX(ConfigFileX.FormatSection(Section), Key, Default, Description, Acceptable, Tags);
		foreach (var from in MigrateFrom)
		if      (config.MigrateEntryX(new(from.Section, from.Key), out T value))
			Entry.Value = value;

		Entry.SettingChanged += (sender, args) => OnChanged(Value);
		OnLoaded?.Invoke(Value);
	}
}
sealed class DebugSetting<T> : Setting<T>
{
	const string DebugDescription = "!! DEBUG SETTING - USE AT YOUR OWN RISK !!";

	[SetsRequiredMembers]
	public DebugSetting()
	{
		Section     = Section.Debug;
		Description = DebugDescription;
		Attributes  = new()
		{
			IsAdvanced = true,
		};
	}
}

