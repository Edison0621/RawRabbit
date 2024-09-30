using System;
using System.Collections.Generic;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Serialization;

namespace RawRabbit.Configuration.BasicPublish
{
	public class BasicPublishConfigurationFactory : IBasicPublishConfigurationFactory
	{
		private readonly INamingConventions _conventions;
		private readonly ISerializer _serializer;
		private readonly RawRabbitConfiguration _config;

		public BasicPublishConfigurationFactory(INamingConventions conventions, ISerializer serializer, RawRabbitConfiguration config)
		{
			this._conventions = conventions;
			this._serializer = serializer;
			this._config = config;
		}

		public virtual BasicPublishConfiguration Create(object message)
		{
			if (message == null)
			{
				return this.Create();
			}
			BasicPublishConfiguration cfg = this.Create(message.GetType());
			cfg.Body = this.GetBody(message);
			return cfg;
		}

		public virtual BasicPublishConfiguration Create(Type type)
		{
			return new BasicPublishConfiguration
			{
				RoutingKey = this.GetRoutingKey(type),
				BasicProperties = this.GetBasicProperties(type),
				ExchangeName = this.GetExchangeName(type),
				Mandatory = this.GetMandatory(type)
			};
		}

		public virtual BasicPublishConfiguration Create()
		{
			return new BasicPublishConfiguration
			{
				BasicProperties = new BasicProperties()
			};
		}

		protected  virtual string GetRoutingKey(Type type)
		{
			return this._conventions.RoutingKeyConvention(type);
		}

		protected virtual bool GetMandatory(Type type)
		{
			return false;
		}

		protected virtual string GetExchangeName(Type type)
		{
			return this._conventions.ExchangeNamingConvention(type);
		}

		protected virtual IBasicProperties GetBasicProperties(Type type)
		{
			return new BasicProperties
			{
				Type = type.GetUserFriendlyName(),
				MessageId = Guid.NewGuid().ToString(),
				DeliveryMode = this._config.PersistentDeliveryMode ? DeliveryModes.Persistent : DeliveryModes.Transient,
				ContentType = this._serializer.ContentType,
				ContentEncoding = "UTF-8",
				UserId = this._config.Username,
				Headers = new Dictionary<string, object>()
			};
		}

		protected virtual byte[] GetBody(object message)
		{
			return this._serializer.Serialize(message);
		}
	}
}
