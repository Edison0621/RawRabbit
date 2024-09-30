using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Logging;

namespace RawRabbit.Pipe.Middleware;

public class ExchangeDeclareOptions
{
	public Func<IPipeContext, ExchangeDeclaration> ExchangeFunc { get; set; }
	public bool ThrowOnFail { get; set; }
	public  Func<IPipeContext, bool> ThrowOnFailFunc { get; set; }
}

public class ExchangeDeclareMiddleware : Middleware
{
	protected readonly ITopologyProvider _topologyProvider;
	protected readonly Func<IPipeContext, ExchangeDeclaration> _exchangeFunc;
	protected readonly Func<IPipeContext, bool> _throwOnFailFunc;
	private readonly ILog _logger = LogProvider.For<ExchangeDeclareMiddleware>();

	public ExchangeDeclareMiddleware(ITopologyProvider topologyProvider, ExchangeDeclareOptions options = null)
	{
		this._topologyProvider = topologyProvider;
		this._exchangeFunc = options?.ExchangeFunc ?? (context => context.GetExchangeDeclaration());
		this._throwOnFailFunc = options?.ThrowOnFailFunc ?? (_ => false);
	}

	public override async Task InvokeAsync(IPipeContext context, CancellationToken token = default)
	{
		ExchangeDeclaration exchangeCfg = this.GetExchangeDeclaration(context);

		if (exchangeCfg != null)
		{
			this._logger.Debug($"Exchange configuration found. Declaring '{exchangeCfg.Name}'.");
			await this.DeclareExchangeAsync(exchangeCfg, context, token);
		}
		else
		{
			if (this.GetThrowOnFail(context))
			{
				throw new ArgumentNullException(nameof(exchangeCfg));
			}
		}

		await this.Next.InvokeAsync(context, token);
	}

	protected virtual ExchangeDeclaration GetExchangeDeclaration(IPipeContext context)
	{
		return this._exchangeFunc?.Invoke(context);
	}

	protected virtual bool GetThrowOnFail(IPipeContext context)
	{
		return this._throwOnFailFunc(context);
	}

	protected virtual Task DeclareExchangeAsync(ExchangeDeclaration exchange, IPipeContext context, CancellationToken token)
	{
		return this._topologyProvider.DeclareExchangeAsync(exchange);
	}
}
