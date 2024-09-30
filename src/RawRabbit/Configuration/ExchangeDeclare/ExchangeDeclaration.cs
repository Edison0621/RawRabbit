using System.Collections.Generic;

namespace RawRabbit.Configuration.Exchange;

public class ExchangeDeclaration
{
	public string Name { get; set; }
	public string ExchangeType { get; set; }
	public bool Durable { get; set; }
	public bool AutoDelete { get; set; }
	public IDictionary<string,object> Arguments { get; set; }

	public ExchangeDeclaration()
	{
		this.Arguments = new Dictionary<string, object>();
	}

	public ExchangeDeclaration(GeneralExchangeConfiguration exchange) : this()
	{
		this.Durable = exchange.Durable;
		this.AutoDelete = exchange.AutoDelete;
		this.ExchangeType = exchange.Type.ToString().ToLower();
	}

	public static ExchangeDeclaration Default => new()
	{
		Name = "",
		ExchangeType = RabbitMQ.Client.ExchangeType.Topic
	};

}