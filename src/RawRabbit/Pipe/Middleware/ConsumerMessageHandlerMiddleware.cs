using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Consumer;
using RawRabbit.Logging;

namespace RawRabbit.Pipe.Middleware;

public class ConsumeOptions
{
	public Action<IPipeBuilder> Pipe { get; set; }
	public Func<IPipeContext, IAsyncBasicConsumer> ConsumerFunc { get; set; }
	public Func<IPipeContext, Action<Func<Task>, CancellationToken>> ThrottleFuncFunc { get; set; }
}

public class ConsumerMessageHandlerMiddleware : Middleware
{
	protected readonly IPipeContextFactory _contextFactory;
	protected readonly Middleware _consumePipe;
	protected readonly Func<IPipeContext, IAsyncBasicConsumer> _consumeFunc;
	protected Func<IPipeContext, SemaphoreSlim> _semaphoreFunc;
	protected readonly Func<IPipeContext, Action<Func<Task>, CancellationToken>> _throttledExecutionFunc;
	private readonly ILog _logger = LogProvider.For<ConsumerMessageHandlerMiddleware>();

	public ConsumerMessageHandlerMiddleware(IPipeBuilderFactory pipeBuilderFactory, IPipeContextFactory contextFactory, ConsumeOptions options = null)
	{
		this._contextFactory = contextFactory;
		this._consumeFunc = options?.ConsumerFunc ?? (context =>context.GetConsumer());
		this._consumePipe = pipeBuilderFactory.Create(options?.Pipe ?? (_ => {}));
		this._throttledExecutionFunc = options?.ThrottleFuncFunc ?? (context => context.GetConsumeThrottleAction());
	}

	public override async Task InvokeAsync(IPipeContext context, CancellationToken token = default)
	{
		IAsyncBasicConsumer consumer = this._consumeFunc(context);
		Action<Func<Task>, CancellationToken> throttlingFunc = this.GetThrottlingFunc(context);
		consumer.OnMessage((_, args) =>
		{
			throttlingFunc(() => this.InvokeConsumePipeAsync(context, args, token), token);

			return Task.CompletedTask;
		});

		await this.Next.InvokeAsync(context, token);
	}

	private Action<Func<Task>, CancellationToken> GetThrottlingFunc(IPipeContext context)
	{
		return this._throttledExecutionFunc(context);
	}

	protected virtual async Task InvokeConsumePipeAsync(IPipeContext context, BasicDeliverEventArgs args, CancellationToken token)
	{
		this._logger.Debug("Invoking consumer pipe for message {messageId}", args?.BasicProperties.MessageId);
		IPipeContext consumeContext = this._contextFactory.CreateContext(context.Properties.ToArray());
		consumeContext.Properties.Add(PipeKey.DeliveryEventArgs, args);
		try
		{
			await this._consumePipe.InvokeAsync(consumeContext, token);
		}
		catch (Exception e)
		{
			this._logger.Error(e, "An unhandled exception was thrown when consuming message with routing key {routingKey}", args?.RoutingKey);
			throw;
		}
	}
}
