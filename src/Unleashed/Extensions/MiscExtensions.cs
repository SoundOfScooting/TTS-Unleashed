namespace Unleashed.Extensions;

static class MiscExtensions
{
	extension<T>(T @this)
	{
		public void Deconstruct(out T @out) => @out = @this;
	}

	extension(zInput)
	{
		public static bool GetButtonChanged(string ButtonName, ControlType CT = ControlType.All) =>
			zInput.GetButtonDown(ButtonName, CT) || zInput.GetButtonUp(ButtonName, CT);
	}

	extension(NetworkID)
	{
		public static int PlayerID(int ID) =>
			(ID == -1) ? NetworkID.ID : ID;

		public static int HotseatID =>
			NetworkUI.Instance.bHotseat
				? NetworkUI.Instance.CurrentHotseat
				: NetworkID.ID;
	}
	extension(PlayerManager @this)
	{
		public bool IsBlinded(int ID) =>
			@this.PlayersDictionary.TryGetValue(NetworkID.PlayerID(ID), out var playerState) &&
			playerState.blind;

		public PlayerState HostPlayerState() =>
			@this.PlayerStateFromID(NetworkPlayer.SERVER_ID);
	}
}

