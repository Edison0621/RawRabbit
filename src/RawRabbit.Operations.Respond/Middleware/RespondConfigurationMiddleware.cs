using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Operations.Respond.Configuration;
using RawRabbit.Operations.Respond.Core;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Respond.Middleware;

public class RespondConfigurationOptions
{
	public Func<IPipeContext, Type> RequestTypeFunc { get; set; }
	public Func<IPipeContext, Type> ResponseTypeFunc { get; set; }
}

public class RespondConfigurationMiddleware : Pipe.Middleware.Middleware
{
	private readonly IRespondConfigurationFactory _factory;
	private readonly Func<IPipeContext, Type> _requestTypeFunc;
	private readonly Func<IPipeContext, Type> _responseTypeFunc;

	public RespondConfigurationMiddleware(IConsumerConfigurationFactory consumerFactory, RespondConfigurationOptions options = null)
		: this(new RespondConfigurationFactory(consumerFactory), options) { }

	public RespondConfigurationMiddleware(IRespondConfigurationFactory factory, RespondConfigurationOptions options = null)
	{
		this._factory = factory;
		this._requestTypeFunc = options?.RequestTypeFunc ?? (context => context.GetRequestMessageType());
		this._responseTypeFunc = options?.RequestTypeFunc ?? (context => context.GetResponseMessageType());
	}

	public override Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
	{
		Type requestType = this._requestTypeFunc(context);
		Type responseType = this._responseTypeFunc(context);
		Action<IRespondConfigurationBuilder> action = context.Get<Action<IRespondConfigurationBuilder>>(PipeKey.ConfigurationAction);

		if (requestType == null)
			throw new ArgumentNullException(nameof(requestType));
		if (responseType == null)
			throw new ArgumentNullException(nameof(responseType));
		RespondConfiguration defaultCfg = this._factory.Create(requestType, responseType);

		RespondConfigurationBuilder builder = new(defaultCfg);
		action?.Invoke(builder);

		ConsumerConfiguration respondCfg = builder.Config;
		context.Properties.TryAdd(PipeKey.ConsumerConfiguration, respondCfg);
		context.Properties.TryAdd(PipeKey.ConsumeConfiguration, respondCfg.Consume);
		context.Properties.TryAdd(PipeKey.QueueDeclaration, respondCfg.Queue);
		context.Properties.TryAdd(PipeKey.ExchangeDeclaration, respondCfg.Exchange);

		return this.Next.InvokeAsync(context, token);
	}
}