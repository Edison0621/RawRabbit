using System;
using System.Threading.Tasks;
using RawRabbit.Enrichers.QueueSuffix;
using RawRabbit.Instantiation;
using RawRabbit.IntegrationTests.TestMessages;
using Xunit;

namespace RawRabbit.IntegrationTests.Enrichers;

public class QueueSuffixTests
{
	[Fact]
	public async Task Should_Be_Able_To_Create_Unique_Queue_With_Application_Name()
	{
		using Instantiation.Disposable.BusClient publisher = RawRabbitFactory.CreateTestClient();
		using Instantiation.Disposable.BusClient firstClient = RawRabbitFactory.CreateTestClient();
		using Instantiation.Disposable.BusClient secondClient = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
		{
			Plugins = p => p.UseApplicationQueueSuffix()
		});
		/* Setup */
		TaskCompletionSource<BasicMessage> firstTsc = new();
		TaskCompletionSource<BasicMessage> secondTsc = new();
		await firstClient.SubscribeAsync<BasicMessage>(message =>
		{
			firstTsc.TrySetResult(message);
			return Task.FromResult(0);
		});
		await secondClient.SubscribeAsync<BasicMessage>(message =>
		{
			secondTsc.TrySetResult(message);
			return Task.FromResult(0);
		});
				
		/* Test */
		await publisher.PublishAsync(new BasicMessage());
		await firstTsc.Task;
		await secondTsc.Task;

		/* Assert */
		Assert.True(true,"Should be delivered to both subscribers");
	}

	[Fact]
	public async Task Should_Be_Able_To_Disable_Application_Suffix()
	{
		using Instantiation.Disposable.BusClient publisher = RawRabbitFactory.CreateTestClient();
		using Instantiation.Disposable.BusClient firstClient = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
		{
			Plugins = p => p.UseApplicationQueueSuffix()
		});
		using Instantiation.Disposable.BusClient secondClient = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
		{
			Plugins = p => p.UseApplicationQueueSuffix()
		});
		/* Setup */
		TaskCompletionSource<BasicMessage> firstTsc = new();
		TaskCompletionSource<BasicMessage> secondTsc = new();
		await firstClient.SubscribeAsync<BasicMessage>(message =>
		{
			firstTsc.TrySetResult(message);
			return Task.FromResult(0);
		});
		await secondClient.SubscribeAsync<BasicMessage>(message =>
		{
			secondTsc.TrySetResult(message);
			return Task.FromResult(0);
		}, ctx => ctx.UseApplicationQueueSuffix(false));

		/* Test */
		await publisher.PublishAsync(new BasicMessage());
		await firstTsc.Task;
		await secondTsc.Task;

		/* Assert */
		Assert.True(true, "Should be delivered to both subscribers");
	}

	[Fact]
	public async Task Should_Append_Application_Name_In_Combination_With_Other_Suffix()
	{
		using Instantiation.Disposable.BusClient publisher = RawRabbitFactory.CreateTestClient();
		using Instantiation.Disposable.BusClient firstClient = RawRabbitFactory.CreateTestClient();
		using Instantiation.Disposable.BusClient secondClient = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
		{
			Plugins = p => p.UseApplicationQueueSuffix()
		});
		/* Setup */
		TaskCompletionSource<BasicMessage> firstTsc = new();
		TaskCompletionSource<BasicMessage> secondTsc = new();
		await firstClient.SubscribeAsync<BasicMessage>(message =>
		{
			firstTsc.TrySetResult(message);
			return Task.FromResult(0);
		}, ctx => ctx
			.UseSubscribeConfiguration(cfg => cfg
				.FromDeclaredQueue(q => q
					.WithNameSuffix("custom"))));
		await secondClient.SubscribeAsync<BasicMessage>(message =>
		{
			secondTsc.TrySetResult(message);
			return Task.FromResult(0);
		}, ctx => ctx
			.UseSubscribeConfiguration(cfg => cfg
				.FromDeclaredQueue(q => q
					.WithNameSuffix("custom"))));

		/* Test */
		await publisher.PublishAsync(new BasicMessage());
		await firstTsc.Task;
		await secondTsc.Task;

		/* Assert */
		Assert.True(true, "Should be delivered to both subscribers");
	}

	[Fact]
	public async Task Should_Be_Able_To_Create_Unique_Queue_With_Host_Name()
	{
		using Instantiation.Disposable.BusClient publisher = RawRabbitFactory.CreateTestClient();
		using Instantiation.Disposable.BusClient firstClient = RawRabbitFactory.CreateTestClient();
		using Instantiation.Disposable.BusClient secondClient = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
		{
			Plugins = p => p.UseHostQueueSuffix()
		});
		/* Setup */
		TaskCompletionSource<BasicMessage> firstTsc = new();
		TaskCompletionSource<BasicMessage> secondTsc = new();
		await firstClient.SubscribeAsync<BasicMessage>(message =>
		{
			firstTsc.TrySetResult(message);
			return Task.FromResult(0);
		});
		await secondClient.SubscribeAsync<BasicMessage>(message =>
		{
			secondTsc.TrySetResult(message);
			return Task.FromResult(0);
		});

		/* Test */
		await publisher.PublishAsync(new BasicMessage());
		await firstTsc.Task;
		await secondTsc.Task;

		/* Assert */
		Assert.True(true, "Should be delivered to both subscribers");
	}

	[Fact]
	public async Task Should_Be_Able_To_Disable_Host_Name_Suffix()
	{
		using Instantiation.Disposable.BusClient publisher = RawRabbitFactory.CreateTestClient();
		using Instantiation.Disposable.BusClient firstClient = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
		{
			Plugins = p => p.UseHostQueueSuffix()
		});
		using Instantiation.Disposable.BusClient secondClient = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
		{
			Plugins = p => p.UseHostQueueSuffix()
		});
		/* Setup */
		TaskCompletionSource<BasicMessage> firstTsc = new();
		TaskCompletionSource<BasicMessage> secondTsc = new();
		await firstClient.SubscribeAsync<BasicMessage>(message =>
		{
			firstTsc.TrySetResult(message);
			return Task.FromResult(0);
		});
		await secondClient.SubscribeAsync<BasicMessage>(message =>
		{
			secondTsc.TrySetResult(message);
			return Task.FromResult(0);
		}, ctx => ctx.UseHostnameQueueSuffix(false));

		/* Test */
		await publisher.PublishAsync(new BasicMessage());
		await firstTsc.Task;
		await secondTsc.Task;

		/* Assert */
		Assert.True(true, "Should be delivered to both subscribers");
	}

	[Fact]
	public async Task Should_Be_Able_To_Create_Unique_Queue_With_Custom_Suffix()
	{
		using Instantiation.Disposable.BusClient publisher = RawRabbitFactory.CreateTestClient();
		using Instantiation.Disposable.BusClient firstClient = RawRabbitFactory.CreateTestClient();
		using Instantiation.Disposable.BusClient secondClient = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
		{
			Plugins = p => p.UseCustomQueueSuffix("custom")
		});
		/* Setup */
		TaskCompletionSource<BasicMessage> firstTsc = new();
		TaskCompletionSource<BasicMessage> secondTsc = new();
		await firstClient.SubscribeAsync<BasicMessage>(message =>
		{
			firstTsc.TrySetResult(message);
			return Task.FromResult(0);
		});
		await secondClient.SubscribeAsync<BasicMessage>(message =>
		{
			secondTsc.TrySetResult(message);
			return Task.FromResult(0);
		});

		/* Test */
		await publisher.PublishAsync(new BasicMessage());
		await firstTsc.Task;
		await secondTsc.Task;

		/* Assert */
		Assert.True(true, "Should be delivered to both subscribers");
	}

	[Fact]
	public async Task Should_Be_Able_To_Combine_Suffix()
	{
		using Instantiation.Disposable.BusClient publisher = RawRabbitFactory.CreateTestClient();
		using Instantiation.Disposable.BusClient firstClient = RawRabbitFactory.CreateTestClient();
		using Instantiation.Disposable.BusClient secondClient = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
		{
			Plugins = p => p
				.UseApplicationQueueSuffix()
				.UseHostQueueSuffix()
				.UseCustomQueueSuffix("custom")
		});
		/* Setup */
		TaskCompletionSource<BasicMessage> firstTsc = new();
		TaskCompletionSource<BasicMessage> secondTsc = new();
		await firstClient.SubscribeAsync<BasicMessage>(message =>
		{
			firstTsc.TrySetResult(message);
			return Task.FromResult(0);
		});
		await secondClient.SubscribeAsync<BasicMessage>(message =>
		{
			secondTsc.TrySetResult(message);
			return Task.FromResult(0);
		}, ctx => ctx
			.UseSubscribeConfiguration(cfg => cfg
				.FromDeclaredQueue(q => q
					.WithNameSuffix("special"))));

		/* Test */
		await publisher.PublishAsync(new BasicMessage());
		await firstTsc.Task;
		await secondTsc.Task;

		/* Assert */
		Assert.True(true, "Should be delivered to both subscribers");
	}

	[Fact]
	public async Task Should_Be_Able_To_Override_Custom_Suffix()
	{
		RawRabbitOptions customSuffix = new()
		{
			Plugins = p => p.UseCustomQueueSuffix("custom")
		};
		using Instantiation.Disposable.BusClient publisher = RawRabbitFactory.CreateTestClient();
		using Instantiation.Disposable.BusClient firstClient = RawRabbitFactory.CreateTestClient(customSuffix);
		using Instantiation.Disposable.BusClient secondClient = RawRabbitFactory.CreateTestClient(customSuffix);
		/* Setup */
		TaskCompletionSource<BasicMessage> firstTsc = new();
		TaskCompletionSource<BasicMessage> secondTsc = new();
		await firstClient.SubscribeAsync<BasicMessage>(message =>
		{
			firstTsc.TrySetResult(message);
			return Task.FromResult(0);
		});
		await secondClient.SubscribeAsync<BasicMessage>(message =>
		{
			secondTsc.TrySetResult(message);
			return Task.FromResult(0);
		}, ctx => ctx.UseCustomQueueSuffix("special"));

		/* Test */
		await publisher.PublishAsync(new BasicMessage());
		await firstTsc.Task;
		await secondTsc.Task;

		/* Assert */
		Assert.True(true, "Should be delivered to both subscribers");
	}

	[Fact]
	public async Task Should_Keep_Queue_Name_Intact_If_No_Custom_Prefix_Is_Used()
	{
		using Instantiation.Disposable.BusClient publisher = RawRabbitFactory.CreateTestClient();
		using Instantiation.Disposable.BusClient firstClient = RawRabbitFactory.CreateTestClient();
		using Instantiation.Disposable.BusClient secondClient = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
		{
			Plugins = p => p.UseCustomQueueSuffix()
		});
		/* Setup */
		TaskCompletionSource<BasicMessage> firstTsc = new();
		TaskCompletionSource<BasicMessage> secondTsc = new();
		await firstClient.SubscribeAsync<BasicMessage>(message =>
		{
			firstTsc.TrySetResult(message);
			return Task.FromResult(0);
		});
		await secondClient.SubscribeAsync<BasicMessage>(message =>
		{
			secondTsc.TrySetResult(message);
			return Task.FromResult(0);
		}, ctx => ctx.UseCustomQueueSuffix(null));

		/* Test */
		await publisher.PublishAsync(new BasicMessage());
		Task.WaitAll([firstTsc.Task, secondTsc.Task], TimeSpan.FromMilliseconds(300));

		/* Assert */
		bool oneCompleted = firstTsc.Task.IsCompleted || secondTsc.Task.IsCompleted;
		bool onlyOneCompleted = !(firstTsc.Task.IsCompleted && secondTsc.Task.IsCompleted);
		Assert.True(oneCompleted, "Should be delivered at least once");
		Assert.True(onlyOneCompleted, "Should not be delivered to both");
	}

	[Fact]
	public async Task Should_Not_Interfere_With_Direct_RPC()
	{
		using Instantiation.Disposable.BusClient responer = RawRabbitFactory.CreateTestClient();
		using Instantiation.Disposable.BusClient requester = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
		{
			Plugins = p => p.UseApplicationQueueSuffix()
		});
		await responer.RespondAsync<BasicRequest, BasicResponse>(request => Task.FromResult(new BasicResponse()));
		BasicResponse response = await requester.RequestAsync<BasicRequest, BasicResponse>();

		Assert.NotNull(response);
	}
}
