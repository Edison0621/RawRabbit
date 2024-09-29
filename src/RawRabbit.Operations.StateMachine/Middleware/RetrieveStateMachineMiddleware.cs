using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Operations.StateMachine.Core;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.StateMachine.Middleware
{
	public class RetrieveStateMachineOptions
	{
		public Func<IPipeContext, Guid> ModelIdFunc { get; set; }
		public Func<IPipeContext, Type> StateMachineTypeFunc { get; set; }
		public Func<IPipeContext, StateMachineBase> StateMachineFunc { get; set; }
		public Action<StateMachineBase, IPipeContext> PostExecuteAction { get; set; }
	}

	public class RetrieveStateMachineMiddleware : Pipe.Middleware.Middleware
	{
		private readonly IStateMachineActivator _stateMachineRepo;
		protected readonly Func<IPipeContext, Guid> _modelIdFunc;
		protected readonly Func<IPipeContext, Type> _stateMachineTypeFunc;
		protected readonly Action<StateMachineBase, IPipeContext> _postExecuteAction;
		protected readonly Func<IPipeContext, StateMachineBase> _stateMachineFunc;

		public RetrieveStateMachineMiddleware(IStateMachineActivator stateMachineRepo, RetrieveStateMachineOptions options = null)
		{
			this._stateMachineRepo = stateMachineRepo;
			this._modelIdFunc = options?.ModelIdFunc ?? (context => context.Get(StateMachineKey.ModelId, Guid.NewGuid()));
			this._stateMachineTypeFunc = options?.StateMachineTypeFunc ?? (context => context.Get<Type>(StateMachineKey.Type));
			this._stateMachineFunc = options?.StateMachineFunc;
			this._postExecuteAction = options?.PostExecuteAction;
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
		{
			Guid id = this.GetModelId(context);
			Type stateMachineType = this.GetStateMachineType(context);
			StateMachineBase stateMachine = await this.GetStateMachineAsync(context, id, stateMachineType);
			context.Properties.TryAdd(StateMachineKey.Machine, stateMachine);
			this._postExecuteAction?.Invoke(stateMachine, context);
			await this.Next.InvokeAsync(context, token);
		}

		protected virtual Task<StateMachineBase> GetStateMachineAsync(IPipeContext context, Guid id, Type type)
		{
			StateMachineBase fromContext = this._stateMachineFunc?.Invoke(context);
			return fromContext != null
				? Task.FromResult(fromContext)
				: this._stateMachineRepo.ActivateAsync(id, type);
		}

		protected virtual Type GetStateMachineType(IPipeContext context)
		{
			return this._stateMachineTypeFunc(context);
		}

		protected virtual Guid GetModelId(IPipeContext context)
		{
			return this._modelIdFunc(context);
		}
	}
}
