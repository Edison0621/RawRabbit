using System;
using RabbitMQ.Client;
using RawRabbit.Consumer;

namespace RawRabbit.Subscription
{
	public interface ISubscription : IDisposable
	{
		string QueueName { get; }
		string ConsumerTag { get; }
		bool Active { get;  }
	}

	public class Subscription : ISubscription
	{
		public string QueueName { get; }
		public string ConsumerTag { get; }
		public bool Active { get; set; }

		private readonly IBasicConsumer _consumer;

		public Subscription(IBasicConsumer consumer, string queueName)
		{
			this.Active = true;
			this._consumer = consumer;
			DefaultBasicConsumer basicConsumer = consumer as DefaultBasicConsumer;
			if (basicConsumer == null)
			{
				return;
			}

			this.QueueName = queueName;
			this.ConsumerTag = basicConsumer.ConsumerTag;
		}

		public void Dispose()
		{
			if (!this._consumer.Model.IsOpen)
			{
				return;
			}
			if (!this.Active)
			{
				return;
			}

			this.Active = false;
			this._consumer.CancelAsync();
		}
	}
}
