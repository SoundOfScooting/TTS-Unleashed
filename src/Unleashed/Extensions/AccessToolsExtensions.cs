using System.Reflection;

namespace Unleashed.Extensions;

// based on later HarmonyX
public static class AccessToolsExtensions
{
	extension(Type type)
	{
		/// <inheritdoc cref="AccessTools.Inner(Type, string)"/>
		public Type Inner(string name)
			=> AccessTools.Inner(type, name);

		/// <inheritdoc cref="AccessTools.FirstInner(Type, Func{Type, bool})"/>
		public Type FirstInner(Func<Type, bool> predicate)
			=> AccessTools.FirstInner(type, predicate);


		/// <inheritdoc cref="AccessTools.GetDeclaredFields(Type)"/>
		public List<FieldInfo> GetDeclaredFields()
			=> AccessTools.GetDeclaredFields(type);

		/// <inheritdoc cref="AccessTools.GetFieldNames(Type)"/>
		public List<string> GetFieldNames()
			=> AccessTools.GetFieldNames(type);

		/// <inheritdoc cref="AccessTools.DeclaredField(Type, string)"/>
		public FieldInfo DeclaredField(string name)
			=> AccessTools.DeclaredField(type, name);

		/// <inheritdoc cref="AccessTools.DeclaredField(Type, int)"/>
		public FieldInfo DeclaredField(int idx)
			=> AccessTools.DeclaredField(type, idx);

		/// <inheritdoc cref="AccessTools.Field(Type, string)"/>
		public FieldInfo Field(string name)
			=> AccessTools.Field(type, name);


		/// <inheritdoc cref="AccessTools.GetDeclaredProperties(Type)"/>
		public List<PropertyInfo> GetDeclaredProperties()
			=> AccessTools.GetDeclaredProperties(type);

		/// <inheritdoc cref="AccessTools.GetPropertyNames(Type)"/>
		public List<string> GetPropertyNames()
			=> AccessTools.GetPropertyNames(type);

		/// <inheritdoc cref="AccessTools.DeclaredProperty(Type, string)"/>
		public PropertyInfo DeclaredProperty(string name)
			=> AccessTools.DeclaredProperty(type, name);

		/// <inheritdoc cref="AccessTools.DeclaredPropertyGetter(Type, string)"/>
		public MethodInfo DeclaredPropertyGetter(string name)
			=> AccessTools.DeclaredPropertyGetter(type, name);

		/// <inheritdoc cref="AccessTools.DeclaredPropertySetter(Type, string)"/>
		public MethodInfo DeclaredPropertySetter(string name)
			=> AccessTools.DeclaredPropertySetter(type, name);

		/// <inheritdoc cref="AccessTools.Property(Type, string)"/>
		public PropertyInfo Property(string name)
			=> AccessTools.Property(type, name);

		/// <inheritdoc cref="AccessTools.PropertyGetter(Type, string)"/>
		public MethodInfo PropertyGetter(string name)
			=> AccessTools.PropertyGetter(type, name);

		/// <inheritdoc cref="AccessTools.PropertySetter(Type, string)"/>
		public MethodInfo PropertySetter(string name)
			=> AccessTools.PropertySetter(type, name);

		/// <inheritdoc cref="AccessTools.FirstProperty(Type, Func{PropertyInfo, bool})"/>
		public PropertyInfo FirstProperty(Func<PropertyInfo, bool> predicate)
			=> AccessTools.FirstProperty(type, predicate);


		/// <inheritdoc cref="AccessTools.GetDeclaredMethods(Type)"/>
		public List<MethodInfo> GetDeclaredMethods()
			=> AccessTools.GetDeclaredMethods(type);

		/// <inheritdoc cref="AccessTools.GetMethodNames(Type)"/>
		public List<string> GetMethodNames()
			=> AccessTools.GetMethodNames(type);

		/// <inheritdoc cref="AccessTools.DeclaredMethod(Type, string, Type[], Type[])"/>
		public MethodInfo DeclaredMethod(string name, Type[] parameters = null, Type[] generics = null)
			=> AccessTools.DeclaredMethod(type, name, parameters, generics);

		/// <inheritdoc cref="AccessTools.Method(Type, string, Type[], Type[])"/>
		public MethodInfo Method(string name, Type[] parameters = null, Type[] generics = null)
			=> AccessTools.Method(type, name, parameters, generics);

		/// <inheritdoc cref="AccessTools.FirstMethod(Type, Func{MethodInfo, bool})"/>
		public MethodInfo FirstMethod(Func<MethodInfo, bool> predicate)
			=> AccessTools.FirstMethod(type, predicate);


		/// <inheritdoc cref="AccessTools.GetDeclaredConstructors(Type, bool?)"/>
		public List<ConstructorInfo> GetDeclaredConstructors(bool? searchForStatic = null)
			=> AccessTools.GetDeclaredConstructors(type, searchForStatic);

		/// <inheritdoc cref="AccessTools.Constructor(Type, Type[], bool)"/>
		public ConstructorInfo Constructor(Type[] parameters = null, bool searchForStatic = false)
			=> AccessTools.Constructor(type, parameters, searchForStatic);

		/// <inheritdoc cref="AccessTools.DeclaredConstructor(Type, Type[], bool)"/>
		public ConstructorInfo DeclaredConstructor(Type[] parameters = null, bool searchForStatic = false)
			=> AccessTools.DeclaredConstructor(type, parameters, searchForStatic);

		/// <inheritdoc cref="AccessTools.FirstConstructor(Type, Func{ConstructorInfo, bool})"/>
		public ConstructorInfo FirstConstructor(Func<ConstructorInfo, bool> predicate)
			=> AccessTools.FirstConstructor(type, predicate);
	}
}

