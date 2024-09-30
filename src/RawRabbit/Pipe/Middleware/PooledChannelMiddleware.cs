using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Channel;

namespace RawRabbit.Pipe.Middleware;

public class PooledChannelOptions
{
	public Func<IPipeContext, string> PoolNameFunc { get; set; }
	public Action<IPipeContext, IChannel> SaveInContextAction { get; set; }
}

public class PooledChannelMiddleware : Middleware
{
	protected readonly IChannelPoolFactory _poolFactory;
	protected readonly Func<IPipeContext, string> _poolNameFunc;
	protected readonly Action<IPipeContext, IChannel> _saveInContextAction;

	public PooledChannelMiddleware(IChannelPoolFactory poolFactory, PooledChannelOptions options = null)
	{
		this._poolFactory = poolFactory;
		this._poolNameFunc = options?.PoolNameFunc;
		this._saveInContextAction = options?.SaveInContextAction ?? ((ctx, value) =>ctx.Properties.TryAdd(PipeKey.TransientChannel, value));
	}

	public override async Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
	{
		IChannel channel = await this.GetChannelAsync(context, token);
		this.SaveInContext(context, channel);
		await this.Next.InvokeAsync(context, token);
	}

	protected virtual string GetChannelPoolName(IPipeContext context)
	{
		return this._poolNameFunc?.Invoke(context);
	}

	protected virtual IChannelPool GetChannelPool(IPipeContext context)
	{
		string poolName = this.GetChannelPoolName(context);
		return this._poolFactory.GetChannelPool(poolName);
	}

	protected virtual Task<IChannel> GetChannelAsync(IPipeContext context, CancellationToken ct)
	{
		IChannelPool channelPool = this.GetChannelPool(context);
		return channelPool.GetAsync(ct);
	}

	protected virtual void SaveInContext(IPipeContext context, IChannel channel)
	{
		this._saveInContextAction?.Invoke(context, channel);
	}
}