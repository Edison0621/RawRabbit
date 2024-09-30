using System;
using RawRabbit.Configuration.Consume;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Configuration.Consumer;

public class ConsumerConfigurationBuilder :  IConsumerConfigurationBuilder
{
	public ConsumerConfiguration Config { get; }

	public ConsumerConfigurationBuilder(ConsumerConfiguration initial)
	{
		this.Config = initial;
	}

	public IConsumerConfigurationBuilder OnDeclaredExchange(Action<IExchangeDeclarationBuilder> exchange)
	{
		ExchangeDeclarationBuilder builder = new(this.Config.Exchange);
		exchange(builder);
		this.Config.Exchange = builder.Declaration;
		this.Config.Consume.ExchangeName = builder.Declaration.Name;
		return this;
	}

	public IConsumerConfigurationBuilder FromDeclaredQueue(Action<IQueueDeclarationBuilder> queue)
	{
		QueueDeclarationBuilder builder = new(this.Config.Queue);
		queue(builder);
		this.Config.Queue = builder.Declaration;
		this.Config.Consume.QueueName = builder.Declaration.Name;
		return this;
	}

	public IConsumerConfigurationBuilder Consume(Action<IConsumeConfigurationBuilder> consume)
	{
		ConsumeConfigurationBuilder builder = new(this.Config.Consume);
		consume(builder);
		this.Config.Consume = builder.Config;
		if (builder.ExistingExchange)
		{
			this.Config.Exchange = null;
		}
		if (builder.ExistingQueue)
		{
			this.Config.Queue = null;
		}
		return this;
	}
}