using System.Globalization;
using System.Reflection;

namespace Unleashed.Extensions;

public static class ActivatorX
{
	extension(Activator)
	{
		public static T CreateInstance<T>(bool nonPublic)
			=> (T) Activator.CreateInstance(typeof(T), nonPublic);
		public static T CreateInstance<T>(params object[] args)
			=> (T) Activator.CreateInstance(typeof(T), args);
		public static T CreateInstance<T>(object[] args, object[] activationAttributes)
			=> (T) Activator.CreateInstance(typeof(T), args, activationAttributes);
		public static T CreateInstance<T>(BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture)
			=> (T) Activator.CreateInstance(typeof(T), bindingAttr, binder, args, culture);
		public static T CreateInstance<T>(BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes)
			=> (T) Activator.CreateInstance(typeof(T), bindingAttr, binder, args, culture, activationAttributes);
	}
}

