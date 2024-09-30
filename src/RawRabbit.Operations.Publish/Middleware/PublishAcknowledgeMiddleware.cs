using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Exceptions;
using RawRabbit.Logging;
using RawRabbit.Operations.Publish;
using RawRabbit.Operations.Publish.Context;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Publish.Middleware
{
	public class PublishAcknowledgeOptions
	{
		public Func<IPipeContext, TimeSpan> TimeOutFunc { get; set; }
		public Func<IPipeContext, IChannel> ChannelFunc { get; set; }
		public Func<IPipeContext, bool> EnabledFunc { get; set; }
	}

	public class PublishAcknowledgeMiddleware : Pipe.Middleware.Middleware
	{
		private readonly IExclusiveLock _exclusive;
		private readonly ILog _logger = LogProvider.For<PublishAcknowledgeMiddleware>();
		protected readonly Func<IPipeContext, TimeSpan> _timeOutFunc;
		protected readonly Func<IPipeContext, IChannel> _channelFunc;
		protected readonly Func<IPipeContext, bool> _enabledFunc;

		protected static readonly Dictionary<IChannel, ConcurrentDictionary<ulong, TaskCompletionSource<ulong>>> ConfirmsDictionary = new();
		protected static readonly ConcurrentDictionary<IChannel, object> ChannelLocks = new();
		protected static Dictionary<IChannel, ulong> ChannelSequences = new();

		public PublishAcknowledgeMiddleware(IExclusiveLock exclusive, PublishAcknowledgeOptions options = null)
		{
			this._exclusive = exclusive;
			this._timeOutFunc = options?.TimeOutFunc ?? (context => context.GetPublishAcknowledgeTimeout());
			this._channelFunc = options?.ChannelFunc ?? (context => context.GetTransientChannel());
			this._enabledFunc = options?.EnabledFunc ?? (context => context.GetPublishAcknowledgeTimeout() != TimeSpan.MaxValue);
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
		{
			bool enabled = this.GetEnabled(context);
			if (!enabled)
			{
				this._logger.Debug("Publish Acknowledgement is disabled.");
				await this.Next.InvokeAsync(context, token);
				return;
			}
			IChannel channel = this.GetChannel(context);

			if (!this.PublishAcknowledgeEnabled(channel))
			{
				this.EnableAcknowledgement(channel, token);
			}

			object channelLock = ChannelLocks.GetOrAdd(channel, c => new object());
			TaskCompletionSource<ulong> ackTcs = new();

			await this._exclusive.ExecuteAsync(channelLock, o =>
			{
				ulong sequence = channel.NextPublishSeqNo;
				this.SetupTimeout(context, sequence, ackTcs);
				if (!this.GetChannelDictionary(channel).TryAdd(sequence, ackTcs))
				{
					this._logger.Info("Unable to add ack '{publishSequence}' on channel {channelNumber}", sequence, channel.ChannelNumber);
				}

				this._logger.Info("Sequence {sequence} added to dictionary", sequence);

				return this.Next.InvokeAsync(context, token);
			}, token);
			await ackTcs.Task;
		}

		protected virtual TimeSpan GetAcknowledgeTimeOut(IPipeContext context)
		{
			return this._timeOutFunc(context);
		}

		protected virtual bool PublishAcknowledgeEnabled(IChannel channel)
		{
			return channel.NextPublishSeqNo != 0UL;
		}

		protected virtual IChannel GetChannel(IPipeContext context)
		{
			return this._channelFunc(context);
		}

		protected virtual bool GetEnabled(IPipeContext context)
		{
			return this._enabledFunc(context);
		}

		protected virtual ConcurrentDictionary<ulong, TaskCompletionSource<ulong>> GetChannelDictionary(IChannel channel)
		{
			if (!ConfirmsDictionary.ContainsKey(channel))
			{
				ConfirmsDictionary.Add(channel, new ConcurrentDictionary<ulong, TaskCompletionSource<ulong>>());
			}
			return ConfirmsDictionary[channel];
		}

		protected virtual void EnableAcknowledgement(IChannel channel, CancellationToken token)
		{
			this._logger.Info("Setting 'Publish Acknowledge' for channel '{channelNumber}'", channel.ChannelNumber);
			this._exclusive.Execute(channel, async c =>
			{
				if (this.PublishAcknowledgeEnabled(c))
				{
					return;
				}
				await c.ConfirmSelectAsync(cancellationToken: token);
				ConcurrentDictionary<ulong, TaskCompletionSource<ulong>> dictionary = this.GetChannelDictionary(c);
				c.BasicAcks += (sender, args) =>
				{
					Task.Run(() =>
					{
						if (args.Multiple)
						{
							foreach (ulong deliveryTag in dictionary.Keys.Where(k => k <= args.DeliveryTag).ToList())
							{
								if (!dictionary.TryRemove(deliveryTag, out TaskCompletionSource<ulong> tcs))
								{
									continue;
								}
								if (!tcs.TrySetResult(deliveryTag))
								{
									// ReSharper disable once RedundantJumpStatement
									continue;
								}
							}
						}
						else
						{
							this._logger.Info("Received ack for {deliveryTag}", args.DeliveryTag);
							if (!dictionary.TryRemove(args.DeliveryTag, out TaskCompletionSource<ulong> tcs))
							{
								this._logger.Warn("Unable to find ack tcs for {deliveryTag}", args.DeliveryTag);
							}
							tcs?.TrySetResult(args.DeliveryTag);
						}
					}, token);
				};
			}, token);
		}

		protected virtual void SetupTimeout(IPipeContext context, ulong sequence, TaskCompletionSource<ulong> ackTcs)
		{
			TimeSpan timeout = this.GetAcknowledgeTimeOut(context);
			Timer ackTimer = null;
			this._logger.Info("Setting up publish acknowledgement for {publishSequence} with timeout {timeout:g}", sequence, timeout);
			ackTimer = new Timer(state =>
			{
				ackTcs.TrySetException(new PublishConfirmException($"The broker did not send a publish acknowledgement for message {sequence} within {timeout:g}."));
				// ReSharper disable once AccessToModifiedClosure
				ackTimer?.Dispose();
			}, null, timeout, new TimeSpan(-1));
		}
	}

	public static class PublishAcknowledgePipeGetExtensions
	{
		public static TimeSpan GetPublishAcknowledgeTimeout(this IPipeContext context)
		{
			TimeSpan fallback = context.GetClientConfiguration().PublishConfirmTimeout;
			return context.Get(PublishKey.PublishAcknowledgeTimeout, fallback);
		}
	}
}

namespace RawRabbit
{
	public static class PublishAcknowledgePipeUseExtensions
	{
		public static IPublishContext UsePublishAcknowledge(this IPublishContext context, TimeSpan timeout)
		{
			CollectionExtensions.TryAdd(context.Properties, PublishKey.PublishAcknowledgeTimeout, timeout);
			return context;
		}

		public static IPublishContext UsePublishAcknowledge(this IPublishContext context, bool use = true)
		{
			return !use
				? context.UsePublishAcknowledge(TimeSpan.MaxValue)
				: context;
		}
	}
}

