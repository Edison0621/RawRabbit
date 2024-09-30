using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Common;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;
using RawRabbit.Logging;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using ExchangeType = RabbitMQ.Client.ExchangeType;

namespace RawRabbit.Middleware
{
	public class RetryLaterOptions
	{
		public Func<IPipeContext, Acknowledgement> AcknowledgementFunc { get; set; }
		public Func<IPipeContext, BasicDeliverEventArgs> DeliveryArgsFunc { get; set; }
	}

	public class RetryLaterMiddleware : StagedMiddleware
	{
		private readonly ILog _logger = LogProvider.For<RetryLaterMiddleware>();
		protected readonly ITopologyProvider _topologyProvider;
		protected readonly INamingConventions _conventions;
		protected readonly IChannelFactory _channelFactory;
		private readonly IRetryInformationHeaderUpdater _headerUpdater;
		protected readonly Func<IPipeContext, Acknowledgement> _acknowledgementFunc;
		protected readonly Func<IPipeContext, BasicDeliverEventArgs> _deliveryArgsFunc;

		public override string StageMarker => Pipe.StageMarker.HandlerInvoked;

		public RetryLaterMiddleware(ITopologyProvider topology, INamingConventions conventions, IChannelFactory channelFactory, IRetryInformationHeaderUpdater headerUpdater, RetryLaterOptions options = null)
		{
			this._topologyProvider = topology;
			this._conventions = conventions;
			this._channelFactory = channelFactory;
			this._headerUpdater = headerUpdater;
			this._acknowledgementFunc = options?.AcknowledgementFunc ?? (context => context.GetMessageAcknowledgement());
			this._deliveryArgsFunc = options?.DeliveryArgsFunc ?? (context => context.GetDeliveryEventArgs());
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
		{
			Acknowledgement ack = this.GetMessageAcknowledgement(context);
			if (ack is not Retry retryAck)
			{
				await this.Next.InvokeAsync(context, token);
				return;
			}

			string deadLetterExchangeName = this.GetDeadLetterExchangeName(retryAck.Span);
			await this._topologyProvider.DeclareExchangeAsync(new ExchangeDeclaration
			{
				Name = deadLetterExchangeName,
				Durable = true,
				ExchangeType = ExchangeType.Direct
			});

			BasicDeliverEventArgs deliveryArgs = this.GetDeliveryEventArgs(context);
			this._logger.Info("Message is marked for Retry. Will be published on exchange {exchangeName} with routing key {routingKey} in {retryIn}", deliveryArgs.Exchange, deliveryArgs.RoutingKey, retryAck.Span);
			this.UpdateRetryHeaders(deliveryArgs, context);
			string deadLetterQueueName = this.GetDeadLetterQueueName(deliveryArgs.Exchange, retryAck.Span);
			string deadLetterExchange = context?.GetConsumerConfiguration()?.Exchange.Name ?? deliveryArgs.Exchange;
			await this._topologyProvider.DeclareQueueAsync(new QueueDeclaration
			{
				Name = deadLetterQueueName,
				Durable = true,
				Arguments = new Dictionary<string, object>
				{
					{QueueArgument.DeadLetterExchange, deadLetterExchange},
					{QueueArgument.Expires, Convert.ToInt32(retryAck.Span.Add(TimeSpan.FromSeconds(1)).TotalMilliseconds)},
					{QueueArgument.MessageTtl, Convert.ToInt32(retryAck.Span.TotalMilliseconds)}
				}
			});

			await this._topologyProvider.BindQueueAsync(deadLetterQueueName, deadLetterExchangeName, deliveryArgs.RoutingKey, deliveryArgs.BasicProperties.Headers);
			using (IChannel publishChannel = await this._channelFactory.CreateChannelAsync(token))
			{
				BasicProperties basicProperties = new()
				{
					//TODO 
					Headers = deliveryArgs.BasicProperties.Headers
				};

				await publishChannel.BasicPublishAsync(deadLetterExchangeName, deliveryArgs.RoutingKey, false, basicProperties, deliveryArgs.Body, token);
			}

			await this._topologyProvider.UnbindQueueAsync(deadLetterQueueName, deadLetterExchangeName, deliveryArgs.RoutingKey, deliveryArgs.BasicProperties.Headers);

			context?.Properties.AddOrReplace(PipeKey.MessageAcknowledgement, new Ack());
			await this.Next.InvokeAsync(context, token);
		}

		private string GetDeadLetterQueueName(string originalExchangeName, TimeSpan retryAckSpan)
		{
			return this._conventions.RetryLaterQueueNameConvetion(originalExchangeName, retryAckSpan);
		}

		protected virtual Acknowledgement GetMessageAcknowledgement(IPipeContext context)
		{
			return this._acknowledgementFunc?.Invoke(context);
		}

		protected virtual BasicDeliverEventArgs GetDeliveryEventArgs(IPipeContext context)
		{
			return this._deliveryArgsFunc?.Invoke(context);
		}

		protected virtual string GetDeadLetterExchangeName(TimeSpan retryIn)
		{
			return this._conventions.RetryLaterExchangeConvention(retryIn);
		}

		protected virtual TimeSpan GetRetryTimeSpan(IPipeContext context)
		{
			return (this.GetMessageAcknowledgement(context) as Retry)?.Span ?? new TimeSpan(-1);
		}

		protected virtual void UpdateRetryHeaders(BasicDeliverEventArgs args, IPipeContext context)
		{
			RetryInformation retryInfo = context.GetRetryInformation();
			this._headerUpdater.AddOrUpdate(args, retryInfo);
		}
	}
}
