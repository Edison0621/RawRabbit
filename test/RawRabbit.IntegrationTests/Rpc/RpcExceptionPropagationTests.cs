using System;
using System.Threading.Tasks;
using RawRabbit.Enrichers.MessageContext;
using RawRabbit.Enrichers.MessageContext.Context;
using RawRabbit.Exceptions;
using RawRabbit.Instantiation;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.Operations.Request.Middleware;
using Xunit;
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace RawRabbit.IntegrationTests.Rpc;

public class RpcExceptionPropagationTests
{
	[Fact]
	public async Task Should_Propegate_Responder_Exception_To_Requester()
	{
		using Instantiation.Disposable.BusClient requester = RawRabbitFactory.CreateTestClient();
		using Instantiation.Disposable.BusClient responder = RawRabbitFactory.CreateTestClient();
		/* Setup */
		Func<BasicRequest, Task<BasicResponse>> errorHandler = request => throw new Exception("Kaboom");
		await responder.RespondAsync(errorHandler);

		/* Test */
		/* Assert */
		await Assert.ThrowsAsync<MessageHandlerException>(
			async () => await requester.RequestAsync<BasicRequest, BasicResponse>(new BasicRequest(), cfg => cfg.UseRequestTimeout(TimeSpan.FromHours(1)))
		);
	}

	[Fact]
	public async Task Should_Propegate_Responder_Exception_To_Requester_When_Request_Handler_Is_Async()
	{
		using Instantiation.Disposable.BusClient requester = RawRabbitFactory.CreateTestClient();
		using Instantiation.Disposable.BusClient responder = RawRabbitFactory.CreateTestClient();
		/* Setup */
		Func<BasicRequest, Task<BasicResponse>> errorHandler = async request => throw new Exception("Kaboom");
		await responder.RespondAsync(errorHandler);

		/* Test */
		/* Assert */
		await Assert.ThrowsAsync<MessageHandlerException>(
			async () => await requester.RequestAsync<BasicRequest, BasicResponse>(new BasicRequest())
		);
	}

	[Fact]
	public async Task Should_Propegate_Responder_Exception_To_Requester_When_Responder_Has_Context()
	{
		using Instantiation.Disposable.BusClient requester = RawRabbitFactory.CreateTestClient();
		using Instantiation.Disposable.BusClient responder = RawRabbitFactory.CreateTestClient();
		/* Setup */

		Func<BasicRequest, MessageContext, Task<BasicResponse>> handler = (request, context) => throw new Exception("Kaboom");
		await responder.RespondAsync(handler);

		/* Test */
		/* Assert */
		await Assert.ThrowsAsync<MessageHandlerException>(
			async () => await requester.RequestAsync<BasicRequest, BasicResponse>(new BasicRequest())
		);
	}

	[Fact]
	public async Task Should_Publish_Message_To_Error_Exchange()
	{
		using Instantiation.Disposable.BusClient publisher = RawRabbitFactory.CreateTestClient();
		using Instantiation.Disposable.BusClient subscriber = RawRabbitFactory.CreateTestClient();
		/* Setup */
		BasicMessage message = new()
		{
			Prop = "I'm handled, and sent to Error Exchange"
		};
		TaskCompletionSource<BasicMessage> tsc = new();
		Func<BasicMessage, Task> errorHandler = request => throw new Exception("Kaboom");
		await subscriber.SubscribeAsync(errorHandler);
		await subscriber.SubscribeAsync<BasicMessage>(msg =>
		{
			tsc.TrySetResult(msg);
			return Task.FromResult(0);
		}, ctx => ctx
			.UseSubscribeConfiguration(cfg => cfg
				.FromDeclaredQueue(q => q.WithName("custom_error_queue"))
				.OnDeclaredExchange(e => e.WithName("default_error_exchange"))
			));

		/* Test */
		await publisher.PublishAsync(message);
		await tsc.Task;

		/* Assert */
		Assert.Equal(message.Prop, tsc.Task.Result.Prop);
	}

	[Fact]
	public async Task Should_Publish_Message_To_Error_Exchange_When_Subscriber_Has_Context()
	{
		using Instantiation.Disposable.BusClient publisher = RawRabbitFactory.CreateTestClient(new RawRabbitOptions { Plugins = p => p.UseMessageContext<MessageContext>()});
		using Instantiation.Disposable.BusClient subscriber = RawRabbitFactory.CreateTestClient();
		/* Setup */
		BasicMessage message = new()
		{
			Prop = "I'm handled, and sent to Error Exchange"
		};
		TaskCompletionSource<BasicMessage> tsc = new();
		Func<BasicMessage, MessageContext, Task> errorHandler = (msg, ctx) => throw new Exception("Kaboom");
		await subscriber.SubscribeAsync(errorHandler);
		await subscriber.SubscribeAsync<BasicMessage, MessageContext>((msg, ctx) =>
		{
			tsc.TrySetResult(msg);
			return Task.FromResult(0);
		}, ctx => ctx.UseSubscribeConfiguration(cfg => cfg
			.FromDeclaredQueue(q => q.WithName("custom_error_queue"))
			.OnDeclaredExchange(e => e.WithName("default_error_exchange"))
		));

		/* Test */
		await publisher.PublishAsync(message);
		await tsc.Task;

		/* Assert */
		Assert.Equal(message.Prop, tsc.Task.Result.Prop);
	}
}
