using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using RabbitMQ.Client;
using RawRabbit.Consumer;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.Polly.Middleware;

public class ConsumerCreationMiddleware : Pipe.Middleware.ConsumerCreationMiddleware
{
	public ConsumerCreationMiddleware(IConsumerFactory consumerFactory, ConsumerCreationOptions options = null)
		: base(consumerFactory, options) { }

	protected override Task<IAsyncBasicConsumer> GetOrCreateConsumerAsync(IPipeContext context, CancellationToken token)
	{
		Policy policy = context.GetPolicy(PolicyKeys.QueueDeclare);
		return policy.ExecuteAsync(
			action: ct => base.GetOrCreateConsumerAsync(context, ct),
			cancellationToken: token,
			contextData: new Dictionary<string, object>
			{
				[RetryKey.PipeContext] = context,
				[RetryKey.CancellationToken] = token,
				[RetryKey.ConsumerFactory] = this._consumerFactory,
			}
		);
	}
}