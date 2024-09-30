using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;
using RawRabbit.Logging;

namespace RawRabbit.Common
{
	public interface ITopologyProvider
	{
		Task DeclareExchangeAsync(ExchangeDeclaration exchange);
		Task DeclareQueueAsync(QueueDeclaration queue);
		Task BindQueueAsync(string queue, string exchange, string routingKey, IDictionary<string, object> arguments);
		Task UnbindQueueAsync(string queue, string exchange, string routingKey, IDictionary<string, object> arguments);
		bool IsDeclared(ExchangeDeclaration exchange);
		bool IsDeclared(QueueDeclaration exchange);
	}

	public class TopologyProvider : ITopologyProvider, IDisposable
	{
		private readonly IChannelFactory _channelFactory;
		private IChannel _channel;
		private readonly object _processLock = new object();
		private readonly Task _completed = Task.CompletedTask;
		private readonly List<string> _initExchanges;
		private readonly List<string> _initQueues;
		private readonly List<string> _queueBinds;
		private readonly ConcurrentQueue<ScheduledTopologyTask> _topologyTasks;
		private readonly ILog _logger = LogProvider.For<TopologyProvider>();

		public TopologyProvider(IChannelFactory channelFactory)
		{
			this._channelFactory = channelFactory;
			this._initExchanges = new List<string>();
			this._initQueues = new List<string>();
			this._queueBinds = new List<string>();
			this._topologyTasks = new ConcurrentQueue<ScheduledTopologyTask>();
		}

		public async Task DeclareExchangeAsync(ExchangeDeclaration exchange)
		{
			if (this.IsDeclared(exchange))
			{
				return;
			}

			ScheduledExchangeTask scheduled = new ScheduledExchangeTask(exchange);
			this._topologyTasks.Enqueue(scheduled);
			await this.EnsureWorkerAsync();
		}

		public async Task DeclareQueueAsync(QueueDeclaration queue)
		{
			if (this.IsDeclared(queue))
			{
				return;
			}

			ScheduledQueueTask scheduled = new ScheduledQueueTask(queue);
			this._topologyTasks.Enqueue(scheduled);
			await this.EnsureWorkerAsync();
		}

		public async Task BindQueueAsync(string queue, string exchange, string routingKey, IDictionary<string,object> arguments)
		{
			if (string.Equals(exchange, string.Empty))
			{
				/*
					"The default exchange is implicitly bound to every queue,
					with a routing key equal to the queue name. It it not possible
					to explicitly bind to, or unbind from the default exchange."
				*/
				return;
			}

			string bindKey = CreateBindKey(queue, exchange, routingKey, arguments);
			if (this._queueBinds.Contains(bindKey))
			{
				return;
			}
			ScheduledBindQueueTask scheduled = new ScheduledBindQueueTask
			{
				Queue = queue,
				Exchange = exchange,
				RoutingKey = routingKey,
				Arguments = arguments
			};
			this._topologyTasks.Enqueue(scheduled);
			await this.EnsureWorkerAsync();
		}

		public async Task UnbindQueueAsync(string queue, string exchange, string routingKey, IDictionary<string, object> arguments)
		{
			ScheduledUnbindQueueTask scheduled = new ScheduledUnbindQueueTask
			{
				Queue = queue,
				Exchange = exchange,
				RoutingKey = routingKey,
				Arguments = arguments
			};

			this._topologyTasks.Enqueue(scheduled);
			await this.EnsureWorkerAsync();
		}

		public bool IsDeclared(ExchangeDeclaration exchange)
		{
			return exchange.IsDefaultExchange() || this._initExchanges.Contains(exchange.Name);
		}

		public bool IsDeclared(QueueDeclaration queue)
		{
			return queue.IsDirectReplyTo() || this._initQueues.Contains(queue.Name);
		}

		private async Task BindQueueToExchange(ScheduledBindQueueTask bind)
		{
			string bindKey = CreateBindKey(bind);
			if (this._queueBinds.Contains(bindKey))
			{
				return;
			}

			this._logger.Info("Binding queue {queueName} to exchange {exchangeName} with routing key {routingKey}", bind.Queue, bind.Exchange, bind.RoutingKey);

			IChannel channel = await this.GetOrCreateChannel();
			await channel.QueueBindAsync(
				queue: bind.Queue,
				exchange: bind.Exchange,
				routingKey: bind.RoutingKey,
				arguments: bind.Arguments
			);
			this._queueBinds.Add(bindKey);
		}

		private async Task UnbindQueueFromExchange(ScheduledUnbindQueueTask bind)
		{
			this._logger.Info("Unbinding queue {queueName} from exchange {exchangeName} with routing key {routingKey}", bind.Queue, bind.Exchange, bind.RoutingKey);

			IChannel channel = await this.GetOrCreateChannel();
			await channel.QueueUnbindAsync(
				queue: bind.Queue,
				exchange: bind.Exchange,
				routingKey: bind.RoutingKey,
				arguments: bind.Arguments
			);
			string bindKey = CreateBindKey(bind);
			if (this._queueBinds.Contains(bindKey))
			{
				this._queueBinds.Remove(bindKey);
			}
		}
		
		private static string CreateBindKey(ScheduledBindQueueTask bind)
		{
			return CreateBindKey(bind.Queue, bind.Exchange, bind.RoutingKey, bind.Arguments);
		}

		private static string CreateBindKey(ScheduledUnbindQueueTask bind)
		{
			return CreateBindKey(bind.Queue, bind.Exchange, bind.RoutingKey, bind.Arguments);
		}

		private static string CreateBindKey(string queue, string exchange, string routingKey, IDictionary<string, object> arguments)
		{
			string bindKey = $"{queue}_{exchange}_{routingKey}";
			if (arguments != null && arguments.Count > 0)
			{
				// order the arguments, for the key to be identical no matter the ordering
				IOrderedEnumerable<KeyValuePair<string, object>> orderedArguments = arguments.OrderBy(pair => pair.Key);
				bindKey = $"{bindKey}_{string.Join("_", orderedArguments.Select(pair => $"{pair.Key}:{pair.Value}"))}";
			}
			return bindKey;
		}

		private async Task DeclareQueue(QueueDeclaration queue)
		{
			if (this.IsDeclared(queue))
			{
				return;
			}

			this._logger.Info("Declaring queue {queueName}.", queue.Name);

			IChannel channel = await this.GetOrCreateChannel();
			await channel.QueueDeclareAsync(
				queue.Name,
				queue.Durable,
				queue.Exclusive,
				queue.AutoDelete,
				queue.Arguments);

			if (queue.AutoDelete)
			{
				this._initQueues.Add(queue.Name);
			}
		}

		private async Task DeclareExchange(ExchangeDeclaration exchange)
		{
			if (this.IsDeclared(exchange))
			{
				return;
			}

			this._logger.Info("Declaring exchange {exchangeName}.", exchange.Name);
			IChannel channel = await this.GetOrCreateChannel();
			await channel.ExchangeDeclareAsync(
				exchange.Name,
				exchange.ExchangeType,
				exchange.Durable,
				exchange.AutoDelete,
				exchange.Arguments);
			if (!exchange.AutoDelete)
			{
				this._initExchanges.Add(exchange.Name);
			}
		}

		private async Task EnsureWorkerAsync()
		{
			if (!Monitor.TryEnter(this._processLock))
			{
				return;
			}

			ScheduledTopologyTask topologyTask;
			while (this._topologyTasks.TryDequeue(out topologyTask))
			{
				switch (topologyTask)
				{
					case ScheduledExchangeTask exchange:
						try
						{
							await this.DeclareExchange(exchange.Declaration);
							exchange.TaskCompletionSource.TrySetResult(true);
						}
						catch (Exception e)
						{
							this._logger.Error(e, "Unable to declare exchange {exchangeName}", exchange.Declaration.Name);
							exchange.TaskCompletionSource.TrySetException(e);
						}

						continue;
					case ScheduledQueueTask queue:
						try
						{
							await this.DeclareQueue(queue.Configuration);
							queue.TaskCompletionSource.TrySetResult(true);
						}
						catch (Exception e)
						{
							this._logger.Error(e, "Unable to declare queue");
							queue.TaskCompletionSource.TrySetException(e);
						}

						continue;
					case ScheduledBindQueueTask bind:
						try
						{
							await this.BindQueueToExchange(bind);
							bind.TaskCompletionSource.TrySetResult(true);
						}
						catch (Exception e)
						{
							this._logger.Error(e, "Unable to bind queue");
							bind.TaskCompletionSource.TrySetException(e);
						}
						continue;
					case ScheduledUnbindQueueTask unbind:
						try
						{
							await this.UnbindQueueFromExchange(unbind);
							unbind.TaskCompletionSource.TrySetResult(true);
						}
						catch (Exception e)
						{
							this._logger.Error(e, "Unable to unbind queue");
							unbind.TaskCompletionSource.TrySetException(e);
						}

						break;
				}
			}

			this._logger.Debug("Done processing topology work.");
			Monitor.Exit(this._processLock);
		}

		private async Task<IChannel> GetOrCreateChannel()
		{
			if (this._channel?.IsOpen ?? false)
			{
				return this._channel;
			}

			this._channel = await this._channelFactory.CreateChannelAsync();
			return this._channel;
		}

		public void Dispose()
		{
			this._channelFactory?.Dispose();
		}

		#region Classes for Scheduled Tasks
		private abstract class ScheduledTopologyTask
		{
			protected ScheduledTopologyTask()
			{
				this.TaskCompletionSource = new TaskCompletionSource<bool>();
			}
			public TaskCompletionSource<bool> TaskCompletionSource { get; }
		}

		private sealed class ScheduledQueueTask : ScheduledTopologyTask
		{
			public ScheduledQueueTask(QueueDeclaration queue)
			{
				this.Configuration = queue;
			}
			public QueueDeclaration Configuration { get; }
		}

		private sealed class ScheduledExchangeTask : ScheduledTopologyTask
		{
			public ScheduledExchangeTask(ExchangeDeclaration exchange)
			{
				this.Declaration = exchange;
			}
			public ExchangeDeclaration Declaration { get; }
		}

		private sealed class ScheduledBindQueueTask : ScheduledTopologyTask
		{
			public string Exchange { get; set; }
			public string Queue { get; set; }
			public string RoutingKey { get; set; }
			public IDictionary<string,object> Arguments { get; set; }
		}

		private sealed class ScheduledUnbindQueueTask : ScheduledTopologyTask
		{
			public string Exchange { get; set; }
			public string Queue { get; set; }
			public string RoutingKey { get; set; }
			public IDictionary<string, object> Arguments { get; set; }
		}
		#endregion
	}
}
