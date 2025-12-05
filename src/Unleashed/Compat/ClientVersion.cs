namespace Unleashed.Compat;

[HarmonyPatch]
static class ClientVersion
{
	[RemoteX(Permission.Server)]
	static void RPCSetIsModded(PlayerManager @this, ushort id) =>
		@this.PlayerStateFromID(id).IsModded = true;

	const string VERSION_HEADER = $"\n{Main.PLUGIN_GUID} V";

	[HarmonyILManipulator]
	[HarmonyPatch(typeof(NetworkUI), nameof(NetworkUI.ConnectedToServer))]
	static void ConnectedToServerIL(ILContext il)
	{
		var c = new ILCursor(il);
		c.GotoNext(MoveType.After,
			// base.networkView.RPC(RPCTarget.Server, Register, playerName, VersionNumber, SystemInfo.deviceUniqueIdentifier, VRHMD.isVR);
			x => x.MatchCall(AccessTools.PropertyGetter(typeof(NetworkUI), nameof(NetworkUI.VersionNumber)))
		);
		c.EmitDelegate(string(string VersionNumber) =>
			VersionNumber + VERSION_HEADER + Main.PLUGIN_VERSION
		);
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(NetworkUI), nameof(NetworkUI.Register))]
	static void RegisterPrefix(ref NetworkPlayer __state) =>
		__state = Network.sender;
	[HarmonyPostfix]
	[HarmonyPatch(typeof(NetworkUI), nameof(NetworkUI.Register))]
	static void RegisterPostfix(string name, string versionnum, NetworkPlayer __state)
	{
		var sender = __state;
		var s = versionnum.IndexOf(VERSION_HEADER, StringComparison.Ordinal);
		if (s < 0)
			return;

		s += VERSION_HEADER.Length;
		var e = versionnum.IndexOf("\n", s, StringComparison.Ordinal);
		if (e < 0)
			e = versionnum.Length;

		var clientVersion = new Version(versionnum[s..e]);
		var hostVersion   = Main.Instance.Info.Metadata.Version;
		if (clientVersion.Major != hostVersion.Major || clientVersion.Minor != hostVersion.Minor)
		{
			Chat.SendChat($"{Colour.YellowHex}{name} is running incompatible {Main.PluginColour.RGBHex}{Main.PLUGIN_NAME}[-] version V{clientVersion}.");
			return;
		}
		// Chat.SendChat($"{Colour.GreenHex}{name} is running compatible {Main.PluginColour.RGBHex}{Main.PLUGIN_NAME}[-] version V{clientVersion}.");
		Wait.Frames(() => Main.Catch(() => 
		{
			PlayerManager.Instance.PlayerStateFromID(sender.id).IsModded = true;
			PlayerManager.Instance.RPC(sender, RPCSetIsModded, NetworkPlayer.SERVER_ID);
		}));
	}
}

