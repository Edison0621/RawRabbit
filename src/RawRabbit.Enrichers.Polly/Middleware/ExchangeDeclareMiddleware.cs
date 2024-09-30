﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Polly.NoOp;
using RawRabbit.Common;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.Polly.Middleware;

public class ExchangeDeclareMiddleware : Pipe.Middleware.ExchangeDeclareMiddleware
{
	public ExchangeDeclareMiddleware(ITopologyProvider topologyProvider, ExchangeDeclareOptions options = null)
		: base(topologyProvider, options) { }

	protected override Task DeclareExchangeAsync(ExchangeDeclaration exchange, IPipeContext context, CancellationToken token)
	{
		AsyncNoOpPolicy policy = context.GetPolicy(PolicyKeys.ExchangeDeclare);
		return policy.ExecuteAsync(
			action: _ => base.DeclareExchangeAsync(exchange, context, token),
			contextData: new Dictionary<string, object>
			{
				[RetryKey.TopologyProvider] = this._topologyProvider,
				[RetryKey.ExchangeDeclaration] = exchange,
				[RetryKey.PipeContext] = context,
				[RetryKey.CancellationToken] = token,
			});
	}
}
