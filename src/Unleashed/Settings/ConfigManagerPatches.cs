using BepInEx.Bootstrap;
using BepInEx.Configuration;

namespace Unleashed.Settings;

static class ConfigManagerPatches
{
	public static void Apply()
	{
		if (!Chainloader.PluginInfos.TryGetValue(Main.CONFIG_MANAGER_GUID, out var info))
			return;

		var Instance_T     = Traverse.Create(info.Instance);
		var Keybind_P      = Instance_T.Field("_keybind").Property<KeyboardShortcut>("Value");
		DisplayingWindow_P = Instance_T.Property<bool>("DisplayingWindow");

		if ((Keybind_P.Value.MainKey == KeyCode.F1) && !Keybind_P.Value.Modifiers.Any())
			Keybind_P.Value = new(KeyCode.F1, KeyCode.Escape);

		Main.Harmony.Patch(
			info.Instance.GetType().Method("Update"),
			postfix:
				new(Reflect.Method(UpdatePostfix))
		);
	}

	static Traverse<bool> DisplayingWindow_P;
	static void UpdatePostfix()
	{
		// #want: unfortunately Unity textbox eats input so you have to click outside first
		if (DisplayingWindow_P.Value && Input.GetKeyDown(KeyCode.Escape))
			DisplayingWindow_P.Value = false;
	}
}

