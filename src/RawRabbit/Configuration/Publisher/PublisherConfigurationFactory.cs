using System;
using RabbitMQ.Client;
using RawRabbit.Configuration.BasicPublish;
using RawRabbit.Configuration.Exchange;

namespace RawRabbit.Configuration.Publisher;

public class PublisherConfigurationFactory : IPublisherConfigurationFactory
{
	private readonly IExchangeDeclarationFactory _exchange;
	private readonly IBasicPublishConfigurationFactory _basicPublish;

	public PublisherConfigurationFactory(IExchangeDeclarationFactory exchange, IBasicPublishConfigurationFactory basicPublish)
	{
		this._exchange = exchange;
		this._basicPublish = basicPublish;
	}

	public PublisherConfiguration Create<TMessage>()
	{
		return this.Create(typeof(TMessage));
	}

	public PublisherConfiguration Create(Type messageType)
	{
		BasicPublishConfiguration cfg = this._basicPublish.Create(messageType);
		return new PublisherConfiguration
		{
			BasicProperties = cfg.BasicProperties,
			Body = cfg.Body,
			Exchange = this._exchange.Create(messageType),
			ExchangeName = cfg.ExchangeName,
			Mandatory = cfg.Mandatory,
			RoutingKey = cfg.RoutingKey
		};
	}

	public PublisherConfiguration Create(string exchangeName, string routingKey)
	{
		return new PublisherConfiguration
		{
			Exchange = this._exchange.Create(exchangeName),
			ExchangeName = exchangeName,
			RoutingKey = routingKey,
			BasicProperties = new BasicProperties()
		};
	}
}
