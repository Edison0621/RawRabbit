using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Logging;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Subscribe.Middleware;

public class SubscriptionConfigurationOptions
{
	public Func<IPipeContext, ConsumerConfiguration> ConfigFunc { get; set; }
	public Func<IPipeContext, Type> MessageTypeFunc { get; set; }
	public Func<IPipeContext, Action<IConsumerConfigurationBuilder>> ConfigActionFunc { get; internal set; }
}

public class SubscriptionConfigurationMiddleware : Pipe.Middleware.Middleware
{
	private readonly ILog _logger = LogProvider.For<SubscriptionConfigurationMiddleware>();

	protected readonly IConsumerConfigurationFactory _configFactory;
	protected readonly Func<IPipeContext, Type> _messageTypeFunc;
	protected readonly Func<IPipeContext, ConsumerConfiguration> _configurationFunc;
	protected readonly Func<IPipeContext, Action<IConsumerConfigurationBuilder>> _configActionFunc;

	public SubscriptionConfigurationMiddleware(IConsumerConfigurationFactory configFactory, SubscriptionConfigurationOptions options = null)
	{
		this._configFactory = configFactory;
		this._messageTypeFunc = options?.MessageTypeFunc ?? (context => context.GetMessageType()) ;
		this._configurationFunc = options?.ConfigFunc;
		this._configActionFunc = options?.ConfigActionFunc ?? (context => context.Get<Action<IConsumerConfigurationBuilder>>(PipeKey.ConfigurationAction));
	}

	public override async Task InvokeAsync(IPipeContext context, CancellationToken token = default)
	{
		ConsumerConfiguration config = this.ExtractFromContextProperty(context) ?? this.ExtractConfigFromMessageType(context);
		if (config == null)
		{
			this._logger.Info("Unable to extract configuration for Subscriber.");
		}

		Action<IConsumerConfigurationBuilder> action = this.GetConfigurationAction(context);
		if (action != null)
		{
			this._logger.Info("Configuration action for {queueName} found.", config?.Consume.QueueName);
			ConsumerConfigurationBuilder builder = new(config);
			action(builder);
			config = builder.Config;
		}

		context.Properties.TryAdd(PipeKey.ConsumerConfiguration, config);
		// ReSharper disable once PossibleNullReferenceException
		context.Properties.TryAdd(PipeKey.ConsumeConfiguration, config.Consume);
		context.Properties.TryAdd(PipeKey.QueueDeclaration, config.Queue);
		context.Properties.TryAdd(PipeKey.ExchangeDeclaration, config.Exchange);

		await this.Next.InvokeAsync(context, token);
	}

	protected virtual ConsumerConfiguration ExtractFromContextProperty(IPipeContext context)
	{
		return this._configurationFunc?.Invoke(context);
	}

	protected virtual ConsumerConfiguration ExtractConfigFromMessageType(IPipeContext context)
	{
		Type messageType = this._messageTypeFunc(context);
		if (messageType != null)
		{
			this._logger.Debug("Found message type {messageType} in context. Creating consumer config based on it.", messageType.Name);
		}
		return messageType == null
			? null
			: this._configFactory.Create(messageType);
	}

	protected Action<IConsumerConfigurationBuilder> GetConfigurationAction(IPipeContext context)
	{
		return this._configActionFunc?.Invoke(context);
	}
}