using Polly;
using Polly.NoOp;
using RawRabbit.Pipe;

namespace RawRabbit.Enrichers.Polly;

public static class PipeContextExtensions
{
	public static AsyncNoOpPolicy GetPolicy(this IPipeContext context, string policyName = null)
	{
		AsyncNoOpPolicy fallback = context.Get<AsyncNoOpPolicy>(PolicyKeys.DefaultPolicy);
		return context.Get(policyName, fallback);
	}

	public static TPipeContext UsePolicy<TPipeContext>(this TPipeContext context, Policy policy, string policyName = null) where TPipeContext : IPipeContext
	{
		policyName = policyName ?? PolicyKeys.DefaultPolicy;
		context.Properties.TryAdd(policyName, policy);
		return context;
	}
}
