using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.DependencyInjection;
using RawRabbit.Pipe;
using RawRabbit.Subscription;

namespace RawRabbit.Instantiation
{
	public interface IInstanceFactory
	{
		IBusClient Create();
	}

	public class InstanceFactory : IDisposable, IInstanceFactory
	{
		private readonly IDependencyResolver _resolver;

		public InstanceFactory(IDependencyResolver resolver)
		{
			this._resolver = resolver;
		}

		public IBusClient Create()
		{
			return new BusClient(this._resolver.GetService<IPipeBuilderFactory>(), this._resolver.GetService<IPipeContextFactory>(), this._resolver.GetService<IChannelFactory>());
		}

		public void Dispose()
		{
			IResourceDisposer diposer = this._resolver.GetService<IResourceDisposer>();
			diposer?.Dispose();
		}

		public async Task ShutdownAsync(TimeSpan? graceful = null)
		{
			List<ISubscription> subscriptions = this._resolver.GetService<ISubscriptionRepository>().GetAll();
			foreach (ISubscription subscription in subscriptions)
			{
				subscription?.Dispose();
			}
			graceful = graceful ?? this._resolver.GetService<RawRabbitConfiguration>().GracefulShutdown;
			await Task.Delay(graceful.Value);
			this.Dispose();
		}
	}
}
