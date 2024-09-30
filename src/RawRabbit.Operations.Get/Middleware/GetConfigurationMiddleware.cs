using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Configuration.Get;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Get.Middleware;

public class GetConfigurationOptions
{
	public Func<IPipeContext, GetConfiguration> CreateFunc { get; set; }
	public Func<IPipeContext, Action<IGetConfigurationBuilder>> ConfigBuilderFunc { get; set; }
	public Action<IPipeContext, GetConfiguration> PostExecuteAction { get; set; }
}

public class GetConfigurationMiddleware : Pipe.Middleware.Middleware
{
	protected readonly Func<IPipeContext, GetConfiguration> _createFunc;
	protected readonly Action<IPipeContext, GetConfiguration> _postExecutionAction;
	protected readonly Func<IPipeContext, Action<IGetConfigurationBuilder>> _configBuilderFunc;

	public GetConfigurationMiddleware(GetConfigurationOptions options = null)
	{
		this._createFunc = options?.CreateFunc ?? (context => new GetConfiguration());
		this._postExecutionAction = options?.PostExecuteAction;
		this._configBuilderFunc = options?.ConfigBuilderFunc ?? (context => context.Get<Action<IGetConfigurationBuilder>>(PipeKey.ConfigurationAction));
	}
	public override Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
	{
		GetConfiguration defaultCfg = this.CreateConfiguration(context);
		Action<IGetConfigurationBuilder> configAction = this.GetConfigurationAction(context);
		GetConfiguration config = this.GetConfiguredConfiguration(defaultCfg, configAction);
		this._postExecutionAction?.Invoke(context, config);
		context.Properties.TryAdd(GetPipeExtensions.GetConfiguration, config);
		return this.Next.InvokeAsync(context, token);
	}

	protected virtual GetConfiguration CreateConfiguration(IPipeContext context)
	{
		return this._createFunc(context);
	}

	protected virtual Action<IGetConfigurationBuilder> GetConfigurationAction(IPipeContext context)
	{
		return this._configBuilderFunc(context);
	}

	protected virtual GetConfiguration GetConfiguredConfiguration(GetConfiguration configuration, Action<IGetConfigurationBuilder> action)
	{
		if (action == null)
		{
			return configuration;
		}
		GetConfigurationBuilder builder = new(configuration);
		action.Invoke(builder);
		return builder.Configuration;
	}
}