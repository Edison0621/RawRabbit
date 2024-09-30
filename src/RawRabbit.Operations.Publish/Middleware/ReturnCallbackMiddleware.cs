using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Logging;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Publish.Middleware
{
	public class ReturnCallbackOptions
	{
		public Func<IPipeContext, EventHandler<BasicReturnEventArgs>> CallbackFunc { get; set; }
		public Func<IPipeContext, IChannel> ChannelFunc { get; set; }
		public Action<IPipeContext, EventHandler<BasicReturnEventArgs>> PostInvokeAction { get; set; }
	}

	public class ReturnCallbackMiddleware : Pipe.Middleware.Middleware
	{
		protected readonly Func<IPipeContext, EventHandler<BasicReturnEventArgs>> _callbackFunc;
		protected readonly Func<IPipeContext, IChannel> _channelFunc;
		protected readonly Action<IPipeContext, EventHandler<BasicReturnEventArgs>> _postInvoke;
		private readonly ILog _logger = LogProvider.For<ReturnCallbackMiddleware>();

		public ReturnCallbackMiddleware(ReturnCallbackOptions options = null)
		{
			this._callbackFunc = options?.CallbackFunc ?? (context => context.GetReturnCallback());
			this._channelFunc = options?.ChannelFunc?? (context => context.GetTransientChannel());
			this._postInvoke = options?.PostInvokeAction;
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
		{
			EventHandler<BasicReturnEventArgs> callback = this.GetCallback(context);
			if (callback == null)
			{
				this._logger.Debug("No Mandatory Callback registered.");
				await this.Next.InvokeAsync(context, token);
				return;
			}

			IChannel channel = this.GetChannel(context);
			if (channel == null)
			{
				this._logger.Warn("Channel not found in Pipe Context. Mandatory Callback not registered.");
				await this.Next.InvokeAsync(context, token);
				return;
			}

			this._logger.Debug("Register Mandatory Callback on channel {channelNumber}", channel.ChannelNumber);
			channel.BasicReturn += callback;
			this._postInvoke?.Invoke(context, callback);

			await this.Next.InvokeAsync(context, token);
			this._logger.Debug("Removing Mandatory Callback on channel {channelNumber}", channel.ChannelNumber);
			channel.BasicReturn -= callback;
		}

		protected virtual IChannel GetChannel(IPipeContext context)
		{
			return this._channelFunc(context);
		}

		protected virtual EventHandler<BasicReturnEventArgs> GetCallback(IPipeContext context)
		{
			return this._callbackFunc(context);
		}
	}
}
