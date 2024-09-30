using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Logging;

namespace RawRabbit.Channel
{
	public class AutoScalingChannelPool : DynamicChannelPool
	{
		private readonly IChannelFactory _factory;
		private readonly AutoScalingOptions _options;
		private Timer _timer;
		private readonly ILog _logger = LogProvider.For<AutoScalingChannelPool>();

		public AutoScalingChannelPool(IChannelFactory factory, AutoScalingOptions options)
		{
			this._factory = factory;
			this._options = options;
			ValidateOptions(options);
			this.SetupScaling();
		}

		private static void ValidateOptions(AutoScalingOptions options)
		{
			if (options.MinimunPoolSize <= 0)
			{
				throw new ArgumentException($"Minimum Pool Size needs to be a positive integer. Got: {options.MinimunPoolSize}");
			}
			if (options.MaximumPoolSize <= 0)
			{
				throw new ArgumentException($"Maximum Pool Size needs to be a positive integer. Got: {options.MinimunPoolSize}");
			}
			if (options.MinimunPoolSize > options.MaximumPoolSize)
			{
				throw new ArgumentException($"The Maximum Pool Size ({options.MaximumPoolSize}) must be larger than the Minimum Pool Size ({options.MinimunPoolSize})");
			}
		}

		public override async Task<IChannel> GetAsync(CancellationToken ct = default(CancellationToken))
		{
			int activeChannels = this.GetActiveChannelCount();
			if (activeChannels  < this._options.MinimunPoolSize)
			{
				this._logger.Debug("Pool currently has {channelCount}, which is lower than the minimal pool size {minimalPoolSize}. Creating channels.", activeChannels, this._options.MinimunPoolSize);
				int delta = this._options.MinimunPoolSize - this._pool.Count;
				for (int i = 0; i < delta; i++)
				{
					IChannel channel = await this._factory.CreateChannelAsync(ct);
					this.Add(channel);
				}
			}

			return await base.GetAsync(ct);
		}

		public void SetupScaling()
		{
			if (this._options.RefreshInterval == TimeSpan.MaxValue || this._options.RefreshInterval == TimeSpan.MinValue)
			{
				return;
			}

			this._timer = new Timer(state =>
			{
				int workPerChannel = this._pool.Count == 0 ? int.MaxValue : this._channelRequestQueue.Count / this._pool.Count;
				bool scaleUp = this._pool.Count < this._options.MaximumPoolSize;
				bool scaleDown = this._options.MinimunPoolSize < this._pool.Count;

				this._logger.Debug("Channel pool currently has {channelCount} channels open and a total workload of {totalWorkload}", this._pool.Count, this._channelRequestQueue.Count);
				if (scaleUp && this._options.DesiredAverageWorkload < workPerChannel)
				{
					this._logger.Debug("The estimated workload is {averageWorkload} operations/channel, which is higher than the desired workload ({desiredAverageWorkload}). Creating channel.", workPerChannel, this._options.DesiredAverageWorkload);

					CancellationTokenSource channelCancellation = new CancellationTokenSource(this._options.RefreshInterval);
					this._factory
						.CreateChannelAsync(channelCancellation.Token)
						.ContinueWith(tChannel =>
						{
							if (tChannel.Status == TaskStatus.RanToCompletion)
							{
								this.Add(tChannel.Result);
							}
						}, CancellationToken.None);
					return;
				}

				if (scaleDown && workPerChannel < this._options.DesiredAverageWorkload)
				{
					this._logger.Debug("The estimated workload is {averageWorkload} operations/channel, which is lower than the desired workload ({desiredAverageWorkload}). Creating channel.", workPerChannel, this._options.DesiredAverageWorkload);
					IChannel toRemove = this._pool.FirstOrDefault();
					this._pool.Remove(toRemove);
					Timer disposeTimer = null;
					disposeTimer = new Timer(o =>
					{
						(o as IChannel)?.Dispose();
						// ReSharper disable once AccessToModifiedClosure
						disposeTimer?.Dispose();
					}, toRemove, this._options.GracefulCloseInterval, new TimeSpan(-1));
				}
			}, null, this._options.RefreshInterval, this._options.RefreshInterval);
		}

		public override void Dispose()
		{
			base.Dispose();
			this._timer?.Dispose();
		}
	}

	public class AutoScalingOptions
	{
		public int DesiredAverageWorkload { get; set; }
		public int MinimunPoolSize { get; set; }
		public int MaximumPoolSize { get; set; }
		public TimeSpan RefreshInterval { get; set; }
		public TimeSpan GracefulCloseInterval { get; set; }

		public static AutoScalingOptions Default => new AutoScalingOptions
		{
			MinimunPoolSize = 1,
			MaximumPoolSize = 10,
			DesiredAverageWorkload = 20000,
			RefreshInterval = TimeSpan.FromSeconds(10),
			GracefulCloseInterval = TimeSpan.FromSeconds(30)
		};
	}
}
