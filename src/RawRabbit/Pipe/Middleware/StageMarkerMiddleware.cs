using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Logging;

namespace RawRabbit.Pipe.Middleware
{
	public class StageMarkerMiddleware : Middleware
	{
		public readonly string _stage;
		private readonly ILog _logger = LogProvider.For<StageMarkerMiddleware>();

		public StageMarkerMiddleware(StageMarkerOptions options)
		{
			if (options == null)
			{
				throw new ArgumentNullException(nameof(options));
			}

			this._stage = options.Stage;
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
		{
			if (this.Next is NoOpMiddleware || this.Next is CancellationMiddleware)
			{
				this._logger.Debug("Stage {pipeStage} has no additional middlewares registered.", this._stage);
			}
			else
			{
				this._logger.Info("Invoking additional middlewares on stage {pipeStage}", this._stage);
			}
			return this.Next.InvokeAsync(context, token);
		}
	}

	public class StageMarkerOptions
	{
		public string Stage { get; set; }

		public static StageMarkerOptions For<TPipe>(TPipe stage)
		{
			return new StageMarkerOptions
			{
				Stage = stage.ToString()
			};
		}
	}


	public abstract class StagedMiddleware : Middleware
	{
		public abstract string StageMarker { get; }
	}
}
