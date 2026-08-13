using System.Diagnostics;
using System.Globalization;

namespace Unleashed.Command;

sealed record class Alias(Command Cmd, string Short, string Prefix = null)
{
	public static string ToString(Command cmd, IEnumerable<Alias> simple)
	{
		var sub = cmd.Base?.Primary != null;
		return $"'{cmd.Base?.Primary ?? "/"}{Message.Join("|", x => sub ? x.Short : x.Short[1..], simple)} ...' -> '{Message.Join(cmd.Primary, "...")}'";
	}
	public override string ToString()
		=> $"'{Message.Join(Cmd.Base?.Primary, Short, "...")}' -> '{Message.Join(Cmd.Primary, Prefix, "...")}'";
}

enum Perm { None, Admin, Server }
enum Hide { Normal, Extra, Hidden }

sealed record class Usage(string Syntax, string Desc = null, Perm Perm = Perm.None, Hide Hide = Hide.Normal)
{
	bool IsPermitted => Perm switch
	{
		Perm.Admin  => Network.IsAdmin,
		Perm.Server => Network.IsServer,
		_  => true,
	};
	public bool IsVisible(bool flagAll)
		=> Hide switch
		{
			Hide.Hidden => false,
			Hide.Extra  => flagAll,
			_ => true,
		};

	public override string ToString()
	{
		var colorHex = !IsPermitted ? Colour.RedHex : Hide switch
		{
			Hide.Extra => Colour.TealHex,
			_ => Colour.GreenHex,
		};
		return colorHex + Syntax + "[-]" + (Desc is null or [] ? "" : $" [{Desc}]");
	}
}

static class CommandX
{
	static class InstanceHolder<T> where T : Command, new()
	{
		public static T Instance = new();
	}
	extension<T>(T) where T : Command, new()
	{
		public static T Instance => InstanceHolder<T>.Instance;
	}
}
abstract class Command
{
	public abstract class Stub : Command
	{
		protected override bool IsStub => true;
		protected sealed override void OnInvoke() => throw new UnreachableException();
	}
	public abstract class Dispatch : Command
	{
		protected override void OnInvoke() => LogUsageError();

		public virtual List<Command> SubCommands
		{
			get
			{
				if (field is null)
				{
					field = [];
					CommandAttribute.RegisterAll(GetType(), this);
				}
				return field;
			}
		}
		public void Register(Command sub)
		{
			sub.Base = this;
			SubCommands.Add(sub);
		}

		public bool Remaining(string rest)
			=> rest is [_, ..] && SubCommands is not [];
		public bool Resolve(ref string rest, out Command cmd)
		{
			var text = Message.Bite(rest, out var rest1).Text;
			foreach (var sub   in SubCommands)
			foreach (var alias in sub.Aliases)
			if      (text.Equals(alias.Short, StringComparison.OrdinalIgnoreCase))
			{
				rest = Message.Join(alias.Prefix, rest1);
				cmd  = sub;
				return true;
			}
			cmd = this;
			return false;
		}
	}
	public sealed class Root : Dispatch
	{
		protected override List<Alias> Aliases { get; } = [];
		protected override List<Usage> Usages  { get; } = [];
		public override List<Command> SubCommands { get; } = [];

		public static IEnumerable<Usage> HelpList
		{
			get
			{
				foreach (var sub in Root.Instance.SubCommands)
				foreach (var usage in sub.Usages)
					yield return usage;
			}
		}

		protected override bool IsStub => true;
		protected override void OnInvoke() => Log("This message should not appear.");

		public static bool Resolve(string msg, out Command cmd)
		{
			cmd = Root.Instance;
			var rest = msg;
			while (cmd is Dispatch disp && disp.Remaining(rest))
			{
				if (!disp.Resolve(ref rest, out cmd))
					return false;
				continue;
			}
			return true;
		}
		public static bool Invoke(ChatMessageType tab, string msg)
		{
			Command cmd = Root.Instance;
			var rest = msg;
			while (cmd is Dispatch disp && disp.Remaining(rest))
			{
				if (!disp.Resolve(ref rest, out cmd))
					break;
				continue;
			}
			return cmd.Invoke(tab, rest, msg);
		}
	}

	protected Command() { }
	public static Command Create(Type type)
	{
		if (!type.IsSubclassOf(typeof(Command)))
			throw new InvalidCastException();
		return (Command)
			typeof(CommandX)
			.GetMethod(nameof(CommandX.get_Instance))
			.MakeGenericMethod(type)
			.Invoke(null, null);
	}

	public bool Invoke(ChatMessageType tab, string rest, string msg)
	{
		(Tab, Rest) = (tab, rest);
		if (Echo)
			Log(msg, Colour.Blue);
		if (IsStub)
			return false;
		OnInvoke();
		return true;
	}

	public Command Base { get; private set; }

	public string Primary
		=> Message.Join(Base?.Primary, Aliases?.FirstOrDefault()?.Short);
	protected abstract List<Alias> Aliases { get; }
	protected abstract List<Usage> Usages  { get; }

	protected virtual bool Echo => true;
	protected virtual bool IsStub => false;
	protected abstract void OnInvoke();

	protected static ChatMessageType Tab { get; private set; }
	protected static string Rest;

	protected static void Log(string message, Colour colour, bool broadcast = false)
		=> Chat.Log(message, colour, Tab, broadcast);
	protected static void Log(string message)
		=> Chat.Log(message, Colour.White, Tab);
	protected static void Log(string message, string label)
		=> Chat.Log(message, label, Tab);
	public void LogUsageHelp()
	{
		if ((Alias[]) [..Aliases.Skip(1).Where(x => x.Prefix == null)] is [_, ..] simple)
			Log(Alias.ToString(this, simple), Colour.Grey);
		foreach (var alias in Aliases.Skip(1).Where(x => x.Prefix != null))
			Log($"{alias}", Colour.Grey);

		List<Usage> usages = [
			..Usages,
			..(this as Dispatch)?.SubCommands?.SelectMany(x => x.Usages) ?? []
		];
		if (usages is [])
			Log($"No usage found.", Colour.Yellow);
		else foreach (var usage in usages)
			Log($"{usage}");
	}

	protected void LogUsageError()
	{
		if (Usages is [])
		{
			Log("Invalid arguments!", Main.ErrorColour);
			return;
		}
		var plural = Usages is [_] ? "Usage" : "Usages";
		Log($"Invalid arguments! {plural}:", Main.ErrorColour);
		foreach (var usage in Usages)
			Log($"{usage}");
	}

	protected static string ParseLabel(string str)
	{
		if (str is null or [])
			return null;

		// intentional invalid color
		if (str is ['!', ..])
			return str;

		var label = char.ToUpper(str[0], CultureInfo.InvariantCulture) + str[1..].ToLowerInvariant();
		if (label == "Gray")
			label = Colour.GreyLabel;
		if (Colour.IsColourLabel(label))
			return label;

		return null;
	}

	protected static Argument Bite() => Message.Bite(ref Rest);
	static Argument Bite(out string rest) => Message.Bite(Rest, out rest);
	protected static bool Match(string match)
	{
		if (Bite(out var rest) is { Quoted: false, Text: {} text })
		if (string.Equals(text, match, StringComparison.OrdinalIgnoreCase))
		{
			Rest = rest;
			return true;
		}
		return false;
	}

	protected bool ExpectRest()
	{
		if (Rest is null)
		{
			LogUsageError();
			return false;
		}
		return true;
	}
	protected static bool ExpectPermission(bool permitted, string message = null)
	{
		if (!permitted)
		{
			Log(Message.Join(delimiter: " ", "Insufficient privileges", message) + ".", Main.ErrorColour);
			return false;
		}
		return true;
	}

	protected bool ExpectToggle(ref bool @bool, string param)
		=> ExpectToggle(ref @bool, null, param);
	protected bool ExpectToggle(ref bool @bool, bool? @default, string param)
	{
		switch (Bite().Text?.ToLowerInvariant())
		{
			case "+" or "1" or "on":
				@bool = true;
				return true;
			case "-" or "0" or "off":
				@bool = false;
				return true;
			case "!" or "not":
				@bool ^= true;
				return true;
			case "=" or "same":
				return true;
			case null when @default.HasValue:
				@bool = @default.Value;
				return true;

			case null or []:
				LogUsageError();
				return false;
			default:
				Log($"Invalid {param}! Expected one of: +|1|on, -|0|off, !|not, =|same", Main.ErrorColour);
				return false;
		}
	}

	static bool ExpectPlayerFromID(out PlayerState player, string param, string strId)
	{
		// #idea: by index of player in top-right?
		if (!int.TryParse(strId, out var id))
		{
			Log($"Invalid {param}! Player ID '#{strId}' is not an integer", Main.ErrorColour);
			player = null;
			return false;
		}
		if (!PlayerManager.Instance.PlayersDictionary.TryGetValue(Network.ToID(id), out var fromId))
		{
			Log($"Invalid {param}! Could not find player with ID #{id}", Main.ErrorColour);
			player = null;
			return false;
		}
		player = fromId;
		return true;
	}
	static bool ExpectPlayerFromLabel(out PlayerState player, string param, string label)
	{
		var matches = PlayerManager.Instance.PlayersList
			.FindAll(player => player.ColorLabel == label);
		if (matches is not [ var match ])
		{
			var plural = matches is [] ? "No player is" : "Multiple players are";
			Log($"Invalid {param}! {plural} seated in color {label}", Main.ErrorColour);
			player = null;
			return false;
		}
		player = match;
		return true;
	}
	static bool ExpectPlayerFromName(out PlayerState player, string param, string name)
	{
		var matches = PlayerManager.Instance.PlayersList
			.FindAll(player => player.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
		if (matches is not [ var match ])
		{
			var plural = matches is [] ? "No player matches" : "Multiple players match";
			Log($"Invalid {param}! {plural} name '{name}'", Main.ErrorColour);
			player = null;
			return false;
		}
		player = match;
		return true;
	}
	protected static bool ExpectPlayer(out PlayerState player, string param, Argument arg)
	{
		if (!arg.Quoted && arg.Text is [ '#', .. var strId ])
			return ExpectPlayerFromID(out player, param, strId);

		// #todo: want invalid colors to be quotable?
		if (!arg.Quoted && ParseLabel(arg.Text) is {} label)
			return ExpectPlayerFromLabel(out player, param, label);

		return ExpectPlayerFromName(out player, param, arg.Text);
	}
	protected bool ExpectPlayer(out PlayerState player, string param)
	{
		var arg = Bite();
		if (arg.Text is null)
		{
			LogUsageError();
			player = null;
			return false;
		}
		return ExpectPlayer(out player, param, arg);
	}
}

