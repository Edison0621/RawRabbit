using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Common;

namespace RawRabbit.Pipe.Middleware;

public class ExplicitAckOptions
{
	public Func<IPipeContext, Task> InvokeMessageHandlerFunc { get; set; }
	public Func<IPipeContext, IAsyncBasicConsumer> ConsumerFunc { get; set; }
	public Func<IPipeContext, BasicDeliverEventArgs> DeliveryArgsFunc { get; set; }
	public Func<IPipeContext, bool> AutoAckFunc { get; set; }
	public Func<IPipeContext, Acknowledgement> GetMessageAcknowledgement { get; set; }
	public Predicate<Acknowledgement> AbortExecution { get; set; }
}

public class ExplicitAckMiddleware : Middleware
{
	protected INamingConventions _conventions;
	protected readonly ITopologyProvider _topology;
	protected readonly IChannelFactory _channelFactory;
	protected readonly Func<IPipeContext, BasicDeliverEventArgs> _deliveryArgsFunc;
	protected readonly Func<IPipeContext, IAsyncBasicConsumer> _consumerFunc;
	protected readonly Func<IPipeContext, Acknowledgement> _messageAcknowledgementFunc;
	protected readonly Predicate<Acknowledgement> _abortExecution;
	protected readonly Func<IPipeContext, bool> _autoAckFunc;

	public ExplicitAckMiddleware(INamingConventions conventions, ITopologyProvider topology, IChannelFactory channelFactory, ExplicitAckOptions options = null)
	{
		this._conventions = conventions;
		this._topology = topology;
		this._channelFactory = channelFactory;
		this._deliveryArgsFunc = options?.DeliveryArgsFunc ?? (context => context.GetDeliveryEventArgs());
		this._consumerFunc = options?.ConsumerFunc ?? (context => context.GetConsumer());
		this._messageAcknowledgementFunc = options?.GetMessageAcknowledgement ?? (context => context.GetMessageAcknowledgement());
		this._abortExecution = options?.AbortExecution ?? (ack => !(ack is Ack));
		this._autoAckFunc = options?.AutoAckFunc ?? (context => context.GetConsumeConfiguration().AutoAck);
	}

	public override async Task InvokeAsync(IPipeContext context, CancellationToken token = default)
	{
		bool autoAck = this.GetAutoAck(context);
		if (!autoAck)
		{
			Acknowledgement ack = await this.AcknowledgeMessageAsync(context);
			if (this._abortExecution(ack))
			{
				return;
			}
		}
		await this.Next.InvokeAsync(context, token);
	}

	protected virtual async Task<Acknowledgement> AcknowledgeMessageAsync(IPipeContext context)
	{
		Acknowledgement ack = this._messageAcknowledgementFunc(context);
		if (ack == null)
		{
			throw new NotSupportedException("Invocation Result of Message Handler not found.");
		}
		BasicDeliverEventArgs deliveryArgs = this._deliveryArgsFunc(context);
		IChannel channel = this._consumerFunc(context).Channel;

		if (channel == null)
		{
			throw new NullReferenceException("Unable to retrieve channel for delivered message.");
		}

		if (!channel.IsOpen)
		{
			if (channel is IRecoverable recoverable)
			{
				TaskCompletionSource<bool> recoverTsc = new();

				EventHandler<EventArgs> onRecover = null;
				onRecover = (_, _) =>
				{
					recoverTsc.TrySetResult(true);
					recoverable.Recovery -= onRecover;
				};
				recoverable.Recovery += onRecover;
				await recoverTsc.Task;
					
			}
			return new Ack();
		}

		switch (ack)
		{
			case Ack async:
				await this.HandleAckAsync(async, channel, deliveryArgs);
				return async;
			case Nack nack:
				await this.HandleNackAsync(nack, channel, deliveryArgs);
				return nack;
			case Reject reject:
				await this.HandleRejectAsync(reject, channel, deliveryArgs);
				return reject;
			default:
				throw new NotSupportedException($"Unable to handle {ack.GetType()} as an Acknowledgement.");
		}
	}

	protected virtual async Task HandleAckAsync(Ack ack, IChannel channel, BasicDeliverEventArgs deliveryArgs)
	{
		await channel.BasicAckAsync(deliveryArgs.DeliveryTag, false);
	}

	protected virtual async Task HandleNackAsync(Nack nack, IChannel channel, BasicDeliverEventArgs deliveryArgs)
	{
		await channel.BasicNackAsync(deliveryArgs.DeliveryTag, false, nack.Requeue);
	}

	protected virtual async Task HandleRejectAsync(Reject reject, IChannel channel, BasicDeliverEventArgs deliveryArgs)
	{
		await channel.BasicRejectAsync(deliveryArgs.DeliveryTag, reject.Requeue);
	}

	protected virtual bool GetAutoAck(IPipeContext context)
	{
		return this._autoAckFunc(context);
	}
}
