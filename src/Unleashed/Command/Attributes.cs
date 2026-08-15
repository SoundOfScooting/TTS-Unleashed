using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Unleashed.Command;

[Conditional(nameof(API.TRUE_ULTIMATE_POWER))]
sealed class PowerCommandAttribute : CommandAttribute;

[AttributeUsage(AttributeTargets.Class)]
class CommandAttribute([CallerLineNumber] int sourceLine = default) : Attribute, IComparable<CommandAttribute>
{
	public int SourceLine { get; } = sourceLine;
	public int CompareTo(CommandAttribute other)
		=> Comparison.Default(other)
		?? Comparison.Compare(SourceLine, other.SourceLine);

	public static void RegisterAll(Type outerType, Command.Dispatch @base)
	{
		foreach(var type in
			from    type in outerType.GetNestedTypes(AccessTools.all)
			where   type.IsDefined(typeof(CommandAttribute))
			orderby type.GetCustomAttribute<CommandAttribute>().SourceLine
			select  type
		){
			try
			{
				@base.Register(Command.Create(type));
			}
			catch (Exception e)
			{
				throw new AggregateException(type.FullDescription(), e);
			}
		}
	}
}

