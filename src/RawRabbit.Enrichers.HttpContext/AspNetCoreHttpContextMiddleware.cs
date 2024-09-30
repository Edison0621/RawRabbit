using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.HttpContext;

public class AspNetCoreHttpContextMiddleware : StagedMiddleware
{
	private readonly IHttpContextAccessor _httpAccessor;

	public AspNetCoreHttpContextMiddleware(IHttpContextAccessor httpAccessor)
	{
		this._httpAccessor = httpAccessor;
	}
	public override string StageMarker => Pipe.StageMarker.Initialized;

	public override Task InvokeAsync(IPipeContext context, CancellationToken token = new())
	{
		context.UseHttpContext(this._httpAccessor.HttpContext);
		return this.Next.InvokeAsync(context, token);
	}

}
