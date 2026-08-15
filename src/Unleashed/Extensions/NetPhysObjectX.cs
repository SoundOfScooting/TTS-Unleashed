using Unleashed.Compat;

namespace Unleashed.Extensions;

static class PlayerActionX
{
	extension(PlayerAction)
	{
		// #todo: pick unique values, add to Player.Action (LuaGlobalPlayer.LuaAction)
		public static PlayerAction TiltIncrementalRight => PlayerAction.FlipIncrementalRight;
		public static PlayerAction TiltOver             => PlayerAction.FlipOver;
		public static PlayerAction TiltIncrementalLeft  => PlayerAction.FlipIncrementalLeft;
	}
}

static class NetPhysObjectX
{
	sealed class Data : MonoBehaviour
	{
		public bool Ephemeral;
		public int HeldTiltRotationIndex;
	}
	extension(NetPhysObject @this)
	{
		Data TryData => @this.TryGetComponent<Data>(out var data) ? data : null;
		Data NewData => @this.gameObject.GetOrAddComponent<Data>();

		public bool Ephemeral
		{
			get => @this.TryData?.Ephemeral ?? default;
			set => @this.NewData.Ephemeral = value;
		}
		public int HeldTiltRotationIndex
		{
			get => @this.TryData?.HeldTiltRotationIndex ?? default;
			set => @this.NewData.HeldTiltRotationIndex = value;
		}
	}
	extension(Pointer @this)
	{
		[RemoteX(Permission.Owner, SendType.ReliableNoDelay)]
		public void ChangeHeldTiltRotationIndex(int tiltRotationDelta, int touchId = -1)
		{
			if (tiltRotationDelta == 0)
				return;
			if (Network.IsClient)
			{
				if (API.HostModded)
					@this.RPC(RPCTarget.Server, @this.ChangeHeldTiltRotationIndex, tiltRotationDelta, touchId);
				return;
			}
			var action = tiltRotationDelta switch
			{
				>= 1 and <= 11 => PlayerAction.TiltIncrementalRight,
				12             => PlayerAction.TiltOver,
				_              => PlayerAction.TiltIncrementalLeft,
			};
			if (!EventManager.CheckPlayerAction(@this.PointerColorLabel, action, @this.GetGrabbedLuaObjects(touchId)))
				return;
			foreach (var grabbableNPO in ManagerPhysicsObject.Instance.GrabbableNPOs)
			if      (grabbableNPO.HeldByPlayerID == @this.ID && grabbableNPO.HeldByTouchID == touchId)
				ManagerPhysicsObject.Instance.SetHeldObjectTiltRotationIndex(grabbableNPO, tiltRotationDelta, @this.ID);
		}

		public void Ephemeral(bool ephemeral)
		{
			foreach (var npo in @this.GetSelectedNPOs())
			if      (npo)
			{
				npo.Ephemeral = ephemeral;
				npo.HighlightNotify(@this.PointerDarkColour);
			}
		}
	}
	extension(ManagerPhysicsObject @this)
	{
		public void SetHeldObjectTiltRotationIndex(NetPhysObject npo, int tiltDelta, int id)
		{
			_ = @this;
			var heldFlipRotationIndex = npo.HeldFlipRotationIndex;
			var heldSpinRotationIndex = npo.HeldSpinRotationIndex;
			var heldTiltRotationIndex = npo.HeldTiltRotationIndex;
			var num = (heldTiltRotationIndex + tiltDelta) % 24;

			var lua = npo.Lua;
			var playerColor = PlayerManager.Instance.PlayerStateFromID(id)?.ColorLabel;
			// #todo: wrong
			if (!lua || lua.CheckObjectRotate(heldSpinRotationIndex, num, playerColor, heldSpinRotationIndex, heldFlipRotationIndex))
			{
				npo.HeldTiltRotationIndex = num;
				npo.DisableFastDragWhileAnimating();
				// #todo: wrong
				EventManager.TriggerObjectRotate(npo, heldSpinRotationIndex, num, playerColor, heldSpinRotationIndex, heldFlipRotationIndex);
			}
		}
	}
}

