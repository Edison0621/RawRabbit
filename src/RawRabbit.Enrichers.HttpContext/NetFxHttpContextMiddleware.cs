using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.HttpContext;

public class NetFxHttpContextMiddleware : StagedMiddleware
{
	public override string StageMarker => Pipe.StageMarker.Initialized;

	public override Task InvokeAsync(IPipeContext context, CancellationToken token = new())
	{
#if NET451
			context.UseHttpContext(System.Web.HttpContext.Current);
#endif
		return this.Next.InvokeAsync(context, token);
	}
}