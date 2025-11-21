namespace Unleashed.Patches;

[HarmonyPatch]
sealed class UZCameraHome : MonoBehaviour
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

	[HarmonyPostfix]
	[HarmonyPatch(typeof(UITopBar), nameof(UITopBar.Awake))]
	static void AwakePostfix() =>
		_ = new GameObject("02 CameraHome", typeof(UZCameraHome));
	void Awake()
	{
		CreateComponents();
		Instance = this;

		// Current = (Home) PlayerPrefs.GetInt(HomePref, (int) Value.Hand);
		Current = Home.Hand;
	}
	void CreateComponents()
	{
		var @base = Resources
			.FindObjectsOfTypeAll<UIPointerRotationSnap>()
			.FirstOrDefault();

		gameObject.CopyParent(@base).localPosition += new Vector3(56f, 0f, 0f);
		gameObject.CopyComponent<BoxCollider2D>(@base);
		gameObject.CopyComponent<UISprite>(@base);
		gameObject.CopyComponent<UIButton>(@base);
		gameObject.AddComponent <UITooltipScript>().Tooltip = "Camera Home";
		gameObject.AddComponent <TweenColor>();
		// I2.Loc.Localize

		var labelObject = new GameObject(@base.ThisLabelObject.name);
		labelObject.CopyParent(@base.ThisLabelObject);
		labelObject.transform.parent        = transform;
		labelObject.transform.localPosition = new(0f, -2f, 0f);
		Label = labelObject.CopyComponent<UILabel>(@base.ThisLabelObject);
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
			leftButtonFunc: (label, _) =>
				Current = Enum.Parse<Home>(label),

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
			colourLabel = "Grey";
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

