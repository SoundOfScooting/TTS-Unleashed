using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;

namespace Unleashed.Util;

// #idea: constructors, indexers

public static class Reflect
{
	static class InstanceHolder<T>
	{
		public static readonly T Instance = (T) FormatterServices.GetUninitializedObject(typeof(T));
	}
	extension<TType>(TType)
	{
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "<Pending>")]
		// #want: extension property, which isn't allowed in expressions
		public static TType __instance()
			=> InstanceHolder<TType>.Instance;
	}

	// [OverloadResolutionPriority(-1)]
	public static MethodInfo Method(Delegate expr)
		=> expr.Method;
	// public static MethodInfo Method(LambdaExpression expr)
	// 	=> Member(expr) as MethodInfo
	// 	?? throw new ArgumentException($"Expression expected to be a method: {expr}", nameof(expr));

	public static FieldInfo Field(LambdaExpression expr)
		=> Member(expr) as FieldInfo
		?? throw new ArgumentException($"Expression expected to be a field: {expr}", nameof(expr));

	public static PropertyInfo Property(LambdaExpression expr)
		=> Member(expr) as PropertyInfo
		?? throw new ArgumentException($"Expression expected to be a property: {expr}", nameof(expr));
	public static MethodInfo PropertyGetter(LambdaExpression expr) => Property(expr).GetMethod;
	public static MethodInfo PropertySetter(LambdaExpression expr) => Property(expr).SetMethod;
	public static MethodInfo PropertyAccessor(LambdaExpression expr, MethodType methodType)
		=> methodType switch
		{
			MethodType.Getter => PropertyGetter(expr),
			MethodType.Setter => PropertySetter(expr),
			_ => throw new ArgumentException($"Method type must be {nameof(MethodType.Getter)} or {nameof(MethodType.Setter)}!", nameof(methodType)),
		};

	// [OverloadResolutionPriority(-1)]
	// public static MemberInfo Member(Delegate expr) => expr.Method;
	public static MemberInfo Member(LambdaExpression expr)
		// #idea: allow methods here: Class.Method(default(Arg1Type), default(Arg2Type))
			// alternative to (Action/Func<...>) Class.Method
		=> expr switch
		{
			{ ReturnType: var returnType } when returnType == typeof(void)
				=> throw new ArgumentException($"Expression must return a value: {expr}", nameof(expr)),

			{ Parameters: not [] }
				=> throw new ArgumentException($"Expression must not have parameters: {expr}", nameof(expr)),

			{ Body: MemberExpression { Member: {} member, Expression: var parentExpr } }
			when ExpressionIsContainingType(parentExpr)
				=> member,

			_ => throw new ArgumentException($"Expression has unknown form: {expr}", nameof(expr)),
		};
	static bool ExpressionIsContainingType(Expression expr)
		=> expr switch
		{
			// static member
			null => true,

			// instance member
			// #api: relax constraints, singletons
			MethodCallExpression { Object: null, Arguments: [], Method: { IsGenericMethod: true } method }
			when method.GetGenericMethodDefinition() == typeof(Reflect).Method(nameof(__instance))
				=> true,

			// else
			_ => false,
		};
}

