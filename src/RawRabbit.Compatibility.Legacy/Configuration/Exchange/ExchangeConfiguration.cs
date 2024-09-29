using System.Collections.Generic;
using RawRabbit.Configuration;

namespace RawRabbit.Compatibility.Legacy.Configuration.Exchange
{
	public class ExchangeConfiguration
	{
		public string ExchangeName { get; set; }
		public string ExchangeType { get; set; }
		public bool Durable { get; set; }
		public bool AutoDelete { get; set; }
		public IDictionary<string,object> Arguments { get; set; }
		public bool AssumeInitialized { get; set; }

		public ExchangeConfiguration()
		{
			this.Arguments = new Dictionary<string, object>();
		}

		public ExchangeConfiguration(GeneralExchangeConfiguration exchange) : this()
		{
			this.Durable = exchange.Durable;
			this.AutoDelete = exchange.AutoDelete;
			this.ExchangeType = exchange.Type.ToString().ToLower();
		}

		public static ExchangeConfiguration Default => new ExchangeConfiguration
		{
			ExchangeName = "",
			ExchangeType = RabbitMQ.Client.ExchangeType.Topic
		};

	}
}
