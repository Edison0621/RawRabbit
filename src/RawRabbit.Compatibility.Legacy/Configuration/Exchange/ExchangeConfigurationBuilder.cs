namespace RawRabbit.Compatibility.Legacy.Configuration.Exchange
{
	public class ExchangeConfigurationBuilder : IExchangeConfigurationBuilder
	{
		public ExchangeConfiguration Configuration { get; }

		public ExchangeConfigurationBuilder(ExchangeConfiguration initialExchange = null)
		{
			this.Configuration = initialExchange ?? ExchangeConfiguration.Default;
		}

		public IExchangeConfigurationBuilder WithName(string exchangeName)
		{
			this.Configuration.ExchangeName = exchangeName;
			return this;
		}

		public IExchangeConfigurationBuilder WithType(ExchangeType exchangeType)
		{
			this.Configuration.ExchangeType = exchangeType.ToString().ToLower();
			return this;
		}

		public IExchangeConfigurationBuilder WithDurability(bool durable = true)
		{
			this.Configuration.Durable = durable;
			return this;
		}

		public IExchangeConfigurationBuilder WithAutoDelete(bool autoDelete = true)
		{
			this.Configuration.AutoDelete = autoDelete;
			return this;
		}

		public IExchangeConfigurationBuilder WithArgument(string name, string value)
		{
			this.Configuration.Arguments.Add(name, value);
			return this;
		}

		public IExchangeConfigurationBuilder AssumeInitialized(bool asumption = true)
		{
			this.Configuration.AssumeInitialized = asumption;
			return this;
		}
	}
}