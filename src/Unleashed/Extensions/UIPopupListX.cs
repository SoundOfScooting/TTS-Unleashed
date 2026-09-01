using UnityEngine.Events;

namespace Unleashed.Extensions;

[HarmonyPatch]
static class UIPopupListX
{
	sealed class Data : MonoBehaviour
	{
		public UnityEvent OnPopupListShow = new();
	}
	extension(UIPopupList @this)
	{
		Data Data => @this.GetOrAddComponent<Data>();
		bool HasData() => @this.TryGetComponent<Data>(out _);
		public ref UnityEvent OnPopupListShow => ref @this.Data.OnPopupListShow;
		public void TriggerPopupListShow()
		{
			if (@this.HasData())
				Main.Catch(@this.OnPopupListShow.Invoke);
		}
	}

	[HarmonyILManipulator]
	[HarmonyPatch(typeof(UIPopupList), nameof(UIPopupList.Show))]
	static void ShowIL(ILContext il)
	{
		var c = new ILCursor(il);
		c.GotoNext(MoveType.Before,
			// Singleton<UIPalette>.Instance.SetColours(this, Singleton<UIPalette>.Instance.CurrentThemeColours, instant: true);
			x => x.MatchCall(() => UIPalette.Instance),
			x => x.MatchLdarg(0),
			x => x.MatchCall(() => UIPalette.Instance),
			x => x.MatchLdfld(() => UIPalette.__instance().CurrentThemeColours)
		);
		c.MoveAfterLabels();
		c.Emit(OpCodes.Ldarg_0);
		c.EmitDelegate(
			void(UIPopupList __instance)
				=> __instance.TriggerPopupListShow()
		);
	}
}

