namespace Unleashed.GridMenu;

[HarmonyPatch]
static class SpawnName
{
	public const string SETUP_CARD = nameof(CardManager.SetupCard);
	public const string SET_OBJECT = nameof(NetPhysObject.SetObject);

	[HarmonyPrefix]
	[HarmonyPatch(typeof(GameMode), nameof(GameMode.SpawnName))]
	static bool SpawnNamePrefix(GameMode __instance, string ObjectName, Vector3 SpawnPos, bool bLocalSpawn, ref GameObject __result)
	{
		// #gold
		if (!bLocalSpawn && Network.IsClient)
			return true;

		var parts = ObjectName.Split(['/'], StringSplitOptions.RemoveEmptyEntries);
		if (parts is not [ Main.PLUGIN_GUID, var prefabName, .. ])
			return true;

		var prefab = __instance.GetPrefab(prefabName);
		if (prefab is null)
			return true;

		__result = !bLocalSpawn
			? Network           .Instantiate(prefab, SpawnPos, prefab.transform.rotation)
			: UnityEngine.Object.Instantiate(prefab, SpawnPos, prefab.transform.rotation);

		using var rest = parts.Skip(2).GetEnumerator();
		while  (rest.MoveNext())
		switch (rest.Current)
		{
			default: return false;
			case SETUP_CARD:
				if (!rest.MoveNext() || !int.TryParse(rest.Current, out var front_id))
					return false;
				if (front_id != -1)
				{
					CardManager.Instance.SetupCard(__result, front_id);
					__result.GetComponent<CardObject>().card_id_ = front_id;
				}
				break;
			case SET_OBJECT:
				if (!rest.MoveNext() || !bool.TryParse(rest.Current, out var bAltSounds) ||
					!rest.MoveNext() || !int .TryParse(rest.Current, out var MeshInt) ||
					!rest.MoveNext() || !int .TryParse(rest.Current, out var MatInt)
				) return false;

				var npo = __result.GetNPO();
				npo.UseAltSounds = bAltSounds;
				if (npo.MeshChange && MeshInt != -1)
					npo.MeshChange.SetMesh(MeshInt);
				if (npo.MaterialChange && MatInt != -1)
					npo.MaterialChange.SetMaterial(MatInt);
				break;
		}
		return false;
	}
}

