using System;
using RawRabbit.Compatibility.Legacy.Configuration.Exchange;
using RawRabbit.Compatibility.Legacy.Configuration.Queue;

namespace RawRabbit.Compatibility.Legacy.Configuration.Subscribe
{
	public class SubscriptionConfigurationBuilder : ISubscriptionConfigurationBuilder
	{
		public SubscriptionConfiguration Configuration => new SubscriptionConfiguration
		{
			Queue = this._queueBuilder.Configuration,
			Exchange = this._exchangeBuilder.Configuration,
			RoutingKey = this._routingKey ?? this._queueBuilder.Configuration.QueueName,
			AutoAck = this._autoAck,
			PrefetchCount = this._prefetchCount == 0 ? (ushort)50 : this._prefetchCount
		};

		private readonly ExchangeConfigurationBuilder _exchangeBuilder;
		private readonly QueueConfigurationBuilder _queueBuilder;
		private string _routingKey;
		private ushort _prefetchCount;
		private bool _autoAck;

		public SubscriptionConfigurationBuilder() : this(null, null, null)
		{ }

		public SubscriptionConfigurationBuilder(QueueConfiguration initialQueue, ExchangeConfiguration initialExchange, string routingKey)
		{
			this._exchangeBuilder = new ExchangeConfigurationBuilder(initialExchange);
			this._queueBuilder = new QueueConfigurationBuilder(initialQueue);
			this._routingKey = routingKey;
		}

		public ISubscriptionConfigurationBuilder WithRoutingKey(string routingKey)
		{
			this._routingKey = routingKey;
			return this;
		}

		public ISubscriptionConfigurationBuilder WithPrefetchCount(ushort prefetchCount)
		{
			this._prefetchCount = prefetchCount;
			return this;
		}

		public ISubscriptionConfigurationBuilder WithNoAck(bool noAck = true)
		{
			return this.WithAutoAck(noAck);
		}

		public ISubscriptionConfigurationBuilder WithAutoAck(bool autoAck = true)
		{
			this._autoAck = autoAck;
			return this;
		}

		public ISubscriptionConfigurationBuilder WithExchange(Action<IExchangeConfigurationBuilder> exchange)
		{
			exchange(this._exchangeBuilder);
			return this;
		}

		public ISubscriptionConfigurationBuilder WithQueue(Action<IQueueConfigurationBuilder> queue)
		{
			queue(this._queueBuilder);
			return this;
		}

		public ISubscriptionConfigurationBuilder WithSubscriberId(string subscriberId)
		{
			this._queueBuilder.WithNameSuffix(subscriberId);
			return this;
		}
	}
}
