using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.Attributes.Middleware;

public class ConsumeAttributeOptions
{
	public Func<IPipeContext, ConsumerConfiguration> PublishConfigFunc { get; set; }
	public Func<IPipeContext, Type> MessageTypeFunc { get; set; }
}

public class ConsumeAttributeMiddleware : StagedMiddleware
{
	public override string StageMarker => Pipe.StageMarker.ConsumeConfigured;
	protected readonly Func<IPipeContext, ConsumerConfiguration> _consumeConfigFunc;
	protected readonly Func<IPipeContext, Type> _messageType;

	public ConsumeAttributeMiddleware(ConsumeAttributeOptions options = null)
	{
		this._consumeConfigFunc = options?.PublishConfigFunc ?? (context => context.GetConsumerConfiguration());
		this._messageType = options?.MessageTypeFunc ?? (context => context.GetMessageType());
	}

	public override Task InvokeAsync(IPipeContext context, CancellationToken token = new())
	{
		ConsumerConfiguration consumeConfig = this.GetConsumerConfig(context);
		Type messageType = this.GetMessageType(context);
		this.UpdateExchangeConfig(consumeConfig, messageType);
		this.UpdateRoutingConfig(consumeConfig, messageType);
		this.UpdateQueueConfig(consumeConfig, messageType);
		return this.Next.InvokeAsync(context, token);
	}

	protected virtual ConsumerConfiguration GetConsumerConfig(IPipeContext context)
	{
		return this._consumeConfigFunc?.Invoke(context);
	}

	protected virtual Type GetMessageType(IPipeContext context)
	{
		return this._messageType?.Invoke(context);
	}

	protected virtual void UpdateExchangeConfig(ConsumerConfiguration config, Type messageType)
	{
		ExchangeAttribute attribute = this.GetAttribute<ExchangeAttribute>(messageType);
		if (attribute == null)
		{
			return;
		}

		if (!string.IsNullOrWhiteSpace(attribute.Name))
		{
			config.Consume.ExchangeName = attribute.Name;
			if (config.Exchange != null)
			{
				config.Exchange.Name = attribute.Name;
			}
		}
		if (config.Exchange == null)
		{
			return;
		}
		if (attribute._nullableDurability.HasValue)
		{
			config.Exchange.Durable = attribute._nullableDurability.Value;
		}
		if (attribute._nullableAutoDelete.HasValue)
		{
			config.Exchange.AutoDelete = attribute._nullableAutoDelete.Value;
		}
		if (attribute.Type != ExchangeType.Unknown)
		{
			config.Exchange.ExchangeType = attribute.Type.ToString().ToLowerInvariant();
		}
	}

	protected virtual void UpdateRoutingConfig(ConsumerConfiguration config, Type messageType)
	{
		RoutingAttribute routingAttr = this.GetAttribute<RoutingAttribute>(messageType);
		if (routingAttr == null)
		{
			return;
		}
		if (routingAttr.RoutingKey != null)
		{
			config.Consume.RoutingKey = routingAttr.RoutingKey;
		}
		if (routingAttr.PrefetchCount != 0)
		{
			config.Consume.PrefetchCount = routingAttr.PrefetchCount;
		}
		if (routingAttr._nullableAutoAck.HasValue)
		{
			config.Consume.AutoAck = routingAttr.AutoAck;
		}
	}

	protected virtual void UpdateQueueConfig(ConsumerConfiguration config, Type messageType)
	{
		QueueAttribute attribute = this.GetAttribute<QueueAttribute>(messageType);
		if (attribute == null)
		{
			return;
		}
		if (!string.IsNullOrWhiteSpace(attribute.Name))
		{
			config.Consume.QueueName = attribute.Name;
			if (config.Queue != null)
			{
				config.Queue.Name = attribute.Name;
			}
		}
		if (config.Queue == null)
		{
			return;
		}
		if (attribute._nullableDurability.HasValue)
		{
			config.Queue.Durable = attribute.Durable;
		}
		if (attribute._nullableExclusitivy.HasValue)
		{
			config.Queue.Exclusive = attribute.Exclusive;
		}
		if (attribute._nullableAutoDelete.HasValue)
		{
			config.Queue.AutoDelete= attribute.AutoDelete;
		}
		if (attribute.MessageTtl >= 0)
		{
			config.Queue.Arguments.AddOrReplace(QueueArgument.MessageTtl, attribute.MessageTtl);
		}
		if (attribute.MaxPriority > 0)
		{
			config.Queue.Arguments.AddOrReplace(QueueArgument.MaxPriority, attribute.MaxPriority);
		}
		if (!string.IsNullOrWhiteSpace(attribute.DeadLeterExchange))
		{
			config.Queue.Arguments.AddOrReplace(QueueArgument.DeadLetterExchange, attribute.DeadLeterExchange);
		}
		if (!string.IsNullOrWhiteSpace(attribute.Mode))
		{
			config.Queue.Arguments.AddOrReplace(QueueArgument.QueueMode, attribute.Mode);
		}
	}

	protected virtual TAttribute GetAttribute<TAttribute>(Type type) where TAttribute : Attribute
	{
		TAttribute attr = type.GetTypeInfo().GetCustomAttribute<TAttribute>();
		return attr;
	}
}