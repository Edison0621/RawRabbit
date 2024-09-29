using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace RawRabbit.Channel
{
	public class ConcurrentChannelQueue
	{
		private readonly ConcurrentQueue<TaskCompletionSource<IModel>> _queue;

		public EventHandler _queued;

		public ConcurrentChannelQueue()
		{
			this._queue = new ConcurrentQueue<TaskCompletionSource<IModel>>();
		}

		public TaskCompletionSource<IModel> Enqueue()
		{
			TaskCompletionSource<IModel> modelTsc = new TaskCompletionSource<IModel>();
			bool raiseEvent = this._queue.IsEmpty;
			this._queue.Enqueue(modelTsc);
			if (raiseEvent)
			{
				this._queued?.Invoke(this, EventArgs.Empty);
			}

			return modelTsc;
		}

		public bool TryDequeue(out TaskCompletionSource<IModel> channel)
		{
			return this._queue.TryDequeue(out channel);
		}

		public bool IsEmpty => this._queue.IsEmpty;

		public int Count => this._queue.Count;
	}
}
