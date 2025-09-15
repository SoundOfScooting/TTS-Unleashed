using System.Runtime.CompilerServices;
using System.Text;

namespace Unleashed;

public readonly record struct Lua(string Text)
{
	// #todo: not usable in hotseat
	public static readonly Variable GetPlayerBySteamID = new(
		nameof(GetPlayerBySteamID),
		$"""
		function (steam_id)
			for _,player in ipairs(Player.getPlayers()) do
				if player.steam_id == steam_id then
					return player
				end
			end
			return nil
		end
		"""
	);
	public static readonly Variable ChangePlayerColorSeated = new(
		nameof(ChangePlayerColorSeated),
		$"""
		function (target, seated, swap)
			if target and seated then
				local target_color = target.color
				local seated_color = seated.color

				seated.changeColor("Grey")
				target.changeColor(seated_color)

				if swap and (target_color ~= "Grey") then
					seated.changeColor(target_color)
				end
			end
		end
		"""
	);

	public string Text { get => field ?? ""; } = Text;
	public override string ToString() => Text;

	public static explicit operator Lua(string text) => new(text);

	public static Lua Latest { get; private set; }
	public static void Execute(in Template template)
	{
		Latest = (Lua) template;
		LuaGlobalScriptManager.Instance.RPCExecuteScript(Latest.Text);
	}

	public static Lua Join<T>(IEnumerable<T> @enum, Func<T, Lua> converter, string delimeter = ", ") =>
		(Lua) @enum.Join(value => converter.Invoke(value).ToString(), delimeter);
	public static Lua Join(IEnumerable<Lua> @enum, string delimeter = ", ") =>
		(Lua) @enum.Join(delimiter: delimeter);
	public static Lua Join(IEnumerable<int>    @enum, string delimeter = ", ") => Join(@enum, Literal.Format, delimeter);
	public static Lua Join(IEnumerable<float>  @enum, string delimeter = ", ") => Join(@enum, Literal.Format, delimeter);
	public static Lua Join(IEnumerable<bool>   @enum, string delimeter = ", ") => Join(@enum, Literal.Format, delimeter);
	public static Lua Join(IEnumerable<string> @enum, string delimeter = ", ") => Join(@enum, Literal.Format, delimeter);

	public static Lua Table(Lua lua = default) =>
		(Lua) $"{{ { lua } }}";
	public static Lua Table<T>(IEnumerable<T> @enum, Func<T, Lua> converter, string delimeter = ", ") =>
		Table(Join(@enum, converter, delimeter));
	public static Lua Table(IEnumerable<Lua>    @enum, string delimeter = ", ") => Table(Join(@enum, delimeter));
	public static Lua Table(IEnumerable<int>    @enum, string delimeter = ", ") => Table(Join(@enum, delimeter));
	public static Lua Table(IEnumerable<float>  @enum, string delimeter = ", ") => Table(Join(@enum, delimeter));
	public static Lua Table(IEnumerable<bool>   @enum, string delimeter = ", ") => Table(Join(@enum, delimeter));
	public static Lua Table(IEnumerable<string> @enum, string delimeter = ", ") => Table(Join(@enum, delimeter));

	public readonly ref struct Literal(Lua Lua)
	{
		readonly Lua Lua = Lua;
		public override string ToString() => Lua.ToString();

		public static implicit operator Lua    (Literal @this) => @this.Lua;
		public static explicit operator Literal(int     value) => new(Format(value));
		public static explicit operator Literal(float   value) => new(Format(value));
		public static explicit operator Literal(bool    value) => new(Format(value));
		public static explicit operator Literal(string  value) => new(Format(value));

		public static Lua Format(int    value) => (Lua) $"{value}";
		public static Lua Format(float  value) => (Lua) $"{value}";
		public static Lua Format(bool   value) => (Lua) $"{value}".ToLower();
		public static Lua Format(string value)
		{
			if (value is null)
				return (Lua) "nil";
			var escape = "";
			while (value.Contains($"]{ escape }]"))
				escape += "=";
			return (Lua) $"[{ escape }[{ value }]{ escape }]";
		}
	}

	public readonly ref struct Script(Lua Body, Variable[] Imports = null)
	{
		public Lua Body { get; } = Body;
		public Variable[] Imports { get => field ?? []; } = Imports;

		public static explicit operator Lua(Script @this) =>
			Join([.. Variable.ResolveImports(@this.Imports), @this.Body], "\n");
	}

	public sealed class Variable(string Name, Script Body)
	{
		public Lua Name { get; } = (Lua) Name;
		public Lua Body { get; } = (Lua) $"local { Name } = { Body.Body }";
		public Variable[] Imports { get => field ?? []; } = Body.Imports;

		public Variable(string Name, Template Body)
			: this(Name, (Script) Body) { }

		public static IEnumerable<Lua> ResolveImports(Variable[] imports)
		{
			var set   = new HashSet<Variable>();
			var next  = new Queue<Variable>(imports);
			var stack = new Stack<Queue<Variable>>();
			while (true)
			{
				if (next.FirstOrDefault() is {} var)
				{
					if (set.Add(var))
					{
						stack.Push(next);
						next = new(var.Imports);
					}
					else next.Dequeue();
					continue;
				}
				if (stack.TryPop(out next))
				{
					yield return next.Dequeue().Body;
					continue;
				}
				break;
			}
		}
	}

	[InterpolatedStringHandler]
	public readonly ref struct Template(int literalLength, int formattedCount)
	{
		readonly StringBuilder  builder = new(literalLength);  // min
		readonly List<Variable> imports = new(formattedCount); // max
		Lua Body => (Lua) builder.ToString();

		public static explicit operator Script(Template @this) =>
			new(@this.Body, [.. @this.imports]);
		public static explicit operator Lua(Template @this) =>
			(Lua) (Script) @this;

		void Append(Lua lua) =>
			builder.Append(lua.Text);
		void Import(Variable variable) =>
			imports.TryAddUnique(variable);

		public void AppendLiteral  (string text)  => Append((Lua) text);
		public void AppendFormatted(Lua    lua)   => Append(lua);
		public void AppendFormatted(int    value) => Append((Literal) value);
		public void AppendFormatted(float  value) => Append((Literal) value);
		public void AppendFormatted(bool   value) => Append((Literal) value);
		public void AppendFormatted(string value) => Append((Literal) value);
		public void AppendFormatted(Variable variable)
		{
			if (variable is null)
				throw new ArgumentNullException(nameof(variable));
			Append(variable.Name);
			Import(variable);
		}
		public void AppendFormatted(Script script)
		{
			Append(script.Body);
			foreach (var variable in script.Imports)
				Import(variable);
		}
		public void AppendFormatted(Template template)
		{
			Append(template.Body);
			foreach (var variable in template.imports)
				Import(variable);
		}
	}
}

