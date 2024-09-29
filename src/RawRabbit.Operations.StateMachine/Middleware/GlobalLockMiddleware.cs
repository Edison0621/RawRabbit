using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Operations.StateMachine.Core;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.StateMachine.Middleware
{
	public class GlobalLockMiddleware : Pipe.Middleware.Middleware
	{
		private readonly IGlobalLock _globalLock;

		public GlobalLockMiddleware(IGlobalLock globalLock)
		{
			this._globalLock = globalLock;
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
		{
			return this._globalLock.ExecuteAsync(context.Get<Guid>(StateMachineKey.ModelId), () => this.Next.InvokeAsync(context, token), token);
		}
	}
}
