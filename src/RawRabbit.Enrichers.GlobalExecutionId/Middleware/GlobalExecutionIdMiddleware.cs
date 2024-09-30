using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Logging;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.GlobalExecutionId.Middleware
{
	public class GlobalExecutionOptions
	{
		public Func<IPipeContext, string> IdFunc { get; set; }
		public Action<IPipeContext, string> PersistAction { get; set; }
	}

	public class GlobalExecutionIdMiddleware : StagedMiddleware
	{
		public override string StageMarker => Pipe.StageMarker.Initialized;
		protected readonly Func<IPipeContext, string> _idFunc;
		protected readonly Action<IPipeContext, string> _persistAction;

		protected static readonly AsyncLocal<string> ExecutionId = new AsyncLocal<string>();
		private readonly ILog _logger = LogProvider.For<GlobalExecutionIdMiddleware>();

		public GlobalExecutionIdMiddleware(GlobalExecutionOptions options = null)
		{
			this._idFunc = options?.IdFunc ?? (context => context.GetGlobalExecutionId());
			this._persistAction = options?.PersistAction ?? ((context, id) => context.Properties.TryAdd(PipeKey.GlobalExecutionId, id));
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
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
				this._persistAction(context, fromProcess);
				return this.Next.InvokeAsync(context, token);
			}
			string created = this.CreateExecutionId(context);
			this._logger.Info("Creating new GlobalExecutionId {globalExecutionId} for this execution.", created);
			this._persistAction(context, created);
			return this.Next.InvokeAsync(context, token);
		}

		protected virtual string CreateExecutionId(IPipeContext context)
		{
			string executionId = Guid.NewGuid().ToString();
			this.SaveIdInProcess(executionId);
			return executionId;
		}

		protected virtual string GetExecutionIdFromProcess()
		{
			string executionId = ExecutionId?.Value;
			return executionId;
		}

		protected virtual string GetExecutionIdFromContext(IPipeContext context)
		{
			string id = this._idFunc(context);
			if (!string.IsNullOrWhiteSpace(id))
			{
				this.SaveIdInProcess(id);
			}
			return id;
		}

		protected virtual void SaveIdInProcess(string executionId)
		{
			ExecutionId.Value = executionId;
		}
	}
}
