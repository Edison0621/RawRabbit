using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Polly.NoOp;
using RawRabbit.Common;
using RawRabbit.Configuration.Queue;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.Polly.Middleware;

public class QueueDeclareMiddleware : Pipe.Middleware.QueueDeclareMiddleware
{
	public QueueDeclareMiddleware(ITopologyProvider topology, QueueDeclareOptions options = null)
		: base(topology, options)
	{
	}

	protected override Task DeclareQueueAsync(QueueDeclaration queue, IPipeContext context, CancellationToken token)
	{
		AsyncNoOpPolicy policy = context.GetPolicy(PolicyKeys.QueueDeclare);
		return policy.ExecuteAsync(
			action: _ => base.DeclareQueueAsync(queue, context, token),
			contextData: new Dictionary<string, object>
			{
				[RetryKey.TopologyProvider] = this._topology,
				[RetryKey.QueueDeclaration] = queue,
				[RetryKey.PipeContext] = context,
				[RetryKey.CancellationToken] = token,
			});
	}
}
