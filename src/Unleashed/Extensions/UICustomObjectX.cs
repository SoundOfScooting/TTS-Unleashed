namespace Unleashed.Extensions;

[HarmonyPatch]
static class UICustomObjectX
{
	public static void QueueFake(this UICustomImage @this, Action<UICustomImage> onImport)
		=> @this.QueueFake<UICustomImage>(onImport);
	public static void QueueFake(this UICustomSky @this, Action<UICustomSky> onImport)
		=> @this.QueueFake<UICustomSky>(onImport);

	sealed class Data : MonoBehaviour // #want: Data<T>, List<Action<T>>
	{
		public readonly List<Delegate> OnImportFakeQueue = [];
		// #todo? onCancel
	}
	extension<T>(T @this) where T : UICustomObject<T>
	{
		Data Data => @this.gameObject.GetOrAddComponent<Data>();
		List<Delegate> OnImportFakeQueue => @this.Data.OnImportFakeQueue;

		public bool TargettingFake()
			=> @this.CustomObjectQueue is [null, ..];

		// overloaded above
		void QueueFake(Action<T> onImport)
		{
			if (@this.OnImportFakeQueue.Contains(onImport))
				return;
			@this.OnImportFakeQueue.Add(onImport);
			@this.CustomObjectQueue.Add(null);
			@this.NumberInQueue      = @this.CustomObjectQueue.Count - 1;
			@this.TargetCustomObject = null;
			@this.gameObject.SetActive(true);
		}

		bool BaseOnEnableFake()
		{
			if (@this.TargettingFake())
			{
				@this.TargetCustomObject = null;
				@this.GetComponent<UIHighlightTargets>().Reset();
				return true;
			}
			return false;
		}

		void BaseCloseFake()
		{
			if (@this.TargettingFake())
				@this.OnImportFakeQueue.RemoveAt(0);
		}

		bool ImportFake()
		{
			if (@this.TargettingFake() && @this.OnImportFakeQueue is [{} onImport, ..])
			{
				onImport.DynamicInvoke(@this);
				return true;
			}
			return false;
		}
	}

	static object Invoke(object @this, string method)
		=> AccessTools.Method(typeof(UICustomObjectX), method)
			.MakeGenericMethod(@this.GetType())
			.Invoke(null, [@this]);

	// #generic
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomObject<MonoBehaviour>), nameof(UICustomObject<>.OnEnable))]
	static bool BaseOnEnablePrefix(object __instance)
		=> !(bool) Invoke(__instance, nameof(BaseOnEnableFake));
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomImage), nameof(UICustomImage.OnEnable))]
	static bool ImageOnEnablePrefix(UICustomImage __instance)
	{
		if (!__instance.BaseOnEnableFake())
			return true;
		__instance.TargetCustomImage = null;
		return false;
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomSky), nameof(UICustomSky.OnEnable))]
	static bool SkyOnEnablePrefix(UICustomSky __instance)
	{
		if (!__instance.BaseOnEnableFake())
			return true;
		__instance.TargetCustomSky = null;
		return false;
	}

	// #generic
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomObject<MonoBehaviour>), nameof(UICustomObject<>.Update))]
	[HarmonyPatch(typeof(UICustomObject<MonoBehaviour>), nameof(UICustomObject<>.CheckUpdateMatchingCustomObjects))]
	static bool BaseUpdatePrefix(object __instance)
		=> !(bool) Invoke(__instance, nameof(TargettingFake));

	// #generic
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomObject<MonoBehaviour>), nameof(UICustomObject<>.Close))]
	static void BaseClosePrefix(object __instance)
		=> Invoke(__instance, nameof(BaseCloseFake));

	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomImage), nameof(UICustomImage.Import))]
	[HarmonyPatch(typeof(UICustomSky),   nameof(UICustomSky  .Import))]
	[HarmonyPriority(Priority.HigherThanNormal)]
	static bool ImportFakePrefix(object __instance)
		=> !(bool) Invoke(__instance, nameof(ImportFake));
}

