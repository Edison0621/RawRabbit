using System.Collections.Generic;
using System.Linq;
using RabbitMQ.Client;

namespace RawRabbit.Channel
{
	public class DynamicChannelPool : StaticChannelPool
	{
		public DynamicChannelPool()
			: this(Enumerable.Empty<IModel>()) { }

		public DynamicChannelPool(IEnumerable<IModel> seed)
			: base(seed) { }

		public void Add(params IModel[] channels)
		{
			this.Add(channels.ToList());
		}

		public void Add(IEnumerable<IModel> channels)
		{
			foreach (IModel channel in channels)
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
			List<IModel> toRemove = this._pool
				.Take(numberOfChannels)
				.ToList();
			this.Remove(toRemove);
		}

		public void Remove(params IModel[] channels)
		{
			this.Remove(channels.ToList());
		}

		public void Remove(IEnumerable<IModel> channels)
		{
			foreach (IModel channel in channels)
			{
				this._pool.Remove(channel);
				this._recoverables.Remove(channel as IRecoverable);
			}
		}
	}
}
