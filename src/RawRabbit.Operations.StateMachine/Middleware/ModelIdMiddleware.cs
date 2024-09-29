using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.StateMachine.Middleware
{
	public class ModelIdOptions
	{
		public Func<IPipeContext, Func<object[], Guid>> CorrelationFunc { get; set; }
		public Func<IPipeContext, Guid> ModelIdFunc { get; set; }
		public Func<IPipeContext, object[]> CorrelationArgsFunc { get; set; }
	}

	public class ModelIdMiddleware : Pipe.Middleware.Middleware
	{
		protected readonly Func<IPipeContext, Func<object[], Guid>> _correlationFunc;
		protected readonly Func<IPipeContext, object[]> _correlationArgsFunc;
		protected readonly Func<IPipeContext, Guid> _modelIdFunc;

		public ModelIdMiddleware(ModelIdOptions options = null)
		{
			this._correlationFunc = options?.CorrelationFunc ?? (context => context.GetIdCorrelationFunc());
			this._correlationArgsFunc = options?.CorrelationArgsFunc ?? (context => context.GetLazyCorrelationArgs());
			this._modelIdFunc = options?.ModelIdFunc ?? (context => context.GetModelId());
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
		{
			Func<object[], Guid> corrFunc = this.GetCorrelationFunc(context);
			object[] corrArgs = this.GetCorrelationArgs(context);
			Guid id = this.GetModelId(context, corrFunc, corrArgs);
			context.Properties.TryAdd(StateMachineKey.ModelId, id);
			await this.Next.InvokeAsync(context, token);
		}

		protected virtual Func<object[], Guid> GetCorrelationFunc(IPipeContext context)
		{
			return this._correlationFunc.Invoke(context);
		}

		protected virtual object[] GetCorrelationArgs(IPipeContext context)
		{
			return this._correlationArgsFunc?.Invoke(context);
		}

		protected virtual Guid GetModelId(IPipeContext context, Func<object[], Guid> corrFunc, object[] args)
		{
			Guid fromContext = this._modelIdFunc.Invoke(context);
			return fromContext != Guid.Empty ? fromContext : corrFunc(args);
		}
	}
}
