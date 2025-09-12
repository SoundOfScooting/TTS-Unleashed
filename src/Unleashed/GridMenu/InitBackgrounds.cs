namespace Unleashed.GridMenu;

[HarmonyPatch]
static class InitBackgrounds
{
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UIGridMenuObjects), nameof(UIGridMenuObjects.InitBackgrounds))]
	static void InitBackgroundsPrefix(UIGridMenuObjects __instance)
	{
		var custom = __instance.BackgroundsButtons.First(x => x.Name == "Custom");
		(custom.OptionsPopupActions ??= [])[$"Edit {Main.PluginColour.RGBHex}+{Main.PLUGIN_ABBR}[-]"] = () => {
			/* if (CustomSky.ActiveCustomSky)
				// doesn't allow "" (deletes)
				CustomSky.ActiveCustomSky.bCustomUI = true;
			else  */try
			{
				if (CustomSky.ActiveCustomSky)
					UICustomSky.Instance.CustomImageURL = CustomSky.ActiveCustomSky.CustomSkyURL;
				else
					UICustomSky.Instance.CustomImageURL = "";
				UICustomSky.Instance.QueueFake(@this =>
				{
					@this.CustomImageURL = @this.CustomImageURL.Trim();
					if (!CustomSky.ActiveCustomSky && string.IsNullOrEmpty(@this.CustomImageURL))
					{
						Chat.LogError("You must supply a custom image URL.");
						return;
					}
					if (Network.isAdmin)
						Lua.Execute(
							$"""
							Backgrounds.setCustomURL({ @this.CustomImageURL })
							"""
						);
					@this.Close();
				});
			}
			catch (Exception e)
			{
				Chat.LogError(e.ToString());
				Main.Log.LogError(e);
			}
		};
	}
}

