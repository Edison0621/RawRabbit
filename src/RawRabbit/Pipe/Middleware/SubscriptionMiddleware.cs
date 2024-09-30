using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Subscription;

namespace RawRabbit.Pipe.Middleware
{
	public class SubscriptionOptions
	{
		public Func<IPipeContext, string> QueueNameFunc { get; set; }
		public Func<IPipeContext, IAsyncBasicConsumer> ConsumeFunc{ get; set; }
		public Action<IPipeContext, ISubscription> SaveInContext { get; set; }
	}

	public class SubscriptionMiddleware : Middleware
	{
		protected readonly ISubscriptionRepository _repo;
		protected readonly Func<IPipeContext, string> _queueNameFunc;
		protected readonly Func<IPipeContext, IAsyncBasicConsumer> _consumerFunc;
		protected readonly Action<IPipeContext, ISubscription> _saveInContext;

		public SubscriptionMiddleware(ISubscriptionRepository repo, SubscriptionOptions options = null)
		{
			this._repo = repo;
			this._queueNameFunc = options?.QueueNameFunc ?? (context => context.GetConsumerConfiguration()?.Consume.QueueName);
			this._consumerFunc = options?.ConsumeFunc ?? (context => context.GetConsumer());
			this._saveInContext = options?.SaveInContext ?? ((ctx, subscription) => ctx.Properties.Add(PipeKey.Subscription, subscription));
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
		{
			IAsyncBasicConsumer consumer = this.GetConsumer(context);
			string queueName = this.GetQueueName(context);
			ISubscription subscription = this.CreateSubscription(consumer, queueName);
			this.SaveSubscriptionInContext(context, subscription);
			this.SaveSubscriptionInRepo(subscription);
			await this.Next.InvokeAsync(context, token);
		}

		protected virtual IAsyncBasicConsumer GetConsumer(IPipeContext context)
		{
			return this._consumerFunc(context);
		}

		protected virtual string GetQueueName(IPipeContext context)
		{
			return this._queueNameFunc(context);
		}

		protected virtual ISubscription CreateSubscription(IAsyncBasicConsumer consumer, string queueName)
		{
			return new Subscription.Subscription(consumer, queueName);
		}

		protected virtual void SaveSubscriptionInContext(IPipeContext context, ISubscription subscription)
		{
			this._saveInContext(context, subscription);
		}

		protected virtual void SaveSubscriptionInRepo(ISubscription subscription)
		{
			this._repo.Add(subscription);
		}
	}
}
