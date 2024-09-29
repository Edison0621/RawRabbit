using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Enrichers.GlobalExecutionId.Dependencies;
using RawRabbit.Logging;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.GlobalExecutionId.Middleware
{
	public class AppendGlobalExecutionIdOptions
	{
		public Func<IPipeContext, string> ExecutionIdFunc { get; set; }
		public Action<IPipeContext, string> SaveInContext { get; set; }
	}

	public class AppendGlobalExecutionIdMiddleware : StagedMiddleware
	{
		public override string StageMarker => Pipe.StageMarker.ProducerInitialized;
		protected readonly Func<IPipeContext, string> _executionIdFunc;
		protected readonly Action<IPipeContext, string> _saveInContext;
		private readonly ILog _logger = LogProvider.For<AppendGlobalExecutionIdMiddleware>();

		public AppendGlobalExecutionIdMiddleware(AppendGlobalExecutionIdOptions options = null)
		{
			this._executionIdFunc = options?.ExecutionIdFunc ?? (context => context.GetGlobalExecutionId());
			this._saveInContext = options?.SaveInContext ?? ((ctx, id) => ctx.Properties.TryAdd(PipeKey.GlobalExecutionId, id));
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token = new CancellationToken())
		{
			string fromContext = this.GetExecutionIdFromContext(context);
			if (!string.IsNullOrWhiteSpace(fromContext))
			{
				this._logger.Info("GlobalExecutionId {globalExecutionId} was allready found in PipeContext.", fromContext);
				return this.Next.InvokeAsync(context, token);
			}
			string fromProcess = this.GetExecutionIdFromProcess();
			if (!string.IsNullOrWhiteSpace(fromProcess))
			{
				this._logger.Info("Using GlobalExecutionId {globalExecutionId} that was found in the execution process.", fromProcess);
				this.AddToContext(context, fromProcess);
				return this.Next.InvokeAsync(context, token);
			}
			string created = this.CreateExecutionId(context);
			this.AddToContext(context, created);
			this._logger.Info("Creating new GlobalExecutionId {globalExecutionId} for this execution.", created);
			return this.Next.InvokeAsync(context, token);
		}

		protected virtual void AddToContext(IPipeContext context, string globalMessageId)
		{
			this._saveInContext(context, globalMessageId);
		}

		protected virtual string CreateExecutionId(IPipeContext context)
		{
			return  Guid.NewGuid().ToString();
		}

		protected virtual string GetExecutionIdFromProcess()
		{
			return GlobalExecutionIdRepository.Get();
		}

		protected virtual string GetExecutionIdFromContext(IPipeContext context)
		{
			return this._executionIdFunc(context);
		}

	}
}
