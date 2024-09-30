using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Polly.NoOp;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.Polly.Middleware;

public class HandlerInvocationMiddleware : Pipe.Middleware.HandlerInvocationMiddleware
{
	public HandlerInvocationMiddleware(HandlerInvocationOptions options = null)
		: base(options) { }

	protected override Task InvokeMessageHandler(IPipeContext context, CancellationToken token)
	{
		AsyncNoOpPolicy policy = context.GetPolicy(PolicyKeys.HandlerInvocation);
		return policy.ExecuteAsync(
			action: _ => base.InvokeMessageHandler(context, token),
			contextData: new Dictionary<string, object>
			{
				[RetryKey.PipeContext] = context,
				[RetryKey.CancellationToken] = token
			});
	}
}
