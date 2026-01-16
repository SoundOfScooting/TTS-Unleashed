using Unleashed.Compat;

namespace Unleashed.Extensions;

static class NetworkPhysicsObjectX
{
	sealed class Data : MonoBehaviour
	{
		public int HeldTiltRotationIndex;
	}
	extension(NetworkPhysicsObject @this)
	{
		Data Data => @this.gameObject.GetOrAddComponent<Data>();
		public bool HasData() => @this.TryGetComponent<Data>(out _);
		public ref int HeldTiltRotationIndex => ref @this.Data.HeldTiltRotationIndex;
	}
	// extension(PlayerAction) // #todo: would prefer string IDs on Lua side
	// {
	// 	public static PlayerAction TiltIncrementalRight => PlayerAction.Under+1;
	// 	public static PlayerAction TiltOver             => PlayerAction.Under+2;
	// 	public static PlayerAction TiltIncrementalLeft  => PlayerAction.Under+3;
	// }
	extension(Pointer @this)
	{
		[RemoteX(Permission.Owner, SendType.ReliableNoDelay)]
		public void ChangeHeldTiltRotationIndex(int tiltRotationDelta, int touchId = -1)
		{
			if (tiltRotationDelta == 0)
				return;
			if (Network.isClient)
			{
				if (API.HostModded)
					@this.RPC(RPCTarget.Server, @this.ChangeHeldTiltRotationIndex, tiltRotationDelta, touchId);
				return;
			}
			// #todo: wrong
			var action = tiltRotationDelta switch
			{
				>= 1 and <= 11 => PlayerAction.FlipIncrementalRight,
				12             => PlayerAction.FlipOver,
				_              => PlayerAction.FlipIncrementalLeft,
			};
			if (!EventManager.CheckPlayerAction(@this.PointerColorLabel, action, @this.GetGrabbedLuaObjects(touchId)))
				return;
			foreach (var grabbableNPO in ManagerPhysicsObject.Instance.GrabbableNPOs)
			if      (grabbableNPO.HeldByPlayerID == @this.ID && grabbableNPO.HeldByTouchID == touchId)
				ManagerPhysicsObject.Instance.SetHeldObjectTiltRotationIndex(grabbableNPO, tiltRotationDelta, @this.ID);
		}
	}
	extension(ManagerPhysicsObject @this)
	{
		public void SetHeldObjectTiltRotationIndex(NetworkPhysicsObject npo, int tiltDelta, int id)
		{
			var heldFlipRotationIndex = npo.HeldFlipRotationIndex;
			var heldSpinRotationIndex = npo.HeldSpinRotationIndex;
			var heldTiltRotationIndex = npo.HeldTiltRotationIndex;
			var num = (heldTiltRotationIndex + tiltDelta) % 24;

			var luaGameObjectScript = npo.luaGameObjectScript;
			var playerColor = PlayerManager.Instance.PlayerStateFromID(id)?.stringColor;
			// #todo: wrong
			if (!luaGameObjectScript || luaGameObjectScript.CheckObjectRotate(heldSpinRotationIndex, num, playerColor, heldSpinRotationIndex, heldFlipRotationIndex))
			{
				npo.HeldTiltRotationIndex = num;
				npo.DisableFastDragWhileAnimating();
				// #todo: wrong
				EventManager.TriggerObjectRotate(npo, heldSpinRotationIndex, num, playerColor, heldSpinRotationIndex, heldFlipRotationIndex);
			}
		}
	}
}

