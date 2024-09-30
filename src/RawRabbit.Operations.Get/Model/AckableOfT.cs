using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace RawRabbit.Operations.Get.Model;

public class Ackable<TType> : IDisposable
{
	public TType Content { get; set; }
	public bool Acknowledged { get; private set; }
	public IEnumerable<ulong> DeliveryTags => this._deliveryTagFunc(this.Content);
	internal readonly IChannel _channel;
	internal readonly Func<TType, ulong[]> _deliveryTagFunc;

	public Ackable(TType content, IChannel channel, params ulong[] deliveryTag) : this(content, channel, _ => deliveryTag)
	{ }

	public Ackable(TType content, IChannel channel, Func<TType, ulong[]> deliveryTagFunc)
	{
		this.Content = content;
		this._channel = channel;
		this._deliveryTagFunc = deliveryTagFunc;
	}

	public async Task Ack()
	{
		foreach (ulong deliveryTag in this._deliveryTagFunc(this.Content))
		{
			await this._channel.BasicAckAsync(deliveryTag, false);
		}

		this.Acknowledged = true;
	}

	public async Task Nack(bool requeue = true)
	{
		foreach (ulong deliveryTag in this._deliveryTagFunc(this.Content))
		{
			await this._channel.BasicNackAsync(deliveryTag, false, requeue);
		}

		this.Acknowledged = true;
	}

	public async Task Reject(bool requeue = true)
	{
		foreach (ulong deliveryTag in this._deliveryTagFunc(this.Content))
		{
			await this._channel.BasicRejectAsync(deliveryTag, requeue);
		}

		this.Acknowledged = true;
	}

	public void Dispose()
	{
		this._channel?.Dispose();
	}
}