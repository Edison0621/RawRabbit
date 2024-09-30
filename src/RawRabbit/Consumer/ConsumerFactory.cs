using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Configuration.Consume;
using RawRabbit.Logging;

namespace RawRabbit.Consumer;

public class ConsumerFactory : IConsumerFactory
{
	private readonly IChannelFactory _channelFactory;
	private readonly ConcurrentDictionary<string, Lazy<Task<IAsyncBasicConsumer>>> _consumerCache;
	private readonly ILog _logger = LogProvider.For<ConsumerFactory>();

	public ConsumerFactory(IChannelFactory channelFactory)
	{
		this._consumerCache = new ConcurrentDictionary<string, Lazy<Task<IAsyncBasicConsumer>>>();
		this._channelFactory = channelFactory;
	}

	public Task<IAsyncBasicConsumer> GetConsumerAsync(ConsumeConfiguration cfg, IChannel channel = null, CancellationToken token = default)
	{
		string consumerKey = this.CreateConsumerKey(cfg);
		Lazy<Task<IAsyncBasicConsumer>> lazyConsumerTask = this._consumerCache.GetOrAdd(consumerKey, routingKey =>
		{
			return new Lazy<Task<IAsyncBasicConsumer>>(async () =>
			{
				IAsyncBasicConsumer consumer = await this.CreateConsumerAsync(channel, token);
				return consumer;
			});
		});
		return lazyConsumerTask.Value;
	}

	public Task<IAsyncBasicConsumer> GetConfiguredConsumerAsync(ConsumeConfiguration cfg, IChannel channel = null, CancellationToken token = default)
	{
		string consumerKey = this.CreateConsumerKey(cfg);
		Lazy<Task<IAsyncBasicConsumer>> lazyConsumerTask = this._consumerCache.GetOrAdd(consumerKey, routingKey =>
		{
			return new Lazy<Task<IAsyncBasicConsumer>>(async () =>
			{
				IAsyncBasicConsumer consumer = await this.CreateConsumerAsync(channel, token);
				await this.ConfigureConsumeAsync(consumer, cfg);
				return consumer;
			});
		});
		if (lazyConsumerTask.Value.IsCompleted && lazyConsumerTask.Value.Result.Channel is { IsClosed: true })
		{
			this._consumerCache.TryRemove(consumerKey, out _);
			return this.GetConsumerAsync(cfg, channel, token);
		}
		return lazyConsumerTask.Value;
	}

	public async Task<IAsyncBasicConsumer> CreateConsumerAsync(IChannel channel = null, CancellationToken token = default)
	{
		if (channel == null)
		{
			channel = await this.GetOrCreateChannelAsync(token);
		}
		return new AsyncEventingBasicConsumer(channel);
	}

	public async Task<IAsyncBasicConsumer> ConfigureConsumeAsync(IAsyncBasicConsumer consumer, ConsumeConfiguration cfg)
	{
		this.CheckPropertyValues(cfg);

		if (cfg.PrefetchCount > 0)
		{
			this._logger.Info("Setting Prefetch Count to {prefetchCount}.", cfg.PrefetchCount);
			if (consumer.Channel != null)
				await consumer.Channel.BasicQosAsync(
					prefetchSize: 0,
					prefetchCount: cfg.PrefetchCount,
					global: false
				);
		}

		this._logger.Info("Preparing to consume message from queue '{queueName}'.", cfg.QueueName);

		if (consumer.Channel != null)
			await consumer.Channel.BasicConsumeAsync(
				queue: cfg.QueueName,
				autoAck: cfg.AutoAck,
				consumerTag: cfg.ConsumerTag,
				noLocal: cfg.NoLocal,
				exclusive: cfg.Exclusive,
				arguments: cfg.Arguments,
				consumer: consumer);

		return consumer;
	}

	protected virtual void CheckPropertyValues(ConsumeConfiguration cfg)
	{
		if (cfg == null)
		{
			throw new ArgumentException("Unable to create consumer. The provided configuration is null");
		}
		if (string.IsNullOrEmpty(cfg.QueueName))
		{
			throw new ArgumentException("Unable to create consume. No queue name provided.");
		}
		if (string.IsNullOrEmpty(cfg.ConsumerTag))
		{
			throw new ArgumentException("Unable to create consume. Consumer tag cannot be undefined.");
		}
	}

	protected virtual Task<IChannel> GetOrCreateChannelAsync(CancellationToken token = default)
	{
		this._logger.Info("Creating a dedicated channel for consumer.");
		return this._channelFactory.CreateChannelAsync(token);
	}

	protected string CreateConsumerKey(ConsumeConfiguration cfg)
	{
		return $"{cfg.QueueName}:{cfg.RoutingKey}:{cfg.AutoAck}";
	}
}

public static class ConsumerExtensions
{
	public static async Task<string> CancelAsync(this AsyncEventingBasicConsumer consumer, CancellationToken token = default)
	{
		AsyncEventingBasicConsumer eventConsumer = consumer as AsyncEventingBasicConsumer;
		if (eventConsumer == null)
		{
			throw new NotSupportedException("Can only cancellation EventBasicConsumer");
		}
		TaskCompletionSource<string> cancelTcs = new();
		token.Register(() => cancelTcs.TrySetCanceled());
		string tag = eventConsumer.ConsumerTags.First();
		consumer.Unregistered += (sender, args) =>
		{
			if (args.ConsumerTags.First() != tag)
			{
				return Task.CompletedTask;
			}

			cancelTcs.TrySetResult(args.ConsumerTags.First());

			return Task.CompletedTask;
		};

		await consumer.Channel.BasicCancelAsync(eventConsumer.ConsumerTags.First(), cancellationToken: token);
		return await cancelTcs.Task;
	}

	public static void OnMessage(this IAsyncBasicConsumer consumer, AsyncEventHandler<BasicDeliverEventArgs> onMessage, Predicate<BasicDeliverEventArgs> abort = null)
	{
		AsyncEventingBasicConsumer eventConsumer = consumer as AsyncEventingBasicConsumer;
		if (eventConsumer == null)
		{
			throw new NotSupportedException("Only supported for EventBasicConsumer");
		}
		eventConsumer.Received += onMessage;

		if (abort == null)
		{
			return;
		}

		AsyncEventHandler<BasicDeliverEventArgs> abortHandler = null;
		abortHandler = (sender, args) =>
		{
			if (abort(args))
			{
				eventConsumer.Received -= onMessage;
				eventConsumer.Received -= abortHandler;
			}

			return Task.CompletedTask;
		};

		eventConsumer.Received += abortHandler;
	}
}