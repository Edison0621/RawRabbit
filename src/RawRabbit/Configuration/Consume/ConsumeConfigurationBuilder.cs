using System.Collections.Generic;
using RawRabbit.Common;

namespace RawRabbit.Configuration.Consume;

public class ConsumeConfigurationBuilder : IConsumeConfigurationBuilder
{
	public ConsumeConfiguration Config { get; }
	public bool ExistingExchange { get; set; }
	public bool ExistingQueue { get; set; }

	public ConsumeConfigurationBuilder(ConsumeConfiguration initial)
	{
		this.Config = initial;
	}

	public IConsumeConfigurationBuilder OnExchange(string exchange)
	{
		Truncator.Truncate(ref exchange);
		this.Config.ExchangeName = exchange;
		this.ExistingExchange = true;
		return this;
	}

	public IConsumeConfigurationBuilder FromQueue(string queue)
	{
		Truncator.Truncate(ref queue);
		this.Config.QueueName = queue;
		this.ExistingQueue = true;
		return this;
	}

	public IConsumeConfigurationBuilder WithNoAck(bool noAck = true)
	{
		return this.WithAutoAck(noAck);
	}

	public IConsumeConfigurationBuilder WithAutoAck(bool autoAck = true)
	{
		this.Config.AutoAck = autoAck;
		return this;
	}

	public IConsumeConfigurationBuilder WithConsumerTag(string tag)
	{
		this.Config.ConsumerTag = tag;
		return this;
	}

	public IConsumeConfigurationBuilder WithRoutingKey(string routingKey)
	{
		this.Config.RoutingKey = routingKey;
		return this;
	}

	public IConsumeConfigurationBuilder WithNoLocal(bool noLocal = true)
	{
		this.Config.NoLocal = noLocal;
		return this;
	}

	public IConsumeConfigurationBuilder WithPrefetchCount(ushort prefetch)
	{
		this.Config.PrefetchCount = prefetch;
		return this;
	}

	public IConsumeConfigurationBuilder WithExclusive(bool exclusive = true)
	{
		this.Config.Exclusive = exclusive;
		return this;
	}

	public IConsumeConfigurationBuilder WithArgument(string key, object value)
	{
		this.Config.Arguments = this.Config.Arguments ?? new Dictionary<string, object>();
		this.Config.Arguments.TryAdd(key, value);
		return this;
	}
}