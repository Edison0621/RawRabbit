using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Configuration.BasicPublish;

namespace RawRabbit.Pipe.Middleware;

public class BasicPublishConfigurationOptions
{
	public Func<IPipeContext, Action<IBasicPublishConfigurationBuilder>> ConfigurationActionFunc { get; set; }
	public Action<IPipeContext, BasicPublishConfiguration> PostInvokeAction { get; set; }
}

public class BasicPublishConfigurationMiddleware : Middleware
{
	private readonly IBasicPublishConfigurationFactory _factory;
	protected readonly Func<IPipeContext, Action<IBasicPublishConfigurationBuilder>> _configurationActionFunc;
	protected readonly Action<IPipeContext, BasicPublishConfiguration> _postInvokeAction;

	public BasicPublishConfigurationMiddleware(IBasicPublishConfigurationFactory factory, BasicPublishConfigurationOptions options = null)
	{
		this._factory = factory;
		this._configurationActionFunc = options?.ConfigurationActionFunc ?? (context => context.Get<Action<IBasicPublishConfigurationBuilder>>(PipeKey.ConfigurationAction));
		this._postInvokeAction = options?.PostInvokeAction;
	}

	public override async Task InvokeAsync(IPipeContext context, CancellationToken token = default)
	{
		BasicPublishConfiguration config = this.GetInitialConfig(context);
		Action<IBasicPublishConfigurationBuilder> configAction = this.GetConfigurationAction(context);
		if (configAction != null)
		{
			BasicPublishConfigurationBuilder builder = new(config);
			configAction.Invoke(builder);
			config = builder.Configuration;
		}

		this._postInvokeAction?.Invoke(context, config);
		context.Properties.TryAdd(PipeKey.BasicPublishConfiguration, config);
		await this.Next.InvokeAsync(context, token);
	}

	protected virtual BasicPublishConfiguration GetInitialConfig(IPipeContext context)
	{
		object message = context.GetMessage();
		return message != null
			? this._factory.Create(message)
			: this._factory.Create();
	}

	protected virtual Action<IBasicPublishConfigurationBuilder> GetConfigurationAction(IPipeContext context)
	{
		return this._configurationActionFunc.Invoke(context);
	}
}