using UnityEngine.Events;

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

	extension(UnityEvent @this)
	{
		public void operator +=(UnityAction rhs) => @this.AddListener(rhs);
		public void operator -=(UnityAction rhs) => @this.RemoveListener(rhs);
	}
	extension<T0>(UnityEvent<T0> @this)
	{
		public void operator +=(UnityAction<T0> rhs) => @this.AddListener(rhs);
		public void operator -=(UnityAction<T0> rhs) => @this.RemoveListener(rhs);
	}
}

