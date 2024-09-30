using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Get.Middleware;

public class BasicGetOptions
{
	public Func<IPipeContext, IChannel> ChannelFunc { get; set; }
	public Func<IPipeContext, bool> AutoAckFunc { get; internal set; }
	public Action<IPipeContext, BasicGetResult> PostExecutionAction { get; set; }
	public Func<IPipeContext, string> QueueNameFunc { get; internal set; }
}

public class BasicGetMiddleware : Pipe.Middleware.Middleware
{
	protected readonly Func<IPipeContext, IChannel> _channelFunc;
	protected readonly Func<IPipeContext, string> _queueNameFunc;
	protected readonly Func<IPipeContext, bool> _autoAckFunc;
	protected readonly Action<IPipeContext, BasicGetResult> _postExecutionAction;

	public BasicGetMiddleware(BasicGetOptions options = null)
	{
		this._channelFunc = options?.ChannelFunc ?? (context => context.GetChannel());
		this._queueNameFunc = options?.QueueNameFunc ?? (context => context.GetGetConfiguration()?.QueueName);
		this._autoAckFunc = options?.AutoAckFunc ?? (context => context.GetGetConfiguration()?.AutoAck ?? false);
		this._postExecutionAction = options?.PostExecutionAction;
	}

	public override async Task InvokeAsync(IPipeContext context, CancellationToken token = default)
	{
		IChannel channel = this.GetChannel(context);
		string queueNamme = this.GetQueueName(context);
		bool autoAck = this.GetAutoAck(context);
		BasicGetResult getResult = await this.PerformBasicGet(channel, queueNamme, autoAck);
		context.Properties.TryAdd(GetPipeExtensions.BasicGetResult, getResult);
		this._postExecutionAction?.Invoke(context, getResult);
		await this.Next.InvokeAsync(context, token);
	}

	protected virtual async Task<BasicGetResult> PerformBasicGet(IChannel channel, string queueName, bool autoAck)
	{
		return await channel.BasicGetAsync(queueName, autoAck);
	}

	protected virtual bool GetAutoAck(IPipeContext context)
	{
		return this._autoAckFunc(context);
	}

	protected virtual string GetQueueName(IPipeContext context)
	{
		return this._queueNameFunc(context);
	}

	protected virtual IChannel GetChannel(IPipeContext context)
	{
		return this._channelFunc(context);
	}
}