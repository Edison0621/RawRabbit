using System.Collections.Generic;
using RawRabbit.Common;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using System.Threading.Tasks;
using Polly.NoOp;
using RawRabbit.Channel.Abstraction;

namespace RawRabbit.Enrichers.Polly.Middleware;

public class ExplicitAckMiddleware : Pipe.Middleware.ExplicitAckMiddleware
{
	public ExplicitAckMiddleware(INamingConventions conventions, ITopologyProvider topology, IChannelFactory channelFactory, ExplicitAckOptions options = null)
		: base(conventions, topology, channelFactory, options) { }

	protected override async Task<Acknowledgement> AcknowledgeMessageAsync(IPipeContext context)
	{
		AsyncNoOpPolicy policy = context.GetPolicy(PolicyKeys.MessageAcknowledge);
		Task<Acknowledgement> result = await policy.ExecuteAsync(
			action: ct => Task.FromResult(base.AcknowledgeMessageAsync(context)),
			contextData: new Dictionary<string, object>
			{
				[RetryKey.PipeContext] = context
			});
		return await result;
	}
}
