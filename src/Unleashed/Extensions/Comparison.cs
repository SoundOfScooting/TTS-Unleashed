namespace Unleashed.Extensions;

public readonly record struct Comparison(int Value) // #want: open enum + extensions?
{
	public const int
		LT = -1,
		EQ =  0,
		GT =  1;

	public static implicit operator Comparison (int value) => new(value);
	public static implicit operator int (Comparison @this) => @this.Value;
	public static implicit operator bool(Comparison @this) => @this.Value is EQ;

	public static bool operator true (Comparison @this) => @this;
	public static bool operator false(Comparison @this) => !@this;

	public static Comparison operator &(Comparison left, Comparison right) => !left ? left : right;
	public static Comparison operator |(Comparison left, Comparison right) =>  left ? left : right;

	public static Comparison Compare<T, U>(T left, U right) where T : IComparable<U> =>
		(left, right) switch {
			(null,  null)  => EQ,
			(null,  var _) => LT,
			(var _, null)  => GT,
			(var a, var b) => a.CompareTo(b),
		};
}

