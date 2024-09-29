using System;
using System.Collections.Generic;
using RawRabbit.Common;

namespace RawRabbit.Configuration.Consume
{
	public class ConsumeConfigurationFactory : IConsumeConfigurationFactory
	{
		private INamingConventions _conventions;

		public ConsumeConfigurationFactory(INamingConventions conventions)
		{
			this._conventions = conventions;
		}

		public ConsumeConfiguration Create<TMessage>()
		{
			return this.Create(typeof(TMessage));
		}

		public ConsumeConfiguration Create(Type messageType)
		{
			string queueName = this._conventions.QueueNamingConvention(messageType);
			string exchangeName = this._conventions.ExchangeNamingConvention(messageType);
			string routingKey = this._conventions.RoutingKeyConvention(messageType);
			return this.Create(queueName, exchangeName, routingKey);
		}

		public ConsumeConfiguration Create(string queueName, string exchangeName, string routingKey)
		{
			return new ConsumeConfiguration
			{
				QueueName = queueName,
				ExchangeName = exchangeName,
				RoutingKey = routingKey,
				ConsumerTag = Guid.NewGuid().ToString(),
				Arguments = new Dictionary<string, object>(),
			};
		}
	}
}