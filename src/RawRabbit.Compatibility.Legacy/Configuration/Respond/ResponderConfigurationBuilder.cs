using System;
using RawRabbit.Compatibility.Legacy.Configuration.Exchange;
using RawRabbit.Compatibility.Legacy.Configuration.Queue;

namespace RawRabbit.Compatibility.Legacy.Configuration.Respond
{
	class ResponderConfigurationBuilder : IResponderConfigurationBuilder
	{
		private readonly ExchangeConfigurationBuilder _exchangeBuilder;
		private readonly QueueConfigurationBuilder _queueBuilder;

		public ResponderConfiguration Configuration { get; }

		public ResponderConfigurationBuilder(QueueConfiguration defaultQueue = null, ExchangeConfiguration defaultExchange = null)
		{
			this._exchangeBuilder = new ExchangeConfigurationBuilder(defaultExchange);
			this._queueBuilder = new QueueConfigurationBuilder(defaultQueue);
			this.Configuration = new ResponderConfiguration
			{
				Queue = this._queueBuilder.Configuration,
				Exchange = this._exchangeBuilder.Configuration,
				RoutingKey = this._queueBuilder.Configuration.QueueName
			};
		}

		public IResponderConfigurationBuilder WithExchange(Action<IExchangeConfigurationBuilder> exchange)
		{
			exchange(this._exchangeBuilder);
			this.Configuration.Exchange = this._exchangeBuilder.Configuration;
			return this;
		}

		public IResponderConfigurationBuilder WithPrefetchCount(ushort count)
		{
			this.Configuration.PrefetchCount = count;
			return this;
		}

		public IResponderConfigurationBuilder WithQueue(Action<IQueueConfigurationBuilder> queue)
		{
			queue(this._queueBuilder);
			this.Configuration.Queue = this._queueBuilder.Configuration;
			if (string.IsNullOrEmpty(this.Configuration.RoutingKey))
			{
				this.Configuration.RoutingKey = this._queueBuilder.Configuration.QueueName;
			}
			return this;
		}

		public IResponderConfigurationBuilder WithRoutingKey(string routingKey)
		{
			this.Configuration.RoutingKey = routingKey;
			return this;
		}

		public IResponderConfigurationBuilder WithNoAck(bool noAck)
		{
			return this.WithAutoAck(noAck);
		}

		public IResponderConfigurationBuilder WithAutoAck(bool autoAck = true)
		{
			this.Configuration.AutoAck = autoAck;
			return this;
		}
	}
}