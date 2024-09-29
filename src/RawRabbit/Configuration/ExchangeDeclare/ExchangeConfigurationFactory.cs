using System;
using System.Collections.Generic;
using RawRabbit.Common;

namespace RawRabbit.Configuration.Exchange
{
	public interface IExchangeDeclarationFactory
	{
		ExchangeDeclaration Create(string exchangeName);
		ExchangeDeclaration Create<TMessage>();
		ExchangeDeclaration Create(Type messageType);
	}

	public class ExchangeDeclarationFactory : IExchangeDeclarationFactory
	{
		private readonly RawRabbitConfiguration _config;
		private readonly INamingConventions _conventions;

		public ExchangeDeclarationFactory(RawRabbitConfiguration config, INamingConventions conventions)
		{
			this._config = config;
			this._conventions = conventions;
		}

		public ExchangeDeclaration Create(string exchangeName)
		{
			return new ExchangeDeclaration
			{
				Arguments = new Dictionary<string, object>(),
				ExchangeType = this._config.Exchange.Type.ToString().ToLower(),
				Durable = this._config.Exchange.Durable,
				AutoDelete = this._config.Exchange.AutoDelete,
				Name = exchangeName
			};
		}

		public ExchangeDeclaration Create<TMessage>()
		{
			return this.Create(typeof(TMessage));
		}

		public ExchangeDeclaration Create(Type messageType)
		{
			string exchangeName = this._conventions.ExchangeNamingConvention(messageType);
			return this.Create(exchangeName);
		}
	}
}
