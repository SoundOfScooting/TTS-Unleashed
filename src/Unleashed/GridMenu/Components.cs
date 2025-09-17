using static UIGridMenu;

namespace Unleashed.GridMenu;

[HarmonyPatch]
static class Components
{
	static readonly List<string> RandomNames =
	[
		"RANDOM", "RAND",
	];

	const int CardCardIDLimit = 51; // #bug: wrong in base game?
	static readonly Dictionary<string, int> CardCardID = new()
	{
		{ "KC", 0  }, { "QC", 1  }, { "JC", 2  }, { "AC", 3  }, { "10C", 4  }, { "9C", 10 }, { "8C", 11 }, { "7C", 12 }, { "6C", 13 }, { "5C", 14 }, { "4C", 20 }, { "3C", 21 }, { "2C", 22 },
		{ "KD", 5  }, { "QD", 6  }, { "JD", 7  }, { "AD", 8  }, { "10D", 9  }, { "9D", 15 }, { "8D", 16 }, { "7D", 17 }, { "6D", 18 }, { "5D", 19 }, { "4D", 25 }, { "3D", 23 }, { "2D", 24 },
		{ "KS", 35 }, { "QS", 36 }, { "JS", 37 }, { "AS", 38 }, { "10S", 39 }, { "9S", 45 }, { "8S", 46 }, { "7S", 47 }, { "6S", 48 }, { "5S", 26 }, { "4S", 27 }, { "3S", 28 }, { "2S", 29 },
		{ "KH", 30 }, { "QH", 31 }, { "JH", 32 }, { "AH", 33 }, { "10H", 34 }, { "9H", 40 }, { "8H", 41 }, { "7H", 42 }, { "6H", 43 }, { "5H", 44 }, { "4H", 49 }, { "3H", 50 }, { "2H", 51 },
		{ "JK", 52 }, { "JOKER", 52 },
	};
	static string CardSpawnName(int front_id) =>
		$"{Main.PLUGIN_GUID}/Card/{SpawnName.SETUP_CARD}/{front_id}";

	const int DominoMeshIndexLimit = 27; // #bug: wrong in base game?
	static readonly Dictionary<string, int> DominoMeshIndex = new()
	{
		{ "0/0", 0  },
		{ "1/0", 24 }, { "1/1", 17 },
		{ "2/0", 3  }, { "2/1", 20 }, { "2/2", 15 },
		{ "3/0", 26 }, { "3/1", 7  }, { "3/2", 1  }, { "3/3", 12 },
		{ "4/0", 5  }, { "4/1", 4  }, { "4/2", 9  }, { "4/3", 16 }, { "4/4", 23 },
		{ "5/0", 22 }, { "5/1", 2  }, { "5/2", 10 }, { "5/3", 21 }, { "5/4", 18 }, { "5/5", 25 },
		{ "6/0", 11 }, { "6/1", 27 }, { "6/2", 6  }, { "6/3", 19 }, { "6/4", 14 }, { "6/5", 13 }, { "6/6", 8 },
	};
	const int DominoMatIndexLimit = 2;
	static readonly Dictionary<string, int> DominoMatIndex = new()
	{
		{ "PLASTIC", 0 }, { "METAL", 1 }, { "GOLD", 2 },
	};
	static string DominoSpawnName(int meshInt, int matInt) =>
		$"{Main.PLUGIN_GUID}/Domino/{SpawnName.SET_OBJECT}/{matInt != 0}/{meshInt}/{matInt}";

	static readonly string[] ChessType = ["Pawn", "Rook", "Knight", "Bishop", "Queen", "King"];
	const int ChessMaterialGold = 4;
	static string ChessGoldSpawnName(string type) =>
		$"{Main.PLUGIN_GUID}/Chess_{type}/{SpawnName.SET_OBJECT}/{false}/{-1}/{ChessMaterialGold}";

	static readonly string[] DiceType  = ["4", "6", "8", "10", "12", "20"];
	const int DiceMaterialGold = 2;
	static string DiceGoldSpawnName(string type) =>
		$"{Main.PLUGIN_GUID}/Die_{type}/{SpawnName.SET_OBJECT}/{true}/{-1}/{DiceMaterialGold}";

	[HarmonyPrefix]
	[HarmonyPatch(typeof(UIGridMenuObjects), nameof(UIGridMenuObjects.InitComponents))]
	static void InitComponentsPrefix(UIGridMenuObjects __instance)
	{
		var cardsFolder = __instance.ComponentsButtons.First(x => x.Name == "Cards");
		cardsFolder.ComponentButtons.AddRange([
			new GridButtonOnSpawn()
			{
				Name = $"Specific Card {Main.PluginColour.RGBHex}+{Main.PLUGIN_ABBR}[-]",
				Tags = [ Main.PLUGIN_GUID, GridButtonX.TAG_HOST, ],
				SpawnName = CardSpawnName(CardCardID["AS"]),

				OnSpawn = GridButtonOnSpawn.ShowInput(
					Placeholder: "[A/K/Q/J/10/#][C/D/S/H]",
					ParseSpawnName: input => {
						input = input.Trim().ToUpperInvariant();

						int front_id;
						if (RandomNames.Contains(input))
							front_id = UnityEngine.Random.Range(0, CardCardIDLimit);
						else if (
							!int.TryParse(input, out front_id) &&
							!CardCardID.TryGetValue(input, out front_id)
						) return null;

						return CardSpawnName(front_id);
					}
				),
			},
		]);
		var chessFolder = __instance.ComponentsButtons.First(x => x.Name == "Chess");
		chessFolder.FolderButtons.Add(new()
		{
			Name = $"Gold {Main.PluginColour.RGBHex}+{Main.PLUGIN_ABBR}[-]",
			Tags = [ Main.PLUGIN_GUID, GridButtonX.TAG_HOST, GridButtonX.TAG_GOLD, ],
			BackgroundColor = Color.clear,
			SpriteName      = "Icon-Folder2",
			SpriteColor     = Color.black,
			ComponentButtons = [..
				from type in ChessType
				select new GridButtonComponent()
				{
					Name = $"{type} Gold",
					Tags = [ Main.PLUGIN_GUID, GridButtonX.TAG_HOST, GridButtonX.TAG_GOLD, ],
					SpawnName = ChessGoldSpawnName(type),
				}
			],
		});
		var diceFolder = __instance.ComponentsButtons.First(x => x.Name == "Dice");
		diceFolder.FolderButtons.Add(new()
		{
			Name = $"Gold {Main.PluginColour.RGBHex}+{Main.PLUGIN_ABBR}[-]",
			Tags = [ Main.PLUGIN_GUID, GridButtonX.TAG_HOST, GridButtonX.TAG_GOLD, ],
			BackgroundColor = Color.clear,
			SpriteName      = "Icon-Folder2",
			SpriteColor     = Color.black,
			ComponentButtons = [..
				from type in DiceType
				select new GridButtonComponent()
				{
					Name = $"D{type} Gold",
					Tags = [ Main.PLUGIN_GUID, GridButtonX.TAG_HOST, GridButtonX.TAG_GOLD, ],
					SpawnName = DiceGoldSpawnName(type),
				}
			],
		});
		var miscFolder = __instance.ComponentsButtons.First(x => x.Name == "Miscellaneous");
		miscFolder.ComponentButtons.AddRange([
			new GridButtonOnSpawn()
			{
				Name = $"Specific Domino {Main.PluginColour.RGBHex}+{Main.PLUGIN_ABBR}[-]",
				Tags = [ Main.PLUGIN_GUID, GridButtonX.TAG_HOST, ],
				SpawnName = DominoSpawnName(DominoMeshIndex["6/6"], 0),

				OnSpawn = GridButtonOnSpawn.ShowInput(
					Placeholder: "[high]/[low] (material)",
					ParseSpawnName: input => {
						var parts = input.ToUpperInvariant().Split([' '], StringSplitOptions.RemoveEmptyEntries);
						if (parts is [])
							return null;

						var meshInt = 0;
						if (parts is [ var mesh, .. ] &&
							!int.TryParse(mesh, out meshInt) &&
							!DominoMeshIndex.TryGetValue(mesh, out meshInt) &&
							!DominoMeshIndex.TryGetValue(new([.. mesh.Reverse()]), out meshInt) // bad
						){
							if (RandomNames.Contains(mesh))
								meshInt = UnityEngine.Random.Range(0, DominoMeshIndexLimit);
							else
								return null;
						}
						var matInt = 0;
						if (parts is [ _, var mat, .. ] &&
							!int.TryParse(mat, out matInt) &&
							!DominoMatIndex.TryGetValue(mat, out matInt)
						){
							if (RandomNames.Contains(mat))
								matInt = UnityEngine.Random.Range(0, DominoMatIndexLimit);
							else
								return null;
						}
						return DominoSpawnName(meshInt, matInt);
					}
				),
			},
		]);
	}
}

