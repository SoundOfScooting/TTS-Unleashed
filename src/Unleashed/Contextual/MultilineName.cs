namespace Unleashed.Contextual;

[HarmonyPatch]
static class MultilineName
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(UIContextual), nameof(UIContextual.Awake))]
	static void AwakePostfix(UIContextual __instance)
	{
		if (__instance.nameInput)
		{
			EventDelegate.Add(__instance.nameInput.onChange, __instance.DelayReposition);

			__instance.nameInput.onReturnKey    = UIInput.OnReturnKey.NewLine; // Submit
			__instance.nameInput.characterLimit = 1024; // 2048 == descriptionInput.characterLimit

			var descLabel = __instance.descriptionInput.label;
			var nameLabel = __instance.nameInput       .label;
			nameLabel.maxLineCount   = 8; // 16 == descLabel.maxLineCount
			nameLabel.overflowMethod = UILabel.Overflow.ResizeHeight;
			nameLabel.pivot          = UIWidget.Pivot.Top;
			nameLabel.leftAnchor  .Set(null, descLabel.leftAnchor  .relative, descLabel.leftAnchor  .absolute);
			nameLabel.bottomAnchor.Set(null, descLabel.bottomAnchor.relative, descLabel.bottomAnchor.absolute);
			nameLabel.rightAnchor .Set(null, descLabel.rightAnchor .relative, descLabel.rightAnchor .absolute);
			nameLabel.topAnchor   .Set(nameLabel.transform.parent,         1, descLabel.topAnchor   .absolute);
			nameLabel.ResetAndUpdateAnchors();

			var descSprite = __instance.descriptionInput.GetComponent<UISprite>();
			var nameSprite = __instance.nameInput       .GetComponent<UISprite>();
			nameSprite.SetAnchor(nameLabel.gameObject,
				left:   descSprite.leftAnchor .absolute,
				bottom: descSprite.topAnchor  .absolute * -1,
				right:  descSprite.rightAnchor.absolute,
				top:    descSprite.topAnchor  .absolute
			);
		}
	}
	[HarmonyPostfix]
	[HarmonyPatch(typeof(UIContextual), nameof(UIContextual.OnDestroy))]
	static void OnDestroyPostfix(UIContextual __instance)
	{
		if (__instance.nameInput)
			EventDelegate.Remove(__instance.nameInput.onChange, __instance.DelayReposition);
	}
}

