using System.Collections.Generic;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using System.Threading.Tasks;
using Polly;
using System;

namespace RawRabbit.Enrichers.Polly.Middleware
{
	public class BasicPublishMiddleware : Pipe.Middleware.BasicPublishMiddleware
	{
		public BasicPublishMiddleware(IExclusiveLock exclusive, BasicPublishOptions options = null)
			: base(exclusive, options) { }

		protected override async Task BasicPublishAsync(
				IChannel channel,
				string exchange,
				string routingKey,
				bool mandatory,
				BasicProperties basicProps,
				ReadOnlyMemory<byte> body,
				IPipeContext context)
		{
			Policy policy = context.GetPolicy(PolicyKeys.BasicPublish);
			Task<bool> policyTask = await policy.ExecuteAsync(
				action: async () =>
				{
					await base.BasicPublishAsync(channel, exchange, routingKey, mandatory, basicProps, body, context);
					return Task.FromResult(true);
				},
				contextData: new Dictionary<string, object>
				{
					[RetryKey.PipeContext] = context,
					[RetryKey.ExchangeName] = exchange,
					[RetryKey.RoutingKey] = routingKey,
					[RetryKey.PublishMandatory] = mandatory,
					[RetryKey.BasicProperties] = basicProps,
					[RetryKey.PublishBody] = body,
				});
			await policyTask.ConfigureAwait(false);
			policyTask.GetAwaiter().GetResult();
		}
	}
}
