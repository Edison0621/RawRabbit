using System.Threading.Tasks;
using RawRabbit.IntegrationTests.TestMessages;
using Xunit;

namespace RawRabbit.IntegrationTests.Features
{
	public class GenericMessagesTest
	{
		[Fact]
		public async void Should_Be_Able_To_Subscribe_To_Generic_Message()
		{
			using (Instantiation.Disposable.BusClient subscriber = RawRabbitFactory.CreateTestClient())
			using (Instantiation.Disposable.BusClient publisher = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				TaskCompletionSource<GenericMessage<int>> doneTsc = new TaskCompletionSource<GenericMessage<int>>();
				GenericMessage<int> message = new GenericMessage<int>
				{
					Prop = 7
				};
				await subscriber.SubscribeAsync<GenericMessage<int>>(received =>
					{
						doneTsc.TrySetResult(received);
						return Task.FromResult(0);
					} 
				);
				/* Test */
				await publisher.PublishAsync(message);
				await doneTsc.Task;

				/* Assert */
				Assert.Equal(doneTsc.Task.Result.Prop, message.Prop);
			}
		}
	}
}
