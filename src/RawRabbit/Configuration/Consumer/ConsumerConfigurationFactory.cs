using System;
using RawRabbit.Common;
using RawRabbit.Configuration.Consume;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Configuration.Consumer
{
	public class ConsumerConfigurationFactory : IConsumerConfigurationFactory
	{
		private readonly IQueueConfigurationFactory _queue;
		private readonly IExchangeDeclarationFactory _exchange;
		private readonly IConsumeConfigurationFactory _consume;
		private readonly INamingConventions _conventions;

		public ConsumerConfigurationFactory(IQueueConfigurationFactory queue, IExchangeDeclarationFactory exchange, IConsumeConfigurationFactory consume, INamingConventions conventions)
		{
			this._queue = queue;
			this._exchange = exchange;
			this._consume = consume;
			this._conventions = conventions;
		}

		public ConsumerConfiguration Create<TMessage>()
		{
			return this.Create(typeof(TMessage));
		}

		public ConsumerConfiguration Create(Type messageType)
		{
			string queueName = this._conventions.QueueNamingConvention(messageType);
			string exchangeName = this._conventions.ExchangeNamingConvention(messageType);
			string routingKey = this._conventions.RoutingKeyConvention(messageType);
			return this.Create(queueName, exchangeName, routingKey);
		}

		public ConsumerConfiguration Create(string queueName, string exchangeName, string routingKey)
		{
			ConsumerConfiguration cfg =  new ConsumerConfiguration
			{
				Queue = this._queue.Create(queueName),
				Exchange = this._exchange.Create(exchangeName),
				Consume = this._consume.Create(queueName, exchangeName, routingKey)
			};
			return cfg;
		}
	}
}