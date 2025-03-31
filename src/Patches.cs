using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using NewNet;
using UnityEngine;

namespace Unleashed;

[HarmonyPatch]
public static class Patches
{
#if TRUE_ULTIMATE_POWER
	[HarmonyILManipulator]
	[HarmonyPatch(typeof(SteamManager), nameof(SteamManager.Init))]
	private static void SteamManagerInitIL(ILContext il)
	{
		ILCursor c = new(il);
		c.GotoNext(MoveType.Before,
			// bKickstarterPointer = SteamApps.BIsSubscribedApp(KickstarterPointer);
			x => x.MatchStsfld(AccessTools.Field(typeof(SteamManager), nameof(SteamManager.bKickstarterPointer)))
		);
		c.EmitDelegate(bool(bool subscribed) =>
			Settings.DebugAllKickstarterRewards.Value || subscribed
		);
		c.GotoNext(MoveType.Before,
			// bKickstarterGold = SteamApps.BIsSubscribedApp(KickstarterGold);
			x => x.MatchStsfld(AccessTools.Field(typeof(SteamManager), nameof(SteamManager.bKickstarterGold)))
		);
		c.EmitDelegate(bool(bool subscribed) =>
			Settings.DebugAllKickstarterRewards.Value || subscribed
		);
	}
	// [HarmonyPrefix]
	// [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.IsSubscribedApp))]
	// private static bool SteamManagerIsSubscribedAppPrefix(ref bool __result)
	// {
	// 	if (Settings.DebugAllDLC.Value)
	// 	{
	// 		__result = true;
	// 		return false;
	// 	}
	// 	return true;
	// }
#endif

	[HarmonyPrefix]
//	[HarmonyPatch(typeof(Developer), nameof(Developer.HasName))]
	[HarmonyPatch(typeof(Developer), nameof(Developer.HasSteamID))]
	private static bool DeveloperHasPrefix(ref bool __result)
	{
		if (Settings.DebugDeveloperMode.Value)
		{
			__result = true;
			return false;
		}
		return true;
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(Network),   nameof(Network  .InitializeServer))]
	[HarmonyPatch(typeof(NetworkUI), nameof(NetworkUI.ConnectedToServer))]
	private static void NetworkUIConnectedToServerPrefix()
	{
		if (Settings.EntryNickname.Value is not (null or []))
			NetworkUI.Instance.SetPlayerName(Settings.EntryNickname.Value);
	}
	[HarmonyILManipulator]
	[HarmonyPatch(typeof(NetworkUI), nameof(NetworkUI.ConnectedToServer))]
	private static void NetworkUIConnectedToServerIL(ILContext il)
	{
		ILCursor c = new(il);
		c.GotoNext(MoveType.After,
			// base.networkView.RPC(RPCTarget.Server, Register, playerName, VersionNumber, SystemInfo.deviceUniqueIdentifier, VRHMD.isVR);
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(NetworkUI), nameof(NetworkUI.VersionNumber)))
		);
		c.EmitDelegate(string(string VersionNumber) =>
			VersionNumber + $"\n{Main.PLUGIN_GUID} V{Main.PLUGIN_VERSION}"
		);
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(NetworkUI), nameof(NetworkUI.Register))]
	private static void NetworkUIRegisterPrefix(ref NetworkPlayer __state) =>
		__state = Network.sender;
	[HarmonyPostfix]
	[HarmonyPatch(typeof(NetworkUI), nameof(NetworkUI.Register))]
	private static void NetworkUIRegisterPostfix(string name, string versionnum, NetworkPlayer __state)
	{
		var sender = __state;
		int i = versionnum.IndexOf($"\n{Main.PLUGIN_GUID} V");
		if (i < 0)
			return;

		i += $"\n{Main.PLUGIN_GUID} V".Length;
		int j = versionnum.IndexOf("\n", i);
		if (j < 0)
			j = versionnum.Length;

		var hostVersion   = Main.Instance.Info.Metadata.Version;
		var clientVersion = new Version(versionnum[i..j]);
		if (clientVersion.Major != hostVersion.Major || clientVersion.Minor != hostVersion.Minor)
		{
			Chat.SendChat($"{Colour.YellowHex}{name} is running incompatible {Main.PluginColour.RGBHex}{Main.PLUGIN_NAME}[-] version V{clientVersion}.");
			return;
		}
		// Chat.SendChat($"{Colour.GreenHex}{name} running compatible {Main.PluginColour.RGBHex}{Main.PLUGIN_NAME}[-] version V{clientVersion}.");
		Wait.Frames(() =>
		{
			try
			{
				PlayerManager.Instance.PlayerStateFromID(sender.id).X().IsModded = true;
				AchievementManager.Instance.RPC_X(sender, Compat.RPCSetIsModded, NetworkPlayer.SERVER_ID);
			}
			catch (Exception e)
			{
				Chat.Log(e.ToString(), Colour.Red);
			}
		});
	}

	[HarmonyILManipulator]
	[HarmonyPatch(typeof(NetworkUI), nameof(NetworkUI.Init))]
	private static void NetworkUIInitIL(ILContext il)
	{
		ILCursor c = new(il);
		c.GotoNext(MoveType.Before,
			// Utilities.SetCursor(WhiteCursorTexture, HardwareCursorOffest);
			x => x.MatchLdfld(AccessTools.Field(typeof(NetworkUI), nameof(NetworkUI.WhiteCursorTexture)))
		);
		c.MoveAfterLabels();
		c.Remove();
		c.EmitDelegate(Texture2D(NetworkUI __instance) =>
			__instance.StringColorToCursorTexture(Settings.EntryMenuPlayerColour.Value)
		);
	}
	[HarmonyILManipulator]
	[HarmonyPatch(typeof(NetworkUI), nameof(NetworkUI.ServerInitialized))]
	private static void NetworkUIServerInitializedIL(ILContext il)
	{
		ILCursor c = new(il);
		c.GotoNext(MoveType.After,
			// int num = UnityEngine.Random.Range(1, 9);
			x => x.MatchLdcI4(1),
			x => x.MatchLdcI4(9),
			x => x.MatchCall(AccessTools.Method(typeof(UnityEngine.Random), nameof(UnityEngine.Random.Range), [ typeof(int), typeof(int) ]))
		);
		// c.Emit(OpCodes.Ldloca, 4);
		c.EmitDelegate(int(int num/*, ref GameObject gameObject*/) =>
		{
			var num2 =  Settings.EntryInitBackground.Value;
			if (num2 == Settings.ValueInitBackground.Random)
				return num;
			return (int) num2;
		});

		c.GotoNext(MoveType.After,
			// switch (UnityEngine.Random.Range(1, 6))
			x => x.MatchLdcI4(1),
			x => x.MatchLdcI4(6),
			x => x.MatchCall(AccessTools.Method(typeof(UnityEngine.Random), nameof(UnityEngine.Random.Range), [ typeof(int), typeof(int) ]))
		);
		c.Emit(OpCodes.Ldloca, 0);
		c.EmitDelegate(int(int num, ref GameObject gameObject2) =>
		{
			var num2 = Settings.EntryInitTable.Value;
			switch (num2)
			{
				case Settings.ValueInitTable.Random:
					return num;
				case Settings.ValueInitTable.RandomPlus:
					num2 = (Settings.ValueInitTable) UnityEngine.Random.Range(1, 7+1);
					break;
				case Settings.ValueInitTable.RandomPlusPlus:
					num2 = (Settings.ValueInitTable) UnityEngine.Random.Range(1, 8+1);
					break;
			}
			switch (num2)
			{
				case Settings.ValueInitTable.Poker:
					gameObject2 = GameMode.Instance.PokerTable;
					break;
				case Settings.ValueInitTable.Glass:
					gameObject2 = GameMode.Instance.GlassTable;
					break;
				case Settings.ValueInitTable.Plastic:
					gameObject2 = GameMode.Instance.GetPrefab("Table_Plastic");
					break;
			}
			return (int) num2;
		});

		c.GotoNext(MoveType.Before,
			// ClientRequestColor("White");
			x => x.MatchLdstr("White"),
			x => x.MatchCall(AccessTools.Method(typeof(NetworkUI), nameof(NetworkUI.ClientRequestColor)))
		);
		c.MoveAfterLabels();
		c.RemoveRange(2);
		c.EmitDelegate(void(NetworkUI __instance) =>
		{
			switch (Settings.EntryInitPlayerColour.Value)
			{
				case "Choose":
					__instance.GUIChangeColor();
					break;
				case "Dialog":
					__instance.bNeedToPickColour = false;
					ChangeColorDialog();
					break;
				default:
					__instance.ClientRequestColor(Settings.EntryInitPlayerColour.Value);
					break;
			}
		});
	}
	[HarmonyILManipulator]
	[HarmonyPatch(typeof(NetworkUI), nameof(NetworkUI.Update))]
	private static void NetworkUIUpdateIL(ILContext il)
	{
		ILCursor c = new(il);
		c.GotoNext(MoveType.After,
			x => x.MatchLdstr("Help"),
			x => x.MatchLdcI4((int) ControlType.Keyboard),
			x => x.MatchCall(AccessTools.Method(typeof(zInput), nameof(zInput.GetButtonDown)))
		);
		c.EmitDelegate(bool(bool helpDown) =>
		{
			if (!helpDown || !Settings.EntryEnableFastCommands.Value || zInput.GetButton("Shift"))
				return helpDown;
			if (!UICamera.SelectIsInput())
			{
				UIChatInput.Instance.ChatButtonOnClick();
				UIChatInput.Instance.mInput.value = "/";
			}
			return false;
		});
	}

	private const string TagHost = $"{Main.PLUGIN_GUID}/Host";
	private const string TagGold = $"{Main.PLUGIN_GUID}/Gold";

	[HarmonyPrefix]
	[HarmonyPatch(typeof(UIGridMenu.GridButton), nameof(UIGridMenu.GridButton.IsSearched))]
	private static bool UIGridMenuGridButtonIsSearchedPrefix(UIGridMenu.GridButton __instance, ref bool __result)
	{
		if (__instance.Tags.Contains(TagHost) && !PlayerStateX.Host.IsModded)
			return __result = false;
		if (__instance.Tags.Contains(TagGold) && !SteamManager.bKickstarterGold)
			return __result = false;
		return true;
	}
	public class GridButtonComponentEvent : UIGridMenu.GridButtonComponent
	{
		public delegate void OnSpawnEvent(GridButtonComponentEvent @this, Vector3 spawnPos, Action<Vector3> spawn);
		public static OnSpawnEvent ShowInput(Func<string, string> ParseSpawnName, string Placeholder = "") =>
			(@this, SpawnPos, Spawn) => UIDialog.ShowInput(
				description: $"Spawn {@this.Name}",
				inputName:   Placeholder,

				leftButtonText: "OK",
				leftButtonFunc: input =>
				{
					var origName = @this.SpawnName;
					{
						@this.SpawnName = ParseSpawnName?.Invoke(input);
						if (@this.SpawnName is null)
							Chat.LogError("Failed to parse input!");
						else
							Spawn(SpawnPos);
					}
					@this.SpawnName = origName;
				},
				rightButtonText: "Cancel",
				rightButtonFunc: null
			);

		public required OnSpawnEvent OnSpawn;
		public override void InteractiveSpawn(Vector3 spawnPos) =>
			OnSpawn(this, spawnPos, base.InteractiveSpawn);
		public override void Spawn(Vector3 spawnPos) =>
			OnSpawn(this, spawnPos, base.Spawn);
	}
	private static readonly List<string> RandomNames =
	[
		"RANDOM", "RAND",
	];
	private static readonly Dictionary<string, int> Card_CardID = new()
	{
		{ "KC", 0  }, { "QC", 1  }, { "JC", 2  }, { "AC", 3  }, { "10C", 4  }, { "9C", 10 }, { "8C", 11 }, { "7C", 12 }, { "6C", 13 }, { "5C", 14 }, { "4C", 20 }, { "3C", 21 }, { "2C", 22 },
		{ "KD", 5  }, { "QD", 6  }, { "JD", 7  }, { "AD", 8  }, { "10D", 9  }, { "9D", 15 }, { "8D", 16 }, { "7D", 17 }, { "6D", 18 }, { "5D", 19 }, { "4D", 25 }, { "3D", 23 }, { "2D", 24 },
		{ "KS", 35 }, { "QS", 36 }, { "JS", 37 }, { "AS", 38 }, { "10S", 39 }, { "9S", 45 }, { "8S", 46 }, { "7S", 47 }, { "6S", 48 }, { "5S", 26 }, { "4S", 27 }, { "3S", 28 }, { "2S", 29 },
		{ "KH", 30 }, { "QH", 31 }, { "JH", 32 }, { "AH", 33 }, { "10H", 34 }, { "9H", 40 }, { "8H", 41 }, { "7H", 42 }, { "6H", 43 }, { "5H", 44 }, { "4H", 49 }, { "3H", 50 }, { "2H", 51 },
		{ "JK", 52 }, { "JOKER", 52 },
	};
	private static readonly Dictionary<string, int> Domino_MeshIndex = new()
	{
		{ "0/0", 0  },
		{ "1/0", 24 }, { "1/1", 17 },
		{ "2/0", 3  }, { "2/1", 20 }, { "2/2", 15 },
		{ "3/0", 26 }, { "3/1", 7  }, { "3/2", 1  }, { "3/3", 12 },
		{ "4/0", 5  }, { "4/1", 4  }, { "4/2", 9  }, { "4/3", 16 }, { "4/4", 23 },
		{ "5/0", 22 }, { "5/1", 2  }, { "5/2", 10 }, { "5/3", 21 }, { "5/4", 18 }, { "5/5", 25 },
		{ "6/0", 11 }, { "6/1", 27 }, { "6/2", 6  }, { "6/3", 19 }, { "6/4", 14 }, { "6/5", 13 }, { "6/6", 8 },
	};
	private static readonly Dictionary<string, int> Domino_MatIndex = new()
	{
		{ "PLASTIC", 0 }, { "METAL", 1 }, { "GOLD", 2 },
	};
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UIGridMenuObjects), nameof(UIGridMenuObjects.InitComponents))]
	private static void UIGridMenuDecalsInitComponentsPrefix(UIGridMenuObjects __instance)
	{
		var cardsFolder = __instance.ComponentsButtons.First(x => x.Name == "Cards");
		cardsFolder.ComponentButtons.AddRange([
			new GridButtonComponentEvent()
			{
				Name = $"Specific Card {Main.PluginColour.RGBHex}+{Main.PLUGIN_ABBR}[-]",
				Tags = [ Main.PLUGIN_GUID, TagHost, ],
				SpawnName = $"{Main.PLUGIN_GUID}/Card" +
					$"/SetupCard/{Card_CardID["AS"]}",

				OnSpawn = GridButtonComponentEvent.ShowInput(
					Placeholder: "[A/K/Q/J/10/#][C/D/S/H]",
					ParseSpawnName: input => {
						input = input.Trim().ToUpper();

						int front_id;
						if (RandomNames.Contains(input))
							front_id = UnityEngine.Random.Range(0, 51);
						else if (
							!int.TryParse(input, out front_id) &&
							!Card_CardID.TryGetValue(input, out front_id)
						)
							return null;

						return $"{Main.PLUGIN_GUID}/Card" +
							$"/SetupCard/{front_id}";
					}
				),
			},
		]);
		var chessFolder = __instance.ComponentsButtons.First(x => x.Name == "Chess");
		chessFolder.FolderButtons.Add(new()
		{
			Name = $"Gold {Main.PluginColour.RGBHex}+{Main.PLUGIN_ABBR}[-]",
			Tags = [ Main.PLUGIN_GUID, TagHost, TagGold, ],
			BackgroundColor = Color.clear,
			SpriteName      = "Icon-Folder2",
			SpriteColor     = Color.black,
			ComponentButtons = [..
				from type in new[] { "Pawn", "Rook", "Knight", "Bishop", "Queen", "King" }
				select new UIGridMenu.GridButtonComponent()
				{
					Name = $"{type} Gold",
					Tags = [ Main.PLUGIN_GUID, TagHost, TagGold, ],
					SpawnName = $"{Main.PLUGIN_GUID}/Chess_{type}" +
						$"/SetObject/{false}/{-1}/{4}"
				}
			],
		});
		var diceFolder = __instance.ComponentsButtons.First(x => x.Name == "Dice");
		diceFolder.FolderButtons.Add(new()
		{
			Name = $"Gold {Main.PluginColour.RGBHex}+{Main.PLUGIN_ABBR}[-]",
			Tags = [ Main.PLUGIN_GUID, TagHost, TagGold, ],
			BackgroundColor = Color.clear,
			SpriteName      = "Icon-Folder2",
			SpriteColor     = Color.black,
			ComponentButtons = [..
				from type in new[] { "4", "6", "8", "10", "12", "20" }
				select new UIGridMenu.GridButtonComponent()
				{
					Name = $"D{type} Gold",
					Tags = [ Main.PLUGIN_GUID, TagHost, TagGold, ],
					SpawnName = $"{Main.PLUGIN_GUID}/Die_{type}" +
						$"/SetObject/{true}/{-1}/{2}"
				}
			],
		});
		var miscFolder = __instance.ComponentsButtons.First(x => x.Name == "Miscellaneous");
		miscFolder.ComponentButtons.AddRange([
			new GridButtonComponentEvent()
			{
				Name = $"Specific Domino {Main.PluginColour.RGBHex}+{Main.PLUGIN_ABBR}[-]",
				Tags = [ Main.PLUGIN_GUID, TagHost, ],
				SpawnName = $"{Main.PLUGIN_GUID}/Domino" +
					$"/SetObject/{false}/{Domino_MeshIndex["6/6"]}/{0}",

				OnSpawn = GridButtonComponentEvent.ShowInput(
					Placeholder: "[high]/[low] (material)",
					ParseSpawnName: input => {
						var parts = input.ToUpper().Split([' '], StringSplitOptions.RemoveEmptyEntries);
						if (parts is [])
							return null;

						var meshInt = 0;
						if (parts is [ var mesh, .. ] &&
							!int.TryParse(mesh, out meshInt) &&
							!Domino_MeshIndex.TryGetValue(mesh, out meshInt) &&
							!Domino_MeshIndex.TryGetValue(new([.. mesh.Reverse()]), out meshInt) // bad
						){
							if (RandomNames.Contains(mesh))
								meshInt = UnityEngine.Random.Range(0, 27);
							else
								return null;
						}
						var matInt = 0;
						if (parts is [ _, var mat, .. ] &&
							!int.TryParse(mat, out matInt) &&
							!Domino_MatIndex.TryGetValue(mat, out matInt)
						){
							if (RandomNames.Contains(mat))
								matInt = UnityEngine.Random.Range(0, 2);
							else
								return null;
						}
						return $"{Main.PLUGIN_GUID}/Domino" +
							$"/SetObject/{matInt != 0}/{meshInt}/{matInt}";
					}
				),
			},
		]);
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(GameMode), nameof(GameMode.SpawnName))]
	private static bool GameModeSpawnNamePrefix(GameMode __instance, string ObjectName, Vector3 SpawnPos, bool bLocalSpawn, ref GameObject __result)
	{
		// @gold
		if (!bLocalSpawn && Network.isClient)
			return true;

		var parts = ObjectName.Split(['/'], StringSplitOptions.RemoveEmptyEntries);
		if (parts is not [ Main.PLUGIN_GUID, var prefabName, .. ])
			return true;

		var prefab = __instance.GetPrefab(prefabName);
		if (__instance.GetPrefab(prefabName) is null)
			return true;

		__result = !bLocalSpawn
			? Network           .Instantiate(prefab, SpawnPos, prefab.transform.rotation)
			: UnityEngine.Object.Instantiate(prefab, SpawnPos, prefab.transform.rotation);

		using var part = parts.Skip(2).GetEnumerator();
		while  (part.MoveNext())
		switch (part.Current)
		{
			case "SetupCard":
				if (!part.MoveNext() || !int.TryParse(part.Current, out var front_id))
					return false;
				if (front_id != -1)
				{
					CardManagerScript.Instance.SetupCard(__result, front_id);
					__result.GetComponent<CardScript>().card_id_ = front_id;
				}
				break;
			case "SetObject":
				if (!part.MoveNext() || !bool.TryParse(part.Current, out var bAltSounds) ||
					!part.MoveNext() || !int .TryParse(part.Current, out var MeshInt) ||
					!part.MoveNext() || !int .TryParse(part.Current, out var MatInt)
				)
					return false;
				var npo = __result.GetComponent<NetworkPhysicsObject>();
				// npo.SetObject(bAltSounds, MeshInt, MatInt); // without isClient
				npo.UseAltSounds = bAltSounds;
				if (npo.meshSyncScript && MeshInt != -1)
					npo.meshSyncScript.SetMesh(MeshInt);
				if (npo.materialSyncScript && MatInt != -1)
					npo.materialSyncScript.SetMaterial(MatInt);
				break;
			default:
				return false;
		}
		return false;
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(UIGridMenuObjects), nameof(UIGridMenuObjects.InitTables))]
	private static void UIGridMenuObjectsInitTablesPrefix(UIGridMenuObjects __instance)
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
							Compat.ExecuteLuaScript(
								$"""
								Tables.setTable("{ tableName }");
								Tables.setCustomURL({ @this.CustomImageURL.LuaEncode() })
								"""
							);
						@this.Close();
					});
					// @todo: is this correct?
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
	private static void UIGridMenuObjectsInitTablesIL(ILContext il)
	{
		ILCursor c = new(il);
		c.GotoNext(MoveType.Before,
			// tablesButton.SpriteColor = ((tablesButton.Name == "None") ? Color.red : Color.white);
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(Color), nameof(Color.white))),
			x => x.MatchBr(out _)
		);
		c.MoveAfterLabels();
		c.Emit(OpCodes.Dup);
		c.Index++;
		c.EmitDelegate(Color(UIGridMenu.GridButtonTable tablesButton, Color color) =>
			tablesButton.Tags.Contains(Main.PLUGIN_GUID)
				? tablesButton.SpriteColor
				: color
		);
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(UIGridMenuObjects), nameof(UIGridMenuObjects.InitBackgrounds))]
	private static void UIGridMenuObjectsInitBackgroundsPrefix(UIGridMenuObjects __instance)
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
						Compat.ExecuteLuaScript(
							$"""
							Backgrounds.setCustomURL({ @this.CustomImageURL.LuaEncode() })
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

	[HarmonyPrefix]
	[HarmonyPatch(typeof(NetworkUI), nameof(NetworkUI.GUIPlayerSelection))]
	private static bool GUIPlayerSelectionPrefix(NetworkUI __instance, string Value, int playerID)
	{
		var playerState = PlayerManager.Instance.PlayerStateFromID(playerID);
		if     (PlayerManager.Instance.NameInUse(playerState.name))
		switch (Value)
		{
			case "Start Turns":
				Turns.Instance.turnsState = new(
					Turns.Instance.turnsState,
					Enable:    true,
					TurnColor: playerState.stringColor
				);
				return false;
			case "Stop Turns":
				Turns.Instance.turnsState = new(
					Turns.Instance.turnsState,
					Enable:    false,
					TurnColor: ""
				);
				return false;
			case "Reverse Turns":
				Turns.Instance.turnsState = new(
					Turns.Instance.turnsState,
					Reverse: !Turns.Instance.turnsState.Reverse
				);
				return false;

			case "Change Color":
				if (UICamera.currentTouchID != UICameraTouch.LEFT)
				{
					ChangeColorDialog(playerID);
					return false;
				}
				if (UZCameraHome.NeedToPickHome)
					UZCameraHome.NeedToPickHome = false;
				else if (__instance.bNeedToPickColour && playerID == Compat.PlayerID(UIColorSelection.id))
				{
					__instance.bNeedToPickColour = false;
					return false;
				}

				if (playerID != NetworkID.ID || zInput.GetButton("Ctrl") || zInput.GetButton("Shift"))
				{
					UIColorSelection.id = playerID;
					__instance.bNeedToPickColour = true;
					return false;
				}
				__instance.GUIChangeColor();
				return false;

			case "Blindfold":
			case "Unblindfold":
				PlayerManager.Instance.ChangeBlindfold(playerID, !playerState.blind);
				return false;

			case "Unmute":
			case "Mute":
				EventManager.TriggerPlayerMute(playerState.muted ^= true, playerID);
				return false;
			case "Server Unmute":
				PlayerManager.Instance.networkView.RPC(RPCTarget.All, PlayerManager.Instance.RPCMute, playerID, false);
				return false;
			case "Server Mute":
				PlayerManager.Instance.networkView.RPC(RPCTarget.All, PlayerManager.Instance.RPCMute, playerID, true);
				return false;
		}
		return true;
	}
	public static void ChangeColorDialog(int nplayerID = -1)
	{
		var playerID    = Compat.PlayerID(nplayerID);
		var playerState = PlayerManager.Instance.PlayerStateFromID(playerID);
		UIDialog.ShowDropDown(
			$"[b]{UZCameraHome.GUIColorText}[/b]" + (
				(playerID == NetworkID.ID) ? "" : $"\n{playerState.name}"
			),
			dropDownOptions: [.. Colour.AllPlayerLabels],
			drowDownValue:   playerState.stringColor,

			leftButtonText: "OK",
			leftButtonFunc: label =>
			{
				if (UZCameraHome.NeedToPickHome)
					UZCameraHome.NeedToPickHome = false;
				NetworkUI.Instance.CheckColor(label, playerID);
			},

			rightButtonText: "Cancel",
			rightButtonFunc: null
		);
	}
	// [HarmonyPostfix]
	// [HarmonyPatch(typeof(NetworkUI), nameof(NetworkUI.SetSpecificPlayerName))]
	// private static void NetworkUISetSpecificPlayerNamePostfix(NetworkUI __instance, string newName)
	// {
	// 	if (!Network.isServer || __instance.bHotseat || __instance.playerIDToSet == -1)
	// 		return;

	// 	var player = PlayerManager.Instance.PlayerStateFromID(__instance.playerIDToSet);
	// 	player.name = newName;
	// 	// update playerName which does nothing
	// 	__instance.networkView.RPC(player.networkPlayer, __instance.UpdateName, newName);

	// 	// would trigger Auto Join Message
	// 	var playerData = new PlayerManager.PlayerData(player);
	// 	PlayerManager.Instance.RemovePlayer(player.id);
	// 	PlayerManager.Instance.AddPlayer   (playerData);
	// 	// PlayerManager.Instance.networkView.RPC(RPCTarget.Others, PlayerManager.Instance.RPCRemovePlayer, player.id);
	// 	// PlayerManager.Instance.networkView.RPC(RPCTarget.Others, PlayerManager.Instance.RPCAddPlayer,    playerData);
	// }

	[HarmonyPrefix]
	[HarmonyPatch(typeof(UIColorSelection), nameof(UIColorSelection.Update))]
	private static bool UIColorSelectionUpdateReplace(UIColorSelection __instance)
	{
// /*
		static bool Permitted(string label) =>
			(label == "Grey") || Network.isAdmin ||
			((label != "Black") && PermissionsOptions.options.ChangeColor);
		static bool Available(string label) =>
			!PlayerManager.Instance.ColourInUse(label) || (
				Network.isAdmin &&
				(label != PlayerManager.Instance.ColourLabelFromID(Compat.PlayerID(UIColorSelection.id))) &&
				(zInput.GetButton("Ctrl") || zInput.GetButton("Shift"))
			);
/*/		static bool Permitted(string label) => false;
		static bool Available(string label) => true;
// */
		var permitted = UZCameraHome.NeedToPickHome || Permitted(__instance.label);
		var available = UZCameraHome.NeedToPickHome || Available(__instance.label);
		var visible   = available;
		var vector    = Vector3.zero;

		if     (visible)
		switch (__instance.label)
		{
			case "Grey":
			{
				if (VRHMD.isVR)
				{
					vector = new(Screen.width / 2, Screen.height / 2 - 60, 0f);
					break;
				}
				Vector3 position = new(0f, 3f, 0f);
				if (CameraController.Instance.bTopDown && !UZCameraHome.NeedToPickHome && Available("Black"))
					position.z -= 2f;

				vector = __instance.MainCamera.WorldToScreenPoint(position);
				break;
			}
			case "Black" when !UZCameraHome.NeedToPickHome:
			{
				if (VRHMD.isVR)
				{
					vector = new(Screen.width / 2, Screen.height / 2 + 60, 0f);
					break;
				}
				Vector3 position = new(0f, 10f, 0f);
				if (CameraController.Instance.bTopDown)
					position.z += 2f;

				vector = __instance.MainCamera.WorldToScreenPoint(position);
				break;
			}
			default:
			{
				var hand = HandZone.GetHand(__instance.label);
				if (!(visible = hand))
					break;
				if (VRHMD.isVR)
				{
					float f = (float) Math.PI / 5f * Colour.IDFromColour(__instance.colour);
					float x = 185f * Mathf.Cos(f)  + Screen.width  / 2;
					float y = 185f * Mathf.Sin(f)  + Screen.height / 2;
					vector  = new(x, y, 0f);
					break;
				}
				vector = __instance.MainCamera.WorldToScreenPoint(hand.transform.position);
				break;
			}
		}
		// every frame :)
		if (__instance.LockObject)
			__instance.LockObject.SetActive(visible && !permitted);
		__instance.ColorUIButton.enabled = visible;
		__instance.ColorUISprite.enabled = visible;
		__instance.GetComponent<BoxCollider2D>().enabled = visible && permitted;
		if (__instance.uiPulse)
			__instance.uiPulse.enabled = visible && permitted;

		if (vector.z >= 0f)
			vector.z  = 0f;
		if (vector != Vector3.zero)
			__instance.transform.position = __instance.CameraUI.ScreenToWorldPoint(vector);
		return false;
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UIColorSelection), nameof(UIColorSelection.OnClick))]
	private static bool UIColorSelectionOnClickPrefix(UIColorSelection __instance)
	{
		if (UZCameraHome.NeedToPickHome)
		{
			UZCameraHome.Current = EnumX.Parse<UZCameraHome.Home>(__instance.label);
			return false;
		}
		if (!Network.isAdmin ||
			!zInput.GetButton("Ctrl") && !zInput.GetButton("Shift") ||
			!PlayerManager.Instance.ColourInUse(__instance.label)
		)
			return true;

		var targetID = Compat.PlayerID(UIColorSelection.id);
		var seatedID = PlayerManager.Instance.IDFromColour(__instance.colour);
		if ((seatedID == -1) || (seatedID == targetID))
			return true;

		// var targetLabel = PlayerManager.Instance.PlayersDictionary[targetID].stringColor;
		// NetworkUI.Instance.CheckColor("Grey",           seatedID);
		// NetworkUI.Instance.CheckColor(__instance.label, targetID);
		// if (zInput.GetButton("Shift"))
		// 	Wait.Frames(
		// 		() => NetworkUI.Instance.CheckColor(targetLabel, seatedID),
		// 		2
		// 	);
		var target = PlayerManager.Instance.PlayersDictionary[targetID];
		var seated = PlayerManager.Instance.PlayersDictionary[seatedID];
		Compat.ExecuteLuaScript(
			$"""
			{Compat.LuaGetPlayerBySteamID}
			local target = {nameof(Compat.LuaGetPlayerBySteamID)}("{target.steamId}")
			local seated = {nameof(Compat.LuaGetPlayerBySteamID)}("{seated.steamId}")
			if target and seated then
				seated.changeColor('Grey')
				target.changeColor('{__instance.label}')
				{(
					zInput.GetButton("Shift") && target.stringColor != "Grey"
						? $"seated.changeColor('{target.stringColor}')"
						: "----"
				)}
			end
			"""
		);
		NetworkUI.Instance.bNeedToPickColour = false;
		return false;
	}

	[HarmonyILManipulator]
	[HarmonyPatch(typeof(CameraController), nameof(CameraController.Update))]
	private static void CameraControllerUpdateIL(ILContext il)
	{
		ILCursor c = new(il);
		c.GotoNext(MoveType.After,
			// x += zInput.GetAxis("Camera Horizontal") * 100f * -1f * Time.deltaTime * xLookMulti;
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(CameraController), nameof(CameraController.xLookMulti))),
			x => x.MatchMul(),
			x => x.MatchAdd(),
			x => x.MatchStfld(AccessTools.Field(typeof(CameraController), nameof(CameraController.x)))
		);
		c.Index -= 2;
		c.Remove();
		c.EmitDelegate(float(float x, float dx) =>
			Settings.EntryInvertHorizontal3PControls.Value
				? x - dx
				: x + dx
		);

		c.GotoNext(MoveType.After,
			// y -= zInput.GetAxis("Camera Vertical") * 100f * -1f * Time.deltaTime * yLookMulti;
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(CameraController), nameof(CameraController.yLookMulti))),
			x => x.MatchMul(),
			x => x.MatchSub(),
			x => x.MatchStfld(AccessTools.Field(typeof(CameraController), nameof(CameraController.y)))
		);
		c.Index -= 2;
		c.Remove();
		c.EmitDelegate(float(float y, float dy) =>
			Settings.EntryInvertVertical3PControls.Value
				? y + dy
				: y - dy
		);

		c.Index   = 0;
		int found = 0;
		while (c.TryGotoNext(MoveType.After,
			// zInput.GetButton("Camera Hold Rotate")
			x => x.MatchLdstr("Camera Hold Rotate"),
			x => x.MatchLdcI4(0),
			x => x.MatchCall(AccessTools.Method(typeof(zInput), nameof(zInput.GetButton)))
		)){
			found++;
			c.EmitDelegate(bool(bool cameraHoldRotateDown) =>
				cameraHoldRotateDown && !(
					Settings.EntryBlockMousePanningOverUI.Value &&
					UICamera.HoverOverUI()
				)
			);
		}
		if (found != 2)
			Main.Log.LogWarning($"{nameof(Patches)}.{nameof(CameraControllerUpdateIL)} expected 2, found {found}");
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(Pointer), nameof(Pointer.Update))]
	private static void PointerUpdatePrefix(Pointer __instance) // HACK
	{
		if      (zInput.GetButtonUp("Grab"))
		foreach (var grabbableNPO in ManagerPhysicsObject.Instance.GrabbableNPOs)
		if      (grabbableNPO.HeldByPlayerID == __instance.ID)
		if      (grabbableNPO.GetX(out var grabbableNPO_X))
			grabbableNPO_X.HeldTiltRotationIndex = 0;
	}
	[HarmonyILManipulator]
	[HarmonyPatch(typeof(Pointer), nameof(Pointer.Update))]
	private static void PointerUpdateIL(ILContext il)
	{
		ILCursor c = new(il);
		for (int i = 0; i < 4; i++)
		{
			c.GotoNext(MoveType.After,
				// 0: ChangeHeldSpinRotationIndex(RotationSnap / 15);
				// 1: ChangeHeldSpinRotationIndex(24 - RotationSnap / 15);
				// 2: (skipped)
				// 3: ChangeHeldSpinRotationIndex(num4);
				x => x.MatchLdcI4(-1),
				x => x.MatchCall(AccessTools.Method(typeof(Pointer), nameof(Pointer.ChangeHeldSpinRotationIndex)))
			);
			if (i == 2)
				continue;
			c.Index--;
			c.MoveAfterLabels();
			c.Remove();
			c.EmitDelegate(void(Pointer __instance, int spinRotationDelta, int touchId) =>
			{
				if (zInput.GetButton("Ctrl"))
					__instance.ChangeHeldTiltRotationIndex(spinRotationDelta, touchId);
				else
					__instance.ChangeHeldSpinRotationIndex(spinRotationDelta, touchId);
			});
		}

		c.Index = 0;
		c.GotoNext(MoveType.After,
			// ChangeHeldFlipRotationIndex(12);
			x => x.MatchLdcI4(12),
			x => x.MatchLdcI4(-1),
			x => x.MatchCall(AccessTools.Method(typeof(Pointer), nameof(Pointer.ChangeHeldFlipRotationIndex)))
		);
		c.Index--;
		c.MoveAfterLabels();
		c.Remove();
		c.EmitDelegate(void(Pointer __instance, int flipRotationDelta, int touchId) =>
		{
			if (zInput.GetButton("Ctrl"))
				__instance.ChangeHeldTiltRotationIndex(flipRotationDelta, touchId);
			else
				__instance.ChangeHeldFlipRotationIndex(flipRotationDelta, touchId);
		});

		c.GotoNext(MoveType.Before,
			// else if (CurrentPointerMode == PointerMode.Paint)
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(Pointer), nameof(Pointer.CurrentPointerMode))),
			x => x.MatchLdcI4((int) PointerMode.Paint),
			x => x.MatchBneUn(out _)
		);
		c.Index++;
		c.EmitDelegate(PointerMode(PointerMode currentPointerMode) =>
			(currentPointerMode == PointerMode.VectorPixel)
				? PointerMode.Paint
				: currentPointerMode
		);
	}
	[HarmonyILManipulator]
	[HarmonyPatch(typeof(ManagerPhysicsObject), nameof(ManagerPhysicsObject.UpdateGrabbedNPO))]
	private static void ManagerPhysicsObjectUpdateGrabbedNPOIL(ILContext il)
	{
		// @todo: held tilt rotation offset
		ILCursor c = new(il);
		for (int i = 1; i <= 4; i++)
			c.GotoNext(MoveType.After,
				// 0: identity = Quaternion.AngleAxis(grabbedNPO.HeldRotationOffset.x, Vector3.right) * identity;
				// 1: identity = Quaternion.AngleAxis(grabbedNPO.HeldRotationOffset.z, Vector3.forward) * identity;
				// 2: identity = Quaternion.AngleAxis(grabbedNPO.HeldFlipRotationIndex * 15, axis) * identity;
				// 3: identity = Quaternion.AngleAxis(grabbedNPO.HeldSpinRotationIndex * 15, Vector3.up) * identity;
				x => x.MatchCall(AccessTools.Method(typeof(Quaternion), "op_Multiply", [ typeof(Quaternion), typeof(Quaternion) ])),
				x => x.MatchStloc(13)
			);
		c.Emit(OpCodes.Ldarg_0);
		c.Emit(OpCodes.Ldarg_1);
		c.Emit(OpCodes.Ldloc, 13);
		c.EmitDelegate(Quaternion(ManagerPhysicsObject __instance, NetworkPhysicsObject grabbedNPO, Quaternion identity) =>
			!grabbedNPO.GetX(out var grabbedNPO_X)
				? identity
			: Quaternion.AngleAxis(
				grabbedNPO_X.HeldTiltRotationIndex * 15,
				__instance.FlipsAroundZAxis(grabbedNPO.gameObject)
					? Vector3.right
					: Vector3.forward
			) * identity
		);
		c.Emit(OpCodes.Stloc, 13);
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(Pointer), nameof(Pointer.IsVectorTool))]
	private static bool PointerIsVectorToolPrefix(PointerMode mode, ref bool __result)
	{
		// allow usage even if not in the toolbar
		if (mode != PointerMode.VectorPixel)
			return true;
		__result = true;
		return false;
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(ToolVector), nameof(ToolVector.UpdateVectorPixel))]
	private static bool ToolVectorUpdateVectorPixelReplace(ToolVector __instance)
	{
		if (__instance.VectorActionDown() || (__instance.VectorAction() && __instance.drawing && __instance.CheckMove()))
			__instance.StartDrawing(2, loop: true);
		if (__instance.VectorActionUp())
			__instance.EndDrawing();
		return false;
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(ToolVector), nameof(ToolVector.UpdateVectorErase))]
	private static bool ToolVectorUpdateVectorErasePrefix(ToolVector __instance)
	{
		// @todo: somehow preserve overlap order of lines?
			// redrawn lines are all on top, but at least in relative order to each other
		if (__instance.VectorActionDown())
		{
			ToolVectorX.EraseBuffer    = [];
			ToolVectorX.EraseCancelled = false;
		}
		if (__instance.VectorActionUp())
			ToolVectorX.EraseCancelled = false;

		return !ToolVectorX.EraseCancelled;
	}
	[HarmonyILManipulator]
	[HarmonyPatch(typeof(ToolVector), nameof(ToolVector.UpdateVectorErase))]
	private static void ToolVectorUpdateVectorEraseIL(ILContext il)
	{
		ILCursor c = new(il);
		int found = 0;
		while (c.TryGotoNext(MoveType.Before,
			// RPCRemoveLine(drawnLine.Key);
			x => x.MatchCall(AccessTools.Method(typeof(ToolVector), nameof(ToolVector.RPCRemoveLine)))
		)){
			found++;
			c.Emit(OpCodes.Ldloc_2);
			c.EmitDelegate(void(KeyValuePair<uint, ToolVector.VectorDrawData> drawnLine) =>
				ToolVectorX.EraseBuffer.Add(new(drawnLine.Value))
			);
			c.Index += 3; // annoying
		}
		if (found != 2)
			Main.Log.LogWarning($"{nameof(Patches)}.{nameof(ToolVectorUpdateVectorEraseIL)} expected 2, found {found}");
	}
	// [HarmonyTranspiler]
	// [HarmonyPatch(typeof(ToolVector), nameof(ToolVector.UpdateVectorErase))]
	// private static IEnumerable<CodeInstruction> ToolVectorUpdateVectorEraseTranspiler(IEnumerable<CodeInstruction> instructions)
	// {
	// 	int found = 0;
	// 	foreach (var instruction in instructions)
	// 	{
	// 		if (instruction.Calls(AccessTools.Method(typeof(ToolVector), nameof(ToolVector.RPCRemoveLine))))
	// 		{
	// 			yield return new(System.Reflection.Emit.OpCodes.Ldloc_2);
	// 			found++;
	// 			yield return Transpilers.EmitDelegate(void(KeyValuePair<uint, ToolVector.VectorDrawData> drawnLine) =>
	// 				ToolVectorX.EraseBuffer.Add(new(drawnLine.Value))
	// 			);
	// 		}
	// 		yield return instruction;
	// 	}
	// 	if (found != 2)
	// 		Main.Log.LogWarning($"{nameof(Patches)}.{nameof(ToolVectorUpdateVectorEraseTranspiler)} expected 2, found {found}");
	// }
	[HarmonyILManipulator]
	[HarmonyPatch(typeof(ToolVector), nameof(ToolVector.LateUpdate))]
	private static void ToolVectorLateUpdateIL(ILContext il)
	{
		ILCursor c = new(il);
		c.GotoNext(MoveType.After,
			// if (zInput.GetButtonDown("Tap") && drawing)
			x => x.MatchLdstr("Tap"),
			x => x.MatchLdcI4(0),
			x => x.MatchCall(AccessTools.Method(typeof(zInput), nameof(zInput.GetButtonDown)))
		);
		c.Emit(OpCodes.Ldarg_0);
		c.EmitDelegate(bool(bool tapDown, ToolVector __instance) =>
		{
			if (tapDown && (__instance.pointerMode == PointerMode.VectorErase) && __instance.VectorAction() && !ToolVectorX.EraseCancelled)
			{
				ToolVectorX.EraseCancelled = true;
				ToolVectorX.EraseBuffer.Sort((a, b) =>
					a.sortingOrder.CompareTo(b.sortingOrder)
				);
				List<ToolVector.LineNetworkData> lines = [];
				foreach (var erased in ToolVectorX.EraseBuffer)
				{
					var drawData = erased.drawData;
					if (drawData.attached != erased.wasAttached)
						continue; // attached was destroyed
					lines.Add(new(
						__instance.GetGUID(),
						drawData.attached,
						erased  .positions,
						drawData.color,
						drawData.thickness,
						drawData.rotation,
						drawData.loop,
						drawData.square
					));
//					__instance.networkView.RPC(RPCTarget.Others, __instance.RPCAddLine, lineNetworkData);
//					__instance.RPCAddLine(lineNetworkData);
				}
				if (lines.Count > 0)
				{
					__instance.networkView.RPC(RPCTarget.Others, __instance.RPCAddLines, lines);
					__instance.RPCAddLines(lines);
				}
			}
			return tapDown;
		});

		ILLabel skipLabel = null;
		c.GotoNext(MoveType.After,
			// if (zInput.GetButtonDown("Tap") && drawing)
			x => x.MatchLdarg(0),
			x => x.MatchLdfld(AccessTools.Field(typeof(ToolVector), nameof(ToolVector.drawing))),
			x => x.MatchBrfalse(out skipLabel)
		);
		c.Emit(OpCodes.Ldarg_0); // could instead emit before "Tap" check
		c.EmitDelegate(bool(ToolVector __instance) =>
			__instance.pointerMode == PointerMode.VectorPixel
		);
		c.Emit(OpCodes.Brtrue, skipLabel);

		c.GotoNext(MoveType.Before,
			// RPCRemoveLine(currentDrawingGuid);
			x => x.MatchLdarg(0),
			x => x.MatchLdarg(0),
			x => x.MatchLdfld(AccessTools.Field (typeof(ToolVector), nameof(ToolVector.currentDrawingGuid))),
			x => x.MatchCall (AccessTools.Method(typeof(ToolVector), nameof(ToolVector.RPCRemoveLine)))
		);
		c.MoveAfterLabels();
		c.RemoveRange(4);
		c.GotoNext(MoveType.Before,
			// EndDrawing();
			x => x.MatchLdarg(0),
			x => x.MatchCall(AccessTools.Method(typeof(ToolVector), nameof(ToolVector.EndDrawing)))
		);
		c.Index++;
		c.Remove();
		c.EmitDelegate(void(ToolVector __instance) =>
			__instance.RPCRemoveLine(__instance.currentDrawingGuid)
		);
	}
	[HarmonyILManipulator]
	[HarmonyPatch(typeof(Pointer), nameof(Pointer.StartLine))]
	private static void PointerStartLineIL(ILContext il)
	{
		ILCursor c = new(il);
		c.GotoNext(MoveType.After,
			// if (HighLightedObjects.Count == 0)
			x => x.MatchLdarg   (0),
			x => x.MatchCall    (AccessTools.PropertyGetter(typeof(Pointer),                    nameof(Pointer.HighLightedObjects))),
			x => x.MatchCallvirt(AccessTools.PropertyGetter(typeof(List<NetworkPhysicsObject>), nameof(List<>.Count)))
		);
		c.Emit(OpCodes.Ldarg_2);
		c.EmitDelegate(int(int count, NetworkPhysicsObject hoverObject) =>
			Network.isClient && Settings.EntryEnableFastFlick.Value && (count == 0) && hoverObject
				? 1
				: count
		);
	}

	[HarmonyILManipulator]
	[HarmonyPatch(typeof(Pointer), nameof(Pointer.StartContextual))]
	private static void PointerStartContextualIL(ILContext il)
	{
		ILCursor c = new(il);
		c.GotoNext(MoveType.After,
			// SetActive(NetworkInstance.GUIContextualHandReveal, NetworkSingleton<NetworkUI>.Instance.handZoneToReveal != null && NetworkSingleton<NetworkUI>.Instance.handZoneToReveal.TriggerLabel == PointerColorLabel);
			x => x.MatchLdfld(AccessTools.Field(typeof(NetworkUI), nameof(NetworkUI.handZoneToReveal))),
			x => x.MatchCallvirt(AccessTools.PropertyGetter(typeof(HandZone), nameof(HandZone.TriggerLabel))),
			x => x.MatchLdarg(0),
			x => x.MatchLdfld(AccessTools.Field(typeof(Pointer), nameof(Pointer.PointerColorLabel))),
			x => x.MatchCall(AccessTools.Method(typeof(string), "op_Equality"))
		);
		c.EmitDelegate(bool(bool eq) => true);

		c.GotoNext(MoveType.After,
			// SetActive(NetworkInstance.GUIContextualPhysics, Network.isServer && flag);
			x => x.MatchLdfld(AccessTools.Field(typeof(NetworkUI), nameof(NetworkUI.GUIContextualPhysics))),
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(Network), nameof(Network.isServer)))
		);
		c.Previous.Operand = AccessTools.PropertyGetter(typeof(Network), nameof(Network.isAdmin));

		c.GotoNext(MoveType.After,
			// SetActive(NetworkInstance.GUIContextualCustom, Network.isServer && (bool)InfoObject.GetComponent<CustomObject>());
			x => x.MatchLdfld(AccessTools.Field(typeof(NetworkUI), nameof(NetworkUI.GUIContextualCustom))),
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(Network), nameof(Network.isServer)))
		);
		c.Previous.Operand = AccessTools.PropertyGetter(typeof(Network), nameof(Network.isAdmin));

#if TRUE_ULTIMATE_POWER
		// @todo: either support or remove Scripting but keep GUID
		c.GotoNext(MoveType.After,
			// SetActive(NetworkInstance.GUIContextualScripting, Network.isServer && !string.IsNullOrEmpty(component.GUID));
			x => x.MatchLdfld(AccessTools.Field(typeof(NetworkUI), nameof(NetworkUI.GUIContextualScripting))),
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(Network), nameof(Network.isServer)))
		);
		c.EmitDelegate(bool(bool isServer) => true);
#endif

		// @gold
		c.GotoNext(MoveType.Before,
			// SetActive(((Component)NetworkInstance.GUIContextualGoldBool.get_transform().get_parent()).get_gameObject(), Network.isServer && SteamManager.bKickstarterGold);
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(Network), nameof(Network.isServer))),
			x => x.MatchBrfalse(out _),
			x => x.MatchLdsfld(AccessTools.Field(typeof(SteamManager), nameof(SteamManager.bKickstarterGold))),
			x => x.MatchBr(out _),
			x => x.MatchLdcI4(0)
		);
		c.MoveAfterLabels();
		c.Remove();
		c.EmitDelegate(bool() =>
			PlayerStateX.Host.IsModded
		);
		// c.RemoveRange(2);
		// c.Index++;
		// c.RemoveRange(2);

		c.GotoNext(MoveType.After,
			// NetworkInstance.GUIContextualMenu.SetActive(value: true);
			x => x.MatchLdarg(0),
			x => x.MatchLdfld(AccessTools.Field(typeof(Pointer), nameof(Pointer.NetworkInstance))),
			x => x.MatchLdfld(AccessTools.Field(typeof(NetworkUI), nameof(NetworkUI.GUIContextualMenu))),
			x => x.MatchLdcI4(1),
			x => x.MatchCallvirt(AccessTools.Method(typeof(GameObject), nameof(GameObject.SetActive)))
		);
		// c.MoveAfterLabels();
		c.Index--;
		c.Emit(OpCodes.Ldarg_1);
		c.EmitDelegate(void(GameObject InfoObject) =>
		{
			// slow?
			foreach (var item in NetworkUI.Instance.GUIContextualMenu.transform.GetChild(0).GetComponentsInChildren<IUZContextual>(true))
				item.OnStartContextual();
		});
	}
	[HarmonyPostfix]
	[HarmonyPatch(typeof(Pointer), nameof(Pointer.StartGlobalContextual))]
	private static void PointerStartGlobalContextualPostfix()
	{
		// slow?
		foreach (var item in NetworkUI.Instance.GUIContextualGlobalMenu.transform.GetChild(0).GetComponentsInChildren<IUZContextual>(true))
			item.OnStartContextual();
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(LuaScript), nameof(LuaScript.DoString))]
	private static bool LuaScriptDoStringPrefix(LuaScript __instance) =>
		LuaScriptExecuteScriptPrefix(__instance, __instance.script_code);
	[HarmonyPrefix]
	[HarmonyPatch(typeof(LuaScript), nameof(LuaScript.ExecuteScript))]
	private static bool LuaScriptExecuteScriptPrefix(LuaScript __instance, string script)
	{
		if (!Settings.EntryInterceptLuaVirus.Value)
			return true;
		if (script is null or [])
			return true;
		if (script.IndexOf("tcejbo gninwapS", StringComparison.OrdinalIgnoreCase) == -1)
			return true;
//		Chat.Log(
		Chat.SendChat(
			(__instance.guid == "-1")
				? $"Infected script executed globally intercepted by {Main.PluginColour.RGBHex}{Main.PLUGIN_NAME}[-]"
				: $"Infected script executed on object {__instance.GetScriptName()} intercepted by {Main.PluginColour.RGBHex}{Main.PLUGIN_NAME}[-]",
			Main.ErrorColour
		);
		return false;
	}
}

