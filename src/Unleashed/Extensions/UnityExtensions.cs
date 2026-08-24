namespace Unleashed.Extensions;

static class UnityExtensions
{
	extension(Component @this)
	{
		public T GetOrAddComponent<T>() where T : Component
			=> @this.gameObject.GetOrAddComponent<T>();
	}
	extension(LineRenderer @this)
	{
		public Vector3[] GetPositions()
		{
			var positions = new Vector3[@this.positionCount];
			@this.GetPositions(positions);
			return positions;
		}
	}
}

