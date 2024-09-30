using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace RawRabbit.Channel;

public class ConcurrentChannelQueue
{
	public EventHandler _queued;
	private readonly ConcurrentQueue<TaskCompletionSource<IChannel>> _queue;

	public ConcurrentChannelQueue()
	{
		this._queue = new ConcurrentQueue<TaskCompletionSource<IChannel>>();
	}

	public int Count => this._queue.Count;

	public bool IsEmpty => this._queue.IsEmpty;

	public TaskCompletionSource<IChannel> Enqueue()
	{
		TaskCompletionSource<IChannel> modelTsc = new();
		bool raiseEvent = this._queue.IsEmpty;
		this._queue.Enqueue(modelTsc);
		if (raiseEvent)
		{
			this._queued?.Invoke(this, EventArgs.Empty);
		}

		return modelTsc;
	}

	public bool TryDequeue(out TaskCompletionSource<IChannel> channel)
	{
		return this._queue.TryDequeue(out channel);
	}
}
