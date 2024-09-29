using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Logging;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using IModel = RabbitMQ.Client.IModel;

namespace RawRabbit.Operations.Subscribe.Middleware
{
	public class SubscriptionExceptionOptions
	{
		public Func<IPipeContext, IChannelFactory, Task<IModel>> ChannelFunc { get; set; }
		public Action<IPipeBuilder> InnerPipe { get; set; }
	}

	public class SubscriptionExceptionMiddleware : ExceptionHandlingMiddleware
	{
		private readonly IChannelFactory _channelFactory;
		private readonly ITopologyProvider _provider;
		private readonly INamingConventions _conventions;
		private readonly ILog _logger = LogProvider.For<SubscriptionExceptionMiddleware>();
		protected readonly Func<IPipeContext, IChannelFactory, Task<IModel>> _channelFunc;

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
			this._channelFunc = options.ChannelFunc ?? ((c, f) =>f.CreateChannelAsync());
		}

		protected override async Task OnExceptionAsync(Exception exception, IPipeContext context, CancellationToken token)
		{
			this._logger.Info(exception, "Unhandled exception thrown when consuming message");
			try
			{
				ExchangeDeclaration exchangeCfg = this.GetExchangeDeclaration(context);
				await this.DeclareErrorExchangeAsync(exchangeCfg);
				IModel channel = await this.GetChannelAsync(context);
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

		protected virtual Task<IModel> GetChannelAsync(IPipeContext context)
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

		protected virtual Task PublishToErrorExchangeAsync(IPipeContext context, IModel channel, Exception exception, ExchangeDeclaration exchange)
		{
			BasicDeliverEventArgs args = context.GetDeliveryEventArgs();
			args.BasicProperties.Headers?.TryAdd(PropertyHeaders.Host, Environment.MachineName);
			args.BasicProperties.Headers?.TryAdd(PropertyHeaders.ExceptionType, exception.GetType().Name);
			args.BasicProperties.Headers?.TryAdd(PropertyHeaders.ExceptionStackTrace, exception.StackTrace);
			channel.BasicPublish(exchange.Name, args.RoutingKey, false, args.BasicProperties, args.Body);
			return Task.FromResult(0);
		}

		protected virtual Task AckMessageIfApplicable(IPipeContext context)
		{
			bool? autoAck = context.GetConsumeConfiguration()?.AutoAck;
			if (!autoAck.HasValue)
			{
				this._logger.Debug("Unable to ack original message. Can not determine if AutoAck is configured.");
				return Task.FromResult(0);
			}
			if (autoAck.Value)
			{
				this._logger.Debug("Consuming in AutoAck mode. No ack'ing will be performed");
				return Task.FromResult(0);
			}
			ulong? deliveryTag = context.GetDeliveryEventArgs()?.DeliveryTag;
			if (deliveryTag == null)
			{
				this._logger.Info("Unable to ack original message. Delivery tag not found.");
				return Task.FromResult(0);
			}
			IModel consumerChannel = context.GetConsumer()?.Model;
			if (consumerChannel != null && consumerChannel.IsOpen)
			{
				this._logger.Debug("Acking message with {deliveryTag} on channel {channelNumber}", deliveryTag, consumerChannel.ChannelNumber);
				consumerChannel.BasicAck(deliveryTag.Value, false);
			}
			return Task.FromResult(0);
		}
	}
}
