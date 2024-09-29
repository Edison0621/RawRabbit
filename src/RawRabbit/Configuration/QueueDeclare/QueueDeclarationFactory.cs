using System;
using System.Collections.Generic;
using RawRabbit.Common;

namespace RawRabbit.Configuration.Queue
{
	public interface IQueueConfigurationFactory
	{
		QueueDeclaration Create(string queueName);
		QueueDeclaration Create<TMessageType>();
		QueueDeclaration Create(Type messageType);
	}

	public class QueueDeclarationFactory : IQueueConfigurationFactory
	{
		private readonly RawRabbitConfiguration _config;
		private readonly INamingConventions _conventions;

		public QueueDeclarationFactory(RawRabbitConfiguration config, INamingConventions conventions)
		{
			this._config = config;
			this._conventions = conventions;
		}

		public QueueDeclaration Create(string queueName)
		{
			return new QueueDeclaration
			{
				AutoDelete = this._config.Queue.AutoDelete,
				Durable = this._config.Queue.Durable,
				Exclusive = this._config.Queue.Exclusive,
				Name = queueName,
				Arguments = new Dictionary<string, object>()
			};
		}

		public QueueDeclaration Create<TMessageType>()
		{
			return this.Create(typeof(TMessageType));
		}

		public QueueDeclaration Create(Type messageType)
		{
			string queueName = this._conventions.QueueNamingConvention(messageType);
			return this.Create(queueName);
		}
	}
}
