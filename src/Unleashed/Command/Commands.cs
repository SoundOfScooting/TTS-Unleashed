namespace Unleashed.Command;

[HarmonyPatch]
static class PatchChat
{
	[HarmonyPrefix]
	[HarmonyPatch(typeof(Chat), nameof(Chat.ChatCMD))]
	static bool ChatCMDPrefix(string message, ChatMessageType type)
		=> Main.Catch(() =>
		{
			// #idea: turn off timestamps during command output?
			return !Command.Root.Invoke(type, message);
		}, type);
}
public static class Commands
{
	public static void Load()
		=> CommandAttribute.RegisterAll(typeof(Commands), Command.Root.Instance);

	[Command]
	sealed class CommandHelp : Command
	{
		protected override List<Alias> Aliases => field ??= [ new(this, "/help") ];
		protected override List<Usage> Usages => field ??=
		[
			new(Syntax: $"{Primary} (-a|cmd)", Desc: "Shows command usages, -a to list extra commands"),
		];
		protected override void OnInvoke()
		{
			var flagAll = Match("-a");
			if (Rest is null)
			{
				Log("Game Console Help, do not type <>[]()|, ex. /kick Batman", Colour.Purple);
				foreach (var usage in Root.HelpList)
				if      (usage.IsVisible(flagAll))
					Log($"{usage}");
				return;
			}
			if (Rest is [] || (flagAll && Rest is [_, ..]))
			{
				LogUsageError();
				return;
			}

			if (Rest is not ['/', ..])
				Rest = '/' + Rest;
			if (!Root.Resolve(Rest, out var cmd))
			{
				Log($"Unknown command '{Rest}'!", Main.ErrorColour);
				return;
			}
			cmd.LogUsageHelp();
		}
	}

	[Command]
	sealed class CommandList : Command
	{
		protected override List<Alias> Aliases => field ??= [ new(this, "/list") ];
		protected override List<Usage> Usages => field ??=
		[
			new(Syntax: Primary, Desc: "Lists information about each player")
		];
		protected override void OnInvoke()
		{
			foreach (var player in PlayerManager.Instance.PlayersList)
				Log(
					$"{player.ID}: {Colour.HexFromLabel(player.ColorLabel)}{player.Name}" +
						(Colour.IsColourLabel(player.ColorLabel) ? "" : $" ({player.ColorLabel})") + "[-]" +
						(!player.IsModded        ? "" : $" {Main.PluginColour.RGBHex}+{Main.PLUGIN_ABBR}[-]") +
						(player.ID != Network.ID ? "" : " (you)")
				);
		}
	}

	static void KickPlayerWithMessage(PlayerState player, string message, bool block = false)
	{
		if (player.ID == Network.ID)
		{
			Chat.Log($"Why are you trying to {(block ? "ban" : "kick")} yourself, silly?", Colour.Red);
			return;
		}
		Chat.SendChat($"{player.Name} is {(block ? "banned" : "kicked")}: {message}", Color.yellow);
		if (block)
			BlockList.Instance.AddBlock(player.Name, player.SteamID);
		NetworkUI.Instance.KickPlayer(player.NetworkPlayer, message);
		return;
	}
	[Command]
	sealed class CommandKick : Command
	{
		protected override List<Alias> Aliases => field ??= [ new(this, "/kick") ];
		protected override List<Usage> Usages => field ??=
		[
			new(Perm: Perm.Admin, Syntax: $"{Primary} <player> (message, host-only)", Desc: "Ejects player from the game")
		];
		protected override void OnInvoke()
		{
			if (!ExpectPlayer(out PlayerState player, $"<{nameof(player)}>"))
				return;
			if (Rest is [_, ..] message)
			{
				if (ExpectPermission(Network.IsServer, $"for argument <{nameof(message)}>"))
					KickPlayerWithMessage(player, message);
				return;
			}
			if (ExpectPermission(Network.IsAdmin))
				PlayerManager.Instance.KickThisPlayer(player.Name);
		}
	}
	[Command]
	sealed class CommandBan : Command
	{
		protected override List<Alias> Aliases => field ??= [ new(this, "/ban") ];
		protected override List<Usage> Usages => field ??=
		[
			new(Perm: Perm.Server, Syntax: $"{Primary} <player> (message)", Desc: "Kicks player and adds them to block list"),
		];
		protected override void OnInvoke()
		{
			if (!ExpectPlayer(out PlayerState player, $"<{nameof(player)}>"))
				return;
			if (Rest is [_, ..] message)
			{
				if (ExpectPermission(Network.IsServer))
					KickPlayerWithMessage(player, message, true);
				return;
			}
			if (ExpectPermission(Network.IsServer))
				PlayerManager.Instance.BanThisPlayer(player.Name);
		}
	}
	[Command]
	sealed class CommandPromote : Command
	{
		protected override List<Alias> Aliases => field ??= [ new(this, "/promote"), new(this, "/p") ];
		protected override List<Usage> Usages => field ??=
		[
			new(Perm: Perm.Admin, Syntax: $"{Primary} <player>", Desc: "Promotes or demotes player as admin"),
		];
		protected override void OnInvoke()
		{
			if (ExpectPlayer(out PlayerState player, $"<{nameof(player)}>"))
			if (ExpectPermission(Network.IsAdmin))
				PlayerManager.Instance.PromoteThisPlayer(player.Name);
		}
	}
	[Command]
	sealed class CommandMute : Command
	{
		protected override List<Alias> Aliases => field ??=
		[
			new(this, "/mute"),
			new(this, "/mutes", Prefix: "-s"),
		];
		protected override List<Usage> Usages => field ??=
		[
			new(Perm: Perm.None, Syntax: $"{Primary} (-s) <player> (status)", Desc: "Mutes or unmutes player's voice chat (-s: server-side)"),
		];
		protected override void OnInvoke()
		{
			var flagServer = Match("-s");
			if (!ExpectPlayer(out PlayerState player, $"<{nameof(player)}>"))
				return;
			if (!flagServer)
			{
				EventManager.TriggerPlayerMute(player.Muted ^= true, player.ID);
				return;
			}

			var status = player.Muted;
			if (ExpectToggle(ref status, !status, $"({nameof(status)})"))
			if (ExpectPermission(Network.IsAdmin))
				PlayerManager.Instance.RPC(RPCTarget.All, PlayerManager.Instance.RPCMute, player.ID, status);
		}
	}
	[Command]
	sealed class CommandExecute : Command
	{
		protected override List<Alias> Aliases => field ??= [ new(this, "/execute"), new(this, "/lua") ];
		protected override List<Usage> Usages => field ??=
		[
			new(Perm: Perm.Admin, Syntax: $"{Primary} <lua statement>", Desc: "Executes Lua statement"),
		];
		protected override void OnInvoke()
		{
			if (ExpectRest())
			if (ExpectPermission(Network.IsAdmin))
				LuaGlobal.Instance.RPCExecuteScript(Rest);
		}
	}

	[Command]
	sealed class CommandColor : Command
	{
		protected override List<Alias> Aliases => field ??=
		[
			new(this, "/color"), new(this, "/c"),
			new(this, "/cf", Prefix: "-f"),
			new(this, "/cs", Prefix: "-s"),
		];
		protected override List<Usage> Usages => field ??=
		[
			new(Perm: Perm.None,  Syntax: $"{Primary} (-f) (player) <color>", Desc: "Changes a player's color, -f to force"),
			new(Perm: Perm.Admin, Syntax: $"{Primary} -s (player) <seated>",  Desc: "Swaps the colors of two players"),
		];
		protected override void OnInvoke()
		{
			var player = PlayerManager.Instance.MyPlayerState();

			var flagSwap  = Match("-s");
			var flagForce = flagSwap || Match("-f");

			var arg1 = Bite();
			if (arg1.Text is null or [])
			{
				LogUsageError();
				return;
			}

			var arg2 = Bite();
			if (arg2.Text is [_, ..])
			{
				if (!ExpectPlayer(out player, $"({nameof(player)})", arg1))
					return;
				arg1 = arg2;
			}

			PlayerState seated = null;
			string      color  = null;
			var nameof_arg2 = flagSwap ? nameof(seated) : nameof(color);

			if (flagSwap)
			{
				if (!ExpectPlayer(out seated, $"<{nameof_arg2}>", arg1))
					return;
				color = seated.ColorLabel;
			}
			else
			{
				color = ParseLabel(arg1.Text);
				if (color is null)
				{
					Log($"Invalid <{nameof_arg2}>!", Main.ErrorColour);
					return;
				}
				if (color != Colour.GreyLabel)
					seated = PlayerManager.Instance.PlayersList.Find(seated => seated.ColorLabel == color);
			}

			PlayerState temp = null;
			if (color is ['!', ..])
				temp = PlayerManager.Instance.PlayersList.Find(seated => seated.ColorLabel == Colour.WhiteLabel);

			if (seated is not null)
			{
				if (player == seated)
				{
					Log($"<{nameof(player)}> is already <{nameof_arg2}>!", Main.ErrorColour);
					return;
				}
				if (!flagForce)
				{
					Log($"<{nameof_arg2}> is already occupied!", Main.ErrorColour);
					return;
				}
				if (ExpectPermission(Network.IsAdmin))
					Lua.Execute(
						$"""
						local temp = { temp }
						if temp then
							temp.changeColor({ Colour.GreyLabel })
						end
						{ Lua.ChangePlayerColorSeated }({ player }, { seated }, { flagSwap })
						if temp then
							temp.changeColor({ Colour.WhiteLabel })
						end
						"""
					);
				return;
			}

			if (temp is not null)
			{
				if (!flagForce)
				{
					Log($"<{nameof_arg2}> is blocked by {Colour.WhiteLabel}!", Main.ErrorColour);
					return;
				}
				if (ExpectPermission(Network.IsAdmin))
					Lua.Execute(
						$"""
						local temp = { temp }
						if temp then
							temp.changeColor({ Colour.GreyLabel })
						end
						{ player }.changeColor({ color })
						if temp then
							temp.changeColor({ Colour.WhiteLabel })
						end
						"""
					);
				return;
			}
			if (player.ColorLabel == color)
			{
				Log($"<{nameof(player)}> is already <{nameof_arg2}>!", Main.ErrorColour);
				return;
			}
			if ((player.ID == Network.ID) && (PermissionsOptions.Options.ChangeColor || (color == "Grey")))
				NetworkUI.Instance.ClientRequestColor(color);
			else if (ExpectPermission(Network.IsAdmin))
				NetworkUI.Instance.CheckColor(color, player.ID);
		}
	}

	[Command]
	sealed class StubCommandColor : Command.Stub
	{
		protected override List<Alias> Aliases => field ??= [
			new(this, "/<color>"),
			.. Colour.HandPlayerLabels.Select(label => new Alias(this, $"/{label}"))
		];
		protected override List<Usage> Usages => field ??= [ new(Syntax: $"{Primary} <message>", Desc: "Whispers the player on this color") ];
		protected override bool Echo => false;
	}
	[Command]
	sealed class CommandWhisper : Command
	{
		protected override List<Alias> Aliases => field ??= [ new(this, "/whisper"), new(this, "/w"), new(this, "/msg") ];
		protected override List<Usage> Usages => field ??=
		[
			new(Syntax: $"{Primary} <recipient> <message>", Desc: "Whispers a message to any player"),
		];
		protected override bool Echo => false;
		protected override void OnInvoke()
		{
			if (ExpectPlayer(out PlayerState recipient, $"<{nameof(recipient)}>"))
			if (ExpectRest())
			foreach (var receiver in Enumerable.Distinct([ Network.Player, recipient.NetworkPlayer ]))
				Chat.Instance.RPC(receiver, Chat.Instance.RPC_ChatWhisperMessage, Rest, recipient.ColorLabel);
		}
	}
	[Command]
	sealed class StubCommandTeam : Command.Stub
	{
		protected override List<Alias> Aliases => field ??= [ new(this, "/team") ];
		protected override List<Usage> Usages => field ??= [ new(Syntax: $"{Primary} <message>", Desc: "Message everyone on your team") ];
	}
	[Command]
	sealed class StubCommandClear : Command.Stub
	{
		protected override List<Alias> Aliases => field ??= [ new(this, "/clear") ];
		protected override List<Usage> Usages => field ??= [ new(Syntax: Primary, Desc: "Deletes all text from this tab") ];
		protected override bool Echo => false;
	}
	[Command]
	sealed class StubCommandFilter : Command.Stub
	{
		protected override List<Alias> Aliases => field ??= [ new(this, "/filter"), new(this, "/nofilter") ];
		protected override List<Usage> Usages => field ??= [ new(Syntax: "/filter /nofilter", Desc: "Enable or disable chat filter") ];
	}

	[Command]
	sealed class StubCommandResetAllSaved : Command.Stub
	{
		protected override List<Alias> Aliases => field ??= [ new(this, "/resetallsaved") ];
		protected override List<Usage> Usages => field ??= [ new(Syntax: Primary, Desc: "Resets all saved data (General, Controls, UI, etc)") ];
	}
	[Command]
	sealed class StubCommandRecompileSave : Command.Stub
	{
		protected override List<Alias> Aliases => field ??= [ new(this, "/recompilesave") ];
		protected override List<Usage> Usages => field ??= [ new(Hide: Hide.Extra, Syntax: Primary, Desc: "Recompiles Lua and XML UI, saving") ];
	}
	[Command]
	sealed class StubCommandRecompile : Command.Stub
	{
		protected override List<Alias> Aliases => field ??= [ new(this, "/recompile") ];
		protected override List<Usage> Usages => field ??= [ new(Hide: Hide.Extra, Syntax: Primary, Desc: "Recompiles Lua and XML UI without saving") ];
	}
	[Command]
	sealed class StubCommandVRResScale : Command.Stub
	{
		protected override List<Alias> Aliases => field ??= [ new(this, "/vrresscale") ];
		protected override List<Usage> Usages => field ??= [ new(Hide: Hide.Extra, Syntax: $"{Primary} <float>", Desc: "Set value") ];
	}
	[Command]
	sealed class StubCommandThreading : Command.Stub
	{
		protected override List<Alias> Aliases => field ??= [ new(this, "/threading") ];
		protected override List<Usage> Usages => field ??= [ new(Hide: Hide.Extra, Syntax: Primary, Desc: "Toggle value") ];
	}
	[Command]
	sealed class StubCommandDebug : Command.Stub
	{
		protected override List<Alias> Aliases => field ??= [ new(this, "/debug") ];
		protected override List<Usage> Usages => field ??= [ new(Hide: Hide.Extra, Syntax: $"{Primary} (bool)", Desc: "Get or set value") ];
	}
	[Command]
	sealed class StubCommandLog : Command.Stub
	{
		protected override List<Alias> Aliases => field ??= [ new(this, "/log") ];
		protected override List<Usage> Usages => field ??= [ new(Hide: Hide.Extra, Syntax: $"{Primary} (bool)", Desc: "Get or set value") ];
	}
	[Command]
	sealed class StubCommandMics : Command.Stub
	{
		protected override List<Alias> Aliases => field ??= [ new(this, "/mics"), new(this, "/setmic") ];
		protected override List<Usage> Usages => field ??= [ new(Hide: Hide.Extra, Syntax: "/mics /setmic <int>", Desc: "List or switch microphone devices") ];
	}
	[Command]
	sealed class StubCommandNetworkTickRate : Command.Stub
	{
		protected override List<Alias> Aliases => field ??= [ new(this, "/networktickrate") ];
		protected override List<Usage> Usages => field ??= [ new(Hide: Hide.Extra, Syntax: $"{Primary} (float)", Desc: "Get or set value") ];
	}
	[Command]
	sealed class StubCommandNetworkPackets : Command.Stub
	{
		protected override List<Alias> Aliases => field ??= [ new(this, "/networkpackets") ];
		protected override List<Usage> Usages => field ??= [ new(Hide: Hide.Extra, Syntax: $"{Primary} (int)", Desc: "Get or set value") ];
	}
	[Command]
	sealed class StubCommandNetworkInterpolate : Command.Stub
	{
		protected override List<Alias> Aliases => field ??= [ new(this, "/networkinterpolate") ];
		protected override List<Usage> Usages => field ??= [ new(Hide: Hide.Extra, Syntax: $"{Primary} (float)", Desc: "Get or set value") ];
	}
	[Command]
	sealed class StubCommandNetworkQuality : Command.Stub
	{
		protected override List<Alias> Aliases => field ??= [ new(this, "/networkquality") ];
		protected override List<Usage> Usages => field ??= [ new(Hide: Hide.Extra, Syntax: $"{Primary} (int)", Desc: "Get or set value") ];
	}
	[Command]
	sealed class StubCommandNetworkBuffering : Command.Stub
	{
		protected override List<Alias> Aliases => field ??= [ new(this, "/networkbuffering") ];
		protected override List<Usage> Usages => field ??= [ new(Hide: Hide.Extra, Syntax: Primary, Desc: "Toggle value") ];
	}

	[Command]
	sealed class CommandDev : Command
	{
		protected override List<Alias> Aliases => field ??= [ new(this, "/dev") ];
		protected override List<Usage> Usages => field ??=
		[
			new(Hide: Hide.Extra, Syntax: Primary, Desc: "Enables the developer console tab"),
		];
		// bugfix: doesn't report invalid command
		protected override void OnInvoke() =>
			Chat.Instance.ShowDeveloperConsole(); // useless :)
	}

	[Command]
	sealed class CommandLoading : Command
	{
		protected override List<Alias> Aliases => field ??= [ new(this, "/loading"), new(this, "/%") ];
		protected override List<Usage> Usages => field ??=
		[
			new(Syntax: $"{Primary} (0-255)(%)", Desc: "Changes your loading percentage, 100%+ is invisible"),
		];
		protected override void OnInvoke()
		{
			byte percent = 100;
			if (Bite().Text is [_, ..] arg)
			{
				if (arg is [ ..var tmp, '%' ])
					arg = tmp;
				if (!byte.TryParse(arg, out percent))
				{
					Log("Percent must be >= 0 and <= 255!", Main.ErrorColour);
					return;
				}
			}
			PlayerManager.Instance.SetLoadingPercent(percent);
		}
	}
	[Command]
	sealed class CommandSys : Command
	{
		protected override List<Alias> Aliases => field ??= [ new(this, "/sys") ];
		protected override List<Usage> Usages => field ??=
		[
			new(Syntax: $"{Primary} <command>", Desc: "Executes a system console command inside this chat tab"),
		];
		protected override void OnInvoke()
		{
			if (ExpectRest())
				SystemConsole.Instance.ProcessCommand(Rest, false);
		}
	}

	[Command]
	sealed class CommandPX : Command.Dispatch
	{
		const string px = Main.PLUGIN_ABBR_LOWER;

		protected override List<Alias> Aliases => field ??= [ new(this, $"/{px}") ];

		protected override List<Usage> Usages => field ??=
		[
			new(Syntax: $"{Primary} [lua|settings] ...")
		];

		[Command]
		sealed class SubcommandLua : Command
		{
			protected override List<Alias> Aliases => field ??= [ new(this, "lua") ];
			protected override List<Usage> Usages => field ??=
			[
				new(Syntax: Primary, Desc: "Copies the last script executed by the mod to the clipboard"),
			];
			protected override void OnInvoke()
			{
				NGUITools.clipboard = Lua.Latest.Text;
				Log("Copied.", Main.PluginColour);
			}
		}

		[Command]
		sealed class SubcommandSettings : Command
		{
			protected override List<Alias> Aliases => field ??= [ new(this, "settings") ];
			protected override List<Usage> Usages => field ??=
			[
				new(Syntax: Primary, Desc: "Reloads the mod's settings file from disk"),
			];
			protected override void OnInvoke()
			{
				Settings.SettingAttribute.Load();
				Log("Reloaded settings file.", Main.PluginColour);
			}
		}

		[PowerCommand]
		sealed class SubcommandTest : Command
		{
			protected override List<Alias> Aliases => field ??= [ new(this, "test") ];
			protected override List<Usage> Usages => field ??= [];
			protected override void OnInvoke()
			{
				/* nop */
			}
		}
	}
}

