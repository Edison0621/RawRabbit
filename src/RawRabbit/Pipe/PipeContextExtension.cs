using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.Configuration.BasicPublish;
using RawRabbit.Configuration.Consume;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Publisher;
using RawRabbit.Configuration.Queue;
using ISubscription = RawRabbit.Subscription.ISubscription;

namespace RawRabbit.Pipe;

public static class PipeContextExtension
{
	public static object GetMessage(this IPipeContext context)
	{
		return context.Get<object>(PipeKey.Message);
	}

	public static Type GetMessageType(this IPipeContext context)
	{
		return context.Get<Type>(PipeKey.MessageType);
	}

	public static object GetMessageContext(this IPipeContext context)
	{
		return context.Get<object>(PipeKey.MessageContext);
	}

	public static IAsyncBasicConsumer GetConsumer(this IPipeContext context)
	{
		return context.Get<IAsyncBasicConsumer>(PipeKey.Consumer);
	}

	public static QueueDeclaration GetQueueDeclaration(this IPipeContext context)
	{
		return context.Get<QueueDeclaration>(PipeKey.QueueDeclaration);
	}

	public static Action<Func<Task>, CancellationToken> GetConsumeThrottleAction(this IPipeContext context)
	{
		return context.Get<Action<Func<Task>, CancellationToken>>(PipeKey.ConsumeThrottleAction, (func, _) => func());
	}

	public static ExchangeDeclaration GetExchangeDeclaration(this IPipeContext context)
	{
		return context.Get<ExchangeDeclaration>(PipeKey.ExchangeDeclaration);
	}

	public static EventHandler<BasicReturnEventArgs> GetReturnCallback(this IPipeContext context)
	{
		return context.Get<EventHandler<BasicReturnEventArgs>>(PipeKey.ReturnCallback);
	}

	public static ConsumeConfiguration GetConsumeConfiguration(this IPipeContext context)
	{
		return context.Get<ConsumeConfiguration>(PipeKey.ConsumeConfiguration);
	}

	public static BasicPublishConfiguration GetBasicPublishConfiguration(this IPipeContext context)
	{
		return context.Get<BasicPublishConfiguration>(PipeKey.BasicPublishConfiguration);
	}

	public static ConsumerConfiguration GetConsumerConfiguration(this IPipeContext context)
	{
		return context.Get<ConsumerConfiguration>(PipeKey.ConsumerConfiguration);
	}

	public static PublisherConfiguration GetPublishConfiguration(this IPipeContext context)
	{
		return context.Get<PublisherConfiguration>(PipeKey.PublisherConfiguration);
	}

	public static string GetRoutingKey(this IPipeContext context)
	{
		return context.Get<string>(PipeKey.RoutingKey);
	}

	public static ISubscription GetSubscription(this IPipeContext context)
	{
		return context.Get<ISubscription>(PipeKey.Subscription);
	}

	public static IChannel GetChannel(this IPipeContext context)
	{
		return context.Get<IChannel>(PipeKey.Channel);
	}

	public static IChannel GetTransientChannel(this IPipeContext context)
	{
		return context.Get<IChannel>(PipeKey.TransientChannel);
	}

	public static BasicProperties GetBasicProperties(this IPipeContext context)
	{
		return context.Get<BasicProperties>(PipeKey.BasicProperties);
	}

	public static BasicDeliverEventArgs GetDeliveryEventArgs(this IPipeContext context)
	{
		return context.Get<BasicDeliverEventArgs>(PipeKey.DeliveryEventArgs);
	}

	public static Func<object[], Task<Acknowledgement>> GetMessageHandler(this IPipeContext context)
	{
		return context.Get<Func<object[], Task<Acknowledgement>>>(PipeKey.MessageHandler);
	}

	public static object[] GetMessageHandlerArgs(this IPipeContext context)
	{
		return context.Get<object[]>(PipeKey.MessageHandlerArgs);
	}

	public static Task GetMessageHandlerResult(this IPipeContext context)
	{
		return context.Get<Task>(PipeKey.MessageHandlerResult);
	}

	public static Acknowledgement GetMessageAcknowledgement(this IPipeContext context)
	{
		return context.Get<Acknowledgement>(PipeKey.MessageAcknowledgement);
	}

	public static RawRabbitConfiguration GetClientConfiguration(this IPipeContext context)
	{
		return context.Get<RawRabbitConfiguration>(PipeKey.ClientConfiguration);
	}
}