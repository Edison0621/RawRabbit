using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace RawRabbit.Pipe.Middleware;

public class QueueDeleteOptions
{
	public Func<IPipeContext, string> QueueNameFunc { get; set; }
	public Func<IPipeContext, IChannel> ChannelFunc { get; set; }
	public Func<IPipeContext, bool> IfUnusedFunc { get; set; }
	public Func<IPipeContext, bool> IfEmptyFunc { get; set; }
}

public class QueueDeleteMiddleware : Middleware
{
	protected readonly Func<IPipeContext, IChannel> _channelFunc;
	protected readonly Func<IPipeContext, string> _queueNameFunc;
	protected readonly Func<IPipeContext, bool> _ifUnusedFunc;
	protected readonly Func<IPipeContext, bool> _ifEmptyFunc;

	public QueueDeleteMiddleware(QueueDeleteOptions options = null)
	{
		this._channelFunc = options?.ChannelFunc ?? (context => context.GetTransientChannel());
		this._queueNameFunc = options?.QueueNameFunc ?? (context => string.Empty);
		this._ifUnusedFunc = options?.IfUnusedFunc ?? (context => false);
		this._ifEmptyFunc = options?.IfEmptyFunc ?? (context => false);
	}

	public override async Task InvokeAsync(IPipeContext context, CancellationToken token = new())
	{
		IChannel channel = this.GetChannel(context);
		string queueName = this.GetQueueName(context);
		bool ifEmpty = this.GetIfEmpty(context);
		bool ifUnused = this.GetIfUnused(context);
		await this.DeleteQueueAsync(channel, queueName, ifUnused, ifEmpty);
		await this.Next.InvokeAsync(context, token);
	}

	private async Task DeleteQueueAsync(IChannel channel, string queueName, bool ifUnused, bool ifEmpty)
	{
		await channel.QueueDeleteAsync(queueName, ifUnused, ifEmpty);
	}

	protected virtual IChannel GetChannel(IPipeContext context)
	{
		return this._channelFunc(context);
	}

	protected virtual string GetQueueName(IPipeContext context)
	{
		return this._queueNameFunc(context);
	}

	protected virtual bool GetIfUnused(IPipeContext context)
	{
		return this._ifUnusedFunc(context);
	}

	protected virtual bool GetIfEmpty(IPipeContext context)
	{
		return this._ifEmptyFunc(context);
	}
}