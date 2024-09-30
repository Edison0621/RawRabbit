using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Channel.Abstraction;

namespace RawRabbit.Channel
{
	public class ResilientChannelPool : DynamicChannelPool
	{
		protected readonly IChannelFactory _channelFactory;
		private readonly int _desiredChannelCount;

		public ResilientChannelPool(IChannelFactory factory, int channelCount)
			: this(factory, CreateSeed(factory, channelCount)) { }

		public ResilientChannelPool(IChannelFactory factory)
			: this(factory, Enumerable.Empty<IChannel>()) { }

		public ResilientChannelPool(IChannelFactory factory, IEnumerable<IChannel> seed) : base(seed)
		{
			this._channelFactory = factory;
			this._desiredChannelCount = seed.Count();
		}

		private static IEnumerable<IChannel> CreateSeed(IChannelFactory factory, int channelCount)
		{
			for (int i = 0; i < channelCount; i++)
			{
				yield return factory.CreateChannelAsync().GetAwaiter().GetResult();
			}
		}

		public override async Task<IChannel> GetAsync(CancellationToken ct = default(CancellationToken))
		{
			int currentCount = this.GetActiveChannelCount();
			if (currentCount < this._desiredChannelCount)
			{
				int createCount = this._desiredChannelCount - currentCount;
				for (int i = 0; i < createCount; i++)
				{
					IChannel channel = await this._channelFactory.CreateChannelAsync(ct);
					this.Add(channel);
				}
			}
			return await base.GetAsync(ct);
		}
	}
}
