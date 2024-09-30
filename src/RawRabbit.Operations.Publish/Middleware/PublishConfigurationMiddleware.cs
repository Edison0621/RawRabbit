using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Configuration.Publisher;
using RawRabbit.Logging;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Publish.Middleware;

public class PublishConfigurationOptions
{
	public Func<IPipeContext, string> ExchangeFunc { get; set; }
	public Func<IPipeContext, string> RoutingKeyFunc { get; set; }
	public Func<IPipeContext, Type> MessageTypeFunc { get; set; }
}

public class PublishConfigurationMiddleware : Pipe.Middleware.Middleware
{
	protected readonly IPublisherConfigurationFactory _publisherFactory;
	protected readonly Func<IPipeContext, string> _exchangeFunc;
	protected readonly Func<IPipeContext, string> _routingKeyFunc;
	protected readonly Func<IPipeContext, Type> _messageTypeFunc;
	private readonly ILog _logger = LogProvider.For<PublishConfigurationMiddleware>();

	public PublishConfigurationMiddleware(IPublisherConfigurationFactory publisherFactory, PublishConfigurationOptions options = null)
	{
		this._publisherFactory = publisherFactory;
		this._exchangeFunc = options?.ExchangeFunc ?? (context => context.GetPublishConfiguration()?.Exchange.Name);
		this._routingKeyFunc = options?.RoutingKeyFunc ?? (context => context.GetPublishConfiguration()?.RoutingKey);
		this._messageTypeFunc = options?.MessageTypeFunc ?? (context => context.GetMessageType());
	}

	public override Task InvokeAsync(IPipeContext context, CancellationToken token = default)
	{
		PublisherConfiguration config = this.ExtractConfigFromMessageType(context) ?? this.ExtractConfigFromStrings(context);
		if (config == null)
		{
			this._logger.Warn("Unable to find PublisherConfiguration from message type or parameters.");
			throw new ArgumentNullException(nameof(config));
		}

		Action<IPublisherConfigurationBuilder> action = context.Get<Action<IPublisherConfigurationBuilder>>(PipeKey.ConfigurationAction);
		if (action != null)
		{
			this._logger.Debug("Custom configuration supplied. Applying.");
			PublisherConfigurationBuilder builder = new(config);
			action(builder);
			config = builder.Config;
		}

		context.Properties.TryAdd(PipeKey.PublisherConfiguration, config);
		context.Properties.TryAdd(PipeKey.BasicPublishConfiguration, config);
		context.Properties.TryAdd(PipeKey.ExchangeDeclaration, config.Exchange);
		context.Properties.TryAdd(PipeKey.BasicProperties, config.BasicProperties);
		context.Properties.TryAdd(PipeKey.ReturnCallback, config.ReturnCallback);

		return this.Next.InvokeAsync(context, token);
	}

	protected virtual Type GetMessageType(IPipeContext context)
	{
		return this._messageTypeFunc(context);
	}

	protected virtual PublisherConfiguration ExtractConfigFromStrings(IPipeContext context)
	{
		string routingKey = this._routingKeyFunc(context);
		string exchange = this._exchangeFunc(context);
		return this._publisherFactory.Create(exchange, routingKey);
	}

	protected virtual PublisherConfiguration ExtractConfigFromMessageType(IPipeContext context)
	{
		Type messageType = this.GetMessageType(context);
		return messageType == null
			? null
			: this._publisherFactory.Create(messageType);
	}
}