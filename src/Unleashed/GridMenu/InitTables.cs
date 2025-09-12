using static UIGridMenu;

namespace Unleashed.GridMenu;

[HarmonyPatch]
static class InitTables
{
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UIGridMenuObjects), nameof(UIGridMenuObjects.InitTables))]
	static void InitTablesPrefix(UIGridMenuObjects __instance)
	{
		foreach (var tableName in new[] { "Custom Rectangle", "Custom Square" })
		{
			var custom = __instance.TablesButtons.First(x => x.Name == tableName);
			(custom.OptionsPopupActions ??= [])[$"Edit {Main.PluginColour.RGBHex}+{Main.PLUGIN_ABBR}[-]"] = () => {
				var table = ManagerPhysicsObject.Instance.TableScript;
				var image = table.GetComponent<CustomImage>();
				/* if (image && Utilities.RemoveCloneFromName(table.name) == TableScript.GetTablePrefabName(tableName))
					// doesn't allow "" (equivalent to cancel)
					image.bCustomUI = true;
				else  */try
				{
					UICustomImage.Instance.QueueFake(@this =>
					{
						@this.CustomImageURL = @this.CustomImageURL.Trim();
						// "" allowed (equivalent to cancel)
						// if (string.IsNullOrEmpty(@this.CustomImageURL))
						// {
						// 	Chat.LogError("You must supply a custom image URL.");
						// 	return;
						// }
						if (Network.isAdmin)
							Lua.Execute(
								$"""
								Tables.setTable    ({ tableName });
								Tables.setCustomURL({ @this.CustomImageURL })
								"""
							);
						@this.Close();
					});
					// #todo: is this correct?
					Language.UpdateUILabel(UICustomImage.Instance.HeaderLabel, (
						tableName == "Custom Square"
							? "Custom Table Square"
							: "Custom Table"
					).ToUpper());
					UICustomImage.Instance.CustomImageURL = image.CustomImageURL;
				}
				catch (Exception e)
				{
					Chat.LogError(e.ToString());
					Main.Log.LogError(e);
				}
			};
		}
		__instance.TablesButtons.Add(new()
		{
			Name  = $"Round Plastic {Main.PluginColour.RGBHex}+{Main.PLUGIN_ABBR}[-]",
			Tags  = [ Main.PLUGIN_GUID, ],
			Table = TableScript.GetTablePrefabName("Round Plastic"),
		});
	}
	[HarmonyILManipulator]
	[HarmonyPatch(typeof(UIGridMenuObjects), nameof(UIGridMenuObjects.InitTables))]
	static void InitTablesIL(ILContext il)
	{
		var c = new ILCursor(il);
		c.GotoNext(MoveType.Before,
			// tablesButton.SpriteColor = ((tablesButton.Name == "None") ? Color.red : Color.white);
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(Color), nameof(Color.white))),
			x => x.MatchBr(out _)
		);
		c.MoveAfterLabels();
		c.Emit(OpCodes.Dup);
		c.Index++;
		c.EmitDelegate(Color(GridButtonTable tablesButton, Color color) =>
			tablesButton.Tags.Contains(Main.PLUGIN_GUID)
				? tablesButton.SpriteColor
				: color
		);
	}
}

