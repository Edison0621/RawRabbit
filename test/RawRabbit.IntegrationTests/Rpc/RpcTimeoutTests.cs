using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.Operations.Request.Middleware;
using Xunit;

namespace RawRabbit.IntegrationTests.Rpc
{
	public class RpcTimeoutTests
	{
		[Fact]
		public async Task Should_Throw_Timeout_Exception_If_Response_Is_Not_Received()
		{
			using (Instantiation.Disposable.BusClient requester = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				/* Test */
				/* Assert */
				await Assert.ThrowsAsync<TimeoutException>(async () =>
					await requester.RequestAsync<BasicRequest, BasicResponse>(context: ctx => ctx
						.UseRequestTimeout(TimeSpan.FromMilliseconds(100)))
				);
			}
		}

		[Fact]
		public async Task Should_Not_Use_Timeout_If_Cancellation_Token_Is_Provided()
		{
			using (Instantiation.Disposable.BusClient requester = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				TimeSpan timeout = TimeSpan.FromMilliseconds(300);
				CancellationTokenSource timeoutCs = new CancellationTokenSource();

				/* Test */
				/* Assert */
				Task<BasicResponse> requestTask = requester.RequestAsync<BasicRequest, BasicResponse>(
					message: new BasicRequest(),
					context: ctx => ctx.UseRequestTimeout(timeout),
					ct: timeoutCs.Token
				);

				await Task.Delay(timeout.Add(TimeSpan.FromMilliseconds(100)), timeoutCs.Token);

				Assert.False(requestTask.IsFaulted);
				Assert.False(requestTask.IsCanceled);
				Assert.False(requestTask.IsCompleted);

				timeoutCs.Cancel();
				await Task.Delay(timeout.Add(TimeSpan.FromMilliseconds(50)), timeoutCs.Token);
				Assert.True(requestTask.IsCanceled);
			}
		}

		[Fact]
		public async Task Should_Not_Time_out_If_Response_Is_Received()
		{
			using (Instantiation.Disposable.BusClient requester = RawRabbitFactory.CreateTestClient())
			using (Instantiation.Disposable.BusClient responder = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				await responder.RespondAsync<BasicRequest, BasicResponse>(request =>
					Task.FromResult(new BasicResponse())
				);

				/* Test */
				BasicResponse response = await requester.RequestAsync<BasicRequest, BasicResponse>();
				
				/* Assert */
				Assert.NotNull(response);
			}
		}
	}
}
