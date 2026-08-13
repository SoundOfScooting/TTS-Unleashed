namespace Unleashed.Extensions;

public static class EnumX
{
	extension(Enum)
	{
		public static string Format<TEnum>(TEnum value, string format) where TEnum: struct, Enum
			=> Enum.Format(typeof(TEnum), value, format);
		public static string Format<TEnum>(object value, string format) where TEnum: struct, Enum
			=> Enum.Format(typeof(TEnum), value, format);
		public static string GetName<TEnum>(TEnum value) where TEnum: struct, Enum
			=> Enum.GetName(typeof(TEnum), value);
		public static string GetName<TEnum>(object value) where TEnum: struct, Enum
			=> Enum.GetName(typeof(TEnum), value);
		public static Type GetUnderlyingType<TEnum>() where TEnum: struct, Enum
			=> Enum.GetUnderlyingType(typeof(TEnum));
		public static TEnum[] GetValues<TEnum>() where TEnum: struct, Enum
			=> (TEnum[]) Enum.GetValues(typeof(TEnum));
		public static bool IsDefined<TEnum>(TEnum value) where TEnum: struct, Enum
			=> Enum.IsDefined(typeof(TEnum), value);
		public static bool IsDefined<TEnum>(object value) where TEnum: struct, Enum
			=> Enum.IsDefined(typeof(TEnum), value);
		public static TEnum ToObject<TEnum>(ulong value) where TEnum: struct, Enum
			=> (TEnum) Enum.ToObject(typeof(TEnum), value);
		// [CLSCompliant(false)]
		public static TEnum ToObject<TEnum>(uint value) where TEnum: struct, Enum
			=> (TEnum) Enum.ToObject(typeof(TEnum), value);
		// [CLSCompliant(false)]
		public static TEnum ToObject<TEnum>(ushort value) where TEnum: struct, Enum
			=> (TEnum) Enum.ToObject(typeof(TEnum), value);
		// [CLSCompliant(false)]
		public static TEnum ToObject<TEnum>(sbyte value) where TEnum: struct, Enum
			=> (TEnum) Enum.ToObject(typeof(TEnum), value);
		public static TEnum ToObject<TEnum>(object value) where TEnum: struct, Enum
			=> (TEnum) Enum.ToObject(typeof(TEnum), value);
		public static TEnum ToObject<TEnum>(long value) where TEnum: struct, Enum
			=> (TEnum) Enum.ToObject(typeof(TEnum), value);
		public static TEnum ToObject<TEnum>(int value) where TEnum: struct, Enum
			=> (TEnum) Enum.ToObject(typeof(TEnum), value);
		public static TEnum ToObject<TEnum>(byte value) where TEnum: struct, Enum
			=> (TEnum) Enum.ToObject(typeof(TEnum), value);
		public static TEnum ToObject<TEnum>(short value) where TEnum: struct, Enum
			=> (TEnum) Enum.ToObject(typeof(TEnum), value);
	}
}

