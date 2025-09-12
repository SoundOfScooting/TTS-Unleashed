namespace Unleashed.Patches;

[HarmonyPatch]
static class MenuHoliday
{
	public enum Value
	{
		Default,
		Thanksgiving,
		Christmas,
		Halloween,
		Random,
		All,
	}
	static Settings.Setting<Value> Enabled => Settings.EntryMenuHoliday;

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

