using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Enrichers.Polly.Services;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit;

public class PolicyOptions
{
	public Action<IPipeContext> PolicyAction { get; set; }
	public ConnectionPolicies ConnectionPolicies { get; set; }
}

public class PolicyMiddleware : StagedMiddleware
{
	protected readonly Action<IPipeContext> _policyAction;

	public PolicyMiddleware(PolicyOptions options = null)
	{
		this._policyAction = options?.PolicyAction;
	}

	public override Task InvokeAsync(IPipeContext context, CancellationToken token = new())
	{
		this.AddPolicies(context);
		return this.Next.InvokeAsync(context, token);
	}

	protected virtual void AddPolicies(IPipeContext context)
	{
		this._policyAction?.Invoke(context);
	}

	public override string StageMarker => Pipe.StageMarker.Initialized;
}