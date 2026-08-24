namespace Unleashed.Util;

class UZMonoBehaviour : MonoBehaviour
{
	public static void Destroy(IEnumerable<UnityEngine.Object> objs)
	{
		foreach (var obj in objs)
			Destroy(obj);
	}

	public static T InstantiateX<T>(T @base, Transform parent = null, bool active = true, string name = null) where T : Component
		=> InstantiateX(@base.gameObject, parent, active, name).GetComponents<T>()[
			Array.IndexOf(@base.GetComponents<T>(), @base)
		];
	public static GameObject InstantiateX(GameObject @base, Transform parent = null, bool active = true, string name = null)
	{
		parent ??= @base.transform.parent;
		var activeSelf = @base.activeSelf;
		if (!active)
			@base.SetActive(false);

		var result = Instantiate(@base, parent);
		if (name is not null)
			result.name = name;

		if (!active)
			@base.SetActive(activeSelf);
		return result;
	}
}

