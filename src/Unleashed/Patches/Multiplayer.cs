using System.Runtime.CompilerServices;
using BepInEx.Configuration;
using Unleashed.Settings;

namespace Unleashed.Patches;

[HarmonyPatch]
static class Multiplayer
{
	[Setting]
	static readonly Setting<string> Nickname = new()
	{
		MigrateFrom = [
			("General", "Nickname"), // 0.1.0
		],
		Section     = Section.Misc,
		Key         = "Nickname",
		Default     = "",
		Description =
			"""
			A nickname used instead of your Steam display name.
			Nickname changes only take effect when joining a new server.
			""",
		Attributes  = new()
		{
			ObjToStr = Setting.IdentityObjectConverter,
			StrToObj = Setting.IdentityStringConverter,
		},
		OnChanged = value => UpdateNickname(),
	};
	static void UpdateNickname()
	{
		NetworkUI.Instance.SetPlayerName(
			Nickname.Value is [_, ..] nickname
				? nickname
			: SteamManager.SteamName is [_, ..] steamName
				? steamName
			: "NotConnectedToSteam" // bugfix
		);
		if (Offline.Singleplayer && !NetworkUI.Instance.IsHotseat)
		if (PlayerManager.Instance.MyPlayerState() is {} player)
			player.Name = NetworkUI.Instance.playerName;
	}

	[Setting]
	static readonly Setting<string> AutoJoinMessage = new()
	{
		MigrateFrom = [
			("General", "Auto Join Message"), // 0.1.0
		],
		Section     = Section.Misc,
		Key         = "Auto Join Message",
		Default     = "",
		Description =
			"""
			Message automatically sent in chat when a player joins.
			""",
		Attributes  = new()
		{
			ObjToStr = Setting.IdentityObjectConverter,
			StrToObj = Setting.IdentityStringConverter,
		},
	};

	public static string[] SplitAutoPromoteIDs { get; private set; }
	static string CacheAutoPromoteIDs;
	[Setting]
	static readonly Setting<string> AutoPromoteIDs = new()
	{
		MigrateFrom = [
			("General", "Auto Promote Steam IDs"), // 0.1.0
		],
		Section     = Section.Misc,
		Key         = "Auto Promote Steam IDs",
		Description =
			"""
			A space- and/or comma-separated list of Steam IDs that are automatically promoted when joining your server.
			Invalid IDs do nothing, so you can write comments.
			""",
		Default     = "",
		Attributes  = new()
		{
			CustomDrawer = DrawAutoPromoteIDs,
			HideDefaultButton = true,
			Description =
				"""
				A list of Steam IDs that are automatically promoted when joining your server.
				Invalid IDs do nothing, so you can write comments.
				""",
		},
		OnLoaded = value =>
		{
			SplitAutoPromoteIDs  = value.Split([',', ' ', '\n'], StringSplitOptions.RemoveEmptyEntries);
			AutoPromoteIDs.Value = SplitAutoPromoteIDs.Join();
		},
	};
	static void DrawAutoPromoteIDs(ConfigEntryBase entry)
	{
		var cache = CacheAutoPromoteIDs ??= SplitAutoPromoteIDs.Join(delimiter: "\n");
		var value = GUILayout.TextArea(cache, GUILayout.ExpandWidth(true));
		// var value = GUILayout.TextArea(cache, GUILayout.Width(125));
		if (value != cache)
		{
			AutoPromoteIDs.OnLoaded(value);
			CacheAutoPromoteIDs = null;
		}
		GUILayout.Space(5);
		// if (GUILayout.Button("Reset", GUILayout.ExpandWidth(false)))
		// {
		// 	AutoPromoteIDs            = [];
		// 	EntryAutoPromoteIDs.Value = "";
		// 	CacheAutoPromoteIDs       = null;
		// }
		GUI.enabled = false;
		GUILayout.Button("Reset", GUILayout.ExpandWidth(false));
		GUI.enabled = true;
		// GUILayout.Space(5+50);
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(NetworkEvents), nameof(NetworkEvents.TriggerServerInitialized))]
	[HarmonyPatch(typeof(NetworkEvents), nameof(NetworkEvents.TriggerConnectingToServer))]
	static void TriggerServerInitializedPrefix() => UpdateNickname();

	[ModuleInitializer]
	internal static void Initializer()
		=> Events.OnPlayersAddOther += OnPlayersAddOther;
	static void OnPlayersAddOther(PlayerState playerState)
	{
		if (AutoJoinMessage.Value is [_, ..] autoJoin)
			Chat.SendChatMessage(autoJoin);

		// #idea: setting to auto-promote as admin
		if (Network.IsServer && SplitAutoPromoteIDs.Contains(playerState.SteamID))
			Wait.Frames(() => PlayerManager.Instance.PromoteThisPlayer(playerState.Name));
	}
}

