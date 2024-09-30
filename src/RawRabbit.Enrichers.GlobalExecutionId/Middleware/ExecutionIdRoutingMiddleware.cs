using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Configuration.BasicPublish;
using RawRabbit.Logging;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.GlobalExecutionId.Middleware;

public class ExecutionIdRoutingOptions
{
	public Func<IPipeContext, bool> EnableRoutingFunc { get; set; }
	public Func<IPipeContext, string> ExecutionIdFunc { get; set; }
	public Func<IPipeContext, string, string> UpdateAction { get; set; }
}

public class ExecutionIdRoutingMiddleware : StagedMiddleware
{
	public override string StageMarker => Pipe.StageMarker.PublishConfigured;
	protected readonly Func<IPipeContext, bool> _enableRoutingFunc;
	protected readonly Func<IPipeContext, string> _executionIdFunc;
	protected readonly Func<IPipeContext, string, string> _updateAction;
	private readonly ILog _logger = LogProvider.For<ExecutionIdRoutingMiddleware>();

	public ExecutionIdRoutingMiddleware(ExecutionIdRoutingOptions options = null)
	{
		this._enableRoutingFunc = options?.EnableRoutingFunc ?? (c => c.GetClientConfiguration()?.RouteWithGlobalId ?? false);
		this._executionIdFunc = options?.ExecutionIdFunc ?? (c => c.GetGlobalExecutionId());
		this._updateAction = options?.UpdateAction ?? ((context, executionId) =>
		{
			BasicPublishConfiguration pubConfig = context.GetBasicPublishConfiguration();
			if (pubConfig != null)
			{
				pubConfig.RoutingKey = $"{pubConfig.RoutingKey}.{executionId}";
				return pubConfig.RoutingKey;
			}
			return string.Empty;
		});
	}

	public override Task InvokeAsync(IPipeContext context, CancellationToken token = default)
	{
		bool enabled = this.GetRoutingEnabled(context);
		if (!enabled)
		{
			this._logger.Debug("Routing with GlobalExecutionId disabled.");
			return this.Next.InvokeAsync(context, token);
		}
		string executionId = this.GetExecutionId(context);
		this.UpdateRoutingKey(context, executionId);
		return this.Next.InvokeAsync(context, token);
	}

	protected virtual void UpdateRoutingKey(IPipeContext context, string executionId)
	{
		this._logger.Debug("Updating routing key with GlobalExecutionId {globalExecutionId}", executionId);
		string updated = this._updateAction(context, executionId);
		this._logger.Info("Routing key updated with GlobalExecutionId: {routingKey}", updated);
	}

	protected virtual bool GetRoutingEnabled(IPipeContext pipeContext)
	{
		return this._enableRoutingFunc(pipeContext);
	}

	protected virtual string GetExecutionId(IPipeContext context)
	{
		return this._executionIdFunc(context);
	}
}