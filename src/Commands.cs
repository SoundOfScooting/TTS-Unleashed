using System.Linq;
using HarmonyLib;
using MonoMod.Cil;
using NewNet;

namespace Unleashed;

[HarmonyPatch]
public static class Commands
{
	public static void LogCommand(ChatMessageType type, string colorHex, string command, string description = null) =>
		Chat.Log(
			colorHex + command + "[-]" +
				(description is null or [] ? "" : $" [{description}]"),
			type
		);

	public static void CommandHelp(ChatMessageType type, bool displayAll) // @global
	{
		Chat.Log("Game Console Help, do not type <>, ex. /kick Batman", Colour.Purple, type);
		LogCommand(type, Colour.GreenHex, "/help <opt. -a>", "Show this help message, -a to list extra commands");

		var adminColor = Network.isAdmin ? Colour.GreenHex : Colour.RedHex;
		LogCommand(type, adminColor, "/kick <player name>",    "Ejects player from the game");
		LogCommand(type, adminColor, "/ban <player name>",     "Kicks player and adds them to block list");
		LogCommand(type, adminColor, "/promote <player name>", "Promotes or Demotes player as admin");
		LogCommand(type, adminColor, "/execute <lua code>",    "Immediately executes Lua script");

		LogCommand(type, Colour.GreenHex, "/mute <player name", "Mutes or Unmutes player's voice chat");
		LogCommand(type, Colour.GreenHex, "/<color> <message>", "Whispers the player on this color");
		LogCommand(type, Colour.GreenHex, "/team <message>",    "Message everyone on your team");
		LogCommand(type, Colour.GreenHex, "/resetallsaved",     "Resets all saved data (General, Controls, UI, etc)");
		LogCommand(type, Colour.GreenHex, "/filter /nofilter",  "Enable or disable chat filter");
		LogCommand(type, Colour.GreenHex, "/clear",             "Deletes all text from this tab");

		if (displayAll)
		{
			LogCommand(type, Colour.TealHex, "/recompilesave",                   "Recompiles Lua and XML UI, saving");
			LogCommand(type, Colour.TealHex, "/recompile",                       "Recompiles Lua and XML UI without saving");
			LogCommand(type, Colour.TealHex, "/resetallsaved",                   "Resets all settings");
			LogCommand(type, Colour.TealHex, "/vrresscale <float>",              "Set value");
			LogCommand(type, Colour.TealHex, "/threading",                       "Toggle value");
			LogCommand(type, Colour.TealHex, "/debug <opt. bool>",               "Get or set value");
			LogCommand(type, Colour.TealHex, "/log <opt. bool>",                 "Get or set value");
			LogCommand(type, Colour.TealHex, "/mics /setmic <int>",              "List microphone devices");
			LogCommand(type, Colour.TealHex, "/setmic <int>",                    "Switch microphone device");
			LogCommand(type, Colour.TealHex, "/networktickrate <opt. float>",    "Get or set value");
			LogCommand(type, Colour.TealHex, "/networkpackets <opt. int>",       "Get or set value");
			LogCommand(type, Colour.TealHex, "/networkinterpolate <opt. float>", "Get or set value");
			LogCommand(type, Colour.TealHex, "/networkquality <opt. int>",       "Get or set value");
			LogCommand(type, Colour.TealHex, "/networkbuffering",                "Toggle value");
			LogCommand(type, Colour.TealHex, "/dev",                             "Enables the developer console tab");

			// ModdedCommandHelp(type);
		}
	}
	private static readonly string px = Main.PLUGIN_ABBR.ToLower();
	public static void ModdedCommandHelp(ChatMessageType type)
	{
		LogCommand(type, Colour.PurpleHex, $"/{px}help",             "Lists new modded commands");
		LogCommand(type, Colour.PurpleHex, $"/{px}settings",         "Reloads the settings file from disk");
		LogCommand(type, Colour.PurpleHex, $"/{px}list",             "Lists information about each player");
		LogCommand(type, Colour.PurpleHex, $"/{px}loading <opt. %>", "Resets/sets your loading percentage");
		LogCommand(type, Colour.PurpleHex, $"/{px}copylua",          "Copies the last script executed by the mod");
		LogCommand(type, Colour.PurpleHex, $"/{px}cmd <command>",    "Executes a system console command from this chat tab");
#if TRUE_ULTIMATE_POWER
		LogCommand(type, Colour.PurpleHex, $"/{px}spooflog <message>");
		LogCommand(type, Colour.PurpleHex, $"/{px}spoofsay <sender ID> <message>");
		LogCommand(type, Colour.PurpleHex, $"/{px}spoofwhisper <sender ID> <recipient ID> <message>");
		LogCommand(type, Colour.PurpleHex, $"/{px}spoofteam <sender ID> <message>");
#endif
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(Chat), nameof(Chat.ChatCMD))]
	private static bool ChatCMDPrefix(string message, ChatMessageType type)
	{
		static bool MessageEqualCmdOpt(string message, string command, string delimiter, out string secondaryCommand) =>
			Chat.MessageEqualCmd(message, command + delimiter, out secondaryCommand) ||
			Chat.MessageEqualCmd(message, command);

		if (MessageEqualCmdOpt(message, "/help", " ", out var rest))
		{
			CommandHelp(type, rest?.ToLower() == "-a");
			return false;
		}
		if (Network.isAdmin && Chat.MessageEqualCmd(message, "/execute ", out rest))
		{
			// @idea: Compat.ExecuteLuaScript(rest);
			LuaGlobalScriptManager.Instance.RPCExecuteScript(rest);
			return false;
		}
		if (Chat.MessageEqualCmd(message, "/dev"))
		{
			Chat.Instance.ShowDeveloperConsole(); // useless :)
			return false; // bugfix
		}

		// Modded commands
		if (Chat.MessageEqualCmd(message, $"/{px}test"))
		{
			return false;
		}
		if (Chat.MessageEqualCmd(message, $"/{px}help"))
		{
			ModdedCommandHelp(type);
			return false;
		}
		if (Chat.MessageEqualCmd(message, $"/{px}settings"))
		{
			Settings.Load();
			Chat.Log("Reloaded settings file.", Main.PluginColour, type);
			return false;
		}
		if (Chat.MessageEqualCmd(message, $"/{px}list"))
		{
			foreach (var player in PlayerManager.Instance.PlayersList)
				Chat.Log(
					$"{player.id}: {player.name}" +
						(!player.X().IsModded      ? "" : $" +{Main.PLUGIN_ABBR}") +
						(player.id != NetworkID.ID ? "" : " (you)"),
					Main.PluginColour,
					type
				);
			return false;
		}
		if (MessageEqualCmdOpt(message, $"/{px}loading", " ", out rest))
		{
			byte percent = 100;
			if (rest is not (null or []))
			{
				if (rest is [ ..var rest2, '%' ])
					rest = rest2;
				if (!byte.TryParse(rest, out percent))
				{
					Chat.Log("Percent must be between 0 and 255!", Main.ErrorColour, type);
					return false;
				}
			}
			PlayerManager.Instance.SetLoadingPercent(percent);
			return false;
		}
		if (Chat.MessageEqualCmd(message, $"/{px}copylua"))
		{
			// NGUITools.clipboard = Compat.LastExecutedLuaScript;
			UnityEngine.GUIUtility.systemCopyBuffer = Compat.LastExecutedLuaScript;
			Chat.Log("Copied.", Main.PluginColour, type);
			return false;
		}
		if (MessageEqualCmdOpt(message, $"/{px}cmd", " ", out rest))
		{
			if (rest is null)
			{
				Chat.Log($"Usage: /{px}cmd <command>", Main.ErrorColour, type);
				return false;
			}
			SystemConsole.Instance.ProcessCommand(rest, false);
			return false;
		}
#if TRUE_ULTIMATE_POWER
		if (MessageEqualCmdOpt(message, $"/{px}spooflog", " ", out rest))
		{
			if (rest is null)
			{
				Chat.Log($"Usage: /{px}spooflog <message>", Main.ErrorColour, type);
				return false;
			}
			Chat.Instance.networkView.RPC(RPCTarget.Server, Chat.Instance.RPC_Chat, rest);
			return false;
		}
		if (MessageEqualCmdOpt(message, $"/{px}spoofsay", " ", out rest))
		{
			if (rest is null or [])
			{
				Chat.Log($"Usage: /{px}spoofsay <sender ID> <message>", Main.ErrorColour, type);
				return false;
			}
			if (!int.TryParse(LibString.bite(ref rest), out var senderId) ||
				!PlayerManager.Instance.PlayersDictionary.TryGetValue(Compat.PlayerID(senderId), out var sender)
			){
				Chat.Log($"Invalid sender ID! See /{px}list to view all player IDs.", Main.ErrorColour, type);
				return false;
			}
			Chat.Instance.networkView.RPC(RPCTarget.Server, Chat.Instance.RPC_ChatMessage, sender.id, rest);
			return false;
		}
		if (MessageEqualCmdOpt(message, $"/{px}spoofwhisper", " ", out rest))
		{
			if (rest is null or [])
			{
				Chat.Log($"Usage: /{px}spoofwhisper <sender ID> <recipient ID> <message>", Main.ErrorColour, type);
				return false;
			}
			if (!int.TryParse(LibString.bite(ref rest), out var senderId) ||
				!PlayerManager.Instance.PlayersDictionary.TryGetValue(Compat.PlayerID(senderId), out var sender)
			){
				Chat.Log($"Invalid sender ID! See /{px}list to view all player IDs.", Main.ErrorColour, type);
				return false;
			}
			if (!int.TryParse(LibString.bite(ref rest), out var recipientId) ||
				!PlayerManager.Instance.PlayersDictionary.TryGetValue(Compat.PlayerID(recipientId), out var recipient)
			){
				Chat.Log($"Invalid recipient ID! See /{px}list to view all player IDs.", Main.ErrorColour, type);
				return false;
			}
			foreach (var networkPlayer in new[] { recipient.networkPlayer, sender.networkPlayer, Network.player }.Distinct())
				Chat.Instance.networkView.RPC(networkPlayer, Chat.Instance.RPC_ChatWhisperMessage, sender.id, rest, recipient.stringColor);
			return false;
		}
		if (MessageEqualCmdOpt(message, $"/{px}spoofteam", " ", out rest))
		{
			if (rest is null or [])
			{
				Chat.Log($"Usage: /{px}spoofteam <sender ID> <message>", Main.ErrorColour, type);
				return false;
			}
			if (!int.TryParse(LibString.bite(ref rest), out var senderId) ||
				!PlayerManager.Instance.PlayersDictionary.TryGetValue(Compat.PlayerID(senderId), out var sender)
			){
				Chat.Log($"Invalid sender ID! See /{px}list to view all player IDs.", Main.ErrorColour, type);
				return false;
			}
			if (sender.team == Team.None)
			{
				Chat.Log("They are not on a Team.", Main.ErrorColour, type);
				return false;
			}
			foreach (var recipient in PlayerManager.Instance.PlayersList)
			if      (PlayerManager.Instance.SameTeam(recipient.id, sender.id))
				Chat.Instance.networkView.RPC(recipient.networkPlayer, Chat.Instance.RPC_ChatTeamMessage, sender.id, rest);
			return false;
		}
#endif
		return true;
	}
	[HarmonyILManipulator]
	[HarmonyPatch(typeof(Chat), nameof(Chat.ChatCMD))]
	private static void ChatCMDIL(ILContext il)
	{
		ILCursor c = new(il);
		var found = false;
		while (c.TryGotoNext(MoveType.Before,
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(Network), nameof(Network.isServer)))
		)){
			found = true;
			c.Next.Operand = AccessTools.PropertyGetter(typeof(Network), nameof(Network.isAdmin));
		}
		if (!found)
			Main.Log.LogWarning($"{nameof(Commands)}.{nameof(ChatCMDIL)} failed to apply!");
	}
}

