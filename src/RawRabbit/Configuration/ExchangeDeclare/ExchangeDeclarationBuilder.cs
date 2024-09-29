using RawRabbit.Common;

namespace RawRabbit.Configuration.Exchange
{
	public class ExchangeDeclarationBuilder : IExchangeDeclarationBuilder
	{
		public ExchangeDeclaration Declaration { get; }

		public ExchangeDeclarationBuilder(ExchangeDeclaration initialExchange = null)
		{
			this.Declaration = initialExchange ?? ExchangeDeclaration.Default;
		}

		public IExchangeDeclarationBuilder WithName(string exchangeName)
		{
			Truncator.Truncate(ref exchangeName);
			this.Declaration.Name = exchangeName;
			return this;
		}

		public IExchangeDeclarationBuilder WithType(ExchangeType exchangeType)
		{
			this.Declaration.ExchangeType = exchangeType.ToString().ToLower();
			return this;
		}

		public IExchangeDeclarationBuilder WithDurability(bool durable = true)
		{
			this.Declaration.Durable = durable;
			return this;
		}

		public IExchangeDeclarationBuilder WithAutoDelete(bool autoDelete = true)
		{
			this.Declaration.AutoDelete = autoDelete;
			return this;
		}

		public IExchangeDeclarationBuilder WithArgument(string name, string value)
		{
			this.Declaration.Arguments.Add(name, value);
			return this;
		}
	}
}