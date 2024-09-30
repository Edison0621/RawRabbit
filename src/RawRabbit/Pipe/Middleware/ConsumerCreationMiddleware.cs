using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Configuration.Consume;
using RawRabbit.Consumer;
using RawRabbit.Logging;

namespace RawRabbit.Pipe.Middleware;

public class ConsumerCreationOptions
{
	public Func<IConsumerFactory, CancellationToken, IPipeContext, Task<IAsyncBasicConsumer>> ConsumerFunc { get; set; }
}

public class ConsumerCreationMiddleware : Middleware
{
	protected readonly IConsumerFactory _consumerFactory;
	protected Func<IPipeContext, ConsumeConfiguration> _configFunc;
	protected readonly Func<IConsumerFactory, CancellationToken, IPipeContext, Task<IAsyncBasicConsumer>> _consumerFunc;
	private readonly ILog _logger = LogProvider.For<ConsumerCreationMiddleware>();

	public ConsumerCreationMiddleware(IConsumerFactory consumerFactory, ConsumerCreationOptions options = null)
	{
		this._consumerFactory = consumerFactory;
		this._consumerFunc = options?.ConsumerFunc ?? ((factory, token, context) => factory.CreateConsumerAsync(context.GetChannel(), token));
	}

	public override async Task InvokeAsync(IPipeContext context, CancellationToken token = default)
	{
		IAsyncBasicConsumer consumer = await this.GetOrCreateConsumerAsync(context, token);
		context.Properties.TryAdd(PipeKey.Consumer, consumer);
		await this.Next.InvokeAsync(context, token);
	}

	protected virtual Task<IAsyncBasicConsumer> GetOrCreateConsumerAsync(IPipeContext context, CancellationToken token)
	{
		Task<IAsyncBasicConsumer> consumerTask = this._consumerFunc(this._consumerFactory, token, context);
		if (consumerTask == null)
		{
			this._logger.Warn("No Consumer creation task found in Pipe context.");
		}
		return consumerTask;
	}
}