using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Operations.Get.Model;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Get.Middleware;

public class AckableResultOptions<TResult>
{
	public Func<IPipeContext, TResult> ContentFunc { get; set; }
	public Func<IPipeContext, IChannel> ChannelFunc { get; internal set; }
	public Func<IPipeContext, ulong> DeliveryTagFunc { get; internal set; }
	public Action<IPipeContext, Ackable<TResult>> PostExecutionAction { get; internal set; }
}

public class AckableResultOptions : AckableResultOptions<object> { }

public class AckableResultMiddleware : AckableResultMiddleware<object>
{
	public AckableResultMiddleware(AckableResultOptions options) : base(options)
	{ }
}

public class AckableResultMiddleware<TResult> : Pipe.Middleware.Middleware
{
	protected readonly Func<IPipeContext, TResult> _getResultFunc;
	protected readonly Func<IPipeContext, IChannel> _channelFunc;
	protected readonly Action<IPipeContext, Ackable<TResult>> _postExecutionAction;
	protected readonly Func<IPipeContext, ulong> _deliveryTagFunc;

	public AckableResultMiddleware(AckableResultOptions<TResult> options = null)
	{
		this._getResultFunc = options?.ContentFunc;
		this._channelFunc = options?.ChannelFunc ?? (context => context.GetChannel());
		this._postExecutionAction = options?.PostExecutionAction;
		this._deliveryTagFunc = options?.DeliveryTagFunc;
	}

	public override Task InvokeAsync(IPipeContext context, CancellationToken token = default)
	{
		IChannel channel = this.GetChannel(context);
		TResult getResult = this.GetResult(context);
		ulong deliveryTag = this.GetDeliveryTag(context);
		Ackable<TResult> ackableResult = this.CreateAckableResult(channel, getResult, deliveryTag);
		context.Properties.TryAdd(GetKey.AckableResult, ackableResult);
		this._postExecutionAction?.Invoke(context, ackableResult);
		return this.Next.InvokeAsync(context, token);
	}

	protected virtual ulong GetDeliveryTag(IPipeContext context)
	{
		return this._deliveryTagFunc(context);
	}

	protected virtual Ackable<TResult> CreateAckableResult(IChannel channel, TResult result, ulong deliveryTag)
	{
		return new Ackable<TResult>(result, channel, deliveryTag);
	}

	protected virtual IChannel GetChannel(IPipeContext context)
	{
		return this._channelFunc(context);
	}

	protected virtual TResult GetResult(IPipeContext context)
	{
		return this._getResultFunc(context);
	}
}