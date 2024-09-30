using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Common;
using RawRabbit.Configuration.BasicPublish;
using RawRabbit.Configuration.Exchange;

namespace RawRabbit.Configuration.Publisher;

public class PublisherConfigurationBuilder : IPublisherConfigurationBuilder
{
	public PublisherConfiguration Config { get; }

	public PublisherConfigurationBuilder(PublisherConfiguration initial)
	{
		this.Config = initial;
	}

	public IPublisherConfigurationBuilder OnDeclaredExchange(Action<IExchangeDeclarationBuilder> exchange)
	{
		ExchangeDeclarationBuilder builder = new(this.Config.Exchange);
		exchange(builder);
		this.Config.Exchange = builder.Declaration;
		this.Config.ExchangeName = builder.Declaration.Name;
		return this;
	}

	public IPublisherConfigurationBuilder WithReturnCallback(Action<BasicReturnEventArgs> callback)
	{
		this.Config.ReturnCallback = this.Config.ReturnCallback ?? ((_, _) =>{}) ;
		this.Config.ReturnCallback += (_, args) => callback(args);
		this.Config.Mandatory = true;
		return this;
	}

	public IBasicPublishConfigurationBuilder OnExchange(string exchange)
	{
		this.Config.Exchange = null;
		Truncation.Truncate(ref exchange);
		this.Config.ExchangeName = exchange;
		return this;
	}

	public IBasicPublishConfigurationBuilder WithRoutingKey(string routingKey)
	{
		this.Config.RoutingKey = routingKey;
		return this;
	}

	public IBasicPublishConfigurationBuilder AsMandatory(bool mandatory = true)
	{
		this.Config.Mandatory = mandatory;
		return this;
	}

	public IBasicPublishConfigurationBuilder WithProperties(Action<IBasicProperties> propAction)
	{
		this.Config.BasicProperties ??= new BasicProperties();
		propAction?.Invoke(this.Config.BasicProperties);
		return this;
	}
}
