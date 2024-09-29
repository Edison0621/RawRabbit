using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Publisher;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.Attributes.Middleware
{
	public class ProduceAttributeOptions
	{
		public Func<IPipeContext, PublisherConfiguration> PublishConfigFunc { get; set; }
		public Func<IPipeContext, Type> MessageTypeFunc { get; set; }
	}

	public class ProduceAttributeMiddleware : StagedMiddleware
	{
		protected readonly Func<IPipeContext, PublisherConfiguration> _publishConfigFunc;
		protected readonly Func<IPipeContext, Type> _messageType;
		public override string StageMarker => Pipe.StageMarker.PublishConfigured;

		public ProduceAttributeMiddleware(ProduceAttributeOptions options = null)
		{
			this._publishConfigFunc = options?.PublishConfigFunc ?? (context => context.GetPublishConfiguration());
			this._messageType = options?.MessageTypeFunc ?? (context => context.GetMessageType());
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token = new CancellationToken())
		{
			PublisherConfiguration publishConfig = this.GetPublishConfig(context);
			Type messageType = this.GetMessageType(context);
			this.UpdateExchangeConfig(publishConfig, messageType);
			this.UpdateRoutingConfig(publishConfig, messageType);
			return this.Next.InvokeAsync(context, token);
		}

		protected virtual PublisherConfiguration GetPublishConfig(IPipeContext context)
		{
			return this._publishConfigFunc?.Invoke(context);
		}

		protected virtual Type GetMessageType(IPipeContext context)
		{
			return this._messageType?.Invoke(context);
		}

		protected virtual void UpdateExchangeConfig(PublisherConfiguration config, Type messageType)
		{
			ExchangeAttribute attribute = this.GetAttribute<ExchangeAttribute>(messageType);
			if (attribute == null)
			{
				return;
			}

			if (!string.IsNullOrWhiteSpace(attribute.Name))
			{
				config.ExchangeName = attribute.Name;
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
				config.Exchange.AutoDelete= attribute._nullableAutoDelete.Value;
			}
			if (attribute.Type != ExchangeType.Unknown)
			{
				config.Exchange.ExchangeType = attribute.Type.ToString().ToLowerInvariant();
			}
		}

		protected virtual void UpdateRoutingConfig(PublisherConfiguration config, Type messageType)
		{
			RoutingAttribute routingAttr = this.GetAttribute<RoutingAttribute>(messageType);
			if (routingAttr?.RoutingKey != null)
			{
				config.RoutingKey = routingAttr.RoutingKey;
			}
		}

		protected virtual TAttribute GetAttribute<TAttribute>(Type type) where TAttribute : Attribute
		{
			TAttribute attr = type.GetTypeInfo().GetCustomAttribute<TAttribute>();
			return attr;
		}
	}
}
