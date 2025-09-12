namespace Unleashed.Extensions;

static class GameObjectX
{
	extension(LineRenderer @this)
	{
		public Vector3[] GetPositions()
		{
			var positions = new Vector3[@this.positionCount];
			@this.GetPositions(positions);
			return positions;
		}
	}

	extension(GameObject @this)
	{
		public void AssignLocalTRS(GameObject src)
		{
			@this.transform.parent        = src.transform.parent;
			@this.transform.localPosition = src.transform.localPosition;
			@this.transform.localRotation = src.transform.localRotation;
			@this.transform.localScale    = src.transform.localScale;
			@this.layer = src.layer;
		}
	}

	extension(GameObject go)
	{
		/// <summary>
		/// Wrapper around <see cref="global::ExtensionMethods.AddComponent{T}(GameObject, T)"/> due to inadequate <see cref="global::ExtensionMethods.GetCopyOf{T}(Component, T)"/>
		/// </summary>
		public UISprite AddComponent(UISprite toAdd)
		{
			var comp  = go.AddComponent<UISprite>(toAdd);
			comp.type = toAdd.type;
			comp.SetDimensions(toAdd.width, toAdd.height);
			return comp;
		}
		/// <summary>
		/// Wrapper around <see cref="global::ExtensionMethods.AddComponent{T}(GameObject, T)"/> due to inadequate <see cref="global::ExtensionMethods.GetCopyOf{T}(Component, T)"/>
		/// </summary>
		public UIButton AddComponent(UIButton toAdd)
		{
			var comp = go.AddComponent<UIButton>(toAdd);
			if (toAdd.tweenTarget)
				comp.tweenTarget = go;
			comp.OnInit();
			comp.hover   = toAdd.hover;
			comp.pressed = toAdd.pressed;
			return comp;
		}
		/// <summary>
		/// Wrapper around <see cref="global::ExtensionMethods.AddComponent{T}(GameObject, T)"/> due to inadequate <see cref="global::ExtensionMethods.GetCopyOf{T}(Component, T)"/>
		/// </summary>
		public UILabel AddComponent(UILabel toAdd)
		{
			var comp      = go.AddComponent<UILabel>(toAdd);
			comp.color    = toAdd.color;
			comp.fontSize = toAdd.fontSize;
			return comp;
		}
	}
}

