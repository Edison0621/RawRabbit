using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.Operations.Get.Model;
using Xunit;

namespace RawRabbit.IntegrationTests.GetOperation;

public class BasicGetTests : IntegrationTestBase
{
	[Fact]
	public async Task Should_Be_Able_To_Get_Message()
	{
		using Instantiation.Disposable.BusClient client = RawRabbitFactory.CreateTestClient();
		/* Setup */
		BasicMessage message = new() {Prop = "Get me, get it?"};
		NamingConventions conventions = new();
		string exchangeName = conventions.ExchangeNamingConvention(message.GetType());
		await this.TestChannel.QueueDeclareAsync(conventions.QueueNamingConvention(message.GetType()), true, false, false);
		await this.TestChannel.ExchangeDeclareAsync(exchangeName, ExchangeType.Topic);
		await this.TestChannel.QueueBindAsync(conventions.QueueNamingConvention(message.GetType()), exchangeName, conventions.RoutingKeyConvention(message.GetType()) + ".#");

		await client.PublishAsync(message, ctx => ctx.UsePublishConfiguration(cfg => cfg.OnExchange(exchangeName)));

		/* Test */
		Ackable<BasicMessage> ackable = await client.GetAsync<BasicMessage>();

		/* Assert */
		Assert.NotNull(ackable);
		Assert.Equal(ackable.Content.Prop, message.Prop);
		await this.TestChannel.QueueDeleteAsync(conventions.QueueNamingConvention(message.GetType()));
		await this.TestChannel.ExchangeDeleteAsync(exchangeName);
	}

	[Fact]
	public async Task Should_Be_Able_To_Get_BasicGetResult_Message()
	{
		using Instantiation.Disposable.BusClient client = RawRabbitFactory.CreateTestClient();
		/* Setup */
		BasicMessage message = new() { Prop = "Get me, get it?" };
		NamingConventions conventions = new();
		string exchangeName = conventions.ExchangeNamingConvention(message.GetType());
		string queueName = conventions.QueueNamingConvention(message.GetType());
		await this.TestChannel.QueueDeclareAsync(queueName, true, false, false);
		await this.TestChannel.ExchangeDeclareAsync(exchangeName, ExchangeType.Topic);
		await this.TestChannel.QueueBindAsync(queueName, exchangeName, conventions.RoutingKeyConvention(message.GetType()) + ".#");

		await client.PublishAsync(message, ctx => ctx.UsePublishConfiguration(cfg => cfg.OnExchange(exchangeName)));

		/* Test */
		Ackable<BasicGetResult> ackable = await client.GetAsync(cfg => cfg.FromQueue(queueName));

		/* Assert */
		Assert.NotNull(ackable);
		Assert.NotNull(ackable.Content.Body);
		await this.TestChannel.QueueDeleteAsync(queueName);
		await this.TestChannel.ExchangeDeleteAsync(exchangeName);
	}

	[Fact]
	public async Task Should_Be_Able_To_Get_BasicGetResult_When_Queue_IsEmpty()
	{
		using Instantiation.Disposable.BusClient client = RawRabbitFactory.CreateTestClient();
		/* Setup */
		BasicMessage message = new();
		NamingConventions conventions = new();
		string queueName = conventions.QueueNamingConvention(message.GetType());
		await this.TestChannel.QueueDeclareAsync(queueName, true, false, false);

		/* Test */
		Ackable<BasicGetResult> ackable = await client.GetAsync(cfg => cfg.FromQueue(queueName));

		/* Assert */
		Assert.NotNull(ackable);
		Assert.Null(ackable.Content);
		await this.TestChannel.QueueDeleteAsync(queueName);
	}
}
