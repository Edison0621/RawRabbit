using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace RawRabbit.Pipe.Middleware
{
	public class ExchangeDeleteOptions
	{
		public Func<IPipeContext, IChannel> ChannelFunc { get; set; }
		public Func<IPipeContext, string> ExchangeNameFunc { get; set; }
		public Func<IPipeContext, bool> IfUsedFunc { get; set; }
	}

	public class ExchangeDeleteMiddleware : Middleware
	{
		protected readonly Func<IPipeContext, IChannel> _channelFunc;
		protected readonly Func<IPipeContext, string> _exchangeNameFunc;
		protected readonly Func<IPipeContext, bool> _ifUsedFunc;

		public ExchangeDeleteMiddleware(ExchangeDeleteOptions options)
		{
			this._channelFunc = options?.ChannelFunc ?? (context => context.GetTransientChannel());
			this._exchangeNameFunc = options?.ExchangeNameFunc ?? (context => string.Empty);
			this._ifUsedFunc = options?.IfUsedFunc ?? (context => false);
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token = new CancellationToken())
		{
			IChannel channel = this.GetChannel(context);
			string exchangeName = this.GetExchangeName(context);
			bool ifUsed = this.GetIfUsed(context);
			await this.DeleteExchangeAsync(channel, exchangeName, ifUsed);
			await this.Next.InvokeAsync(context, token);
		}

		protected virtual async Task DeleteExchangeAsync(IChannel channel, string exchangeName, bool ifUsed)
		{
			await channel.ExchangeDeleteAsync(exchangeName, ifUsed);
		}

		protected virtual IChannel GetChannel(IPipeContext context)
		{
			return this._channelFunc?.Invoke(context);
		}

		protected virtual string GetExchangeName(IPipeContext context)
		{
			return this._exchangeNameFunc?.Invoke(context);
		}

		protected virtual bool GetIfUsed(IPipeContext context)
		{
			return this._ifUsedFunc.Invoke(context);
		}
	}
}
