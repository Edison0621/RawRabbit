using System;
using System.Threading.Tasks;
using RawRabbit.Instantiation;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.Operations.Get.Model;
using Xunit;

namespace RawRabbit.IntegrationTests.Features;

public class GracefulShutdownTest : IntegrationTestBase
{
	[Fact]
	public async Task Should_Cancel_Subscription_When_Shutdown_Is_Called()
	{
		Instantiation.Disposable.BusClient singleton = RawRabbitFactory.CreateTestClient();
		InstanceFactory instanceFactory = RawRabbitFactory.CreateTestInstanceFactory();
		IBusClient client = instanceFactory.Create();
		int processMs =50;
		BasicMessage firstMsg = (new() { Prop = "I'll get processed" });
		BasicMessage secondMsg = (new() { Prop = "I'll get stuck in the queue" });

		TaskCompletionSource<BasicMessage> firstTsc = new();
		await client.SubscribeAsync<BasicMessage>(async message =>
		{
			firstTsc.TrySetResult(message);
			await Task.Delay(processMs);
		}, ctx => ctx
			.UseSubscribeConfiguration(cfg => cfg
				.FromDeclaredQueue(q => q
					.WithAutoDelete(false)
				)
			));

		await client.PublishAsync(firstMsg);
		await firstTsc.Task;
		Task shutdownTask = instanceFactory.ShutdownAsync(TimeSpan.FromMilliseconds(processMs));
		await Task.Delay(TimeSpan.FromMilliseconds(10));
		await singleton.PublishAsync(secondMsg);
		await shutdownTask;

		Ackable<BasicMessage> secondReceived = await singleton.GetAsync<BasicMessage>(get => get.WithAutoAck());
		await singleton.DeleteQueueAsync<BasicMessage>();
		await singleton.DeleteExchangeAsync<BasicMessage>();
		singleton.Dispose();

		Assert.Equal(firstMsg.Prop, firstTsc.Task.Result.Prop);
		Assert.Equal(secondMsg.Prop, secondReceived.Content.Prop);
	}
}
