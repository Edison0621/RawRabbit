using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Compatibility.Legacy.Configuration.Exchange;
using RawRabbit.Compatibility.Legacy.Configuration.Request;

namespace RawRabbit.Compatibility.Legacy.Configuration.Publish;

public class PublishConfigurationBuilder : IPublishConfigurationBuilder
{
	private readonly ExchangeConfigurationBuilder _exchange;
	private string _routingKey;
	private Action<IBasicProperties> _properties;
	private const string OneOrMoreWords = "#";
	private EventHandler<BasicReturnEventArgs> _basicReturn;

	public PublishConfiguration Configuration => new()
	{
		Exchange = this._exchange.Configuration,
		RoutingKey = this._routingKey,
		PropertyModifier = this._properties ?? (b => {}),
		BasicReturn = this._basicReturn
	};

	public PublishConfigurationBuilder(ExchangeConfiguration defaultExchange = null, string routingKey =null)
	{
		this._exchange = new ExchangeConfigurationBuilder(defaultExchange);
		this._routingKey = routingKey ?? OneOrMoreWords;
	}

	public PublishConfigurationBuilder(RequestConfiguration defaultConfig)
	{
		this._exchange = new ExchangeConfigurationBuilder(defaultConfig.Exchange);
	}

	public IPublishConfigurationBuilder WithExchange(Action<IExchangeConfigurationBuilder> exchange)
	{
		exchange(this._exchange);
		this.Configuration.Exchange = this._exchange.Configuration;
		return this;
	}

	public IPublishConfigurationBuilder WithRoutingKey(string routingKey)
	{
		this._routingKey = routingKey;
		return this;
	}

	public IPublishConfigurationBuilder WithProperties(Action<IBasicProperties> properties)
	{
		this._properties = properties;
		return this;
	}

	public IPublishConfigurationBuilder WithMandatoryDelivery(EventHandler<BasicReturnEventArgs> basicReturn)
	{
		this._basicReturn = basicReturn;
		return this;
	}
}
