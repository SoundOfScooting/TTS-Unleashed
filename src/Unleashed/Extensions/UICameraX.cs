namespace Unleashed.Extensions;

static class UICameraTouch
{
	// #want: extension const
	public const int
		LEFT   = -1,
		RIGHT  = -2,
		MIDDLE = -3;
	public const int
		UNITY_LEFT   = 0,
		UNITY_RIGHT  = 1,
		UNITY_MIDDLE = 2;
}
static class UICameraX
{
	extension(UICamera)
	{
		public static void SpoofOnClick(GameObject go, int touchID = UICameraTouch.LEFT)
		{
			Swap(ref UICamera.currentTouchID, ref touchID);
				UICamera.Notify(go, "OnClick", null);
			Swap(ref UICamera.currentTouchID, ref touchID);
		}
	}
}

