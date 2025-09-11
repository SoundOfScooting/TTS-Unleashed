using System.Globalization;
using System.Reflection;

namespace Unleashed
{
	public static class Extensions
	{
		public static void Deconstruct<T>(this T @this, out T @out) => @out = @this;

		public static bool IsBlinded(this PlayerManager @this, int ID) =>
			@this.PlayersDictionary.TryGetValue(Compat.PlayerID(ID), out var playerState) &&
			playerState.blind;

		// public static bool GetButtonChanged(this zInput @null, string ButtonName, ControlType CT = ControlType.All) =>
		// 	zInput.GetButtonDown(ButtonName, CT) || zInput.GetButtonUp(ButtonName, CT);
	}

	public readonly record struct Comparison(int Value) // #net10: open enum + extensions?
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

	public static class UICameraTouch
	{
		public const int
			LEFT   = -1,
			RIGHT  = -2,
			MIDDLE = -3;
		public const int
			UNITY_LEFT   = 0,
			UNITY_RIGHT  = 1,
			UNITY_MIDDLE = 2;
	}

	public static class ActivatorX
	{
		public static T CreateInstance<T>(bool nonPublic) =>
			(T) Activator.CreateInstance(typeof(T), nonPublic);
		public static T CreateInstance<T>(params object[] args) =>
			(T) Activator.CreateInstance(typeof(T), args);
		public static T CreateInstance<T>(object[] args, object[] activationAttributes) =>
			(T) Activator.CreateInstance(typeof(T), args, activationAttributes);
		public static T CreateInstance<T>(BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture) =>
			(T) Activator.CreateInstance(typeof(T), bindingAttr, binder, args, culture);
		public static T CreateInstance<T>(BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes) =>
			(T) Activator.CreateInstance(typeof(T), bindingAttr, binder, args, culture, activationAttributes);
	}

	public static class EnumX // partially implemented in .NET 5
	{
		public static string Format<TEnum>(TEnum value, string format) where TEnum: struct, Enum =>
			Enum.Format(typeof(TEnum), value, format);
		public static string Format<TEnum>(object value, string format) where TEnum: struct, Enum =>
			Enum.Format(typeof(TEnum), value, format);
		public static string GetName<TEnum>(TEnum value) where TEnum: struct, Enum =>
			Enum.GetName(typeof(TEnum), value);
		public static string GetName<TEnum>(object value) where TEnum: struct, Enum =>
			Enum.GetName(typeof(TEnum), value);
		public static string[] GetNames<TEnum>() where TEnum: struct, Enum =>
			Enum.GetNames(typeof(TEnum));
		public static Type GetUnderlyingType<TEnum>() where TEnum: struct, Enum =>
			Enum.GetUnderlyingType(typeof(TEnum));
		// public static Array GetValues<TEnum>() where TEnum: struct, Enum =>
		// 	Enum.GetValues(typeof(TEnum));
		public static TEnum[] GetValues<TEnum>() where TEnum: struct, Enum =>
			(TEnum[]) Enum.GetValues(typeof(TEnum));
		public static bool IsDefined<TEnum>(TEnum value) where TEnum: struct, Enum =>
			Enum.IsDefined(typeof(TEnum), value);
		public static bool IsDefined<TEnum>(object value) where TEnum: struct, Enum =>
			Enum.IsDefined(typeof(TEnum), value);
		public static TEnum Parse<TEnum>(string value) where TEnum: struct, Enum =>
			(TEnum) Enum.Parse(typeof(TEnum), value);
		public static TEnum Parse<TEnum>(string value, bool ignoreCase) where TEnum: struct, Enum =>
			(TEnum) Enum.Parse(typeof(TEnum), value, ignoreCase);
		// [CLSCompliant(false)]
		public static TEnum ToObject<TEnum>(ulong value) where TEnum: struct, Enum =>
			(TEnum) Enum.ToObject(typeof(TEnum), value);
		// [CLSCompliant(false)]
		public static TEnum ToObject<TEnum>(uint value) where TEnum: struct, Enum =>
			(TEnum) Enum.ToObject(typeof(TEnum), value);
		// [CLSCompliant(false)]
		public static TEnum ToObject<TEnum>(ushort value) where TEnum: struct, Enum =>
			(TEnum) Enum.ToObject(typeof(TEnum), value);
		// [CLSCompliant(false)]
		public static TEnum ToObject<TEnum>(sbyte value) where TEnum: struct, Enum =>
			(TEnum) Enum.ToObject(typeof(TEnum), value);
		public static TEnum ToObject<TEnum>(object value) where TEnum: struct, Enum =>
			(TEnum) Enum.ToObject(typeof(TEnum), value);
		public static TEnum ToObject<TEnum>(long value) where TEnum: struct, Enum =>
			(TEnum) Enum.ToObject(typeof(TEnum), value);
		public static TEnum ToObject<TEnum>(int value) where TEnum: struct, Enum =>
			(TEnum) Enum.ToObject(typeof(TEnum), value);
		public static TEnum ToObject<TEnum>(byte value) where TEnum: struct, Enum =>
			(TEnum) Enum.ToObject(typeof(TEnum), value);
		public static TEnum ToObject<TEnum>(short value) where TEnum: struct, Enum =>
			(TEnum) Enum.ToObject(typeof(TEnum), value);
	}
}

// namespace Unleashed
// {
// 	static class RuntimeHelpersX
// 	{
// 		public static T[] GetSubArray<T>(T[] array, Range range)
// 		{
// 			if (array is null)
// 				throw new ArgumentNullException(nameof(array));
// 			var (offset, length) = range.GetOffsetAndLength(array.Length);
// 			var dest = (T[]) Array.CreateInstance(array.GetType().GetElementType(), length);
// 			Array.Copy(array, offset, dest, 0, length);
// 			return dest;
// 		}
// 	}
// }

