using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Logging;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Operations.Subscribe.Middleware;

public class SubscriptionExceptionOptions
{
	public Func<IPipeContext, IChannelFactory, Task<IChannel>> ChannelFunc { get; set; }
	public Action<IPipeBuilder> InnerPipe { get; set; }
}

public class SubscriptionExceptionMiddleware : ExceptionHandlingMiddleware
{
	private readonly IChannelFactory _channelFactory;
	private readonly ITopologyProvider _provider;
	private readonly INamingConventions _conventions;
	private readonly ILog _logger = LogProvider.For<SubscriptionExceptionMiddleware>();
	protected readonly Func<IPipeContext, IChannelFactory, Task<IChannel>> _channelFunc;

	public SubscriptionExceptionMiddleware(
		IPipeBuilderFactory factory,
		IChannelFactory channelFactory,
		ITopologyProvider provider,
		INamingConventions conventions,
		SubscriptionExceptionOptions options)
		: base(factory, new ExceptionHandlingOptions {InnerPipe = options.InnerPipe})
	{
		this._channelFactory = channelFactory;
		this._provider = provider;
		this._conventions = conventions;
		this._channelFunc = options.ChannelFunc ?? ((_, f) =>f.CreateChannelAsync());
	}

	protected override async Task OnExceptionAsync(Exception exception, IPipeContext context, CancellationToken token)
	{
		this._logger.Info(exception, "Unhandled exception thrown when consuming message");
		try
		{
			ExchangeDeclaration exchangeCfg = this.GetExchangeDeclaration(context);
			await this.DeclareErrorExchangeAsync(exchangeCfg);
			IChannel channel = await this.GetChannelAsync(context);
			await this.PublishToErrorExchangeAsync(context, channel, exception, exchangeCfg);
			channel.Dispose();
		}
		catch (Exception e)
		{
			this._logger.Error(e, "Unable to publish message to Error Exchange");
		}
		try
		{
			await this.AckMessageIfApplicable(context);
		}
		catch (Exception e)
		{
			this._logger.Error(e, "Unable to ack message.");
		}
	}

	protected virtual Task<IChannel> GetChannelAsync(IPipeContext context)
	{
		return this._channelFunc(context, this._channelFactory);
	}

	protected virtual Task DeclareErrorExchangeAsync(ExchangeDeclaration exchange)
	{
		return this._provider.DeclareExchangeAsync(exchange);
	}

	protected virtual ExchangeDeclaration GetExchangeDeclaration(IPipeContext context)
	{
		GeneralExchangeConfiguration generalCfg = context?.GetClientConfiguration()?.Exchange;
		return new ExchangeDeclaration(generalCfg)
		{
			Name = this._conventions.ErrorExchangeNamingConvention()
		};
	}

	protected virtual async Task PublishToErrorExchangeAsync(IPipeContext context, IChannel channel, Exception exception, ExchangeDeclaration exchange)
	{
		BasicDeliverEventArgs args = context.GetDeliveryEventArgs();

		BasicProperties basicProperties = new();
		basicProperties.Headers?.TryAdd(PropertyHeaders.Host, Environment.MachineName);
		basicProperties.Headers?.TryAdd(PropertyHeaders.ExceptionType, exception.GetType().Name);
		basicProperties.Headers?.TryAdd(PropertyHeaders.ExceptionStackTrace, exception.StackTrace);

		await channel.BasicPublishAsync(exchange.Name, args.RoutingKey, false, basicProperties, args.Body);
	}

	protected virtual async Task AckMessageIfApplicable(IPipeContext context)
	{
		bool? autoAck = context.GetConsumeConfiguration()?.AutoAck;
		if (!autoAck.HasValue)
		{
			this._logger.Debug("Unable to ack original message. Can not determine if AutoAck is configured.");
			return;
		}
		if (autoAck.Value)
		{
			this._logger.Debug("Consuming in AutoAck mode. No ack'ing will be performed");
			return;
		}
		ulong? deliveryTag = context.GetDeliveryEventArgs()?.DeliveryTag;
		if (deliveryTag == null)
		{
			this._logger.Info("Unable to ack original message. Delivery tag not found.");
			return;
		}
		IChannel consumerChannel = context.GetConsumer()?.Channel;
		if (consumerChannel != null && consumerChannel.IsOpen)
		{
			this._logger.Debug("Acking message with {deliveryTag} on channel {channelNumber}", deliveryTag, consumerChannel.ChannelNumber);
			await consumerChannel.BasicAckAsync(deliveryTag.Value, false);
		}
	}
}