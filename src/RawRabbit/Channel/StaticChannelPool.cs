using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Exceptions;
using RawRabbit.Logging;

namespace RawRabbit.Channel
{
	public interface IChannelPool
	{
		Task<IChannel> GetAsync(CancellationToken ct = default(CancellationToken));
	}

	public class StaticChannelPool : IDisposable, IChannelPool
	{
		protected readonly LinkedList<IChannel> _pool;
		protected readonly List<IRecoverable> _recoverables;
		protected readonly ConcurrentChannelQueue _channelRequestQueue;
		private readonly object _workLock = new object();
		private LinkedListNode<IChannel> _current;
		private readonly ILog _logger = LogProvider.For<StaticChannelPool>();

		public StaticChannelPool(IEnumerable<IChannel> seed)
		{
			seed = seed.ToList();
			this._pool = new LinkedList<IChannel>(seed);
			this._recoverables = new List<IRecoverable>();
			this._channelRequestQueue = new ConcurrentChannelQueue();
			this._channelRequestQueue._queued += (sender, args) => this.StartServeChannels();
			foreach (IChannel channel in seed)
			{
				this.ConfigureRecovery(channel);
			}
		}

		private void StartServeChannels()
		{
			if (this._channelRequestQueue.IsEmpty || this._pool.Count == 0)
			{
				this._logger.Debug("Unable to serve channels. The pool consists of {channelCount} channels and {channelRequests} requests for channels.");
				return;
			}

			if (!Monitor.TryEnter(this._workLock))
			{
				this._logger.Debug("Unable to aquire work lock for service channels.");
				return;
			}

			try
			{
				this._logger.Debug("Starting serving channels.");
				do
				{
					this._current = this._current?.Next ?? this._pool.First;
					if (this._current == null)
					{
						this._logger.Debug("Unable to server channels. Pool empty.");
						return;
					}
					if (this._current.Value.IsClosed)
					{
						this._pool.Remove(this._current);
						if (this._pool.Count != 0)
						{
							continue;
						}
						if (this._recoverables.Count == 0)
						{
							throw new ChannelAvailabilityException("No open channels in pool and no recoverable channels");
						}

						this._logger.Info("No open channels in pool, but {recoveryCount} waiting for recovery", this._recoverables.Count);
						return;
					}
					if (this._channelRequestQueue.TryDequeue(out TaskCompletionSource<IChannel> cTsc))
					{
						cTsc.TrySetResult(this._current.Value);
					}
				} while (!this._channelRequestQueue.IsEmpty);
			}
			catch (Exception e)
			{
				this._logger.Info(e, "An unhandled exception occured when serving channels.");
			}
			finally
			{
				Monitor.Exit(this._workLock);
			}
		}

		protected virtual int GetActiveChannelCount()
		{
			return this._pool
				.Concat<object>(this._recoverables)
				.Distinct()
				.Count();
		}

		protected void ConfigureRecovery(IChannel channel)
		{
			if (!(channel is IRecoverable recoverable))
			{
				this._logger.Debug("Channel {channelNumber} is not recoverable. Recovery disabled for this channel.", channel.ChannelNumber);
				return;
			}
			if (channel.IsClosed && channel.CloseReason is { Initiator: ShutdownInitiator.Application })
			{
				this._logger.Debug("{Channel {channelNumber} is closed by the application. Channel will remain closed and not be part of the channel pool", channel.ChannelNumber);
				return;
			}

			this._recoverables.Add(recoverable);
			recoverable.Recovery += (sender, args) =>
			{
				this._logger.Info("Channel {channelNumber} has been recovered and will be re-added to the channel pool", channel.ChannelNumber);
				if (this._pool.Contains(channel))
				{
					return;
				}

				this._pool.AddLast(channel);
				this.StartServeChannels();
			};
			channel.ChannelShutdown += (sender, args) =>
			{
				if (args.Initiator == ShutdownInitiator.Application)
				{
					this._logger.Info("Channel {channelNumber} is being closed by the application. No recovery will be performed.", channel.ChannelNumber);
					this._recoverables.Remove(recoverable);
				}
			};
		}

		public virtual Task<IChannel> GetAsync(CancellationToken ct = default(CancellationToken))
		{
			TaskCompletionSource<IChannel> channelTcs = this._channelRequestQueue.Enqueue();
			ct.Register(() => channelTcs.TrySetCanceled());
			return channelTcs.Task;
		}

		public virtual void Dispose()
		{
			foreach (IChannel channel in this._pool)
			{
				channel?.Dispose();
			}
			foreach (IRecoverable recoverable in this._recoverables)
			{
				(recoverable as IChannel)?.Dispose();
			}
		}
	}
}
