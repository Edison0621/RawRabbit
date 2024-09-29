using System;
using RawRabbit.Common;
using RawRabbit.Compatibility.Legacy.Configuration.Exchange;
using RawRabbit.Compatibility.Legacy.Configuration.Publish;
using RawRabbit.Compatibility.Legacy.Configuration.Queue;
using RawRabbit.Compatibility.Legacy.Configuration.Request;
using RawRabbit.Compatibility.Legacy.Configuration.Respond;
using RawRabbit.Compatibility.Legacy.Configuration.Subscribe;
using RawRabbit.Configuration;

namespace RawRabbit.Compatibility.Legacy.Configuration
{
	public interface IConfigurationEvaluator
	{
		SubscriptionConfiguration GetConfiguration<TMessage>(Action<ISubscriptionConfigurationBuilder> configuration = null);
		PublishConfiguration GetConfiguration<TMessage>(Action<IPublishConfigurationBuilder> configuration);
		ResponderConfiguration GetConfiguration<TRequest, TResponse>(Action<IResponderConfigurationBuilder> configuration);
		RequestConfiguration GetConfiguration<TRequest, TResponse>(Action<IRequestConfigurationBuilder> configuration);

		SubscriptionConfiguration GetConfiguration(Type messageType, Action<ISubscriptionConfigurationBuilder> configuration = null);
		PublishConfiguration GetConfiguration(Type messageType, Action<IPublishConfigurationBuilder> configuration);
		ResponderConfiguration GetConfiguration(Type requestType, Type responseType, Action<IResponderConfigurationBuilder> configuration);
		RequestConfiguration GetConfiguration(Type requestType, Type responseType, Action<IRequestConfigurationBuilder> configuration);
	}

	public class ConfigurationEvaluator : IConfigurationEvaluator
	{
		private readonly RawRabbitConfiguration _clientConfig;
		private readonly INamingConventions _conventions;
		private readonly string _directReplyTo = "amq.rabbitmq.reply-to";

		public ConfigurationEvaluator(RawRabbitConfiguration clientConfig, INamingConventions conventions)
		{
			this._clientConfig = clientConfig;
			this._conventions = conventions;
		}

		public SubscriptionConfiguration GetConfiguration<TMessage>(Action<ISubscriptionConfigurationBuilder> configuration = null)
		{
			return this.GetConfiguration(typeof(TMessage), configuration);
		}

		public PublishConfiguration GetConfiguration<TMessage>(Action<IPublishConfigurationBuilder> configuration)
		{
			return this.GetConfiguration(typeof(TMessage), configuration);
		}

		public ResponderConfiguration GetConfiguration<TRequest, TResponse>(Action<IResponderConfigurationBuilder> configuration)
		{
			return this.GetConfiguration(typeof(TRequest), typeof(TResponse), configuration);
		}

		public RequestConfiguration GetConfiguration<TRequest, TResponse>(Action<IRequestConfigurationBuilder> configuration)
		{
			return this.GetConfiguration(typeof(TRequest), typeof(TResponse), configuration);
		}

		public SubscriptionConfiguration GetConfiguration(Type messageType, Action<ISubscriptionConfigurationBuilder> configuration = null)
		{
			string routingKey = this._conventions.QueueNamingConvention(messageType);
			QueueConfiguration queueConfig = new QueueConfiguration(this._clientConfig.Queue)
			{
				QueueName = routingKey,
				NameSuffix = this._conventions.SubscriberQueueSuffix(messageType)
			};

			ExchangeConfiguration exchangeConfig = new ExchangeConfiguration(this._clientConfig.Exchange)
			{
				ExchangeName = this._conventions.ExchangeNamingConvention(messageType)
			};

			SubscriptionConfigurationBuilder builder = new SubscriptionConfigurationBuilder(queueConfig, exchangeConfig, routingKey);
			configuration?.Invoke(builder);
			return builder.Configuration;
		}

		public PublishConfiguration GetConfiguration(Type messageType, Action<IPublishConfigurationBuilder> configuration)
		{
			ExchangeConfiguration exchangeConfig = new ExchangeConfiguration(this._clientConfig.Exchange)
			{
				ExchangeName = this._conventions.ExchangeNamingConvention(messageType)
			};
			string routingKey = this._conventions.QueueNamingConvention(messageType);
			PublishConfigurationBuilder builder = new PublishConfigurationBuilder(exchangeConfig, routingKey);
			configuration?.Invoke(builder);
			return builder.Configuration;
		}

		public ResponderConfiguration GetConfiguration(Type requestType, Type responseType, Action<IResponderConfigurationBuilder> configuration)
		{
			QueueConfiguration queueConfig = new QueueConfiguration(this._clientConfig.Queue)
			{
				QueueName = this._conventions.QueueNamingConvention(requestType)
			};

			ExchangeConfiguration exchangeConfig = new ExchangeConfiguration(this._clientConfig.Exchange)
			{
				ExchangeName = this._conventions.ExchangeNamingConvention(requestType)
			};

			ResponderConfigurationBuilder builder = new ResponderConfigurationBuilder(queueConfig, exchangeConfig);
			configuration?.Invoke(builder);
			return builder.Configuration;
		}

		public RequestConfiguration GetConfiguration(Type requestType, Type responseType, Action<IRequestConfigurationBuilder> configuration)
		{
			// leverage direct reply to: https://www.rabbitmq.com/direct-reply-to.html
			QueueConfiguration replyQueueConfig = new QueueConfiguration
			{
				QueueName = this._directReplyTo,
				AutoDelete = true,
				Durable = false,
				Exclusive = true
			};

			RequestConfiguration defaultConfig = new RequestConfiguration
			{
				ReplyQueue = replyQueueConfig,
				Exchange = ExchangeConfiguration.Default,
				RoutingKey = this._conventions.QueueNamingConvention(requestType),
				ReplyQueueRoutingKey = replyQueueConfig.QueueName
			};

			RequestConfigurationBuilder builder = new RequestConfigurationBuilder(defaultConfig);
			configuration?.Invoke(builder);
			return builder.Configuration;
		}
	}
}
