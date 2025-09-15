namespace Unleashed.Compat;

static class API
{
	public static bool HostModded => PlayerManager.Instance.HostPlayerState().IsModded;
}

