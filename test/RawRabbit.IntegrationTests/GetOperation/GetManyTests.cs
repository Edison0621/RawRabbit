using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.Operations.Get.Model;
using Xunit;

namespace RawRabbit.IntegrationTests.GetOperation
{
	public class GetManyTests : IntegrationTestBase
	{
		[Fact]
		public async Task Should_Be_Able_To_Get_Message_When_Batch_Size_And_Queue_Length_Are_Equal()
		{
			using (Instantiation.Disposable.BusClient client = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				BasicMessage message = new BasicMessage { Prop = "Get me, get it?" };
				NamingConventions conventions = new NamingConventions();
				string exchangeName = conventions.ExchangeNamingConvention(message.GetType());
				this.TestChannel.QueueDeclare(conventions.QueueNamingConvention(message.GetType()), true, false, false, null);
				this.TestChannel.ExchangeDeclare(exchangeName, ExchangeType.Topic);
				this.TestChannel.QueueBind(conventions.QueueNamingConvention(message.GetType()), exchangeName, conventions.RoutingKeyConvention(message.GetType()) + ".#");

				await client.PublishAsync(message, ctx => ctx.UsePublishConfiguration(cfg => cfg.OnExchange(exchangeName)));
				await client.PublishAsync(message, ctx => ctx.UsePublishConfiguration(cfg => cfg.OnExchange(exchangeName)));
				await client.PublishAsync(message, ctx => ctx.UsePublishConfiguration(cfg => cfg.OnExchange(exchangeName)));

				/* Test */
				Ackable<List<Ackable<BasicMessage>>> ackable = await client.GetManyAsync<BasicMessage>(3);
				this.TestChannel.QueueDelete(conventions.QueueNamingConvention(message.GetType()));
				this.TestChannel.ExchangeDelete(exchangeName);

				/* Assert */
				Assert.NotNull(ackable);
				Assert.Equal(ackable.Content.Count, 3);
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Get_Message_When_Batch_Size_Is_Larger_Than_Queue_Length()
		{
			using (Instantiation.Disposable.BusClient client = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				BasicMessage message = new BasicMessage { Prop = "Get me, get it?" };
				NamingConventions conventions = new NamingConventions();
				string exchangeName = conventions.ExchangeNamingConvention(message.GetType());
				this.TestChannel.QueueDeclare(conventions.QueueNamingConvention(message.GetType()), true, false, false, null);
				this.TestChannel.ExchangeDeclare(conventions.ExchangeNamingConvention(message.GetType()), ExchangeType.Topic);
				this.TestChannel.QueueBind(conventions.QueueNamingConvention(message.GetType()), exchangeName, conventions.RoutingKeyConvention(message.GetType()) + ".#");

				await client.PublishAsync(message, ctx => ctx.UsePublishConfiguration(cfg => cfg.OnExchange(exchangeName)));
				await client.PublishAsync(message, ctx => ctx.UsePublishConfiguration(cfg => cfg.OnExchange(exchangeName)));
				await client.PublishAsync(message, ctx => ctx.UsePublishConfiguration(cfg => cfg.OnExchange(exchangeName)));

				/* Test */
				Ackable<List<Ackable<BasicMessage>>> ackable = await client.GetManyAsync<BasicMessage>(10);
				this.TestChannel.QueueDelete(conventions.QueueNamingConvention(message.GetType()));
				this.TestChannel.ExchangeDelete(exchangeName);

				/* Assert */
				Assert.NotNull(ackable);
				Assert.Equal(ackable.Content.Count, 3);
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Get_Message_When_Batch_Size_Is_Smaller_Than_Queue_Length()
		{
			using (Instantiation.Disposable.BusClient client = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				BasicMessage message = new BasicMessage { Prop = "Get me, get it?" };
				NamingConventions conventions = new NamingConventions();
				string exchangeName = conventions.ExchangeNamingConvention(message.GetType());
				this.TestChannel.QueueDeclare(conventions.QueueNamingConvention(message.GetType()), true, false, false, null);
				this.TestChannel.ExchangeDeclare(conventions.ExchangeNamingConvention(message.GetType()), ExchangeType.Topic);
				this.TestChannel.QueueBind(conventions.QueueNamingConvention(message.GetType()), conventions.ExchangeNamingConvention(message.GetType()), conventions.RoutingKeyConvention(message.GetType()) + ".#");

				await client.PublishAsync(message, ctx => ctx.UsePublishConfiguration(cfg => cfg.OnExchange(exchangeName)));
				await client.PublishAsync(message, ctx => ctx.UsePublishConfiguration(cfg => cfg.OnExchange(exchangeName)));
				await client.PublishAsync(message, ctx => ctx.UsePublishConfiguration(cfg => cfg.OnExchange(exchangeName)));

				/* Test */
				Ackable<List<Ackable<BasicMessage>>> ackable = await client.GetManyAsync<BasicMessage>(2);
				this.TestChannel.QueueDelete(conventions.QueueNamingConvention(message.GetType()));
				this.TestChannel.ExchangeDelete(exchangeName);

				/* Assert */
				Assert.NotNull(ackable);
				Assert.Equal(ackable.Content.Count, 2);
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Nack_One_In_Batch()
		{
			using (Instantiation.Disposable.BusClient client = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				BasicMessage message = new BasicMessage { Prop = "Get me, get it?" };
				BasicMessage nacked = new BasicMessage { Prop = "Not me! Plz?" };
				NamingConventions conventions = new NamingConventions();
				string exchangeName = conventions.ExchangeNamingConvention(message.GetType());
				this.TestChannel.QueueDeclare(conventions.QueueNamingConvention(message.GetType()), true, false, false, null);
				this.TestChannel.ExchangeDeclare(exchangeName, ExchangeType.Topic);
				this.TestChannel.QueueBind(conventions.QueueNamingConvention(message.GetType()), conventions.ExchangeNamingConvention(message.GetType()), conventions.RoutingKeyConvention(message.GetType()) + ".#");

				await client.PublishAsync(message, ctx => ctx.UsePublishConfiguration(cfg => cfg.OnExchange(exchangeName)));
				await client.PublishAsync(message, ctx => ctx.UsePublishConfiguration(cfg => cfg.OnExchange(exchangeName)));
				await client.PublishAsync(nacked, ctx => ctx.UsePublishConfiguration(cfg => cfg.OnExchange(exchangeName)));

				/* Test */
				Ackable<List<Ackable<BasicMessage>>> ackableList = await client.GetManyAsync<BasicMessage>(3);
				foreach (Ackable<BasicMessage> ackableMsg in ackableList.Content)
				{
					if (string.Equals(ackableMsg.Content.Prop, message.Prop))
					{
						ackableMsg.Ack();
					}
					else
					{
						ackableMsg.Nack();
					}
				}
				Ackable<BasicMessage> getAgain = await client.GetAsync<BasicMessage>();
				this.TestChannel.QueueDelete(conventions.QueueNamingConvention(message.GetType()));
				this.TestChannel.ExchangeDelete(exchangeName);

				/* Assert */
				Assert.NotNull(getAgain);
				Assert.Equal(getAgain.Content.Prop, nacked.Prop);
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Ack_Messages_And_Then_Full_List()
		{
			using (Instantiation.Disposable.BusClient client = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				BasicMessage message = new BasicMessage { Prop = "Get me, get it?" };
				NamingConventions conventions = new NamingConventions();
				string exchangeName = conventions.ExchangeNamingConvention(message.GetType());
				this.TestChannel.QueueDeclare(conventions.QueueNamingConvention(message.GetType()), true, false, false, null);
				this.TestChannel.ExchangeDeclare(conventions.ExchangeNamingConvention(message.GetType()), ExchangeType.Topic);
				this.TestChannel.QueueBind(conventions.QueueNamingConvention(message.GetType()), exchangeName, conventions.RoutingKeyConvention(message.GetType()) + ".#");

				await client.PublishAsync(message, ctx => ctx.UsePublishConfiguration(cfg => cfg.OnExchange(exchangeName)));
				await client.PublishAsync(message, ctx => ctx.UsePublishConfiguration(cfg => cfg.OnExchange(exchangeName)));
				await client.PublishAsync(message, ctx => ctx.UsePublishConfiguration(cfg => cfg.OnExchange(exchangeName)));

				/* Test */
				Ackable<List<Ackable<BasicMessage>>> ackable = await client.GetManyAsync<BasicMessage>(3);
				ackable.Content[1].Ack();
				ackable.Ack();
				this.TestChannel.QueueDelete(conventions.QueueNamingConvention(message.GetType()));
				this.TestChannel.ExchangeDelete(exchangeName);

				/* Assert */
				Assert.True(true);
			}
		}
	}
}
