using System;
using System.Globalization;
using System.Reflection;

namespace Unleashed
{
	public static class UZUtil
	{
		// https://stackoverflow.com/a/457708
		public static bool TypeSatisfies(Type type, Type toSatisfy)
		{
			while (type != null && type != typeof(object)) {
				var cur = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
				if (toSatisfy == cur)
					return true;
				type = type.BaseType;
			}
			return false;
		}
	}
	public static class Extensions
	{
		public static bool IsBlinded(this PlayerManager @this, int ID) =>
			@this.PlayersDictionary.TryGetValue(Compat.PlayerID(ID), out var playerState) &&
			playerState.blind;

		// public static bool GetButtonChanged(this zInput @null, string ButtonName, ControlType CT = ControlType.All) =>
		// 	zInput.GetButtonDown(ButtonName, CT) || zInput.GetButtonUp(ButtonName, CT);
	}

	public static class Comparison
	{
		public const int
			LT = -1,
			EQ =  0,
			GT =  1;
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
		// public static T CreateInstance<T>() =>
		// 	Activator.CreateInstance<T>();
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
		public static string Format<TEnum>(TEnum  value, string format) =>
			Enum.Format(typeof(TEnum), value, format);
		public static string Format<TEnum>(object value, string format) =>
			Enum.Format(typeof(TEnum), value, format);
		public static string GetName<TEnum>(TEnum  value) =>
			Enum.GetName(typeof(TEnum), value);
		public static string GetName<TEnum>(object value) =>
			Enum.GetName(typeof(TEnum), value);
		public static string[] GetNames<TEnum>() =>
			Enum.GetNames(typeof(TEnum));
		public static Type GetUnderlyingType<TEnum>() =>
			Enum.GetUnderlyingType(typeof(TEnum));
		// public static Array GetValues<TEnum>() =>
		// 	Enum.GetValues(typeof(TEnum));
		public static TEnum[] GetValues<TEnum>() =>
			(TEnum[]) Enum.GetValues(typeof(TEnum));
		public static bool IsDefined<TEnum>(TEnum  value) =>
			Enum.IsDefined(typeof(TEnum), value);
		public static bool IsDefined<TEnum>(object value) =>
			Enum.IsDefined(typeof(TEnum), value);
		public static TEnum Parse<TEnum>(string value) =>
			(TEnum) Enum.Parse(typeof(TEnum), value);
		public static TEnum Parse<TEnum>(string value, bool ignoreCase) =>
			(TEnum) Enum.Parse(typeof(TEnum), value, ignoreCase);
		// [CLSCompliant(false)]
		public static TEnum ToObject<TEnum>(ulong value) =>
			(TEnum) Enum.ToObject(typeof(TEnum), value);
		// [CLSCompliant(false)]
		public static TEnum ToObject<TEnum>(uint value) =>
			(TEnum) Enum.ToObject(typeof(TEnum), value);
		// [CLSCompliant(false)]
		public static TEnum ToObject<TEnum>(ushort value) =>
			(TEnum) Enum.ToObject(typeof(TEnum), value);
		// [CLSCompliant(false)]
		public static TEnum ToObject<TEnum>(sbyte value) =>
			(TEnum) Enum.ToObject(typeof(TEnum), value);
		public static TEnum ToObject<TEnum>(object value) =>
			(TEnum) Enum.ToObject(typeof(TEnum), value);
		public static TEnum ToObject<TEnum>(long value) =>
			(TEnum) Enum.ToObject(typeof(TEnum), value);
		public static TEnum ToObject<TEnum>(int value) =>
			(TEnum) Enum.ToObject(typeof(TEnum), value);
		public static TEnum ToObject<TEnum>(byte value) =>
			(TEnum) Enum.ToObject(typeof(TEnum), value);
		public static TEnum ToObject<TEnum>(short value) =>
			(TEnum) Enum.ToObject(typeof(TEnum), value);
		// public static bool TryParse<TEnum>(string value, out TEnum result) where TEnum : struct =>
		// 	Enum.TryParse(value, out result);
		// public static bool TryParse<TEnum>(string value, bool ignoreCase, out TEnum result) where TEnum : struct =>
		// 	Enum.TryParse(value, ignoreCase, out result);
	}

}
