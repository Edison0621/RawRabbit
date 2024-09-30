using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Logging;

namespace RawRabbit.Pipe.Middleware;

public class QueueBindOptions
{
	public Func<IPipeContext, string> QueueNameFunc { get; set; }
	public Func<IPipeContext, string> ExchangeNameFunc { get; set; }
	public Func<IPipeContext, string> RoutingKeyFunc { get; set; }
}

public class QueueBindMiddleware : Middleware
{
	protected readonly ITopologyProvider _topologyProvider;
	protected readonly Func<IPipeContext, string> _queueNameFunc;
	protected readonly Func<IPipeContext, string> _exchangeNameFunc;
	protected readonly Func<IPipeContext, string> _routingKeyFunc;
	private readonly ILog _logger = LogProvider.For<QueueBindMiddleware>();

	public QueueBindMiddleware(ITopologyProvider topologyProvider, QueueBindOptions options = null)
	{
		this._topologyProvider = topologyProvider;
		this._queueNameFunc = options?.QueueNameFunc ?? (context => context.GetConsumeConfiguration()?.QueueName);
		this._exchangeNameFunc = options?.ExchangeNameFunc ?? (context => context.GetConsumeConfiguration()?.ExchangeName);
		this._routingKeyFunc = options?.RoutingKeyFunc ?? (context => context.GetConsumeConfiguration()?.RoutingKey);
	}

	public override async Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
	{
		string queueName = this.GetQueueName(context);
		string exchangeName = this.GetExchangeName(context);
		string routingKey = this.GetRoutingKey(context);

		await this.BindQueueAsync(queueName, exchangeName, routingKey, context, token);
		await this.Next.InvokeAsync(context, token);
	}

	protected virtual Task BindQueueAsync(string queue, string exchange, string routingKey, IPipeContext context, CancellationToken ct)
	{
		return this._topologyProvider.BindQueueAsync(queue, exchange, routingKey, context.GetConsumeConfiguration()?.Arguments);
	}

	protected virtual string GetRoutingKey(IPipeContext context)
	{
		string routingKey = this._routingKeyFunc(context);
		if (routingKey == null)
		{
			this._logger.Warn("Routing key not found in Pipe context.");
		}
		return routingKey;
	}

	protected virtual string GetExchangeName(IPipeContext context)
	{
		string exchange = this._exchangeNameFunc(context);
		if (exchange == null)
		{
			this._logger.Warn("Exchange name not found in Pipe context.");
		}
		return exchange;
	}

	protected virtual string GetQueueName(IPipeContext context)
	{
		string queue = this._queueNameFunc(context);
		if (queue == null)
		{
			this._logger.Warn("Queue name not found in Pipe context.");
		}
		return queue;
	}
}