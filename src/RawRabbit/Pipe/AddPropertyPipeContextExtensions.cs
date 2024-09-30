using System;
using System.Threading;
using System.Threading.Tasks;

namespace RawRabbit.Pipe;

public static class AddPropertyPipeContextExtensions
{
	public static TPipeContext UseConsumerConcurrency<TPipeContext>(this TPipeContext context, uint concurrency) where TPipeContext : IPipeContext
	{
		SemaphoreSlim semaphore = new((int)concurrency, (int)concurrency);
		return UseConsumeSemaphore(context, semaphore);
	}

	public static TPipeContext UseConsumeSemaphore<TPipeContext>(this TPipeContext context, SemaphoreSlim semaphore) where TPipeContext : IPipeContext
	{
		return UseThrottledConsume(context, (asyncAction, ct) => semaphore
			.WaitAsync(ct)
			.ContinueWith(_ =>
			{
				Task.Run(asyncAction, ct)
					.ContinueWith(_ => semaphore.Release(), ct);
			}, ct));
	}

	public static TPipeContext UseThrottledConsume<TPipeContext>(this TPipeContext context, Action<Func<Task>, CancellationToken> throttle) where TPipeContext : IPipeContext
	{
		context.Properties.TryAdd(PipeKey.ConsumeThrottleAction, throttle);
		return context;
	}
}