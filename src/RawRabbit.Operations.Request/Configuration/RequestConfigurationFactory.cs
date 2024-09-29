using System;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Configuration.Publisher;
using RawRabbit.Operations.Request.Configuration.Abstraction;

namespace RawRabbit.Operations.Request.Configuration
{
	public class RequestConfigurationFactory : IRequestConfigurationFactory
	{
		private readonly IPublisherConfigurationFactory _publisher;
		private readonly IConsumerConfigurationFactory _consumer;

		public RequestConfigurationFactory(IPublisherConfigurationFactory publisher, IConsumerConfigurationFactory consumer)
		{
			this._publisher = publisher;
			this._consumer = consumer;
		}

		public RequestConfiguration Create<TRequest, TResponse>()
		{
			return this.Create(typeof(TRequest), typeof(TResponse));
		}

		public RequestConfiguration Create(Type requestType, Type responseType)
		{
			RequestConfiguration cfg = new RequestConfiguration
			{
				Request = this._publisher.Create(requestType),
				Response = this._consumer.Create(responseType)
			};
			cfg.ToDirectRpc();
			return cfg;
		}

		public RequestConfiguration Create(string requestExchange, string requestRoutingKey, string responseQueue, string responseExchange, string responseRoutingKey)
		{
			RequestConfiguration cfg = new RequestConfiguration
			{
				Request = this._publisher.Create(requestExchange, requestRoutingKey),
				Response = this._consumer.Create(responseQueue, responseExchange, responseRoutingKey)
			};
			cfg.ToDirectRpc();
			return cfg;
		}
	}
}
