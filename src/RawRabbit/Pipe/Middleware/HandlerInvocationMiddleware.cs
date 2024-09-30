using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Common;

namespace RawRabbit.Pipe.Middleware;

public class HandlerInvocationOptions
{
	public Func<IPipeContext, Func<object[], Task<Acknowledgement>>> MessageHandlerFunc { get; set; }
	public Func<IPipeContext, object[]> HandlerArgsFunc { get; set; }
	public Action<IPipeContext, Acknowledgement> PostInvokeAction { get; set; }
}

public class HandlerInvocationMiddleware : Middleware
{
	protected readonly Func<IPipeContext, object[]> _handlerArgsFunc;
	protected readonly Action<IPipeContext, Acknowledgement> _postInvokeAction;
	protected readonly Func<IPipeContext, Func<object[], Task<Acknowledgement>>> _messageHandlerFunc;

	public HandlerInvocationMiddleware(HandlerInvocationOptions options = null)
	{
		this._handlerArgsFunc = options?.HandlerArgsFunc ?? (context => context.GetMessageHandlerArgs()) ;
		this._messageHandlerFunc = options?.MessageHandlerFunc ?? (context => context.GetMessageHandler());
		this._postInvokeAction = options?.PostInvokeAction;
	}

	public override async Task InvokeAsync(IPipeContext context, CancellationToken token = default)
	{
		await this.InvokeMessageHandler(context, token);
		await this.Next.InvokeAsync(context, token);
	}

	protected virtual async Task InvokeMessageHandler(IPipeContext context, CancellationToken token)
	{
		object[] args = this._handlerArgsFunc(context);
		Func<object[], Task<Acknowledgement>> handler = this._messageHandlerFunc(context);

		Acknowledgement acknowledgement = await handler(args);
		context.Properties.TryAdd(PipeKey.MessageAcknowledgement, acknowledgement);
		this._postInvokeAction?.Invoke(context, acknowledgement);
	}
}