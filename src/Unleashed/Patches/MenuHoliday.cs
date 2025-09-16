using Unleashed.Settings;

namespace Unleashed.Patches;

[HarmonyPatch]
static class MenuHoliday
{
	[Setting]
	static readonly Setting<Value> Enabled = new()
	{
		Section     = Section.Menu,
		Key         = "Menu Holiday",
		Default     = Value.Default,
		Description =
			"""
			The holiday logo that appears on the main menu.
			""",
		OnChanged = value =>
		{
			if (Network.peerType == NetworkPeerMode.Disconnected)
				NetworkUI.Instance.GUIDisconnected.transform
					.Find("TTS Logo/Holidays")
					.GetComponent<UIHoliday>()
					.Start();
		},
	};
	public enum Value
	{
		Default,
		Thanksgiving,
		Christmas,
		Halloween,
		Random,
		All,
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(UIHoliday), nameof(UIHoliday.Start))]
	static bool StartPrefix(UIHoliday __instance)
	{
		if (Enabled.Value == Value.Default)
			return true;

		foreach (var holiday in __instance.Holidays)
			holiday.Target.SetActive(false);

		if (Enabled.Value == Value.Random)
			__instance.Holidays[UnityEngine.Random.Range(0, __instance.Holidays.Count)]
				.Target.SetActive(true);
		else foreach (var holiday in __instance.Holidays)
			holiday.Target.SetActive(
				Enabled.Value == Value.All ||
				Enabled.Value.ToString() == holiday.Target.name
			);
		return false;
	}
}

