using System.ComponentModel;
using BepInEx.Configuration;
using Unleashed.Settings;

namespace Unleashed.Patches;

[HarmonyPatch]
static class ServerSetup
{
	[Setting]
	static readonly Setting<string> InitialColour = new()
	{
		MigrateFrom = [
			("General", "Initial Player Color"), // 0.1.0
		],
		Section     = Section.Setup,
		Key         = "Initial Player Color",
		Default     = Colour.White.Label,
		Acceptable  = new AcceptableValueList<string>(["Choose", "Dialog", .. Colour.AllPlayerLabels]),
		Description =
			"""
			The initial player color after server creation, or Choose/Dialog to open the color selection UI/dialog window.
			""",
	};
	[Setting]
	static readonly Setting<BackgroundID> InitialBackground = new()
	{
		MigrateFrom = [
			("General", "Initial Background"), // 0.1.0
		],
		Section     = Section.Setup,
		Key         = "Initial Background",
		Default     = BackgroundID.Random,
		Description =
			"""
			The initial background after server creation, or Random.
			""",
	};
	[Setting]
	static readonly Setting<TableID> InitialTable = new()
	{
		MigrateFrom = [
			("General", "Initial Table"), // 0.1.0
		],
		Section     = Section.Setup,
		Key         = "Initial Table",
		Default     = TableID.Random,
		Description =
			"""
			The initial table after server creation, or Random.
			""",
	};
	enum BackgroundID
	{
		Museum    = 8,
		Field     = 2,
		Forest    = 1,
		Tunnel    = 3,
		Cathedral = 4,
		Downtown  = 5,
		Regal     = 6,
		Sunset    = 7,
		// Custom,
		Random    = 0,
	}
	enum TableID
	{
		Hexagon  = 3,
		Octagon  = 2,
		Square   = 1,
		Poker    = 6,
		[Description("RPG")]
		RPG      = 4,
		Circular = 5,
		// Custom,
		Glass    = 7,
		Plastic  = 8,
		Random   = 0,
		[Description("Random+")]
		RandomPlus = -2,
		[Description("Random++")]
		RandomPlusPlus = -1,
	}

	[HarmonyILManipulator]
	[HarmonyPatch(typeof(NetworkUI), nameof(NetworkUI.ServerInitialized))]
	static void ServerInitializedIL(ILContext il)
	{
		var c = new ILCursor(il);
		c.GotoNext(MoveType.After,
			// int num = UnityEngine.Random.Range(1, 9);
			x => x.MatchLdcI4(1),
			x => x.MatchLdcI4(9),
			x => x.MatchCall(AccessTools.Method(typeof(UnityEngine.Random), nameof(UnityEngine.Random.Range), [ typeof(int), typeof(int) ]))
		);
		// c.Emit(OpCodes.Ldloca, 4);
		c.EmitDelegate(int(int num/*, ref GameObject gameObject*/) =>
		{
			var num2 =  InitialBackground.Value;
			if (num2 == BackgroundID.Random)
				return num;
			return (int) num2;
		});

		c.GotoNext(MoveType.After,
			// v13.3: switch (UnityEngine.Random.Range(1, 6))
			// x => x.MatchLdcI4(1),
			// v14.0: switch (UnityEngine.Random.Range((!bHotseat) ? 1 : 3, 6))
			x => x.MatchLdcI4(6),
			x => x.MatchCall(AccessTools.Method(typeof(UnityEngine.Random), nameof(UnityEngine.Random.Range), [ typeof(int), typeof(int) ]))
		);
		c.Emit(OpCodes.Ldloca, 0);
		c.EmitDelegate(int(int num, ref GameObject gameObject2) =>
		{
			var num2 = InitialTable.Value;
			switch (num2)
			{
				case TableID.Random:
					return num;
				case TableID.RandomPlus:
					num2 = (TableID) UnityEngine.Random.Range(1, 7+1);
					break;
				case TableID.RandomPlusPlus:
					num2 = (TableID) UnityEngine.Random.Range(1, 8+1);
					break;
			}
			switch (num2)
			{
				case TableID.Poker:
					gameObject2 = GameMode.Instance.PokerTable;
					break;
				case TableID.Glass:
					gameObject2 = GameMode.Instance.GlassTable;
					break;
				case TableID.Plastic:
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
			switch (InitialColour.Value)
			{
				case "Choose":
					__instance.GUIChangeColor();
					break;
				case "Dialog":
					__instance.bNeedToPickColour = false;
					UIColorSelection.ShowDialog();
					break;
				default:
					__instance.ClientRequestColor(InitialColour.Value);
					break;
			}
		});
	}
}

