using System.Runtime.CompilerServices;

namespace Unleashed.Patches;

[HarmonyPatch]
class GUIEndTurnX : MonoBehaviour
{
	[ModuleInitializer]
	internal static void Initializer() =>
		Events.OnStartConnected += OnStartConnected;
	static void OnStartConnected() =>
		NetworkUI.Instance.GUIEndTurn.GetOrAddComponent<GUIEndTurnX>();

	[HarmonyILManipulator]
	[HarmonyPatch(typeof(Turns), nameof(Turns.GUIEndTurn))]
	static void GUIEndTurnIL(ILContext il)
	{
		var c = new ILCursor(il);
		c.GotoNext(MoveType.Before,
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(Network), nameof(Network.IsServer)))
		);
		c.Next.Operand =     AccessTools.PropertyGetter(typeof(Network), nameof(Network.IsAdmin));
	}

	void OnAltClick()
	{
		if (!Network.IsAdmin/* && !Turns.Instance.turnsState.PassTurns*/)
			return;
		Turns.Instance.TurnsState.Reverse ^= true;
		{
			UICamera.SpoofOnClick(gameObject);
		}
		Turns.Instance.TurnsState.Reverse ^= true;
	}
}
[HarmonyPatch]
sealed class UIStarTurnX : GUIEndTurnX
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(UIStarTurn), nameof(UIStarTurn.Awake))]
	static void AwakePostfix(UIStarTurn __instance) =>
		__instance.gameObject.GetOrAddComponent<UIStarTurnX>();

	[HarmonyPrefix]
	[HarmonyPatch(typeof(UIStarTurn), nameof(UIStarTurn.OnClick))]
	static bool OnClickReplace()
	{
		if (Network.IsAdmin)
			Turns.Instance.GUIEndTurn();
		return false;
	}

	void Awake() =>
		EventManager.OnPlayerPromoted += OnPlayerPromoted;
	void OnDestroy() =>
		EventManager.OnPlayerPromoted -= OnPlayerPromoted;

	void OnPlayerPromoted(bool isPromoted, int id)
	{
		if (id == Network.ID)
			GetComponent<UITooltipObject>().Tooltip = isPromoted
				? "Turn (Click to skip)"
				: "Turn";
	}
}

