using System;
using RabbitMQ.Client;

namespace RawRabbit.Configuration.BasicPublish;

public class BasicPublishConfigurationBuilder : IBasicPublishConfigurationBuilder
{
	public BasicPublishConfiguration Configuration { get; }

	public BasicPublishConfigurationBuilder(BasicPublishConfiguration initial)
	{
		this.Configuration = initial;
	}

	public IBasicPublishConfigurationBuilder OnExchange(string exchange)
	{
		this.Configuration.ExchangeName = exchange;
		return this;
	}

	public IBasicPublishConfigurationBuilder WithRoutingKey(string routingKey)
	{
		this.Configuration.RoutingKey = routingKey;
		return this;
	}

	public IBasicPublishConfigurationBuilder AsMandatory(bool mandatory = true)
	{
		this.Configuration.Mandatory = mandatory;
		return this;
	}

	public IBasicPublishConfigurationBuilder WithProperties(Action<IBasicProperties> propAction)
	{
		this.Configuration.BasicProperties = this.Configuration.BasicProperties ?? new BasicProperties();
		propAction?.Invoke(this.Configuration.BasicProperties);
		return this;
	}
}