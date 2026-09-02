using System.Runtime.CompilerServices;

namespace Unleashed.Patches;

[HarmonyPatch]
class GUIEndTurnX : UZMonoBehaviour
{
	[ModuleInitializer]
	internal static void ModuleInitializer()
		=> Events.OnStartConnected += OnStartConnected;
	static void OnStartConnected()
		=> NetworkUI.Instance.GUIEndTurn.GetOrAddComponent<GUIEndTurnX>();

	[HarmonyILManipulator]
	[HarmonyPatch(typeof(Turns), nameof(Turns.GUIEndTurn))]
	static void GUIEndTurnIL(ILContext il)
	{
		var c = new ILCursor(il);
		c.GotoNext(MoveType.Before,
			x => x.MatchCall(() => Network.IsServer)
		);
		c.Next.OperandAsGetter = () => Network.IsAdmin;
	}

	void OnAltClick()
	{
		if (!Network.IsAdmin && !Turns.Instance.TurnsState.PassTurns)
			return;
		Turns.Instance.TurnsState.Reverse ^= true;
			UICamera.SpoofOnClick(gameObject);
		Turns.Instance.TurnsState.Reverse ^= true;
	}
}
[HarmonyPatch]
sealed class UIStarTurnX : GUIEndTurnX
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(UIStarTurn), nameof(UIStarTurn.Awake))]
	static void AwakePostfix(UIStarTurn __instance)
		=> __instance.GetOrAddComponent<UIStarTurnX>();

	[HarmonyPrefix]
	[HarmonyPatch(typeof(UIStarTurn), nameof(UIStarTurn.OnClick))]
	static bool OnClickReplace()
	{
		if (Network.IsAdmin)
			Turns.Instance.GUIEndTurn();
		return false;
	}

	void Awake()
		=> EventManager.OnPlayerPromoted += OnPlayerPromoted;
	void OnDestroy()
		=> EventManager.OnPlayerPromoted -= OnPlayerPromoted;

	void OnPlayerPromoted(bool isPromoted, int id)
	{
		if (id == Network.ID)
			GetComponent<UITooltipObject>().Tooltip = isPromoted
				? "Turn (Click to skip)"
				: "Turn";
	}
}

