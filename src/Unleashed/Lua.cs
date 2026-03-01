using System.Runtime.CompilerServices;
using System.Text;

namespace Unleashed;

public readonly record struct Lua(string Text)
{
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

				seated.changeColor({ Colour.GreyLabel })
				target.changeColor(seated_color)

				if swap and (seated.color ~= target_color) then
					seated.changeColor(target_color)
				end
			end
		end
		"""
	);

	public string Text { get => field ?? ""; } = Text;
	public override string ToString() => Text;

	public static explicit operator Lua(string rhs) => new(rhs);

	public static Lua Latest { get; private set; }
	public static void Execute(in Template template)
	{
		Latest = (Lua) template;
		LuaGlobal.Instance.RPCExecuteScript(Latest.Text);
	}

	public static Lua Join<T>(IEnumerable<T> values, Func<T, Lua> converter, string delimeter = ", ")
		=> (Lua) values.Join(value => converter.Invoke(value).ToString(), delimeter);
	public static Lua Join(IEnumerable<Lua> values, string delimeter = ", ")
		=> (Lua) values.Join(delimiter: delimeter);
	public static Lua Join(IEnumerable<int>    values, string delimeter = ", ") => Join(values, Literal.Format, delimeter);
	public static Lua Join(IEnumerable<float>  values, string delimeter = ", ") => Join(values, Literal.Format, delimeter);
	public static Lua Join(IEnumerable<bool>   values, string delimeter = ", ") => Join(values, Literal.Format, delimeter);
	public static Lua Join(IEnumerable<string> values, string delimeter = ", ") => Join(values, Literal.Format, delimeter);

	public static Lua Table(Lua lua = default)
		=> (Lua) $"{{ { lua } }}";
	public static Lua Table<T>(IEnumerable<T> values, Func<T, Lua> converter, string delimeter = ", ")
		=> Table(Join(values, converter, delimeter));
	public static Lua Table(IEnumerable<Lua>    values, string delimeter = ", ") => Table(Join(values, delimeter));
	public static Lua Table(IEnumerable<int>    values, string delimeter = ", ") => Table(Join(values, delimeter));
	public static Lua Table(IEnumerable<float>  values, string delimeter = ", ") => Table(Join(values, delimeter));
	public static Lua Table(IEnumerable<bool>   values, string delimeter = ", ") => Table(Join(values, delimeter));
	public static Lua Table(IEnumerable<string> values, string delimeter = ", ") => Table(Join(values, delimeter));

	public readonly ref struct Literal(Lua Lua)
	{
		readonly Lua Lua = Lua;
		public override string ToString() => Lua.ToString();

		public static implicit operator Lua    (Literal rhs) => rhs.Lua;
		public static explicit operator Literal(int     rhs) => new(Format(rhs));
		public static explicit operator Literal(float   rhs) => new(Format(rhs));
		public static explicit operator Literal(bool    rhs) => new(Format(rhs));
		public static explicit operator Literal(string  rhs) => new(Format(rhs));

		public static Lua Format(int    value) => (Lua) $"{value}";
		public static Lua Format(float  value) => (Lua) $"{value}";
		public static Lua Format(bool   value) => (Lua) $"{value}".ToLowerInvariant();
		public static Lua Format(string value)
		{
			if (value is null)
				return (Lua) "nil";
			var escape = "";
			while (value.Contains($"]{ escape }]"))
				escape += "=";
			return (Lua) $" [{ escape }[{ value }]{ escape }] ";
		}
	}

	public sealed class Variable(string Name, Lua Body, Variable[] Imports)
	{
		public Lua Name { get; } = (Lua) Name;
		public Lua Body { get; } = (Lua) $"local { Name } = { Body }";
		public Variable[] Imports { get => field ?? []; } = Imports;

		public Variable(string Name, Template Body)
			: this(Name, Body.Body, [.. Body.Imports]) { }

		public static Lua Join(IEnumerable<Variable> imports, Lua body)
			=> Lua.Join(ResolveImports(imports, body), "\n");
		static IEnumerable<Lua> ResolveImports(IEnumerable<Variable> imports, Lua? body = null)
		{
			var set   = new HashSet<Variable>();
			var next  = new Queue<Variable>(imports);
			var stack = new Stack<Queue<Variable>>();
			while (true)
			{
				if (next.TryPeek(out var var))
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
			if (body is {} lua)
				yield return lua;
		}
	}

	[InterpolatedStringHandler]
	public readonly ref struct Template(int literalLength, int formattedCount)
	{
		readonly StringBuilder  builder = new(literalLength);  // min
		readonly List<Variable> imports = new(formattedCount); // max

		public Lua Body => (Lua) builder.ToString();
		public IEnumerable<Variable> Imports => imports;

		public static explicit operator Lua(Template rhs)
			=> Variable.Join(rhs.imports, rhs.Body);

		void Append(Lua lua)
			=> builder.Append(lua.Text);
		Lua Import(Variable variable)
		{
			ArgumentNullException.ThrowIfNull(variable);
			imports.TryAddUnique(variable);
			return variable.Name;
		}
		Lua Import(Template template)
		{
			foreach (var variable in template.imports)
				Import(variable);
			return template.Body;
		}

		public void AppendLiteral  (string   text)  => Append((Lua) text);
		public void AppendFormatted(Lua      value) => Append(value);
		public void AppendFormatted(int      value) => Append((Literal) value);
		public void AppendFormatted(float    value) => Append((Literal) value);
		public void AppendFormatted(bool     value) => Append((Literal) value);
		public void AppendFormatted(string   value) => Append((Literal) value);
		public void AppendFormatted(Variable value) => Append(Import(value));

		public void AppendFormatted(NetPhysObject value)
			=> Append(
				value is null ? (Lua) "nil" :
				Import((Template) $"""getObjectFromGUID({ value.GUID })""")
			);
		public void AppendFormatted(PlayerState value)
			=> Append(
				value is null ? (Lua) "nil" :
				Import(
					NetworkUI.Instance.IsHotseat
					? (Template) $"""Player.getPlayers()[{ 1+PlayerManager.Instance.PlayersList.IndexOf(value) }]"""
					: (Template) $"""{ GetPlayerBySteamID }({ value.SteamID })"""
				)
			);
	}
}

