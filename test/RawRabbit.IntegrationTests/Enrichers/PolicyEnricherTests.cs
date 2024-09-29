using System;
using System.Threading.Tasks;
using Polly;
using Polly.Fallback;
using Polly.Retry;
using RabbitMQ.Client.Exceptions;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.Configuration.Queue;
using RawRabbit.Enrichers.Polly;
using RawRabbit.Instantiation;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.IntegrationTests.Enrichers
{
	public class PolicyEnricherTests
	{
		[Fact]
		public async Task Should_Use_Custom_Policy()
		{
			bool defaultCalled = false;
			bool customCalled = false;
			FallbackPolicy defaultPolicy = Policy
				.Handle<Exception>()
				.FallbackAsync(ct =>
				{
					defaultCalled = true;
					return Task.FromResult(0);
				});
			RetryPolicy declareQueuePolicy = Policy
				.Handle<OperationInterruptedException>()
				.RetryAsync(async (e, retryCount, ctx) =>
				{
					customCalled = true;
					GeneralQueueConfiguration defaultQueueCfg = ctx.GetPipeContext().GetClientConfiguration().Queue;
					ITopologyProvider topology = ctx.GetTopologyProvider();
					QueueDeclaration queue = new QueueDeclaration(defaultQueueCfg) { Name = ctx.GetQueueName(), Durable = false};
					await topology.DeclareQueueAsync(queue);
				});

			RawRabbitOptions options = new RawRabbitOptions
			{
				Plugins = p => p.UsePolly(c => c
					.UsePolicy(defaultPolicy)
					.UsePolicy(declareQueuePolicy, PolicyKeys.QueueBind)
				)
			};

			using (Instantiation.Disposable.BusClient client = RawRabbitFactory.CreateTestClient(options))
			{
				await client.SubscribeAsync<BasicMessage>(
					message => Task.FromResult(0),
					ctx => ctx.UseSubscribeConfiguration(cfg => cfg
						.Consume(c => c
							.FromQueue("does_not_exist"))
					));
			}

			Assert.True(customCalled);
			Assert.False(defaultCalled, "The custom retry policy should be called");
		}
	}
}
