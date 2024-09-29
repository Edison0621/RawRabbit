using System;
using RawRabbit.Compatibility.Legacy.Configuration.Exchange;
using RawRabbit.Compatibility.Legacy.Configuration.Queue;

namespace RawRabbit.Compatibility.Legacy.Configuration.Request
{
	public class RequestConfigurationBuilder : IRequestConfigurationBuilder
	{
		private readonly QueueConfigurationBuilder _replyQueue;
		private readonly ExchangeConfigurationBuilder _exchange;
		public RequestConfiguration Configuration { get; }

		public RequestConfigurationBuilder(RequestConfiguration defaultConfig)
		{
			this._replyQueue = new QueueConfigurationBuilder(defaultConfig.ReplyQueue);
			this._exchange = new ExchangeConfigurationBuilder(defaultConfig.Exchange);
			this.Configuration = defaultConfig;
		}

		public IRequestConfigurationBuilder WithExchange(Action<IExchangeConfigurationBuilder> exchange)
		{
			exchange(this._exchange);
			this.Configuration.Exchange = this._exchange.Configuration;
			return this;
		}

		public IRequestConfigurationBuilder WithRoutingKey(string routingKey)
		{
			this.Configuration.RoutingKey = routingKey;
			return this;
		}

		public IRequestConfigurationBuilder WithReplyQueue(Action<IQueueConfigurationBuilder> replyTo)
		{
			replyTo(this._replyQueue);
			this.Configuration.ReplyQueue = this._replyQueue.Configuration;
			return this;
		}

		public IRequestConfigurationBuilder WithNoAck(bool noAck)
		{
			return this.WithAutoAck(noAck);
		}

		public IRequestConfigurationBuilder WithAutoAck(bool autoAck)
		{
			this.Configuration.AutoAck = autoAck;
			return this;
		}
	}
}
