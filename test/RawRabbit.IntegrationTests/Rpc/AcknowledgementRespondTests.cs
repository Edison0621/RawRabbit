#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using System.Threading.Tasks;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.Operations.Respond.Acknowledgement;
using Xunit;

namespace RawRabbit.IntegrationTests.Rpc;

public class AcknowledgementRespondTests
{
	[Fact]
	public async Task Should_Be_Able_To_Auto_Ack()
	{
		using Instantiation.Disposable.BusClient requester = RawRabbitFactory.CreateTestClient();
		using Instantiation.Disposable.BusClient responder = RawRabbitFactory.CreateTestClient();
		/* Setup */
		BasicResponse sent = new()
		{
			Prop = "I am the response"
		};
		await responder.RespondAsync<BasicRequest, BasicResponse>(async request =>
			sent
		);

		/* Test */
		BasicResponse received = await requester.RequestAsync<BasicRequest, BasicResponse>(new BasicRequest());

		/* Assert */
		Assert.Equal(received.Prop, sent.Prop);
	}

	[Fact]
	public async Task Should_Be_Able_To_Return_Ack()
	{
		using Instantiation.Disposable.BusClient requester = RawRabbitFactory.CreateTestClient();
		using Instantiation.Disposable.BusClient responder = RawRabbitFactory.CreateTestClient();
		/* Setup */
		BasicResponse sent = new()
		{
			Prop = "I am the response"
		};
		await responder.RespondAsync<BasicRequest, BasicResponse>(async request =>
			new Ack<BasicResponse>(sent)
		);

		/* Test */
		BasicResponse received = await requester.RequestAsync<BasicRequest, BasicResponse>(new BasicRequest());

		/* Assert */
		Assert.Equal(received.Prop, sent.Prop);
	}

	[Fact]
	public async Task Should_Be_Able_To_Return_Nack()
	{
		using Instantiation.Disposable.BusClient requester = RawRabbitFactory.CreateTestClient();
		using Instantiation.Disposable.BusClient responder = RawRabbitFactory.CreateTestClient();
		/* Setup */
		TaskCompletionSource<BasicRequest> firstTsc = new();
		TaskCompletionSource<BasicRequest> secondTsc = new();
		BasicResponse sent = new() {Prop = "I'm from the second handler"};

		await responder.RespondAsync<BasicRequest, BasicResponse>(async request =>
			{
				firstTsc.TrySetResult(request);
				return Respond.Nack<BasicResponse>();
			}
		);
		await responder.RespondAsync<BasicRequest, BasicResponse>(async request =>
			{
				secondTsc.TrySetResult(request);
				return Respond.Ack(sent);
			}
		);

		/* Test */
		BasicResponse received = await requester.RequestAsync<BasicRequest, BasicResponse>(new BasicRequest());
		await firstTsc.Task;
		await secondTsc.Task;
		/* Assert */
		Assert.Equal(received.Prop, sent.Prop);
	}

	[Fact]
	public async Task Should_Be_Able_To_Return_Reject()
	{
		using Instantiation.Disposable.BusClient requester = RawRabbitFactory.CreateTestClient();
		using Instantiation.Disposable.BusClient responder = RawRabbitFactory.CreateTestClient();
		/* Setup */
		TaskCompletionSource<BasicRequest> firstTsc = new();
		TaskCompletionSource<BasicRequest> secondTsc = new();
		BasicResponse sent = new() { Prop = "I'm from the second handler" };

		await responder.RespondAsync<BasicRequest, BasicResponse>(async request =>
			{
				firstTsc.TrySetResult(request);
				return Respond.Reject<BasicResponse>();
			}
		);
		await responder.RespondAsync<BasicRequest, BasicResponse>(async request =>
			{
				secondTsc.TrySetResult(request);
				return Respond.Ack(sent);
			}
		);

		/* Test */
		BasicResponse received = await requester.RequestAsync<BasicRequest, BasicResponse>(new BasicRequest());
		await firstTsc.Task;
		await secondTsc.Task;
		/* Assert */
		Assert.Equal(received.Prop, sent.Prop);
	}
}
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
