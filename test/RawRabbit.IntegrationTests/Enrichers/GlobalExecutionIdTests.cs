using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using RawRabbit.Common;
using RawRabbit.Configuration.Queue;
using RawRabbit.Enrichers.GlobalExecutionId;
using RawRabbit.Instantiation;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.Pipe.Middleware;
using Xunit;

namespace RawRabbit.IntegrationTests.Enrichers;

public class GlobalExecutionIdTests
{
	[Fact]
	public async Task Should_Forward_On_Pub_Sub()
	{
		RawRabbitOptions withGloblalExecutionId = new()
		{
			Plugins = p => p.UseGlobalExecutionId()
		};
		using Instantiation.Disposable.BusClient publisher = RawRabbitFactory.CreateTestClient(withGloblalExecutionId);
		using Instantiation.Disposable.BusClient firstSubscriber = RawRabbitFactory.CreateTestClient(withGloblalExecutionId);
		using Instantiation.Disposable.BusClient secondSubscriber = RawRabbitFactory.CreateTestClient(withGloblalExecutionId);
		using Instantiation.Disposable.BusClient thridSubscriber = RawRabbitFactory.CreateTestClient(withGloblalExecutionId);
		using Instantiation.Disposable.BusClient consumer = RawRabbitFactory.CreateTestClient();
		/* Setup */
		List<TaskCompletionSource<BasicDeliverEventArgs>> taskCompletionSources =
		[
			new(),
			new(),
			new()
		];
		await firstSubscriber.SubscribeAsync<FirstMessage>(message => firstSubscriber.PublishAsync(new SecondMessage(), ctx => ctx.UsePublishAcknowledge(false)));
		await secondSubscriber.SubscribeAsync<SecondMessage>(message => secondSubscriber.PublishAsync(new ThirdMessage()));
		await thridSubscriber.SubscribeAsync<ThirdMessage>(message => Task.FromResult(0));
		await consumer.DeclareQueueAsync(new QueueDeclaration
		{
			Name = "take_all",
			AutoDelete = true
		});
		await consumer.BasicConsumeAsync(args =>
			{
				TaskCompletionSource<BasicDeliverEventArgs> tsc = taskCompletionSources.First(t => !t.Task.IsCompleted);
				tsc.TrySetResult(args);
				return Task.FromResult<Acknowledgement>(new Ack());
			}, ctx => ctx.UseConsumeConfiguration(cfg => cfg
				.FromQueue("take_all")
				.OnExchange("rawrabbit.integrationtests.testmessages")
				.WithRoutingKey("#")
			)
		);

		/* Test */
		await publisher.PublishAsync(new FirstMessage());
		Task.WaitAll(taskCompletionSources.Select(t => t.Task).ToArray<Task>());

		List<string> results = [];
		foreach (TaskCompletionSource<BasicDeliverEventArgs> tcs in taskCompletionSources)
		{
			string id = Encoding.UTF8.GetString(tcs.Task.Result.BasicProperties.Headers?[RawRabbit.Enrichers.GlobalExecutionId.PropertyHeaders.GlobalExecutionId] as byte[]);
			results.Add(id);
		}

		/* Assert */
		Assert.NotNull(results[0]);
		Assert.Equal(results[0], results[1]);
		Assert.Equal(results[1], results[2]);
	}

	[Fact]
	public async Task Should_Forward_For_Rpc()
	{
		RawRabbitOptions withGloblalExecutionId = new()
		{
			Plugins = p => p.UseGlobalExecutionId()
		};
		using Instantiation.Disposable.BusClient requester = RawRabbitFactory.CreateTestClient(withGloblalExecutionId);
		using Instantiation.Disposable.BusClient firstResponder = RawRabbitFactory.CreateTestClient(withGloblalExecutionId);
		using Instantiation.Disposable.BusClient secondResponder = RawRabbitFactory.CreateTestClient(withGloblalExecutionId);
		using Instantiation.Disposable.BusClient thridResponder = RawRabbitFactory.CreateTestClient(withGloblalExecutionId);
		using Instantiation.Disposable.BusClient consumer = RawRabbitFactory.CreateTestClient();
		/* Setup */
		List<TaskCompletionSource<BasicDeliverEventArgs>> taskCompletionSources =
		[
			new(),
			new(),
			new()
		];
		await firstResponder.RespondAsync<FirstRequest, FirstResponse>(async message =>
		{
			await firstResponder.PublishAsync(new SecondMessage());
			return new FirstResponse();
		});
		await secondResponder.SubscribeAsync<SecondMessage>(message => secondResponder.PublishAsync(new ThirdMessage()));
		await thridResponder.SubscribeAsync<ThirdMessage>(message => Task.FromResult(0));
		await consumer.DeclareQueueAsync(new QueueDeclaration
		{
			AutoDelete = true,
			Name = "take_all",
		});
		await consumer.BasicConsumeAsync(args =>
			{
				TaskCompletionSource<BasicDeliverEventArgs> tsc = taskCompletionSources.First(t => !t.Task.IsCompleted);
				tsc.TrySetResult(args);
				return Task.FromResult<Acknowledgement>(new Ack());
			}, ctx => ctx
				.UseConsumeConfiguration(cfg => cfg
					.FromQueue("take_all")
					.OnExchange("rawrabbit.integrationtests.testmessages")
					.WithRoutingKey("#")
				)
		);

		/* Test */
		await requester.RequestAsync<FirstRequest, FirstResponse>();
		Task.WaitAll(taskCompletionSources.Select(t => t.Task).ToArray<Task>());

		List<string> results = [];
		foreach (TaskCompletionSource<BasicDeliverEventArgs> tcs in taskCompletionSources)
		{
			string id = Encoding.UTF8.GetString(tcs.Task.Result.BasicProperties.Headers?[RawRabbit.Enrichers.GlobalExecutionId.PropertyHeaders.GlobalExecutionId] as byte[]);
			results.Add(id);
		}

		/* Assert */
		Assert.NotNull(results[0]);
		Assert.Equal(results[0], results[1]);
		Assert.Equal(results[1], results[2]);
	}
}
