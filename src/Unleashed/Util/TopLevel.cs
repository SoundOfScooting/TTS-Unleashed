using System.Runtime.CompilerServices;

namespace Unleashed.Util;

// #issue: https://github.com/dotnet/csharplang/issues/9803
static class TopLevel
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Swap<T>(ref T loc1, ref T loc2)
		=> (loc1, loc2) = (loc2, loc1);
}

