using System;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Configuration.Publisher;
using RawRabbit.Operations.Request.Configuration.Abstraction;

namespace RawRabbit.Operations.Request.Configuration;

public class RequestConfigurationBuilder : IRequestConfigurationBuilder
{
	public RequestConfiguration Config { get; }

	public RequestConfigurationBuilder(RequestConfiguration initial)
	{
		this.Config = initial;
	}

	public IRequestConfigurationBuilder PublishRequest(Action<IPublisherConfigurationBuilder> publish)
	{
		PublisherConfigurationBuilder builder = new(this.Config.Request);
		publish(builder);
		this.Config.Request = builder.Config;
		return this;
	}

	public IRequestConfigurationBuilder ConsumeResponse(Action<IConsumerConfigurationBuilder> consume)
	{
		ConsumerConfigurationBuilder builder = new(this.Config.Response);
		consume(builder);
		this.Config.Response = builder.Config;
		return this;
	}
}