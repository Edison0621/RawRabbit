using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.Operations.Get.Model;
using Xunit;

namespace RawRabbit.IntegrationTests.GetOperation
{
	public class BasicGetTests : IntegrationTestBase
	{
		[Fact]
		public async Task Should_Be_Able_To_Get_Message()
		{
			using (Instantiation.Disposable.BusClient client = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				BasicMessage message = new BasicMessage {Prop = "Get me, get it?"};
				NamingConventions conventions = new NamingConventions();
				string exchangeName = conventions.ExchangeNamingConvention(message.GetType());
				this.TestChannel.QueueDeclare(conventions.QueueNamingConvention(message.GetType()), true, false, false, null);
				this.TestChannel.ExchangeDeclare(exchangeName, ExchangeType.Topic);
				this.TestChannel.QueueBind(conventions.QueueNamingConvention(message.GetType()), exchangeName, conventions.RoutingKeyConvention(message.GetType()) + ".#");

				await client.PublishAsync(message, ctx => ctx.UsePublishConfiguration(cfg => cfg.OnExchange(exchangeName)));

				/* Test */
				Ackable<BasicMessage> ackable = await client.GetAsync<BasicMessage>();

				/* Assert */
				Assert.NotNull(ackable);
				Assert.Equal(ackable.Content.Prop, message.Prop);
				this.TestChannel.QueueDelete(conventions.QueueNamingConvention(message.GetType()));
				this.TestChannel.ExchangeDelete(exchangeName);
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Get_BasicGetResult_Message()
		{
			using (Instantiation.Disposable.BusClient client = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				BasicMessage message = new BasicMessage { Prop = "Get me, get it?" };
				NamingConventions conventions = new NamingConventions();
				string exchangeName = conventions.ExchangeNamingConvention(message.GetType());
				string queueName = conventions.QueueNamingConvention(message.GetType());
				this.TestChannel.QueueDeclare(queueName, true, false, false, null);
				this.TestChannel.ExchangeDeclare(exchangeName, ExchangeType.Topic);
				this.TestChannel.QueueBind(queueName, exchangeName, conventions.RoutingKeyConvention(message.GetType()) + ".#");

				await client.PublishAsync(message, ctx => ctx.UsePublishConfiguration(cfg => cfg.OnExchange(exchangeName)));

				/* Test */
				Ackable<BasicGetResult> ackable = await client.GetAsync(cfg => cfg.FromQueue(queueName));

				/* Assert */
				Assert.NotNull(ackable);
				Assert.NotEmpty(ackable.Content.Body);
				this.TestChannel.QueueDelete(queueName);
				this.TestChannel.ExchangeDelete(exchangeName);
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Get_BasicGetResult_When_Queue_IsEmpty()
		{
			using (Instantiation.Disposable.BusClient client = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				BasicMessage message = new BasicMessage();
				NamingConventions conventions = new NamingConventions();
				string queueName = conventions.QueueNamingConvention(message.GetType());
				this.TestChannel.QueueDeclare(queueName, true, false, false, null);

				/* Test */
				Ackable<BasicGetResult> ackable = await client.GetAsync(cfg => cfg.FromQueue(queueName));

				/* Assert */
				Assert.NotNull(ackable);
				Assert.Null(ackable.Content);
				this.TestChannel.QueueDelete(queueName);
			}
		}
	}
}
