using System.Threading.Tasks;
using MessagePack;
using RawRabbit.Enrichers.MessagePack;
using RawRabbit.Instantiation;
using Xunit;
#pragma warning disable CS1587 // XML comment is not placed on a valid language element

namespace RawRabbit.IntegrationTests.Enrichers;

public class MessagePackTests
{
	[Fact]
	public async Task Should_Publish_And_Subscribe_with_Zero_Formatter()
	{
		using Instantiation.Disposable.BusClient client = RawRabbitFactory.CreateTestClient(new RawRabbitOptions { Plugins = p => p.UseMessagePack() });
		/** Setup **/
		TaskCompletionSource<MessagePackMessage> tcs = new();
		MessagePackMessage message = new()
		{
			TagLine = "Extremely Fast MessagePack Serializer for C#"
		};
		await client.SubscribeAsync<MessagePackMessage>(msg =>
		{
			tcs.TrySetResult(msg);
			return Task.CompletedTask;
		});

		/** Test **/
		await client.PublishAsync(message);
		await tcs.Task;

		/** Assert **/
		Assert.Equal(tcs.Task.Result.TagLine, message.TagLine);
	}
}

[MessagePackObject]
public class MessagePackMessage
{
	[Key(0)]
	public string TagLine { get; set; }
}
