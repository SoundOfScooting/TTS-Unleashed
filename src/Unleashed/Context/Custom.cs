namespace Unleashed.Context;

[HarmonyPatch]
static class Custom
{
	[HarmonyILManipulator]
	[HarmonyPatch(typeof(Pointer), nameof(Pointer.StartContextual))]
	static void StartContextualIL(ILContext il)
	{
		var c = new ILCursor(il);
		c.GotoNext(MoveType.After,
			// SetActive(NetworkInstance.GUIContextualCustom, Network.IsServer && (bool)InfoObject.GetComponent<CustomObject>());
			x => x.MatchLdfld(AccessTools.Field(typeof(NetworkUI), nameof(NetworkUI.GUIContextualCustom))),
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(Network), nameof(Network.IsServer)))
		);
		c.Previous.Operand = AccessTools.PropertyGetter(typeof(Network), nameof(Network.IsAdmin));
	}

	[HarmonyILManipulator]
	[HarmonyPatch(typeof(CustomAssetbundle),  nameof(CustomAssetbundle .bCustomUI), MethodType.Setter)]
	[HarmonyPatch(typeof(CustomCard),         nameof(CustomCard        .bCustomUI), MethodType.Setter)]
	[HarmonyPatch(typeof(CustomDeck),         nameof(CustomDeck        .bCustomUI), MethodType.Setter)]
	[HarmonyPatch(typeof(CustomDice),         nameof(CustomDice        .bCustomUI), MethodType.Setter)]
	[HarmonyPatch(typeof(CustomImage),        nameof(CustomImage       .bCustomUI), MethodType.Setter)]
	[HarmonyPatch(typeof(CustomJigsawPuzzle), nameof(CustomJigsawPuzzle.bCustomUI), MethodType.Setter)] // #todo: fully support
	[HarmonyPatch(typeof(CustomMesh),         nameof(CustomMesh        .bCustomUI), MethodType.Setter)]
	[HarmonyPatch(typeof(CustomPDF),          nameof(CustomPDF         .bCustomUI), MethodType.Setter)] // #todo: fully support
	[HarmonyPatch(typeof(CustomSky),          nameof(CustomSky         .bCustomUI), MethodType.Setter)]
	[HarmonyPatch(typeof(CustomTile),         nameof(CustomTile        .bCustomUI), MethodType.Setter)]
	[HarmonyPatch(typeof(CustomToken),        nameof(CustomToken       .bCustomUI), MethodType.Setter)]
	static void AllowAdminIL(ILContext il)
	{
		var c = new ILCursor(il);
		c.GotoNext(MoveType.Before,
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(Network), nameof(Network.IsServer)))
		);
		c.Next.Operand     = AccessTools.PropertyGetter(typeof(Network), nameof(Network.IsAdmin));
	}
	// #generic
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomObject<MonoBehaviour>), nameof(UICustomObject<>.CheckUpdateMatchingCustomObjects))]
	static bool CheckUpdateMatchingCustomObjectsPrefix() =>
		Network.IsServer;

	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomAssetbundle), nameof(UICustomAssetbundle.Import))]
	static bool AssetbundleImportPrefix(UICustomAssetbundle __instance, bool __runOriginal)
	{
		if (!__runOriginal || API.IsServer)
			return __runOriginal;
		__instance.CustomAssetbundleURL          = __instance.CustomAssetbundleURL         .Trim();
		__instance.CustomAssetbundleSecondaryURL = __instance.CustomAssetbundleSecondaryURL.Trim();
		if (string.IsNullOrEmpty(__instance.CustomAssetbundleURL))
		{
			Chat.LogError("You must supply a custom assetbundle URL.");
			return false;
		}
		Lua.Execute(
			$$"""
			local obj = {{ __instance.TargetCustomObject.NPO }}
			if obj then
				obj.setCustomObject({
					assetbundle           = {{ __instance.CustomAssetbundleURL }},
					assetbundle_secondary = {{ __instance.CustomAssetbundleSecondaryURL }},
					type                  = {{ __instance.TypeInt }},
					material              = {{ __instance.MaterialInt }},
				})
				obj.reload()
			end
			"""
		);
		__instance.Close();
		return false;
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomCard), nameof(UICustomCard.Import))]
	static bool CardImportPrefix(UICustomCard __instance, bool __runOriginal)
	{
		if (!__runOriginal || API.IsServer)
			return __runOriginal;
		__instance.URLFace = __instance.URLFace.Trim();
		__instance.URLBack = __instance.URLBack.Trim();
		if (string.IsNullOrEmpty(__instance.URLFace))
		{
			Chat.LogError("You must supply a face image URL.");
			return false;
		}
		if (string.IsNullOrEmpty(__instance.URLBack))
		{
			Chat.LogError("You must supply a back image URL.");
			return false;
		}
		Lua.Execute(
			$$"""
			local obj = {{ __instance.TargetCustomObject.NPO }}
			if obj then
				obj.setCustomObject({
					face     = {{ __instance.URLFace }},
					back     = {{ __instance.URLBack }},
					sideways = {{ __instance.bSideways }},
					type     = {{ __instance.TypePopupList.items.IndexOf(__instance.TypePopupList.value) }},
				})
				obj.reload()
			end
			"""
		);
		__instance.Close();
		return false;
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomDeck), nameof(UICustomDeck.Import))]
	static bool DeckImportPrefix(UICustomDeck __instance, bool __runOriginal)
	{
		if (!__runOriginal || API.IsServer)
			return __runOriginal;
		__instance.URLFace = __instance.URLFace.Trim();
		__instance.URLBack = __instance.URLBack.Trim();
		if (string.IsNullOrEmpty(__instance.URLFace))
		{
			Chat.LogError("You must supply a face image URL.");
			return false;
		}
		if (string.IsNullOrEmpty(__instance.URLBack))
		{
			Chat.LogError("You must supply a back image URL.");
			return false;
		}
		Lua.Execute(
			$$"""
			local obj = {{ __instance.TargetCustomObject.NPO }}
			if obj then
				obj.setCustomObject({
					face           = {{ __instance.URLFace }},
					unique_back    = {{ __instance.bUniqueBacks }},
					back           = {{ __instance.URLBack }},
					width          = {{ __instance.WidthRange .intValue }},
					height         = {{ __instance.HeightRange.intValue }},
					number         = {{ __instance.NumberRange.intValue }},
					sideways       = {{ __instance.bSideways }},
					back_is_hidden = {{ __instance.bBackIsHidden }},
					type           = {{ __instance.TypePopupList.items.IndexOf(__instance.TypePopupList.value) }},
				})
				obj.reload()
			end
			"""
		);
		__instance.Close();
		return false;
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomDice), nameof(UICustomDice.Import))]
	static bool DiceImportPrefix(UICustomDice __instance, bool __runOriginal)
	{
		if (!__runOriginal || API.IsServer)
			return __runOriginal;
		__instance.CustomImageURL = __instance.CustomImageURL.Trim();
		if (string.IsNullOrEmpty(__instance.CustomImageURL))
		{
			Chat.LogError("You must supply a custom image URL.");
			return false;
		}
		Lua.Execute(
			$$"""
			local obj = {{ __instance.TargetCustomObject.NPO }}
			if obj then
				obj.setCustomObject({
					image = {{ __instance.CustomImageURL }},
					type  = {{ __instance.TypeInt }},
				})
				obj.reload()
			end
			"""
		);
		__instance.Close();
		return false;
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomImage), nameof(UICustomImage.Import))]
	static bool ImageImportPrefix(UICustomImage __instance, bool __runOriginal)
	{
		if (!__runOriginal || API.IsServer)
			return __runOriginal;
		__instance.CustomImageURL = __instance.CustomImageURL.Trim();
		if (string.IsNullOrEmpty(__instance.CustomImageURL))
		{
			Chat.LogError("You must supply a custom image URL.");
			return false;
		}
		Lua.Execute(
			__instance.TargetCustomObject.gameObject == ManagerPhysicsObject.Instance.Table
				? (Lua.Template) $"""
				Tables.setCustomURL({ __instance.CustomImageURL })
				"""
				: (Lua.Template) $$"""
				local obj = {{ __instance.TargetCustomObject.NPO }}
				if obj then
					obj.setCustomObject({
						image = {{ __instance.CustomImageURL }},
					})
					obj.reload()
				end
				"""
		);
		__instance.Close();
		return false;
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomImageDouble), nameof(UICustomImageDouble.Import))]
	static bool ImageDoubleImportPrefix(UICustomImageDouble __instance, bool __runOriginal)
	{
		if (!__runOriginal || API.IsServer)
			return __runOriginal;
		__instance.CustomImageURL = __instance.CustomImageURL.Trim();
		// #bug: (base game) CustomImageSecondaryURL is not trimmed
		if (string.IsNullOrEmpty(__instance.CustomImageURL))
		{
			Chat.LogError("You must supply a custom image URL.");
			return false;
		}
		Lua.Execute(
			$$"""
			local obj = {{ __instance.TargetCustomObject.NPO }}
			if obj then
				obj.setCustomObject({
					image           = {{ __instance.CustomImageURL }},
					image_secondary = {{ __instance.CustomImageSecondaryURL }},
					image_scalar    = {{ __instance.CustomImageScalar }},
				})
				obj.reload()
			end
			"""
		);
		__instance.Close();
		return false;
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomMesh), nameof(UICustomMesh.Import))]
	static bool MeshImportPrefix(UICustomMesh __instance, bool __runOriginal)
	{
		if (!__runOriginal || API.IsServer)
			return __runOriginal;
		__instance.MeshURL     = __instance.MeshURL    .Trim();
		__instance.DiffuseURL  = __instance.DiffuseURL .Trim();
		__instance.NormalURL   = __instance.NormalURL  .Trim();
		__instance.ColliderURL = __instance.ColliderURL.Trim();
		if (string.IsNullOrEmpty(__instance.MeshURL))
		{
			Chat.LogError("You must supply a model URL to create a custom model.");
			return false;
		}
		Lua.Execute(
			$$"""
			local obj = {{ __instance.TargetCustomObject.NPO }}
			if obj then
				obj.setCustomObject({
					mesh     = {{ __instance.MeshURL }},
					diffuse  = {{ __instance.DiffuseURL }},
					normal   = {{ __instance.NormalURL }},
					collider = {{ __instance.ColliderURL }},
					convex   = {{ !__instance.NonConvex }},
					type     = {{ __instance.TypeIndex }},
					material = {{ __instance.MaterialIndex }},
					specular_intensity = {{ __instance.CustomShader.SpecularIntensity }},
					specular_color     = {
						r = {{ __instance.CustomShader.SpecularColor.r }},
						g = {{ __instance.CustomShader.SpecularColor.g }},
						b = {{ __instance.CustomShader.SpecularColor.b }},
						a = {{ __instance.CustomShader.SpecularColor.a ?? 1 }},
					},
					specular_sharpness = {{ __instance.CustomShader.SpecularSharpness }},
					fresnel_strength   = {{ __instance.CustomShader.FresnelStrength   }},
					cast_shadows = {{ __instance.CastShadows }},
				})
				obj.reload()
			end
			"""
		);
		__instance.Close();
		return false;
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomSky), nameof(UICustomSky.Import))]
	static bool SkyImportPrefix(UICustomSky __instance, bool __runOriginal)
	{
		if (!__runOriginal || API.IsServer)
			return __runOriginal;
		__instance.CustomImageURL = __instance.CustomImageURL.Trim();
		if (string.IsNullOrEmpty(__instance.CustomImageURL))
		{
			Chat.LogError("You must supply a custom image URL.");
			return false;
		}
		Lua.Execute(
			$"""
			Backgrounds.setCustomURL({ __instance.CustomImageURL })
			"""
		);
		__instance.Close();
		return false;
	}

	// #issue https://tabletopsimulator.nolt.io/2904
	[HarmonyPostfix]
	[HarmonyPatch(typeof(UICustomTile), nameof(UICustomTile.OnEnable))]
	static void TileStartPostfix(UICustomTile __instance) =>
		__instance.StretchToggle.GetComponent<BoxCollider2D>().enabled = API.HostModded;
	[HarmonyPostfix]
	[HarmonyPatch(typeof(LuaObject), nameof(LuaObject.GetCustomObject))]
	static void GetCustomObjectPostfix(LuaObject __instance, ref MoonSharp.Interpreter.Table __result)
	{
		if (__instance.NPO.CustomImage && __instance.NPO.CustomTile)
			__result["stretch"] = __instance.NPO.CustomTile.bStretch;
	}
	[HarmonyPostfix]
	[HarmonyPatch(typeof(LuaObject), nameof(LuaObject.SetCustomObject))]
	static void SetCustomObjectPostfix(LuaObject __instance, MoonSharp.Interpreter.Table Params)
	{
		if (Params == null)
			return;
		if (__instance.NPO.CustomImage && __instance.NPO.CustomTile)
		{
			if (Params["stretch"] != null && bool.TryParse(Params["stretch"].ToString(), out var bStretch))
				__instance.NPO.CustomTile.bStretch = bStretch;
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomTile), nameof(UICustomTile.Import))]
	static bool TileImportPrefix(UICustomTile __instance, bool __runOriginal)
	{
		if (!__runOriginal || API.IsServer)
			return __runOriginal;
		__instance.CustomImageURL = __instance.CustomImageURL.Trim();
		if (string.IsNullOrEmpty(__instance.CustomImageURL))
		{
			Chat.LogError("You must supply a custom image URL.");
			return false;
		}
		Lua.Execute(
			$$"""
			local obj = {{ __instance.TargetCustomObject.NPO }}
			if obj then
				obj.setCustomObject({
					image        = {{ __instance.CustomImageURL }},
					image_bottom = {{ __instance.CustomImageSecondaryURL }},
					type         = {{ __instance.TypeInt }},
					thickness    = {{ __instance.ThicknessSlider.value * 0.9f + 0.1f }},
					stackable    = {{ __instance.StackableToggle.value }},
					stretch      = {{ __instance.StretchToggle  .value }}, -- modded
				})
				obj.reload()
			end
			"""
		);
		__instance.Close();
		return false;
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(UICustomToken), nameof(UICustomToken.Import))]
	static bool TokenImportPrefix(UICustomToken __instance, bool __runOriginal)
	{
		if (!__runOriginal || API.IsServer)
			return __runOriginal;
		__instance.CustomImageURL = __instance.CustomImageURL.Trim();
		if (string.IsNullOrEmpty(__instance.CustomImageURL))
		{
			Chat.LogError("You must supply a custom image URL.");
			return false;
		}
		Lua.Execute(
			$$"""
			local obj = {{ __instance.TargetCustomObject.NPO }}
			if obj then
				obj.setCustomObject({
					image          = {{ __instance.CustomImageURL }},
					thickness      = {{ __instance.ThicknessSlider    .value * 0.9f + 0.1f }},
					merge_distance = {{ __instance.MergeDistanceSlider.value * 20f  + 5f }},
					stand_up       = {{ __instance.StandupToggle  .value }},
					stackable      = {{ __instance.StackableToggle.value }},
				})
				obj.reload()
			end
			"""
		);
		__instance.Close();
		return false;
	}
}

