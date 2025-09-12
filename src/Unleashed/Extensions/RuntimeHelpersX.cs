using System.Runtime.CompilerServices;

namespace Unleashed.Extensions;

static class RuntimeHelpersX
{
	extension(RuntimeHelpers)
	{
		// #idea: not recognized by compiler?
		public static T[] GetSubArray<T>(T[] array, Range range)
		{
			if (array is null)
				throw new ArgumentNullException(nameof(array));

			var (offset, length) = range.GetOffsetAndLength(array.Length);

			var dest = (T[]) Array.CreateInstance(array.GetType().GetElementType(), length);
			Array.Copy(array, offset, dest, 0, length);
			return dest;
		}
	}
}

