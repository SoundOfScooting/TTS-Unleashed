using BepInEx.Configuration;
using Unleashed.Settings;

namespace Unleashed.Extensions;

static class ConfigFileX
{
	public static string FormatSection(Section section) =>
		$"{(int) section}: {section}";
	static string TrimSectionFormat(string section)
	{
		var i = section.IndexOf(':', StringComparison.Ordinal);
		if ((i >= 0) && int.TryParse(section[..i], out _))
			section = section[(i + 1)..].TrimStart();
		return section;
	}

	static bool TryGetPair<T>(Dictionary<ConfigDefinition, T> dict, ConfigDefinition def, out KeyValuePair<ConfigDefinition, T> pair)
	{
		foreach (var pair2 in dict)
		if      (def.Key == pair2.Key.Key && TrimSectionFormat(def.Section) == TrimSectionFormat(pair2.Key.Section))
		{
			pair = pair2;
			return true;
		}
		pair = default;
		return false;
	}
	extension(ConfigFile @this)
	{
		public ConfigEntry<T> BindX<T>(string section, string key,  T defaultValue, string description = "", AcceptableValueBase acceptableValues = null, params object[] tags) =>
			@this.BindX(new(section, key), defaultValue, new ConfigDescription(description, acceptableValues, tags));

		// public ConfigEntry<T> BindX<T>(ConfigDefinition definition, T defaultValue, string description = "", AcceptableValueBase acceptableValues = null, params object[] tags) =>
		// 	@this.BindX(definition,        defaultValue, new ConfigDescription(description, acceptableValues, tags));

		// public ConfigEntry<T> BindX<T>(string section, string key,  T defaultValue, string description = "", params object[] tags) =>
		// 	@this.BindX(new(section, key), defaultValue, new ConfigDescription(description, null, tags));

		// public ConfigEntry<T> BindX<T>(ConfigDefinition definition, T defaultValue, string description = "", params object[] tags) =>
		// 	@this.BindX(definition,        defaultValue, new ConfigDescription(description, null, tags));

		// public ConfigEntry<T> BindX<T>(string section, string key,  T defaultValue, ConfigDescription description) =>
		// 	@this.BindX(new(section, key), defaultValue, description);

		Dictionary<ConfigDefinition, ConfigEntryBase> Entries =>
			new Traverse(@this).Property<Dictionary<ConfigDefinition, ConfigEntryBase>>("Entries").Value;
		Dictionary<ConfigDefinition, string> OrphanedEntries =>
			new Traverse(@this).Property<Dictionary<ConfigDefinition, string>>("OrphanedEntries").Value;

		public bool MigrateEntryX<T>(ConfigDefinition oldDefinition, out T value)
		{
			var OrphanedEntries = @this.OrphanedEntries;
			if (!TryGetPair(OrphanedEntries, oldDefinition, out var pair))
			{
				value = default;
				return false;
			}
			try
			{
				value = TomlTypeConverter.ConvertToValue<T>(pair.Value);
			}
			catch (Exception e)
			{
				new Traverse(typeof(BepInEx.Logging.Logger))
					.Method("Log", [
						BepInEx.Logging.LogLevel.Warning,
						$"""Config value of setting "{pair.Key}" could not be parsed and will be ignored. Reason: {e.Message}; Value: {pair.Value}"""
					])
					.GetValue();
				value = default;
				return false;
			}
			OrphanedEntries.Remove(pair.Key);
			return true;
		}
		// #todo: reverse patch?
		public ConfigEntry<T> BindX<T>(ConfigDefinition definition, T defaultValue, ConfigDescription description)
		{
			if (!TomlTypeConverter.CanConvert(typeof(T)))
				throw new ArgumentException(
					$"Type {typeof(T)} is not supported by the config system. Supported types: " +
					TomlTypeConverter.GetSupportedTypes().Join(x => x.Name)
				);
			lock (new Traverse(@this).Field("_ioLock").GetValue())
			{
				var Entries         = @this.Entries;
				var OrphanedEntries = @this.OrphanedEntries;

				if (TryGetPair(Entries, definition, out var pair))
					return (ConfigEntry<T>) pair.Value;

				var entry = Activator.CreateInstance<ConfigEntry<T>>(AccessTools.all, null, [@this, definition, defaultValue, description], null);
				Entries[definition] = entry;
				if (TryGetPair(OrphanedEntries, definition, out var pair2))
				{
					entry.SetSerializedValue(pair2.Value);
					OrphanedEntries.Remove  (pair2.Key);
				}

				if (@this.SaveOnConfigSet)
					@this.Save();
				return entry;
			}
		}
	}
}

