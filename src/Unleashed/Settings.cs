using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using BepInEx.Bootstrap;
using BepInEx.Configuration;

namespace Unleashed;

static class Settings
{
	public enum Section
	{
		General = 1,
		Debug   = 2,
	}
	public static string FormatSection(Section section) =>
		$"{(int) section}: {section}";
	public static string TrimSectionFormat(string section)
	{
		var i = section.IndexOf(":");
		if ((i >= 0) && int.TryParse(section[..i], out _))
			section = section[(i + 1)..].TrimStart();
		return section;
	}

	public static int OrderByLine([CallerLineNumber] int line = default) =>
		-line;
	public static string IdentityObjectConverter(object @object) => $"{@object}";
	public static object IdentityStringConverter(string @string) => $"{@string}";

	interface IConfigBind
	{
		public void Bind(ConfigFile config);
	}
	public class Setting<T> : IConfigBind
	{
		public required Section Section { get; init; }
		public required string Key { get; init; }
		public (string Section, string Key)[] MigrateFrom { get; init; } = [];

		public string Description { get; init; }
		public required T DefaultValue { get; init; }
		public AcceptableValueBase AcceptableValues { get; init; }

		internal ConfigurationManagerAttributes Attributes { get; init; }
		public object[] Tags
		{
			get  => [ Attributes, .. field ??= [] ];
			init => field = value;
		}

		public EventHandler SettingChanged { get; init; }
		public Action<T>    SettingLoaded  { get; init; }

		public ConfigEntry<T> Entry { get; private set; }
		public T Value
		{
			get => Entry is null ? DefaultValue : Entry.Value;
			set => Entry.Value = value;
		}

		public void Bind(ConfigFile config)
		{
			Entry = config.BindX(FormatSection(Section), Key, DefaultValue, Description, AcceptableValues, Tags);
			foreach (var from in MigrateFrom)
			if      (config.MigrateEntryX(new(from.Section, from.Key), out T value))
				Entry.Value = value;

			Entry.SettingChanged += SettingChanged;
			SettingLoaded?.Invoke(Value);
		}
	}
	public sealed class DebugSetting<T> : Setting<T>
	{
		const string DebugDescription = "!! DEBUG SETTING - USE AT YOUR OWN RISK !!";

		[SetsRequiredMembers]
		public DebugSetting([CallerLineNumber] int line = default)
		{
			Section     = Section.Debug;
			Description = DebugDescription;
			Attributes  = new()
			{
				Order      = OrderByLine(line),
				IsAdvanced = true,
			};
		}
	}

	public static readonly DebugSetting<bool> DebugDeveloperMode = new()
	{
		Key          = "TTS Developer Mode",
		DefaultValue = false,
	};
	public static readonly DebugSetting<bool> DebugChangeNameButton = new()
	{
		Key          = "Change Name Button",
		DefaultValue = false,
	};
#if TRUE_ULTIMATE_POWER
	public static readonly DebugSetting<bool> DebugAllRewards = new()
	{
		Key            = "All Kickstarter Rewards",
		DefaultValue   = false,
		SettingLoaded  = _ => Patches.Debug.UpdateRewards(),
		SettingChanged = (sender, args) => Patches.Debug.UpdateRewards(),
	};
	// public static DebugSetting<bool> DebugAllDLC = new()
	// {
	// 	Key          = "All DLC",
	// 	DefaultValue = false,
	// };
#endif

	public static readonly Setting<string> EntryNickname = new()
	{
		Section     = Section.General,
		Key         = "Nickname",
		Description =
			"""
			A nickname used instead of your Steam display name.
			Nickname changes only take effect when joining a new server.
			""",
		DefaultValue = "",
		Attributes   = new()
		{
			Order    = OrderByLine(),
			ObjToStr = IdentityObjectConverter,
			StrToObj = IdentityStringConverter,
		},
	};

	public static readonly Setting<string> EntryMenuCursorColour = new()
	{
		Section     = Section.General,
		Key         = "Menu Cursor Color",
		MigrateFrom = [
			("General", "Menu Player Color")
		],
		Description =
			"""
			The color of the cursor on the main menu.
			""",
		DefaultValue     = Main.PluginColour.Label,
		AcceptableValues = new AcceptableValueList<string>(Colour.AllPlayerLabels),
		Attributes       = new() { Order = OrderByLine() },
		SettingChanged   = (sender, args) => {
			if (Network.peerType == NetworkPeerMode.Disconnected)
				Utilities.SetCursor(
					NetworkUI.Instance.StringColorToCursorTexture(EntryMenuCursorColour.Value),
					NetworkUI.HardwareCursorOffest
				);
		},
	};
#if TRUE_ULTIMATE_POWER
	public static readonly Setting<string> EntryMenuErrorColour = new()
	{
		Section     = Section.General,
		Key         = "Menu Error Color",
		Description =
			"""
			The color of the cursor on the main menu if the mod failed to load.
			""",
		DefaultValue     = Main.ErrorColour.Label,
		AcceptableValues = new AcceptableValueList<string>(Colour.AllPlayerLabels),
		Attributes       = new()
		{
			Order = OrderByLine(),
			IsAdvanced = true,
		},
	};
#endif
	public static readonly Setting<Patches.MenuHoliday.Value> EntryMenuHoliday = new()
	{
		Section     = Section.General,
		Key         = "Menu Holiday",
		Description =
			"""
			The holiday logo that appears on the main menu.
			""",
		DefaultValue   = Patches.MenuHoliday.Value.Default,
		Attributes     = new() { Order = OrderByLine() },
		SettingChanged = (sender, args) =>
		{
			if (Network.peerType == NetworkPeerMode.Disconnected)
				NetworkUI.Instance.GUIDisconnected.transform
					.Find("TTS Logo/Holidays")
					.GetComponent<UIHoliday>()
					.Start();
		},
	};

	public static readonly Setting<string> EntryInitPlayerColour = new()
	{
		Section     = Section.General,
		Key         = "Initial Player Color",
		Description =
			"""
			The initial player color after server creation, or Choose/Dialog to open the color selection UI/dialog window.
			""",
		DefaultValue     = Colour.White.Label,
		AcceptableValues = new AcceptableValueList<string>([.. Colour.AllPlayerLabels, "Choose", "Dialog"]),
		Attributes       = new() { Order = OrderByLine() },
	};
	public static readonly Setting<Patches.ServerSetup.BackgroundID> EntryInitBackground = new()
	{
		Section     = Section.General,
		Key         = "Initial Background",
		Description =
			"""
			The initial background after server creation, or Random.
			""",
		DefaultValue = Patches.ServerSetup.BackgroundID.Random,
		Attributes   = new() { Order = OrderByLine() },
	};
	public static readonly Setting<Patches.ServerSetup.TableID> EntryInitTable = new()
	{
		Section     = Section.General,
		Key         = "Initial Table",
		Description =
			"""
			The initial table after server creation, or Random.
			""",
		DefaultValue = Patches.ServerSetup.TableID.Random,
		Attributes   = new() { Order = OrderByLine() },
	};

	// public static readonly Setting<UZCameraHome.Home>  EntryInitCameraHome = new()
	// {
	// 	Section     = Section.General,
	// 	Key         = "Initial Camera Home",
	// 	Description =
	// 		"""
	// 		The initial camera home.
	// 		""",
	// 	DefaultValue = UZCameraHome.Home.Hand,
	// 	Attributes   = new() { Order = Order(), },
	// };

	public static readonly Setting<bool> EntryInvertHorizontal3PControls = new()
	{
		Section     = Section.General,
		Key         = "Invert Horizontal 3P Controls",
		Description =
			"""
			Swaps controls 'Camera Left' with 'Camera Right' in third-person and top-down view.
			This makes them align with first-person view and mouse panning.
			""",
		DefaultValue = false,
		Attributes   = new() { Order = OrderByLine() },
	};
	public static readonly Setting<bool> EntryInvertVertical3PControls = new()
	{
		Section     = Section.General,
		Key         = "Invert Vertical 3P Controls",
		Description =
			"""
			Swaps controls 'Camera Down' with 'Camera Up' in third-person and top-down view.
			This makes them align with first-person view and mouse panning.
			""",
		DefaultValue = false,
		Attributes   = new() { Order = OrderByLine() },
	};
	public static readonly Setting<bool> EntryBlockMousePanningOverUI = new()
	{
		Section     = Section.General,
		Key         = "Block Mouse Panning Over UI",
		Description =
			"""
			Blocks 'Camera Hold Rotate' control while hovering over UI.
			This eases right clicking UI elements, but prevents mouse panning over large panels.
			""",
		DefaultValue = true,
		Attributes   = new() { Order = OrderByLine() },
	};

	public static readonly Setting<bool> EntryEnableVectorPixel = new()
	{
		Section     = Section.General,
		Key         = "Enable Pixel Draw",
		Description =
			"""
			Fully implements the unfinished pixel draw tool, an apparent vector-based rework of the removed pixel paint tool.
			It is located under the draw toolbar between the circle and erase tools.
			Each pixel drawn is one vector line.
			NOTE: If disabled, the tool is not added to GUI but is still accessible with the console command `tool_vector_pixel`.
			""",
		DefaultValue   = true,
		Attributes     = new() { Order = OrderByLine() },
		SettingChanged = (sender, args) =>
			ToolVectorX.UpdateUI(),
	};
	public static readonly Setting<bool> EntryEnableFastFlick = new()
	{
		Section     = Section.General,
		Key         = "Enable Fast Flick",
		Description =
			"""
			Flicking an object when not the host no longer requires two clicks (previously the first click would only highlight the object).
			BUG: This allows you to try (and fail) to flick objects that you are prevented from selecting by Lua scripts.
			""",
		DefaultValue = true,
		Attributes   = new() { Order = OrderByLine() },
	};
	public static readonly Setting<bool> EntryEnableFastCommands = new()
	{
		Section     = Section.General,
		Key         = "Enable Fast Commands",
		Description =
			"""
			When shift is not held down, the 'Help' control instead starts typing a command in chat.
			Best used when 'Help' is bound to /.
			""",
		DefaultValue = true,
		Attributes   = new() { Order = OrderByLine() },
	};
	public static readonly Setting<bool> EntryInterceptLuaVirus = new()
	{
		Section     = Section.General,
		Key         = "Intercept Lua Virus",
		Description =
			"""
			Host-only: Intercepts the \"tcejbo gninwapS\" Lua virus before it can spread to any other objects.
			NOTE: This does not actually disinfect objects; consider additionally subscribing to CleanerBlock on the Workshop:
			https://steamcommunity.com/sharedfiles/filedetails/?id=2967684892
			""",
		DefaultValue = true,
		Attributes   = new() { Order = OrderByLine() },
	};

	public static readonly Setting<string> EntryAutoJoinMessage = new()
	{
		Section     = Section.General,
		Key         = "Auto Join Message",
		Description =
			"""
			Message automatically sent in chat when a player joins.
			Leave empty for no message.
			""",
		DefaultValue = "",
		Attributes   = new()
		{
			Order    = OrderByLine(),
			ObjToStr = IdentityObjectConverter,
			StrToObj = IdentityStringConverter,
		},
	};
	public static string[] AutoPromoteIDs { get; private set; }
	static string CacheAutoPromoteIDs;
	static readonly Setting<string> EntryAutoPromoteIDs = new()
	{
		Section     = Section.General,
		Key         = "Auto Promote Steam IDs",
		Description =
			"""
			A space- and/or comma-separated list of Steam IDs that are automatically promoted when joining your server.
			Invalid IDs do nothing, so you can write comments.
			""",
		DefaultValue = "",
		Attributes   = new()
		{
			CustomDrawer = DrawAutoPromoteIDs,
			HideDefaultButton = true,
			Description =
				"""
				A list of Steam IDs that are automatically promoted when joining your server.
				Invalid IDs do nothing, so you can write comments.
				""",
			Order = OrderByLine(),
		},
		SettingLoaded = value =>
		{
			AutoPromoteIDs = value.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries);
			EntryAutoPromoteIDs.Value = AutoPromoteIDs.Join();
		},
	};
	static void DrawAutoPromoteIDs(ConfigEntryBase entry)
	{
		var display = CacheAutoPromoteIDs ??= AutoPromoteIDs.Join(delimiter: "\n");
		var changed = GUILayout.TextArea(display, GUILayout.ExpandWidth(true));
//		var changed = GUILayout.TextArea(display, GUILayout.Width(125));
		if (changed != display)
		{
			AutoPromoteIDs            = changed.Split([',', ' ', '\n'], StringSplitOptions.RemoveEmptyEntries);
			EntryAutoPromoteIDs.Value = AutoPromoteIDs.Join();
			CacheAutoPromoteIDs       = null;
		}
		GUILayout.Space(5);
//		if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
//		{
//			AutoPromoteIDs            = [];
//			EntryAutoPromoteIDs.Value = "";
//			CacheAutoPromoteIDs       = null;
//		}
		GUI.enabled = false;
		GUILayout.Button("Reset", GUILayout.ExpandWidth(false));
		GUI.enabled = true;
//		GUILayout.Space(5+50);
	}

	static Traverse<bool> DisplayingWindow_P;
	static void ConfigManagerUpdatePostfix()
	{
		// unfortunately textbox eats input so you have to click outside first
		if (DisplayingWindow_P.Value && Input.GetKeyDown(KeyCode.Escape))
			DisplayingWindow_P.Value = false;
	}

	static bool loaded = false;
	public static void Load()
	{
		if (loaded)
			Main.Config.Reload();
		else if (Chainloader.PluginInfos.TryGetValue(Main.CONFIG_MANAGER_GUID, out var info))
		{
			var Instance_T     = Traverse.Create(info.Instance);
			var Keybind_P      = Instance_T.Field("_keybind").Property<KeyboardShortcut>("Value");
			DisplayingWindow_P = Instance_T.Property<bool>("DisplayingWindow");

			if ((Keybind_P.Value.MainKey == KeyCode.F1) && !Keybind_P.Value.Modifiers.Any())
				Keybind_P.Value = new(KeyCode.F1, KeyCode.Escape);

			Main.Harmony.Patch(
				AccessTools.Method(info.Instance.GetType(), "Update"),
				postfix:
					new(AccessTools.Method(typeof(Settings), nameof(ConfigManagerUpdatePostfix)))
			);
		}
		loaded = true;
		Main.Config.SaveOnConfigSet = false;

		foreach (var field in typeof(Settings).GetFields(AccessTools.all))
		if      (field.GetValue(null) is IConfigBind setting)
			setting.Bind(Main.Config);

		Main.Config.SaveOnConfigSet = true;
		Main.Config.Save();
	}
}
static class ConfigFileX
{
	static bool TryGetPair<T>(Dictionary<ConfigDefinition, T> dict, ConfigDefinition def, out KeyValuePair<ConfigDefinition, T> pair)
	{
		foreach (var pair2 in dict)
		if      (def.Key == pair2.Key.Key && Settings.TrimSectionFormat(def.Section) == Settings.TrimSectionFormat(pair2.Key.Section))
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

