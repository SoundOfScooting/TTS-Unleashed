namespace Unleashed.Command;

[HarmonyPatch]
static class PatchChat
{
	[HarmonyPrefix]
	[HarmonyPatch(typeof(Chat), nameof(Chat.ChatCMD))]
	static bool ChatCMDPrefix(string message, ChatMessageType type)
	{
		try
		{
			// #idea: turn off timestamps during command output?
			return !Command.Root.Invoke(type, message);
		}
		catch (Exception e)
		{
			Chat.LogError(e.ToString(), type);
			Main.Log.LogError(e);
			return false;
		}
	}
}
public static class Commands
{
	public static void Load() =>
		CommandAttribute.RegisterAll(typeof(Commands), Command.Root.Instance);

	const string px = Main.PLUGIN_ABBR_LOWER;

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
					$"{player.id}: {Colour.HexFromLabel(player.stringColor)}{player.name}" +
						(Colour.IsColourLabel(player.stringColor) ? "" : $" ({player.stringColor})") + "[-]" +
						(!player.IsModded          ? "" : $" {Main.PluginColour.RGBHex}+{Main.PLUGIN_ABBR}[-]") +
						(player.id != NetworkID.ID ? "" : " (you)")
				);
		}
	}

	static void KickPlayerWithMessage(PlayerState player, string message, bool block = false)
	{
		if (player.id == NetworkID.ID)
		{
			Chat.Log($"Why are you trying to {(block ? "ban" : "kick")} yourself, silly?", Colour.Red);
			return;
		}
		Chat.SendChat($"{player.name} is {(block ? "banned" : "kicked")}: {message}", Color.yellow);
		if (block)
			BlockList.Instance.AddBlock(player.name, player.steamId);
		NetworkUI.Instance.KickPlayer(player.networkPlayer, message);
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
				if (!Network.isServer)
				{
					Log($"Argument ({nameof(message)}) is host-only!", Main.ErrorColour);
					return;
				}
				KickPlayerWithMessage(player, message);
				return;
			}
			if (ExpectPermission(Network.isAdmin))
				PlayerManager.Instance.KickThisPlayer(player.name);
		}
	}
	[Command]
	sealed class CommandBan : Command
	{
		protected override List<Alias> Aliases => field ??= [ new(this, "/ban") ];
		protected override List<Usage> Usages => field ??=
		[
			new(Perm: Perm.Host, Syntax: $"{Primary} <player> (message)", Desc: "Kicks player and adds them to block list"),
		];
		protected override void OnInvoke()
		{
			if (!ExpectPlayer(out PlayerState player, $"<{nameof(player)}>"))
				return;
			if (Rest is [_, ..] message)
			{
				if (!Network.isServer)
				{
					Log($"Argument ({nameof(message)}) is host-only!", Main.ErrorColour);
					return;
				}
				KickPlayerWithMessage(player, message, true);
				return;
			}
			if (ExpectPermission(Network.isServer))
				PlayerManager.Instance.BanThisPlayer(player.name);
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
			if (ExpectPermission(Network.isAdmin))
				PlayerManager.Instance.PromoteThisPlayer(player.name);
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
				EventManager.TriggerPlayerMute(player.muted ^= true, player.id);
				return;
			}

			var status = player.muted;
			if (ExpectToggle(ref status, !status, $"({nameof(status)})"))
			if (ExpectPermission(Network.isAdmin))
				PlayerManager.Instance.RPC(RPCTarget.All, PlayerManager.Instance.RPCMute, player.id, status);
		}
	}
	[Command]
	sealed class CommandExecute : Command
	{
		protected override List<Alias> Aliases => field ??= [ new(this, "/execute") ];
		protected override List<Usage> Usages => field ??=
		[
			new(Perm: Perm.Admin, Syntax: $"{Primary} <lua statement>", Desc: "Executes Lua statement"),
		];
		protected override void OnInvoke()
		{
			if (ExpectRest())
			if (ExpectPermission(Network.isAdmin))
				LuaGlobalScriptManager.Instance.RPCExecuteScript(Rest);
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

			var flagForce = Match("-f");
			var flagSwap  = !flagForce && Match("-s");

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
			}
			else
			{
				if (ParseLabel(arg1.Text) is not {} color_)
				{
					Log($"Invalid <{nameof_arg2}>!", Main.ErrorColour);
					return;
				}
				color = color_;
				if (color != "Grey")
					seated = PlayerManager.Instance.PlayersList.Find(seated => seated.stringColor == color);
			}

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
				if (ExpectPermission(Network.isAdmin))
					Lua.Execute(
						$"""
						local target = { Lua.GetPlayerBySteamID }({ player.steamId })
						local seated = { Lua.GetPlayerBySteamID }({ seated.steamId })
						local swap   = { flagSwap }
						{ Lua.ChangePlayerColorSeated }(target, seated, swap)
						"""
					);
				return;
			}

			if (player.stringColor == color)
			{
				Log($"<{nameof(player)}> is already <{nameof_arg2}>!", Main.ErrorColour);
				return;
			}
			if (player.id == NetworkID.ID && PermissionsOptions.options.ChangeColor)
			{
				NetworkUI.Instance.ClientRequestColor(color);
				return;
			}
			if (ExpectPermission(Network.isAdmin))
				NetworkUI.Instance.CheckColor(color, player.id);
		}
	}

	[Command]
	sealed class StubCommandColor : Command.Stub
	{
		protected override List<Alias> Aliases => field ??= [ new(this, "/<color>") ];
		protected override List<Usage> Usages => field ??= [ new(Syntax: $"{Primary} <message>", Desc: "Whispers the player on this color") ];
	}
	[Command]
	sealed class CommandWhisper : Command
	{
		protected override List<Alias> Aliases => field ??= [ new(this, "/whisper"), new(this, "/w") ];
		protected override List<Usage> Usages => field ??=
		[
			new(Syntax: $"{Primary} <recipient> <message>", Desc: "Whispers a message to any player"),
		];
		protected override void OnInvoke()
		{
			if (ExpectPlayer(out PlayerState recipient, $"<{nameof(recipient)}>"))
			if (ExpectRest())
			foreach (var receiver in Enumerable.Distinct([ Network.player, recipient.networkPlayer ]))
				Chat.Instance.RPC(receiver, Chat.Instance.RPC_ChatWhisperMessage, NetworkID.ID, Rest, recipient.stringColor);
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
	sealed class CommandPXTest : Command
	{
		protected override List<Alias> Aliases => field ??= [ new(this, $"/{px}test") ];
		protected override List<Usage> Usages => field ??= [];
		protected override void OnInvoke()
		{
			/* nop */
		}
	}
	[Command]
	sealed class CommandPXSettings : Command
	{
		protected override List<Alias> Aliases => field ??= [ new(this, $"/{px}settings") ];
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
	[Command]
	sealed class CommandPXCopyLua : Command
	{
		protected override List<Alias> Aliases => field ??= [ new(this, $"/{px}copylua") ];
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

	[PowerCommand]
	sealed class CommandSpoof : Command.Dispatch
	{
		protected override List<Alias> Aliases => field ??= [ new(this, "/spoof") ];
		protected override List<Usage> Usages => field ??=
		[
			new(Hide: Hide.Hidden, Syntax: $"{Primary} [log| chat|say | whisper|w | team] ..."),
		];

		[PowerCommand]
		sealed class CommandSpoofLog : Command
		{
			protected override List<Alias> Aliases => field ??= [ new(this, "log") ];
			protected override List<Usage> Usages => field ??=
			[
				new(Syntax: $"{Primary} <message>"),
			];
			protected override void OnInvoke()
			{
				if (ExpectRest())
					Chat.Instance.RPC(RPCTarget.All, Chat.Instance.RPC_Chat, Rest);
			}
		}
		[PowerCommand]
		sealed class CommandSpoofChat : Command
		{
			protected override List<Alias> Aliases => field ??= [ new(this, "chat"), new(this, "say") ];
			protected override List<Usage> Usages => field ??=
			[
				new(Syntax: $"{Primary} <sender> <message>"),
			];

			protected override void OnInvoke()
			{
				if (ExpectPlayer(out PlayerState sender, $"<{nameof(sender)}>"))
				if (ExpectRest())
					Chat.Instance.RPC(RPCTarget.Server, Chat.Instance.RPC_ChatMessage, sender.id, Rest);
			}
		}
		[PowerCommand]
		sealed class CommandSpoofWhisper : Command
		{
			protected override List<Alias> Aliases => field ??=
			[
				new(this, "whisper"), new(this, "w"),
				new(this, "wg", Prefix: "-g"),
			];
			protected override List<Usage> Usages => field ??=
			[
				new(Syntax: $"{Primary} (-g) <sender> <recipient> <message>"),
			];
			protected override void OnInvoke()
			{
				var flagGlobal = Match("-g");
				if (ExpectPlayer(out PlayerState sender,    $"<{nameof(sender)}>"))
				if (ExpectPlayer(out PlayerState recipient, $"<{nameof(recipient)}>"))
				if (ExpectRest())
				{
					if (flagGlobal)
						Chat.Instance.RPC(RPCTarget.All, Chat.Instance.RPC_ChatWhisperMessage, sender.id, Rest, recipient.stringColor);
					else foreach (var receiver in Enumerable.Distinct([ Network.player, sender.networkPlayer, recipient.networkPlayer ]))
						Chat.Instance.RPC(receiver,      Chat.Instance.RPC_ChatWhisperMessage, sender.id, Rest, recipient.stringColor);
				}
			}
		}
		[PowerCommand]
		sealed class CommandSpoofTeam : Command
		{
			protected override List<Alias> Aliases => field ??= [ new(this, "team") ];
			protected override List<Usage> Usages => field ??=
			[
				new(Syntax: $"{Primary} (-a|-g) <sender> <message>"),
			];
			protected override void OnInvoke()
			{
				var flagAll    = Match("-a");
				var flagGlobal = !flagAll && Match("-g");
				if (!ExpectPlayer(out PlayerState sender, $"<{nameof(sender)}>"))
					return;
				if (!flagGlobal && !flagAll && sender.team == Team.None)
				{
					Log($"<{nameof(sender)}> is not on a Team.", Main.ErrorColour);
					return;
				}
				if (!ExpectRest())
					return;
				if (flagGlobal)
				{
					var message = "<TEAM> " + Colour.HexFromColour(sender.color) + sender.name + ": [FFFFFF]" + Rest;
					Chat.Instance.RPC(RPCTarget.All, Chat.Instance.RPC_Chat, message);
					return;
				}
				foreach (var recipient in PlayerManager.Instance.PlayersList)
				if      (flagAll ? recipient.team != Team.None : recipient.team == sender.team)
					Chat.Instance.RPC(recipient.networkPlayer, Chat.Instance.RPC_ChatTeamMessage, sender.id, Rest);
			}
		}
	}
}

