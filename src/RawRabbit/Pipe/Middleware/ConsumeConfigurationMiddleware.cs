using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Configuration.Consume;
using RawRabbit.Logging;

namespace RawRabbit.Pipe.Middleware;

public class ConsumeConfigurationOptions
{
	public Func<IPipeContext, string> QueueFunc { get; set; }
	public Func<IPipeContext, string> RoutingKeyFunc { get; set; }
	public Func<IPipeContext, string> ExchangeFunc { get; set; }
	public Func<IPipeContext, Type> MessageTypeFunc { get; set; }
	public Func<IPipeContext, Action<IConsumeConfigurationBuilder>> ConfigActionFunc { get; set; }
}

public class ConsumeConfigurationMiddleware : Middleware
{
	protected readonly IConsumeConfigurationFactory _configFactory;
	protected readonly Func<IPipeContext, string> _queueFunc;
	protected readonly Func<IPipeContext, string> _exchangeFunc;
	protected readonly Func<IPipeContext, string> _routingKeyFunc;
	protected readonly Func<IPipeContext, Type> _messageTypeFunc;
	protected readonly Func<IPipeContext, Action<IConsumeConfigurationBuilder>> _configActionFunc;
	private readonly ILog _logger = LogProvider.For<ConsumeConfigurationMiddleware>();

	public ConsumeConfigurationMiddleware(IConsumeConfigurationFactory configFactory, ConsumeConfigurationOptions options = null)
	{
		this._configFactory = configFactory;
		this._queueFunc = options?.QueueFunc ?? (context => context.GetQueueDeclaration()?.Name);
		this._exchangeFunc = options?.ExchangeFunc ?? (context => context.GetExchangeDeclaration()?.Name);
		this._routingKeyFunc = options?.RoutingKeyFunc ?? (context => context.GetRoutingKey());
		this._messageTypeFunc = options?.MessageTypeFunc ?? (context => context.GetMessageType());
		this._configActionFunc = options?.ConfigActionFunc ?? (context => context.Get<Action<IConsumeConfigurationBuilder>>(PipeKey.ConfigurationAction));
	}

	public override async Task InvokeAsync(IPipeContext context, CancellationToken token = default)
	{
		ConsumeConfiguration config = this.ExtractConfigFromMessageType(context) ?? this.ExtractConfigFromStrings(context);

		Action<IConsumeConfigurationBuilder> action = this.GetConfigurationAction(context);
		if (action != null)
		{
			this._logger.Info("Configuration action for {queueName} found.", config?.QueueName);
			ConsumeConfigurationBuilder builder = new(config);
			action(builder);
			config = builder.Config;
		}

		context.Properties.TryAdd(PipeKey.ConsumeConfiguration, config);

		await this.Next.InvokeAsync(context, token);
	}

	protected virtual Type GetMessageType(IPipeContext context)
	{
		return this._messageTypeFunc(context);
	}

	protected Action<IConsumeConfigurationBuilder> GetConfigurationAction(IPipeContext context)
	{
		return this._configActionFunc(context);
	}

	protected virtual ConsumeConfiguration ExtractConfigFromStrings(IPipeContext context)
	{
		string routingKey = this._routingKeyFunc(context);
		string queueName = this._queueFunc(context);
		string exchangeName = this._exchangeFunc(context);
		this._logger.Debug("Consuming from queue {queueName} on {exchangeName} with routing key {routingKey}", queueName, exchangeName, routingKey);
		return this._configFactory.Create(queueName, exchangeName, routingKey);
	}

	protected virtual ConsumeConfiguration ExtractConfigFromMessageType(IPipeContext context)
	{
		Type messageType = this._messageTypeFunc(context);
		if (messageType != null)
		{
			this._logger.Debug("Found message type {messageType} in context. Creating consume config based on it.", messageType.Name);
		}
		return messageType == null
			? null
			: this._configFactory.Create(messageType);
	}
}

public static class BasicConsumeExtensions
{
	public static TPipeContext UseConsumeConfiguration<TPipeContext>(this TPipeContext context, Action<IConsumeConfigurationBuilder> config) where TPipeContext : IPipeContext
	{
		context.Properties.TryAdd(PipeKey.ConfigurationAction, config);
		return context;
	}
}