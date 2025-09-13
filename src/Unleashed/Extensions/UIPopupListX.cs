namespace Unleashed.Extensions;

[HarmonyPatch]
static class UIPopupListX
{
	sealed class Data : MonoBehaviour
	{
		public Event<Action> OnPopupListShow;
	}
	extension(UIPopupList @this)
	{
		Data Data => @this.gameObject.GetOrAddComponent<Data>();
		bool HasData() => @this.gameObject.TryGetComponent<Data>(out _);
		public ref Event<Action> OnPopupListShow => ref @this.Data.OnPopupListShow;
		public void TriggerPopupListShow()
		{
			if (@this.HasData())
				@this.OnPopupListShow.Trigger?.Invoke();
		}
	}

	[HarmonyILManipulator]
	[HarmonyPatch(typeof(UIPopupList), nameof(UIPopupList.Show))]
	static void ShowIL(ILContext il)
	{
		var c = new ILCursor(il);
		c.GotoNext(MoveType.Before,
			// Singleton<UIPalette>.Instance.InitTheme(this);
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(Singleton<UIPalette>), nameof(Singleton<>.Instance))),
			x => x.MatchLdarg(0),
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(Singleton<UIPalette>), nameof(Singleton<>.Instance))),
			x => x.MatchLdfld(AccessTools.Field(typeof(UIPalette), nameof(UIPalette.CurrentThemeColours)))
		);
		c.MoveAfterLabels();
		c.Emit(OpCodes.Ldarg_0);
		c.EmitDelegate(void(UIPopupList __instance) =>
			__instance.TriggerPopupListShow()
		);
	}
}

