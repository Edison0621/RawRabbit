using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.GlobalExecutionId.Middleware;

public class PublishHeaderAppenderOptions
{
	public Func<IPipeContext, IBasicProperties> BasicPropsFunc { get; set; }
	public Func<IPipeContext, string> GlobalExecutionIdFunc { get; set; }
	public Action<IBasicProperties, string> AppendHeaderAction { get; set; }
}

public class PublishHeaderAppenderMiddleware : StagedMiddleware
{
	protected readonly Func<IPipeContext, IBasicProperties> _basicPropsFunc;
	protected readonly Func<IPipeContext, string> _globalExecutionIdFunc;
	protected readonly Action<IBasicProperties, string> _appendAction;
	public override string StageMarker => Pipe.StageMarker.BasicPropertiesCreated;

	public PublishHeaderAppenderMiddleware(PublishHeaderAppenderOptions options = null)
	{
		this._basicPropsFunc = options?.BasicPropsFunc ?? (context => context.GetBasicProperties());
		this._globalExecutionIdFunc = options?.GlobalExecutionIdFunc ?? (context => context.GetGlobalExecutionId());
		this._appendAction = options?.AppendHeaderAction ?? ((props, id) => props.Headers.TryAdd(PropertyHeaders.GlobalExecutionId, id));
	}

	public override Task InvokeAsync(IPipeContext context, CancellationToken token = new())
	{
		IBasicProperties props = this.GetBasicProps(context);
		string id = this.GetGlobalExecutionId(context);
		this.AddIdToHeader(props, id);
		return this.Next.InvokeAsync(context, token);
	}

	protected virtual IBasicProperties GetBasicProps(IPipeContext context)
	{
		return this._basicPropsFunc?.Invoke(context);
	}

	protected virtual string GetGlobalExecutionId(IPipeContext context)
	{
		return this._globalExecutionIdFunc?.Invoke(context);
	}

	protected virtual void AddIdToHeader(IBasicProperties props, string globalExecutionId)
	{
		this._appendAction?.Invoke(props, globalExecutionId);
	}
}