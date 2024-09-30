using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.IntegrationTests.Features;

public class ConsumeThrottleTests
{
	[Fact]
	public async Task Should_Throttle_With_Provided_Action()
	{
		using Instantiation.Disposable.BusClient subscriber = RawRabbitFactory.CreateTestClient();
		using Instantiation.Disposable.BusClient publisher = RawRabbitFactory.CreateTestClient();
		/* Setup */
		TaskCompletionSource<bool> doneTsc = new();
		await subscriber.SubscribeAsync<BasicMessage>(
			message => Task.FromResult(0),
			c => c.UseThrottledConsume((func, token) => doneTsc.TrySetResult(true)));

		/* Test */
		await publisher.PublishAsync(new BasicMessage());
		await doneTsc.Task;

		/* Assert */
		Assert.True(true, "Throttler should be invoked.");
	}

	[Fact]
	public async Task Should_Throttle_With_Provided_Semaphore()
	{
		using Instantiation.Disposable.BusClient subscriber = RawRabbitFactory.CreateTestClient();
		using Instantiation.Disposable.BusClient publisher = RawRabbitFactory.CreateTestClient();
		/* Setup */
		const int messageCount = 6;
		const int concurrencyLevel = 2;
		const int waitTimeMs = concurrencyLevel / messageCount * 100;
		TaskCompletionSource<bool> doneTsc = new();
		ConcurrentQueue<DateTime> concurrentEntryTimes = new();
		await subscriber.SubscribeAsync<BasicMessage>(async message =>
		{
			concurrentEntryTimes.Enqueue(DateTime.Now);
			await Task.Delay(TimeSpan.FromMilliseconds(waitTimeMs));
			if (concurrentEntryTimes.Count == messageCount)
			{
				doneTsc.TrySetResult(true);
			}
		}, c => c.UseConsumerConcurrency(concurrencyLevel));

		/* Test */
		for (int i = 0; i < messageCount; i++)
		{
			await publisher.PublishAsync(new BasicMessage());
		}
		await doneTsc.Task;

		/* Assert */
		List<DateTime> entryTimes = concurrentEntryTimes.ToList();
		for (int i = concurrencyLevel; i < messageCount-1; i++)
		{
			TimeSpan timeDiff = entryTimes[i] - entryTimes[i - 1];
			Assert.True(timeDiff.TotalMilliseconds >= 0, $"Entry {entryTimes[i]} is before previous exit {entryTimes[i - 1]}");
		}
	}

	[Fact]
	public async Task Should_Not_Throttle_If_No_Semaphore_Provided()
	{
		using Instantiation.Disposable.BusClient subscriber = RawRabbitFactory.CreateTestClient();
		using Instantiation.Disposable.BusClient publisher = RawRabbitFactory.CreateTestClient();
		/* Setup */
		const int messageCount = 6;
		TaskCompletionSource<bool> doneTsc = new();
		ConcurrentQueue<DateTime> concurrentEntryTimes = new();
		ConcurrentQueue<DateTime> concurrentExitTimes = new();
		await subscriber.SubscribeAsync<BasicMessage>(async message =>
		{
			concurrentEntryTimes.Enqueue(DateTime.Now);
			await Task.Delay(TimeSpan.FromMilliseconds(200));
			concurrentExitTimes.Enqueue(DateTime.Now);
			if (concurrentExitTimes.Count == messageCount)
			{
				doneTsc.TrySetResult(true);
			}
		});

		/* Test */
		for (int i = 0; i < messageCount; i++)
		{
			await publisher.PublishAsync(new BasicMessage());
		}
		await doneTsc.Task;

		/* Assert */
		List<DateTime> entryTimes = concurrentEntryTimes.ToList();
		List<DateTime> exitTimes = concurrentExitTimes.ToList();
		for (int i = 1; i < messageCount - 1; i++)
		{
			Assert.True(
				entryTimes[i] < exitTimes[i - 1],
				$"Expected exit {exitTimes[i - 1]} to occure after entry {entryTimes[i]}, since execution is not throttled."
			);
		}
	}
}
