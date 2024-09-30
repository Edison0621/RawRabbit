using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Polly.NoOp;
using RabbitMQ.Client;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Pipe;

namespace RawRabbit.Enrichers.Polly.Middleware;

public class TransientChannelMiddleware : Pipe.Middleware.TransientChannelMiddleware
{
	public TransientChannelMiddleware(IChannelFactory factory)
		: base(factory) { }

	protected override Task<IChannel> CreateChannelAsync(IPipeContext context, CancellationToken token)
	{
		AsyncNoOpPolicy policy = context.GetPolicy(PolicyKeys.ChannelCreate);
		return policy.ExecuteAsync(
			action: _ => base.CreateChannelAsync(context, token),
			contextData: new Dictionary<string, object>
			{
				[RetryKey.PipeContext] = context,
				[RetryKey.CancellationToken] = token,
				[RetryKey.ChannelFactory] = this._channelFactory
			}
		);
	}
}
