using System.Runtime.CompilerServices;

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
		public Transform CopyParent(GameObject other) =>
			@this.CopyParent(other.transform);
		public Transform CopyParent(Component other) =>
			@this.CopyParent(other.transform);
		public Transform CopyParent(Transform src) =>
			@this.transform.CopyParent(src);

		public T CopyComponent<T>(GameObject other) where T : Component =>
			@this.CopyComponent(other.GetComponent<T>());
		public T CopyComponent<T>(Component other) where T : Component =>
			@this.CopyComponent(other.GetComponent<T>());
		[OverloadResolutionPriority(1)]
		public T CopyComponent<T>(T source) where T : Component =>
			@this.AddComponent<T>().Copy(source);
	}
	extension(Transform @this)
	{
		public Transform CopyParent(Transform source)
		{
			@this.SetParent(source.parent, false);
			@this.localPosition    = source.localPosition;
			@this.localRotation    = source.localRotation;
			@this.localScale       = source.localScale;
			@this.gameObject.layer = source.gameObject.layer;
			return @this;
		}
	}
	extension<T>(T @this) where T : Component
	{
		[System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0030:Do not use banned APIs", Justification = "<Pending>")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "<Pending>")]
		public T Copy(T source)
		{
			@this = @this.GetCopyOf(source);
			switch ((@this, source))
			{
				case (UISprite dst, UISprite src):
					dst.type = src.type;
					dst.SetDimensions(src.width, src.height);
					break;
				case (UIButton dst, UIButton src):
					if (src.tweenTarget) // #todo:
						dst.tweenTarget = dst.gameObject;
					dst.OnInit();
					dst.hover   = src.hover;
					dst.pressed = src.pressed;
					break;
				case (UILabel dst, UILabel src):
					dst.color    = src.color;
					dst.fontSize = src.fontSize;
					break;
			}
			return @this;
		}
	}
}

