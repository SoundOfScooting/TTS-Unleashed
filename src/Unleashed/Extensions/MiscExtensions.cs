namespace Unleashed.Extensions;

static class MiscExtensions
{
	extension<T>(T @this)
	{
		public void Deconstruct(out T @out) => @out = @this;
	}

	extension(zInput)
	{
		public static bool GetButtonChanged(string ButtonName, ControlType CT = ControlType.All)
			=> zInput.GetButtonDown(ButtonName, CT) || zInput.GetButtonUp(ButtonName, CT);
	}

	extension(Network)
	{
		public static int ToID(int id)
			=> (id == -1) ? Network.ID : id;
	}
	extension(PlayerManager @this)
	{
		public bool IsBlinded(int ID)
			=> @this.PlayersDictionary.TryGetValue(Network.ToID(ID), out var playerState)
			&& playerState.Blind;

		public PlayerState HostPlayerState()
			=> @this.PlayerStateFromID(NetworkPlayer.SERVER_ID);
	}
}

