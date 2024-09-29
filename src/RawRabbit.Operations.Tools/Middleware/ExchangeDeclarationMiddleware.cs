using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Tools.Middleware
{
	public class ExchangeDeclarationOptions
	{
		public Func<IPipeContext, ExchangeDeclaration> ExchangeDeclarationFunc { get; internal set; }
		public Action<IPipeContext, ExchangeDeclaration> SaveToContextAction { get; internal set; }
	}

	public class ExchangeDeclarationMiddleware : Pipe.Middleware.Middleware
	{
		protected readonly IExchangeDeclarationFactory _cfgFactory;
		protected readonly Func<IPipeContext, ExchangeDeclaration> _exchangeDeclarationFunc;
		protected readonly Action<IPipeContext, ExchangeDeclaration> _saveToContextAction;

		public ExchangeDeclarationMiddleware(IExchangeDeclarationFactory cfgFactory, ExchangeDeclarationOptions options = null)
		{
			this._cfgFactory = cfgFactory;
			this._exchangeDeclarationFunc = options?.ExchangeDeclarationFunc ?? (ctx => ctx.GetExchangeDeclaration());
			this._saveToContextAction = options?.SaveToContextAction ?? ((ctx, d) => ctx.Properties.TryAdd(PipeKey.ExchangeDeclaration, d)); 
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
		{
			ExchangeDeclaration queueDeclaration = this.GetQueueDeclaration(context);
			this.SaveToContext(context, queueDeclaration);
			return this.Next.InvokeAsync(context, token);
		}

		protected virtual ExchangeDeclaration GetQueueDeclaration(IPipeContext context)
		{
			ExchangeDeclaration declaration = this._exchangeDeclarationFunc?.Invoke(context);
			if (declaration != null)
			{
				return declaration;
			}
			Type messageType = context.GetMessageType();
			return this._cfgFactory.Create(messageType);
		}

		protected virtual void SaveToContext(IPipeContext context, ExchangeDeclaration declaration)
		{
			this._saveToContextAction?.Invoke(context, declaration);
		}
	}
}
