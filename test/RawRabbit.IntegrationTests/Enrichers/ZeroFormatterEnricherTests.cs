using System.Threading.Tasks;
using RawRabbit.Enrichers.ZeroFormatter;
using RawRabbit.Instantiation;
using Xunit;
using ZeroFormatter;
#pragma warning disable CS1587 // XML comment is not placed on a valid language element

namespace RawRabbit.IntegrationTests.Enrichers
{
	public class ZeroFormatterEnricherTests
	{
		[Fact]
		public async Task Should_Publish_And_Subscribe_with_Zero_Formatter()
		{
			using (Instantiation.Disposable.BusClient client = RawRabbitFactory.CreateTestClient(new RawRabbitOptions { Plugins = p => p.UseZeroFormatter() }))
			{
				/** Setup **/
				TaskCompletionSource<ZeroFormatterMessage> tcs = new TaskCompletionSource<ZeroFormatterMessage>();
				ZeroFormatterMessage message = new ZeroFormatterMessage
				{
					Payload = "Zero formatter!"
				};
				await client.SubscribeAsync<ZeroFormatterMessage>(msg =>
				{
					tcs.TrySetResult(msg);
					return Task.CompletedTask;
				});

				/** Test **/
				await client.PublishAsync(message);
				await tcs.Task;

				/** Assert **/
				Assert.Equal(tcs.Task.Result.Payload, message.Payload);
			}
		}
	}

	[ZeroFormattable]
	public class ZeroFormatterMessage
	{
		[Index(0)]
		public virtual string Payload { get; set; }
	}
}
