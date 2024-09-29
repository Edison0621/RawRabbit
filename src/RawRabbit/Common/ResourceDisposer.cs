using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Channel;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Configuration;
using RawRabbit.Subscription;

namespace RawRabbit.Common
{
	public interface IResourceDisposer :IDisposable
	{
		Task ShutdownAsync(TimeSpan? graceful = null);
	}

	public class ResourceDisposer : IResourceDisposer
	{
		private readonly IChannelFactory _channelFactory;
		private readonly IConnectionFactory _connectionFactory;
		private readonly ISubscriptionRepository _subscriptionRepo;
		private readonly IChannelPoolFactory _channelPoolFactory;
		private readonly RawRabbitConfiguration _config;

		public ResourceDisposer(
			IChannelFactory channelFactory,
			IConnectionFactory connectionFactory,
			ISubscriptionRepository subscriptionRepo,
			IChannelPoolFactory channelPoolFactory,
			RawRabbitConfiguration config)
		{
			this._channelFactory = channelFactory;
			this._connectionFactory = connectionFactory;
			this._subscriptionRepo = subscriptionRepo;
			this._channelPoolFactory = channelPoolFactory;
			this._config = config;
		}

		public void Dispose()
		{
			this._channelFactory.Dispose();
			(this._connectionFactory as IDisposable)?.Dispose();
			(this._channelPoolFactory as IDisposable)?.Dispose();
		}

		public async Task ShutdownAsync(TimeSpan? graceful = null)
		{
			List<ISubscription> subscriptions = this._subscriptionRepo.GetAll();
			foreach (ISubscription subscription in subscriptions)
			{
				subscription?.Dispose();
			}
			graceful = graceful ?? this._config.GracefulShutdown;
			await Task.Delay(graceful.Value);
			this.Dispose();
		}
	}
}
