using System;
using System.Collections.Generic;
using RabbitMQ.Client;

namespace RawRabbit.Operations.Get.Model
{
	public class Ackable<TType> : IDisposable
	{
		public TType Content { get; set; }
		public bool Acknowledged { get; private set; }
		public IEnumerable<ulong> DeliveryTags => this._deliveryTagFunc(this.Content);
		internal readonly IModel _channel;
		internal readonly Func<TType, ulong[]> _deliveryTagFunc;

		public Ackable(TType content, IModel channel, params ulong[] deliveryTag) : this(content, channel, type => deliveryTag)
		{ }

		public Ackable(TType content, IModel channel, Func<TType, ulong[]> deliveryTagFunc)
		{
			this.Content = content;
			this._channel = channel;
			this._deliveryTagFunc = deliveryTagFunc;
		}

		public void Ack()
		{
			foreach (ulong deliveryTag in this._deliveryTagFunc(this.Content))
			{
				this._channel.BasicAck(deliveryTag, false);
			}

			this.Acknowledged = true;
		}

		public void Nack(bool requeue = true)
		{
			foreach (ulong deliveryTag in this._deliveryTagFunc(this.Content))
			{
				this._channel.BasicNack(deliveryTag, false, requeue);
			}

			this.Acknowledged = true;
		}

		public void Reject(bool requeue = true)
		{
			foreach (ulong deliveryTag in this._deliveryTagFunc(this.Content))
			{
				this._channel.BasicReject(deliveryTag, requeue);
			}

			this.Acknowledged = true;
		}

		public void Dispose()
		{
			this._channel?.Dispose();
		}
	}
}
