namespace Unleashed.Extensions;

static class NetworkBehaviorX
{
	// static
	extension<T>(T @this) where T : NetworkBehavior
	{
		public void RPC(RPCTarget target, Action<T> action) =>
			@this.NetView.RPC(target, action, @this);
		public void RPC<T1>(RPCTarget target, Action<T, T1> action, T1 arg1) =>
			@this.NetView.RPC(target, action, @this, arg1);
		public void RPC<T1, T2>(RPCTarget target, Action<T, T1, T2> action, T1 arg1, T2 arg2) =>
			@this.NetView.RPC(target, action, @this, arg1, arg2);
		public void RPC<T1, T2, T3>(RPCTarget target, Action<T, T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3) =>
			@this.NetView.RPC(target, action, @this, arg1, arg2, arg3);
		public void RPC<T1, T2, T3, T4>(RPCTarget target, NetworkView.Action<T, T1, T2, T3, T4> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4) =>
			@this.NetView.RPC(target, action, @this, arg1, arg2, arg3, arg4);
		public void RPC<T1, T2, T3, T4, T5>(RPCTarget target, NetworkView.Action<T, T1, T2, T3, T4, T5> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) =>
			@this.NetView.RPC(target, action, @this, arg1, arg2, arg3, arg4, arg5);

		public void RPC<TResult>(RPCTarget target, Func<T, TResult> action) =>
			@this.NetView.RPC(target, action, @this);
		public void RPC<T1, TResult>(RPCTarget target, Func<T, T1, TResult> action, T1 arg1) =>
			@this.NetView.RPC(target, action, @this, arg1);
		public void RPC<T1, T2, TResult>(RPCTarget target, Func<T, T1, T2, TResult> action, T1 arg1, T2 arg2) =>
			@this.NetView.RPC(target, action, @this, arg1, arg2);
		public void RPC<T1, T2, T3, TResult>(RPCTarget target, Func<T, T1, T2, T3, TResult> action, T1 arg1, T2 arg2, T3 arg3) =>
			@this.NetView.RPC(target, action, @this, arg1, arg2, arg3);
		public void RPC<T1, T2, T3, T4, TResult>(RPCTarget target, Func<T, T1, T2, T3, T4, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4) =>
			@this.NetView.RPC(target, action, @this, arg1, arg2, arg3, arg4);
		public void RPC<T1, T2, T3, T4, T5, TResult>(RPCTarget target, Func<T, T1, T2, T3, T4, T5, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) =>
			@this.NetView.RPC(target, action, @this, arg1, arg2, arg3, arg4, arg5);

		public void RPC(NetworkPlayer receiver, Action<T> action) =>
			@this.NetView.RPC(receiver, action, @this);
		public void RPC<T1>(NetworkPlayer receiver, Action<T, T1> action, T1 arg1) =>
			@this.NetView.RPC(receiver, action, @this, arg1);
		public void RPC<T1, T2>(NetworkPlayer receiver, Action<T, T1, T2> action, T1 arg1, T2 arg2) =>
			@this.NetView.RPC(receiver, action, @this, arg1, arg2);
		public void RPC<T1, T2, T3>(NetworkPlayer receiver, Action<T, T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3) =>
			@this.NetView.RPC(receiver, action, @this, arg1, arg2, arg3);
		public void RPC<T1, T2, T3, T4>(NetworkPlayer receiver, NetworkView.Action<T, T1, T2, T3, T4> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4) =>
			@this.NetView.RPC(receiver, action, @this, arg1, arg2, arg3, arg4);
		public void RPC<T1, T2, T3, T4, T5>(NetworkPlayer receiver, NetworkView.Action<T, T1, T2, T3, T4, T5> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) =>
			@this.NetView.RPC(receiver, action, @this, arg1, arg2, arg3, arg4, arg5);

		public void RPC<TResult>(NetworkPlayer receiver, Func<T, TResult> action) =>
			@this.NetView.RPC(receiver, action, @this);
		public void RPC<T1, TResult>(NetworkPlayer receiver, Func<T, T1, TResult> action, T1 arg1) =>
			@this.NetView.RPC(receiver, action, @this, arg1);
		public void RPC<T1, T2, TResult>(NetworkPlayer receiver, Func<T, T1, T2, TResult> action, T1 arg1, T2 arg2) =>
			@this.NetView.RPC(receiver, action, @this, arg1, arg2);
		public void RPC<T1, T2, T3, TResult>(NetworkPlayer receiver, Func<T, T1, T2, T3, TResult> action, T1 arg1, T2 arg2, T3 arg3) =>
			@this.NetView.RPC(receiver, action, @this, arg1, arg2, arg3);
		public void RPC<T1, T2, T3, T4, TResult>(NetworkPlayer receiver, Func<T, T1, T2, T3, T4, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4) =>
			@this.NetView.RPC(receiver, action, @this, arg1, arg2, arg3, arg4);
		public void RPC<T1, T2, T3, T4, T5, TResult>(NetworkPlayer receiver, Func<T, T1, T2, T3, T4, T5, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) =>
			@this.NetView.RPC(receiver, action, @this, arg1, arg2, arg3, arg4, arg5);
	}
	// instance
	extension(NetworkBehavior @this)
	{
		public void RPC(RPCTarget target, Action action) =>
			@this.NetView.RPC(target, action);
		public void RPC<T1>(RPCTarget target, Action<T1> action, T1 arg1) =>
			@this.NetView.RPC(target, action, arg1);
		public void RPC<T1, T2>(RPCTarget target, Action<T1, T2> action, T1 arg1, T2 arg2) =>
			@this.NetView.RPC(target, action, arg1, arg2);
		public void RPC<T1, T2, T3>(RPCTarget target, Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3) =>
			@this.NetView.RPC(target, action, arg1, arg2, arg3);
		public void RPC<T1, T2, T3, T4>(RPCTarget target, Action<T1, T2, T3, T4> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4) =>
			@this.NetView.RPC(target, action, arg1, arg2, arg3, arg4);
		public void RPC<T1, T2, T3, T4, T5>(RPCTarget target, NetworkView.Action<T1, T2, T3, T4, T5> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) =>
			@this.NetView.RPC(target, action, arg1, arg2, arg3, arg4, arg5);
		public void RPC<T1, T2, T3, T4, T5, T6>(RPCTarget target, NetworkView.Action<T1, T2, T3, T4, T5, T6> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) =>
			@this.NetView.RPC(target, action, arg1, arg2, arg3, arg4, arg5, arg6);

		public void RPC<TResult>(RPCTarget target, Func<TResult> action) =>
			@this.NetView.RPC(target, action);
		public void RPC<T1, TResult>(RPCTarget target, Func<T1, TResult> action, T1 arg1) =>
			@this.NetView.RPC(target, action, arg1);
		public void RPC<T1, T2, TResult>(RPCTarget target, Func<T1, T2, TResult> action, T1 arg1, T2 arg2) =>
			@this.NetView.RPC(target, action, arg1, arg2);
		public void RPC<T1, T2, T3, TResult>(RPCTarget target, Func<T1, T2, T3, TResult> action, T1 arg1, T2 arg2, T3 arg3) =>
			@this.NetView.RPC(target, action, arg1, arg2, arg3);
		public void RPC<T1, T2, T3, T4, TResult>(RPCTarget target, Func<T1, T2, T3, T4, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4) =>
			@this.NetView.RPC(target, action, arg1, arg2, arg3, arg4);
		public void RPC<T1, T2, T3, T4, T5, TResult>(RPCTarget target, Func<T1, T2, T3, T4, T5, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) =>
			@this.NetView.RPC(target, action, arg1, arg2, arg3, arg4, arg5);
		public void RPC<T1, T2, T3, T4, T5, T6, TResult>(RPCTarget target, Func<T1, T2, T3, T4, T5, T6, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) =>
			@this.NetView.RPC(target, action, arg1, arg2, arg3, arg4, arg5, arg6);

		public void RPC(NetworkPlayer receiver, Action action) =>
			@this.NetView.RPC(receiver, action);
		public void RPC<T1>(NetworkPlayer receiver, Action<T1> action, T1 arg1) =>
			@this.NetView.RPC(receiver, action, arg1);
		public void RPC<T1, T2>(NetworkPlayer receiver, Action<T1, T2> action, T1 arg1, T2 arg2) =>
			@this.NetView.RPC(receiver, action, arg1, arg2);
		public void RPC<T1, T2, T3>(NetworkPlayer receiver, Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3) =>
			@this.NetView.RPC(receiver, action, arg1, arg2, arg3);
		public void RPC<T1, T2, T3, T4>(NetworkPlayer receiver, Action<T1, T2, T3, T4> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4) =>
			@this.NetView.RPC(receiver, action, arg1, arg2, arg3, arg4);
		public void RPC<T1, T2, T3, T4, T5>(NetworkPlayer receiver, NetworkView.Action<T1, T2, T3, T4, T5> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) =>
			@this.NetView.RPC(receiver, action, arg1, arg2, arg3, arg4, arg5);
		public void RPC<T1, T2, T3, T4, T5, T6>(NetworkPlayer receiver, NetworkView.Action<T1, T2, T3, T4, T5, T6> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) =>
			@this.NetView.RPC(receiver, action, arg1, arg2, arg3, arg4, arg5, arg6);

		public void RPC<TResult>(NetworkPlayer receiver, Func<TResult> action) =>
			@this.NetView.RPC(receiver, action);
		public void RPC<T1, TResult>(NetworkPlayer receiver, Func<T1, TResult> action, T1 arg1) =>
			@this.NetView.RPC(receiver, action, arg1);
		public void RPC<T1, T2, TResult>(NetworkPlayer receiver, Func<T1, T2, TResult> action, T1 arg1, T2 arg2) =>
			@this.NetView.RPC(receiver, action, arg1, arg2);
		public void RPC<T1, T2, T3, TResult>(NetworkPlayer receiver, Func<T1, T2, T3, TResult> action, T1 arg1, T2 arg2, T3 arg3) =>
			@this.NetView.RPC(receiver, action, arg1, arg2, arg3);
		public void RPC<T1, T2, T3, T4, TResult>(NetworkPlayer receiver, Func<T1, T2, T3, T4, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4) =>
			@this.NetView.RPC(receiver, action, arg1, arg2, arg3, arg4);
		public void RPC<T1, T2, T3, T4, T5, TResult>(NetworkPlayer receiver, Func<T1, T2, T3, T4, T5, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) =>
			@this.NetView.RPC(receiver, action, arg1, arg2, arg3, arg4, arg5);
		public void RPC<T1, T2, T3, T4, T5, T6, TResult>(NetworkPlayer receiver, Func<T1, T2, T3, T4, T5, T6, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) =>
			@this.NetView.RPC(receiver, action, arg1, arg2, arg3, arg4, arg5, arg6);
	}
}

