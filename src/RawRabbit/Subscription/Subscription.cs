using System;
using RabbitMQ.Client;

namespace RawRabbit.Subscription;

public interface ISubscription : IDisposable
{
	string QueueName { get; }
	string[] ConsumerTag { get; }
	bool Active { get;  }
}

public class Subscription : ISubscription
{
	public string QueueName { get; }
	public string[] ConsumerTag { get; }
	public bool Active { get; set; }

	private readonly IAsyncBasicConsumer _consumer;

	public Subscription(IAsyncBasicConsumer consumer, string queueName)
	{
		this.Active = true;
		this._consumer = consumer;
		AsyncDefaultBasicConsumer basicConsumer = consumer as AsyncDefaultBasicConsumer;
		if (basicConsumer == null)
		{
			return;
		}

		this.QueueName = queueName;
		this.ConsumerTag = basicConsumer.ConsumerTags;
	}

	public void Dispose()
	{
		if (!(this._consumer.Channel is { IsOpen: true }))
		{
			return;
		}
		if (!this.Active)
		{
			return;
		}

		this.Active = false;
		this._consumer.Channel.CloseAsync().Wait();
	}
}