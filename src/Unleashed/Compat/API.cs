namespace Unleashed.Compat;

static class API
{
	public const bool TRUE_ULTIMATE_POWER =
#if TRUE_ULTIMATE_POWER
#warning TRUE_ULTIMATE_POWER=1
		true;
#else
		false;
#endif
	public const bool DEBUG_COMPAT =
#if DEBUG_COMPAT
#warning DEBUG_COMPAT=1
		true;
#else
		false;
#endif

	public static bool IsServer   => !DEBUG_COMPAT && Network.isServer;
	public static bool HostModded => !DEBUG_COMPAT && PlayerManager.Instance.HostPlayerState().IsModded;
}

