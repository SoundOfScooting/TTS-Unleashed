namespace Unleashed.Context;

static class Contextual
{
	public const string LABEL_PADDING = "            ";

	public static void Check(GameObject gameObject, Func<bool> active)
	{
		gameObject.SetActive(false);
		if (active())
			Pointer.MyPointer.SetActive(gameObject, true);
	}
}

