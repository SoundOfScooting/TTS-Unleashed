namespace Unleashed.Util;

public readonly record struct Comparison(int Delta) // #want: open enum + extensions?
{
	public const int
		LT = -1,
		EQ =  0,
		GT =  1;

	public static implicit operator Comparison (int rhs) => new(rhs);
	public static implicit operator int (Comparison rhs) => rhs.Delta;
	public static implicit operator bool(Comparison rhs) => rhs.Delta is EQ;

	public static bool operator true (Comparison rhs) => rhs;
	public static bool operator false(Comparison rhs) => !rhs;

	public static Comparison operator &(Comparison lhs, Comparison rhs) => !lhs ? lhs : rhs;
	public static Comparison operator |(Comparison lhs, Comparison rhs) =>  lhs ? lhs : rhs;

	public static Comparison? Default<R>(R rhs) =>
		Default(true, rhs is not null);
	public static Comparison? Default<L, R>(L lhs, R rhs) =>
		Default(lhs is not null, rhs is not null);
	public static Comparison? Default(bool lhsExists, bool rhsExists) =>
		(lhsExists, rhsExists) switch {
			(false, false) => EQ,
			(false, true)  => LT,
			(true,  false) => GT,
			(true,  true)  => null,
		};

	public static Comparison Compare<L, R>(L lhs, R rhs) where L : IComparable<R> =>
		Default(lhs, rhs) ?? lhs.CompareTo(rhs);
	public static Comparison Compare<T, U>(T lhs, T rhs, Func<T, U> selector) where U : IComparable<U> =>
		Compare(selector.Invoke(lhs), selector.Invoke(rhs));

	public static Comparison Compare<L, R>(IEnumerable<L> lhs, IEnumerable<R> rhs) where L : IComparable<R>
	{
		using var lhsIter = lhs.GetEnumerator();
		using var rhsIter = rhs.GetEnumerator();
		while (true)
		{
			if (Default(lhsIter.MoveNext(), rhsIter.MoveNext()) is {} @default)
				return @default;
			var cmp = Compare(lhsIter.Current, rhsIter.Current);
			if (!cmp)
				return cmp;
		}
	}
	public static Comparison Compare<T, U>(IEnumerable<T> lhs, IEnumerable<T> rhs, Func<T, U> keySelector) where U : IComparable<U> =>
		Compare(lhs.Select(keySelector), rhs.Select(keySelector));
}

