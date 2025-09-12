namespace Unleashed.Extensions;

static class UICameraX
{
	extension(UICamera)
	{
		public static void SpoofOnClick(GameObject go, int touchID = UICameraTouch.LEFT)
		{
			var      currentTouchID = UICamera.currentTouchID;
			UICamera.currentTouchID = touchID;
			{
				UICamera.Notify(go, "OnClick", null);
			}
			UICamera.currentTouchID = currentTouchID;
		}
	}	
}

