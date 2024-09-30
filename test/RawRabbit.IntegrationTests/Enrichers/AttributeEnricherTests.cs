using System.Threading.Tasks;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Enrichers.Attributes;
using RawRabbit.Instantiation;
using Xunit;

namespace RawRabbit.IntegrationTests.Enrichers;

public class AttributeEnricherTests
{
	[Fact]
	public async Task Should_Work_For_Publish()
	{
		using Instantiation.Disposable.BusClient publisher = RawRabbitFactory.CreateTestClient(new RawRabbitOptions { Plugins = plugin => plugin.UseAttributeRouting() });
		using Instantiation.Disposable.BusClient subscriber = RawRabbitFactory.CreateTestClient();
		/* Setup */
		TaskCompletionSource<AttributedMessage> receivedTcs = new();
		await subscriber.SubscribeAsync<AttributedMessage>(received =>
		{
			receivedTcs.TrySetResult(received);
			return Task.FromResult(true);
		}, ctx => ctx
			.UseSubscribeConfiguration(cfg => cfg
				.Consume(c => c
					.WithRoutingKey("my_key"))
				.OnDeclaredExchange(e => e
					.WithName("my_topic")
					.WithType(ExchangeType.Topic))
			));

		/* Test */
		await publisher.PublishAsync(new AttributedMessage());
		await receivedTcs.Task;

		/* Assert */
		Assert.True(true, "Routing successful");
	}

	[Fact]
	public async Task Should_Work_For_Subscribe()
	{
		using Instantiation.Disposable.BusClient subscriber = RawRabbitFactory.CreateTestClient(new RawRabbitOptions { Plugins = plugin => plugin.UseAttributeRouting() });
		using Instantiation.Disposable.BusClient publisher = RawRabbitFactory.CreateTestClient();
		/* Setup */
		TaskCompletionSource<AttributedMessage> receivedTcs = new();
		await subscriber.SubscribeAsync<AttributedMessage>(received =>
		{
			receivedTcs.TrySetResult(received);
			return Task.FromResult(true);
		});

		/* Test */
		await publisher.PublishAsync(new AttributedMessage(), ctx => ctx
			.UsePublishConfiguration(cfg => cfg
				.OnDeclaredExchange(e => e
					.WithName("my_topic")
					.WithType(ExchangeType.Topic))
				.WithRoutingKey("my_key")
			));
		await receivedTcs.Task;

		/* Assert */
		Assert.True(true, "Routing successful");
	}

	[Fact]
	public async Task Should_Work_For_Request()
	{
		using Instantiation.Disposable.BusClient requester = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
			{
				Plugins = plugin => plugin.UseAttributeRouting()
			}
		);
		using Instantiation.Disposable.BusClient responder = RawRabbitFactory.CreateTestClient();
		/* Setup */
		TaskCompletionSource<AttributedRequest> receivedTcs = new();
		await responder.RespondAsync<AttributedRequest, AttributedResponse>(received =>
		{
			receivedTcs.TrySetResult(received);
			return Task.FromResult(new AttributedResponse());
		}, ctx => ctx.UseRespondConfiguration(cfg => cfg
			.Consume(c => c
				.WithRoutingKey("my_request_key"))
			.OnDeclaredExchange(e => e
				.WithName("rpc_exchange")
				.WithType(ExchangeType.Topic))
		));

		/* Test */
		await requester.RequestAsync<AttributedRequest, AttributedResponse>(new AttributedRequest());
		await receivedTcs.Task;

		/* Assert */
		Assert.True(true, "Routing successful");
	}

	[Fact]
	public async Task Should_Work_For_Responder()
	{
		using Instantiation.Disposable.BusClient responder = RawRabbitFactory.CreateTestClient(new RawRabbitOptions { Plugins = plugin => plugin.UseAttributeRouting() });
		using Instantiation.Disposable.BusClient requester = RawRabbitFactory.CreateTestClient();
		/* Setup */
		TaskCompletionSource<AttributedRequest> receivedTcs = new();
		await responder.RespondAsync<AttributedRequest, AttributedResponse>(received =>
		{
			receivedTcs.TrySetResult(received);
			return Task.FromResult(new AttributedResponse());
		});

		/* Test */
		await requester.RequestAsync<AttributedRequest, AttributedResponse>(new AttributedRequest(), ctx => ctx
			.UseRequestConfiguration(cfg => cfg
				.PublishRequest(req => req
					.OnDeclaredExchange(e => e
						.WithName("rpc_exchange")
						.WithType(ExchangeType.Topic))
					.WithRoutingKey("my_request_key")
				)
			));
		await receivedTcs.Task;

		/* Assert */
		Assert.True(true, "Routing successful");
	}

	[Fact]
	public async Task Should_Work_For_Full_Rpc()
	{
		using Instantiation.Disposable.BusClient responder = RawRabbitFactory.CreateTestClient(new RawRabbitOptions { Plugins = plugin => plugin.UseAttributeRouting() });
		using Instantiation.Disposable.BusClient requester = RawRabbitFactory.CreateTestClient(new RawRabbitOptions { Plugins = plugin => plugin.UseAttributeRouting() });
		/* Setup */
		TaskCompletionSource<AttributedRequest> receivedTcs = new();
		await responder.RespondAsync<AttributedRequest, AttributedResponse>(received =>
		{
			receivedTcs.TrySetResult(received);
			return Task.FromResult(new AttributedResponse());
		});

		/* Test */
		await requester.RequestAsync<AttributedRequest, AttributedResponse>(new AttributedRequest());
		await receivedTcs.Task;

		/* Assert */
		Assert.True(true, "Routing successful");
	}

	[Fact]
	public async Task Should_Work_For_Pub_Sub()
	{
		using Instantiation.Disposable.BusClient publisher = RawRabbitFactory.CreateTestClient(new RawRabbitOptions { Plugins = plugin => plugin.UseAttributeRouting() });
		using Instantiation.Disposable.BusClient subscriber = RawRabbitFactory.CreateTestClient(new RawRabbitOptions { Plugins = plugin => plugin.UseAttributeRouting() });
		/* Setup */
		TaskCompletionSource<AttributedMessage> receivedTcs = new();
		await subscriber.SubscribeAsync<AttributedMessage>(received =>
		{
			receivedTcs.TrySetResult(received);
			return Task.FromResult(new AttributedResponse());
		});

		/* Test */
		await publisher.PublishAsync(new AttributedMessage());
		await receivedTcs.Task;

		/* Assert */
		Assert.True(true, "Routing successful");
	}

	[Queue(Name = "my_queue", MessageTtl = 300, DeadLeterExchange = "dlx", Durable = false)]
	[Exchange(Name = "my_topic", Type = ExchangeType.Topic)]
	[Routing(RoutingKey = "my_key", AutoAck = true, PrefetchCount = 50)]
	private sealed class AttributedMessage { }

	[Queue(Name = "attributed_request", MessageTtl = 300, DeadLeterExchange = "dlx", Durable = false)]
	[Exchange(Name = "rpc_exchange", Type = ExchangeType.Topic)]
	[Routing(RoutingKey = "my_request_key", AutoAck = true, PrefetchCount = 50)]
	private sealed class AttributedRequest { }

	[Queue(Name = "attributed_response", MessageTtl = 300, DeadLeterExchange = "dlx", Durable = false)]
	[Exchange(Name = "rpc_exchange", Type = ExchangeType.Topic)]
	[Routing(RoutingKey = "my_response_key", AutoAck = true, PrefetchCount = 50)]
	private sealed class AttributedResponse { }
}
