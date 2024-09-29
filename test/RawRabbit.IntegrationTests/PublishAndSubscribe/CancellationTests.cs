using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.IntegrationTests.TestMessages;
using Xunit;
// ReSharper disable All

namespace RawRabbit.IntegrationTests.PublishAndSubscribe
{
	public class CancellationTests
	{
		[Fact]
		public async Task Should_Honor_Task_Cancellation()
		{
			using (Instantiation.Disposable.BusClient publisher = RawRabbitFactory.CreateTestClient())
			using (Instantiation.Disposable.BusClient subscriber = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				BasicMessage message = new BasicMessage {Prop = Guid.NewGuid().ToString()};
				TaskCompletionSource<BasicMessage> receivedTcs = new TaskCompletionSource<BasicMessage>();
				CancellationTokenSource sendCts = new CancellationTokenSource();
				await subscriber.SubscribeAsync<BasicMessage>(received =>
				{
					if (received.Prop == message.Prop)
					{
						receivedTcs.TrySetResult(received);
					}
					return Task.FromResult(true);
				});

				/* Test */
				sendCts.CancelAfter(TimeSpan.FromTicks(400));
				Task publishTask = publisher.PublishAsync(new BasicMessage(), token: sendCts.Token);
				receivedTcs.Task.Wait(100);

				/* Assert */
				Assert.False(receivedTcs.Task.IsCompleted, "Message was sent, even though execution was cancelled.");
				Assert.True(publishTask.IsCanceled, "The publish task should be cancelled.");
			}
		}
	}
}
