using System.Collections.Generic;
using System.Linq;
using RabbitMQ.Client;

namespace RawRabbit.Channel;

public class DynamicChannelPool : StaticChannelPool
{
	public DynamicChannelPool()
		: this(Enumerable.Empty<IChannel>()) { }

	public DynamicChannelPool(IEnumerable<IChannel> seed)
		: base(seed) { }

	public void Add(params IChannel[] channels)
	{
		this.Add(channels.ToList());
	}

	public void Add(IEnumerable<IChannel> channels)
	{
		foreach (IChannel channel in channels)
		{
			this.ConfigureRecovery(channel);
			if (this._pool.Contains(channel))
			{
				continue;
			}

			this._pool.AddLast(channel);
		}
	}

	public void Remove(int numberOfChannels = 1)
	{
		List<IChannel> toRemove = this._pool
			.Take(numberOfChannels)
			.ToList();
		this.Remove(toRemove);
	}

	public void Remove(params IChannel[] channels)
	{
		this.Remove(channels.ToList());
	}

	public void Remove(IEnumerable<IChannel> channels)
	{
		foreach (IChannel channel in channels)
		{
			this._pool.Remove(channel);
			this._recoverables.Remove(channel as IRecoverable);
		}
	}
}