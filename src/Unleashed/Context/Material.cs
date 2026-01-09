using System.Diagnostics;
using System.Runtime.CompilerServices;
using I2.Loc;
using Unleashed.Compat;

namespace Unleashed.Context;

static class PointerX
{
	extension(Pointer @this)
	{
		[RemoteX(Permission.Owner, SendType.ReliableNoDelay, "Permissions/Contextual", SerializationMethod.Default)]
		public void SetMaterialID(string matID)
		{
			if (!API.HostModded)
			{
				@this.Material(MaterialID.Compat[matID]);
				return;
			}
			if (Network.isClient)
			{
				@this.RPC(RPCTarget.Server, @this.SetMaterialID, matID);
				return;
			}
			foreach (var npo in @this.GetSelectedNPOs())
			{
				if (MaterialClass.Find(npo.gameObject, out var matInt, out var matClass))
				if (matClass.Lookup(matInt, matID, out var altSounds, out matInt))
					npo.SetObject(bAltSounds: altSounds, MatInt: matInt);
			}
		}
	}
}

static class MaterialID
{
	public static readonly Dictionary<string, int> Compat = new()
	{
		{ PLASTIC, 0 }, { WOOD, 0 }, { METAL, 1 }, { GOLD, 2 },
		// incorrect AltSounds but working
		{ ARMS, 0 }, { CROWNS, 1 }, { MOONS, 2 },
	};
	public const string
		INVALID = "???",
		PLASTIC = "Plastic",
		METAL = "Metal", METAL_LIGHT = "Chrome",     METAL_DARK = "Cast Iron",
		WOOD  = "Wood",  WOOD_LIGHT  = "Wood Light", WOOD_DARK  = "Wood Dark",
		GOLD = "Gold",
		ARMS = "Arms", CROWNS = "Crowns", MOONS = "Moons", SUNS = "Suns",
		WHITE = "White", RED = "Red", ORANGE = "Orange", YELLOW = "Yellow", GREEN = "Green", BLUE = "Blue", PURPLE = "Purple", PINK = "Pink", BLACK = "Black";
}
public readonly record struct MaterialSlot(string ID, bool AltSounds = false)
{
	public static implicit operator MaterialSlot(string name) =>
		new(name);
	public static implicit operator MaterialSlot((string Name, bool AltSounds) @this) =>
		new(@this.Name, @this.AltSounds);
}
public readonly record struct MaterialClass(MaterialSlot[] Slots, Dictionary<(int MatInt, string MatName), int> SlotAlias = null)
{
	Dictionary<(int MatInt, string MatName), int> SlotAlias { get; } =
		SlotAlias ?? Slots.Index().ToDictionary(x => (-1, x.Item.ID), y => y.Index);
	public bool Lookup(int matInt, string matID, out bool altSounds, out int index)
	{
		if (SlotAlias.TryGetValue((matInt, matID), out index) ||
			SlotAlias.TryGetValue((-1,     matID), out index)
		){
			if (matInt != index)
			{
				altSounds = Slots[index].AltSounds;
				return true;
			}
		}
		altSounds = default;
		return false;
	}
	public static readonly MaterialClass Default = new(
		[MaterialID.PLASTIC, (MaterialID.METAL, true), (MaterialID.GOLD, true) ],
		new()
		{
			{ (-1, MaterialID.PLASTIC), 0 },
			{ (-1, MaterialID.WOOD),    0 }, { (-1, MaterialID.WOOD_LIGHT),  0 }, { (-1, MaterialID.WOOD_DARK),  0 },
			{ (-1, MaterialID.METAL),   1 }, { (-1, MaterialID.METAL_LIGHT), 1 }, { (-1, MaterialID.METAL_DARK), 1 },
			{ (-1, MaterialID.GOLD),    2 },
		}
	);
	public static readonly MaterialClass ChessCompat = new(
		[MaterialID.METAL, (MaterialID.WOOD, true), MaterialID.GOLD, ],
		new()
		{
			{ (-1, MaterialID.METAL), 0 },
			{ (-1, MaterialID.WOOD),  1 }, { (-1, MaterialID.PLASTIC), 1 },
			{ (-1, MaterialID.GOLD),  2 },
		}
	);
	public static readonly MaterialClass Chess = new(
		[MaterialID.METAL_LIGHT, MaterialID.METAL_DARK, (MaterialID.WOOD_LIGHT, true), (MaterialID.WOOD_DARK, true), MaterialID.GOLD ],
		new()
		{
			{ (-1, MaterialID.METAL_LIGHT), 0 }, { (-1, MaterialID.METAL),   0 }, { (1, MaterialID.METAL),   1 }, { (2, MaterialID.METAL),   0 }, { (3, MaterialID.METAL),   1 },
			{ (-1, MaterialID.METAL_DARK),  1 },
			{ (-1, MaterialID.WOOD_LIGHT),  2 }, { (-1, MaterialID.WOOD),    2 }, { (3, MaterialID.WOOD),    3 }, { (0, MaterialID.WOOD),    2 }, { (1, MaterialID.WOOD),    3 },
			{ (-1, MaterialID.WOOD_DARK),   3 }, { (-1, MaterialID.PLASTIC), 2 }, { (3, MaterialID.PLASTIC), 3 }, { (0, MaterialID.PLASTIC), 2 }, { (1, MaterialID.PLASTIC), 3 },
			{ (-1, MaterialID.GOLD),        4 },
		}
	);
	public static readonly MaterialClass DiePiecepack = new(
		[MaterialID.ARMS, MaterialID.CROWNS, MaterialID.MOONS, MaterialID.SUNS ]
	);
	public static readonly MaterialClass Die6Rounded = new(
		[MaterialID.BLACK, MaterialID.RED, MaterialID.GREEN, MaterialID.BLUE ]
	);
	public static readonly MaterialClass PlayerPawn = new(
		[MaterialID.WHITE, MaterialID.RED, MaterialID.ORANGE, MaterialID.YELLOW, MaterialID.GREEN, MaterialID.BLUE, MaterialID.PURPLE, MaterialID.PINK, MaterialID.BLACK ]
	);
	public static readonly MaterialClass ChineseCheckersPiece = new(
		[MaterialID.WHITE, MaterialID.RED, MaterialID.YELLOW, MaterialID.GREEN, MaterialID.BLUE, MaterialID.PINK, MaterialID.BLACK ]
	);
	public static bool Find(GameObject gameObject, out int matInt, out MaterialClass matClass)
	{
		if (!gameObject || gameObject.GetComponent<MaterialSyncScript>() is not {} matSync)
		{
			matInt = -1;
			matClass = default;
			return false;
		}
		matInt = matSync.GetMaterial();
		switch ((gameObject.tag, Utilities.RemoveCloneFromName(gameObject.name)))
		{
			default:
				matClass = default;
				return false;
			case ("Chess", _):
				matClass = API.HostModded ? Chess : ChessCompat;
				return true;
			case ("Dice", "Die_6_Rounded"):
				matClass = Die6Rounded;
				return true;
			case ("Dice", "Die_Piecepack"):
				matClass = DiePiecepack;
				return true;
			case ("Dice" or "Domino", _):
				matClass = Default;
				return true;
			case ("Checker", "Chinese_Checkers_Piece"):
				matClass = ChineseCheckersPiece;
				return true;
			case ("Figurine", "PlayerPawn"):
				matClass = PlayerPawn;
				return true;
		}
	}
}

// [HarmonyPatch]
sealed class UZContextualMaterial : MonoBehaviour
{
	// [ModuleInitializer]
	// internal static void Initializer() =>
	// 	Events.OnStartConnected += OnStartConnected;
	// static void OnStartConnected() =>
	// 	NetworkUI.Instance.GUIContextualMaterial.GetOrAddComponent<UZContextualMaterial>();

	[HarmonyILManipulator]
	[HarmonyPatch(typeof(Pointer), nameof(Pointer.StartContextual))]
	static void StartContextualIL(ILContext il)
	{
		var c = new ILCursor(il);
		c.GotoNext(MoveType.After,
			// SetActive(NetworkInstance.GUIContextualGroup, CheckGroup());
			x => x.MatchCall(AccessTools.Method(typeof(Pointer), nameof(Pointer.CheckGroup))),
			x => x.MatchCall(AccessTools.Method(typeof(Pointer), nameof(Pointer.SetActive)))
			// if ((InfoObject.tag == "Dice" || InfoObject.tag == "Domino" || InfoObject.tag == "Chess") && InfoObject.name != "Die_6_Rounded(Clone)" && (bool)InfoObject.GetComponent<MaterialSyncScript>())
		);
		var s = c.Index;
		c.GotoNext(MoveType.After,
			// SetActive(NetworkInstance.GUIContextualMaterial, enabled: false);
			x => x.MatchLdfld(AccessTools.Field(typeof(NetworkUI), nameof(NetworkUI.GUIContextualMaterial))),
			x => x.MatchLdcI4(0),
			x => x.MatchCall(AccessTools.Method(typeof(Pointer), nameof(Pointer.SetActive)))			
		);
		var e = c.Index;
		c.Index = s;
		c.MoveAfterLabels();
		c.RemoveRange(e-s);
	}

	UIToggle OldGoldToggle;
	UITable ToggleTable;
	readonly List<UIToggle> Toggles = [];
	MaterialClass MatClass;

	// #todo: add a checkbox for AltSounds on top
	// #idea: add sub-panel for chess materials
	void Start()
	{
		Events.OnStartContextual += OnStartContextual;

		UIToggle[] oldToggles = [
			NetworkUI.Instance.GUIContextualWoodBool   .GetComponent<UIToggle>(),
			NetworkUI.Instance.GUIContextualPlasticBool.GetComponent<UIToggle>(),
			NetworkUI.Instance.GUIContextualMetalBool  .GetComponent<UIToggle>(),
			NetworkUI.Instance.GUIContextualGoldBool   .GetComponent<UIToggle>(),
		];
		OldGoldToggle = oldToggles[^1];
		foreach (var toggle in oldToggles)
			toggle.transform.parent.gameObject.SetActive(false);

		ToggleTable = OldGoldToggle.transform.parent.parent.GetComponent<UITable>();
		OnStartContextual();
	}
	void OnDestroy() =>
		Events.OnStartContextual -= OnStartContextual;

	void OnStartContextual() =>
		Contextual.Check(gameObject, CheckContextual);

	void OnClickMaterial(int index)
	{
		if (PlayerScript.Pointer)
			PlayerScript.PointerScript.SetMaterialID(MatClass.Slots[index].ID);
	}
	bool CheckContextual()
	{
		if (!PlayerScript.Pointer)
			return false;
		if (!MaterialClass.Find(PlayerScript.PointerScript.InfoObject, out var matInt, out MatClass))
			return false;

		if (Toggles.Count < MatClass.Slots.Length)
		{
			do CreateToggle();
			while (Toggles.Count < MatClass.Slots.Length);

			ToggleTable.repositionNow = true;
		}
		foreach (var (i, toggle) in Toggles.Index())
		{
			var valid = i < MatClass.Slots.Length;
			var matID = valid ? MatClass.Slots[i].ID : MaterialID.INVALID;
			var enabled = valid &&
				(API.HostModded || MaterialID.Compat.ContainsKey(matID)) &&
				(SteamManager.bKickstarterGold || matID != MaterialID.GOLD);

			toggle.GetComponent<BoxCollider2D>().enabled = enabled;
			toggle.transform.parent.GetComponent<UILabel>().text = Contextual.LABEL_PADDING + matID;
			toggle.transform.parent.gameObject.SetActive(valid);

			// #todo: sometimes not working
			var group = toggle.group;
			toggle.group = 0;
			{
				// #hack
				if (!API.HostModded && PlayerScript.PointerScript.InfoObject.tag == "Chess")
					toggle.value = matID switch {
						MaterialID.METAL => matInt is 0 or 1,
						MaterialID.WOOD  => matInt is 2 or 3,
						MaterialID.GOLD  => matInt is 4,
						_ => throw new UnreachableException(),
					};
				else
					toggle.value = matInt == i;
			}
			toggle.group = group;
		}
		return true;
	}
	void CreateToggle()
	{
		var i = Toggles.Count;
		var @base = OldGoldToggle.transform.parent.gameObject;

		var gameObject  = Instantiate(@base, @base.transform.parent);
		gameObject.name = $"{i} {Main.PLUGIN_ABBR} Material";
		gameObject.GetComponent<Localize>().enabled = false; // #loc

		var radio  = gameObject.transform.Find("Gold Radio").gameObject;
		radio.name = $"{gameObject.name} Radio";
		radio.GetComponent<UIButton>().onClick = [ new(() => OnClickMaterial(i)) ];

		GetComponent<UIHoverEnableObjects>().HoverEnableObjects.Add(radio);
		Toggles.Add(radio.GetComponent<UIToggle>());
	}
}

