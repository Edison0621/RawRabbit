using System;
using System.Collections.Concurrent;
using RawRabbit.Channel.Abstraction;

namespace RawRabbit.Channel;

public interface IChannelPoolFactory
{
	IChannelPool GetChannelPool(string name = null);
}

public class AutoScalingChannelPoolFactory : IChannelPoolFactory, IDisposable
{
	private readonly IChannelFactory _factory;
	private readonly AutoScalingOptions _options;
	private readonly ConcurrentDictionary<string, Lazy<IChannelPool>> _channelPools;
	private const string DefaultPoolName = "default";

	public AutoScalingChannelPoolFactory(IChannelFactory factory, AutoScalingOptions options = null)
	{
		this._factory = factory;
		this._options = options ?? AutoScalingOptions.Default;
		this._channelPools = new ConcurrentDictionary<string, Lazy<IChannelPool>>();
	}

	public IChannelPool GetChannelPool(string name = null)
	{
		name = name ?? DefaultPoolName;
		Lazy<IChannelPool> pool = this._channelPools.GetOrAdd(name, s => new Lazy<IChannelPool>(() => new AutoScalingChannelPool(this._factory, this._options)));
		return pool.Value;
	}

	public void Dispose()
	{
		this._factory?.Dispose();
		foreach (Lazy<IChannelPool> channelPool in this._channelPools.Values)
		{
			(channelPool.Value as IDisposable)?.Dispose();
		}
	}
}