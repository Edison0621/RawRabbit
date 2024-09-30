using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Polly.NoOp;
using RabbitMQ.Client;
using RawRabbit.Channel;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.Polly.Middleware;

public class PooledChannelMiddleware : Pipe.Middleware.PooledChannelMiddleware
{
	public PooledChannelMiddleware(IChannelPoolFactory poolFactory, PooledChannelOptions options = null)
		: base(poolFactory, options) { }

	protected override Task<IChannel> GetChannelAsync(IPipeContext context, CancellationToken token)
	{
		AsyncNoOpPolicy policy = context.GetPolicy(PolicyKeys.ChannelCreate);
		return policy.ExecuteAsync(
			action: ct => base.GetChannelAsync(context, token),
			contextData: new Dictionary<string, object>
			{
				[RetryKey.PipeContext] = context,
				[RetryKey.CancellationToken] = token,
				[RetryKey.ChannelPoolFactory] = this._poolFactory
			}
		);
	}
}
