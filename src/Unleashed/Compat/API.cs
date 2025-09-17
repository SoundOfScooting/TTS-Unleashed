namespace Unleashed.Compat;

static class API
{
	const bool DEBUG_COMPAT =
#if DEBUG_COMPAT
		true;
#else
		false;
#endif
	public static bool IsServer   => !DEBUG_COMPAT && Network.isServer;
	public static bool HostModded => !DEBUG_COMPAT && PlayerManager.Instance.HostPlayerState().IsModded;
}

