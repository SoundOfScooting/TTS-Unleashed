namespace Unleashed.Command;

readonly record struct Argument(string Text, bool Quoted = false);

static class Message
{
	public static string Join(params IEnumerable<string> @enum) =>
		@enum.Aggregate((string) null, Join);
	public static string Join(string a, string b)
	{
		if (a is null) return b;
		if (b is null) return a;
		return $"{a} {b}";
	}

	public static Argument Bite(ref string rest) =>
		Bite(rest, out rest);
	public static Argument Bite(string msg, out string rest)
	{
		var text = "";
		var quoted = false;

		var (s, e) = (0, 0);
		char? quot = null;

		if (msg is null or [])
			return new(rest = null); // missing space and arg
		while (true)
		{
			if (e >= msg.Length)
			{
				text += msg[s..];
				rest = null; // missing space
				return new(text, quoted);
			}
			var c = msg[e];
			switch ((c, quot))
			{
				default:
					e++;
					continue;

				case (' ', null):
					text += msg[s..e++];
					rest  = msg[e..];
					if (e == 1)
						return new(null); // missing arg
					return new(text, quoted);

				case ('"' or '\'', null):
					quoted = true;
					quot  = c;
					text += msg[s..e++];
					s = e;
					continue;
				case ('"' or '\'', {} q) when q == c:
					quot  = null;
					text += msg[s..e++];
					s = e;
					continue;

				// #idea: allow outside quote?
				case ('\\', {}) when (msg.Length - e) > 1:
					text += msg[s..e++];
					s = e;
					goto default;
			}
		}
	}
}

