using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Configuration.Consume;
using RawRabbit.Consumer;
using RawRabbit.Logging;

namespace RawRabbit.Pipe.Middleware;

public class BasicConsumeOptions
{
	public Func<IPipeContext, ConsumeConfiguration> ConsumeConfigFunc { get; set; }
	public Func<IPipeContext, IAsyncBasicConsumer> ConsumerFunc { get; set; }
	public Func<IPipeContext, bool> ConfigValidatePredicate { get; set; }
}

public class ConsumerConsumeMiddleware : Middleware
{
	private readonly IConsumerFactory _factory;
	protected readonly Func<IPipeContext, ConsumeConfiguration> _consumeConfigFunc;
	protected readonly Func<IPipeContext, IAsyncBasicConsumer> _consumerFunc;
	protected Func<IPipeContext, bool> _configValidatePredicate;
	private readonly ILog _logger = LogProvider.For<ConsumerConsumeMiddleware>();

	public ConsumerConsumeMiddleware(IConsumerFactory factory, BasicConsumeOptions options = null)
	{
		this._factory = factory;
		this._consumeConfigFunc = options?.ConsumeConfigFunc ?? (context => context.GetConsumeConfiguration());
		this._consumerFunc = options?.ConsumerFunc ?? (context => context.GetConsumer());
		this._configValidatePredicate = options?.ConfigValidatePredicate ?? (_ => true);
	}

	public override async Task InvokeAsync(IPipeContext context, CancellationToken token = new())
	{
		ConsumeConfiguration config = this.GetConsumeConfiguration(context);
		if (config == null)
		{
			this._logger.Info("Consumer configuration not found, skipping consume.");
			return;
		}

		IAsyncBasicConsumer consumer = this.GetConsumer(context);
		if (consumer == null)
		{
			this._logger.Info("Consumer not found. Will not consume on queue {queueName}.", config.QueueName);
			return;
		}

		this.BasicConsume(consumer, config);
		await this.Next.InvokeAsync(context, token);
	}

	protected virtual ConsumeConfiguration GetConsumeConfiguration(IPipeContext context)
	{
		return this._consumeConfigFunc?.Invoke(context);
	}

	protected virtual IAsyncBasicConsumer GetConsumer(IPipeContext context)
	{
		return this._consumerFunc?.Invoke(context);
	}

	protected virtual void BasicConsume(IAsyncBasicConsumer consumer, ConsumeConfiguration config)
	{
		this._factory.ConfigureConsumeAsync(consumer, config);
	}
}
