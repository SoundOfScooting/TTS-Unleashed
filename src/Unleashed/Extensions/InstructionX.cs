using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Unleashed.Extensions;

public static class InstructionX
{
	extension(Instruction @this)
	{
		public Delegate         OperandAsMethod { set => @this.Operand = Reflect.Method(value); }
		public LambdaExpression OperandAsGetter { set => @this.Operand = Reflect.PropertyGetter(value); }
		public LambdaExpression OperandAsSetter { set => @this.Operand = Reflect.PropertySetter(value); }
		public LambdaExpression OperandAsField  { set => @this.Operand = Reflect.Field(value); }

		[OverloadResolutionPriority(-1)]
		public bool MatchCall          (Delegate expr) => @this.MatchCall          (Reflect.Method(expr));
		[OverloadResolutionPriority(-1)]
		public bool MatchCallvirt      (Delegate expr) => @this.MatchCallvirt      (Reflect.Method(expr));
		[OverloadResolutionPriority(-1)]
		public bool MatchCallOrCallvirt(Delegate expr) => @this.MatchCallOrCallvirt(Reflect.Method(expr));

		public bool MatchCall          <T>(Expression<Func<T>> expr, MethodType methodType = MethodType.Getter) => @this.MatchCall          (Reflect.PropertyAccessor(expr, methodType));
		public bool MatchCallvirt      <T>(Expression<Func<T>> expr, MethodType methodType = MethodType.Getter) => @this.MatchCallvirt      (Reflect.PropertyAccessor(expr, methodType));
		public bool MatchCallOrCallvirt<T>(Expression<Func<T>> expr, MethodType methodType = MethodType.Getter) => @this.MatchCallOrCallvirt(Reflect.PropertyAccessor(expr, methodType));

		public bool MatchLdfld  <T>(Expression<Func<T>> expr) => @this.MatchLdfld  (Reflect.Field(expr));
		public bool MatchStfld  <T>(Expression<Func<T>> expr) => @this.MatchStfld  (Reflect.Field(expr));
		public bool MatchLdflda <T>(Expression<Func<T>> expr) => @this.MatchLdflda (Reflect.Field(expr));
		public bool MatchLdsfld <T>(Expression<Func<T>> expr) => @this.MatchLdsfld (Reflect.Field(expr));
		public bool MatchStsfld <T>(Expression<Func<T>> expr) => @this.MatchStsfld (Reflect.Field(expr));
		public bool MatchLdsflda<T>(Expression<Func<T>> expr) => @this.MatchLdsflda(Reflect.Field(expr));
	}
}

