using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Operations.Request.Configuration;
using RawRabbit.Operations.Request.Context;
using RawRabbit.Operations.Request.Core;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Request.Middleware
{
	public class RequestTimeoutOptions
	{
		public Func<IPipeContext, TimeSpan> TimeSpanFunc { get; set; }
	}

	public class RequestTimeoutMiddleware : Pipe.Middleware.Middleware
	{
		protected readonly Func<IPipeContext, TimeSpan> _timeSpanFunc;

		public RequestTimeoutMiddleware(RequestTimeoutOptions options = null)
		{
			this._timeSpanFunc = options?.TimeSpanFunc ?? (context => context.GetRequestTimeout());
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
		{
			if (token != default(CancellationToken))
			{
				await this.Next.InvokeAsync(context, token);
				return;
			}

			TimeSpan timeout = this.GetTimeoutTimeSpan(context);
			CancellationTokenSource ctc = new CancellationTokenSource(timeout);
			TaskCompletionSource<bool> timeoutTsc = new TaskCompletionSource<bool>();

			ctc.Token.Register(() =>
			{
				string correlationId = context?.GetBasicProperties()?.CorrelationId;
				RequestConfiguration cfg = context?.GetRequestConfiguration();
				timeoutTsc.TrySetException(new TimeoutException($"The request '{correlationId}' with routing key '{cfg?.Request.RoutingKey}' timed out after {timeout:g}."));
			});

			Task pipeTask = this.Next
				.InvokeAsync(context, ctc.Token)
				.ContinueWith(t =>
				{
					if (!ctc.IsCancellationRequested)
					{
						timeoutTsc.TrySetResult(true);
					}
					return t;
				}, token)
				.Unwrap();

			await timeoutTsc.Task;
			await pipeTask;
			ctc.Dispose();
		}

		protected virtual TimeSpan GetTimeoutTimeSpan(IPipeContext context)
		{
			return this._timeSpanFunc(context);
		}
	}

	public static class RequestTimeoutExtensions
	{
		private const string RequestTimeout = "RequestTimeout";

		public static IRequestContext UseRequestTimeout(this IRequestContext context, TimeSpan time)
		{
			context.Properties.TryAdd(RequestTimeout, time);
			return context;
		}

		public static TimeSpan GetRequestTimeout(this IPipeContext context)
		{
			TimeSpan fallback = context.GetClientConfiguration().RequestTimeout;
			return context.Get(RequestTimeout, fallback);
		}
	}
}
