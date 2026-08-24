using System.Runtime.CompilerServices;

namespace Unleashed.Patches;

[HarmonyPatch]
sealed class UZCameraHome : UZMonoBehaviour
{
	// public const string HomePref = $"{Main.PLUGIN_GUID}/{nameof(UZCameraHome)}";

	// [Settings.Setting]
	// static readonly Settings.Setting<Home> EntryInitCameraHome = new()
	// {
	// 	Section     = Settings.Section.General,
	// 	Key         = "Initial Camera Home",
	// 	Default     = Home.Hand,
	// 	Description =
	// 		"""
	// 		The initial camera home.
	// 		""",
	// };

	public enum Home
	{
		Hand,
		White, Brown, Red, Orange, Yellow, Green, Teal, Blue, Purple, Pink,
		Grey, Black,
	}

	UILabel Label;

	public static UZCameraHome Instance { get; private set; }
	public static string GUIColorText { get; private set; }

	public static bool ForceResetCameraRotation { get; set; }
	Home mCurrent;
	public static Home Current
	{
		get => Instance ? Instance.mCurrent : default;
		set
		{
			if (!Instance)
				return;
			Instance.mCurrent = value;
			NeedToPickHome    = false;
			// PlayerPrefs.SetInt(HomePref, (int) value);
			CameraController.Instance.ResetCameraRotation();
		}
	}
	bool mNeedToPickHome;
	public static bool NeedToPickHome
	{
		get => Instance && Instance.mNeedToPickHome;
		set
		{
			if (!Instance)
				return;
			Instance.mNeedToPickHome = value;
			Instance.Label.text = value
				? $"({nameof(Home.Hand)})"
				: $"{Current}";

			NetworkUI.Instance.bNeedToPickColour = value;
			var label = NetworkUI.Instance.GUIColor
				.transform.Find("!Label").GetComponent<UILabel>();
			GUIColorText ??= label.text;
			label.text = value
				? "Choose Camera Home"
				: GUIColorText;
		}
	}

	[ModuleInitializer]
	internal static void ModuleInitializer()
		=> Events.OnStartConnected += OnStartConnected;
	static void OnStartConnected()
	{
		var @base = Resources
			.FindObjectsOfTypeAll<UIPointerRotationSnap>()
			.FirstOrDefault();
		InstantiateX(@base.transform, active: false, name: "02 CameraHome")
			.GetOrAddComponent<UZCameraHome>()
			.Initialize(@base);
	}
	void Initialize(UIPointerRotationSnap @base)
	{
		transform.localPosition += new Vector3(56f, 0f, 0f);
		Destroy(GetComponent<UIPointerRotationSnap>());
		Destroy(GetComponent<UIPointerRotationSnapX>()); // #todo: avoid these kinds of issues
		GetComponent<UITooltipObject>().Tooltip = "Camera Home";
		GetComponent<I2.Loc.Localize>().enabled = false; // #loc

		var labelObject = transform.Find(@base.ThisLabelObject.name);
		labelObject.localPosition = new(0f, -2f, 0f);
		Label = labelObject.GetComponent<UILabel>();

		gameObject.SetActive(@base.gameObject.activeSelf);
	}
	void Awake()
	{
		Instance = this;
		// Current = (Home) PlayerPrefs.GetInt(HomePref, (int) Value.Hand);
		Current = Home.Hand;
	}

	void OnClick()
	{
		_ = this;
		if (NeedToPickHome)
		{
			Current = Home.Hand;
			return;
		}
		NeedToPickHome = true;
	}
	void OnAltClick()
	{
		_ = this;
		if (NeedToPickHome)
		{
			NeedToPickHome           = false;
			ForceResetCameraRotation = true;
			CameraController.Instance.ResetCameraRotation();
			return;
		}
		UIDialog.ShowDropDown(
			"[b]Choose Camera Home[/b]",
			dropDownOptions: [.. Enum.GetNames<Home>()],
			drowDownValue:   Current.ToString(),

			leftButtonText: "OK",
			leftButtonFunc: (label, _)
				=> Current = Enum.Parse<Home>(label),

			rightButtonText: "Cancel",
			rightButtonFunc: null
		);
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(CameraController), nameof(CameraController.DelayResetCameraRotation))]
	static void DelayResetCameraRotationPrefix(CameraController __instance, ref string colourLabel, ref CameraState __state)
	{
		__state = null;
		if (ForceResetCameraRotation)
		{
			ForceResetCameraRotation = false;
			__state = __instance.CameraStates[0];
			__instance.CameraStates[0] = null;
			return;
		}
		if (NeedToPickHome)
			colourLabel = Colour.GreyLabel;
		else switch (Current)
		{
			case Home.Hand:
				break;
			default:
				colourLabel = Current.ToString();
				break;
		}
	}
	[HarmonyPostfix]
	[HarmonyPatch(typeof(CameraController), nameof(CameraController.DelayResetCameraRotation))]
	static void DelayResetCameraRotationPostfix(CameraController __instance, CameraState __state)
	{
		if (__state != null)
			__instance.CameraStates[0] = __state;
	}
}

