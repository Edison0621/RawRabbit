using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Configuration.Publisher;
using RawRabbit.Operations.Request.Configuration;
using RawRabbit.Operations.Request.Configuration.Abstraction;
using RawRabbit.Operations.Request.Core;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Request.Middleware;

public class RequestConfigurationMiddleware : Pipe.Middleware.Middleware
{
	private readonly IRequestConfigurationFactory _factory;

	public RequestConfigurationMiddleware(IPublisherConfigurationFactory publisher, IConsumerConfigurationFactory consumer)
	{
		this._factory = new RequestConfigurationFactory(publisher, consumer);
	}

	public RequestConfigurationMiddleware(IRequestConfigurationFactory factory)
	{
		this._factory = factory;
	}

	public override Task InvokeAsync(IPipeContext context, CancellationToken token = default)
	{
		Type requestType = context.GetRequestMessageType();
		Type responseType = context.GetResponseMessageType();

		if (requestType == null)
			throw new ArgumentNullException(nameof(requestType));
		if (responseType == null)
			throw new ArgumentNullException(nameof(responseType));

		RequestConfiguration defaultCfg = this._factory.Create(requestType, responseType);

		RequestConfigurationBuilder builder = new(defaultCfg);
		Action<IRequestConfigurationBuilder> action = context.Get<Action<IRequestConfigurationBuilder>>(PipeKey.ConfigurationAction);
		action?.Invoke(builder);
		RequestConfiguration requestConfig = builder.Config;

		context.Properties.TryAdd(RequestKey.Configuration, requestConfig);
		context.Properties.TryAdd(PipeKey.PublisherConfiguration, requestConfig.Request);
		context.Properties.TryAdd(PipeKey.ConsumerConfiguration, requestConfig.Response);
		context.Properties.TryAdd(PipeKey.ConsumeConfiguration, requestConfig.Response.Consume);
		return this.Next.InvokeAsync(context, token);
	}
}