using System.Threading;
using System.Threading.Tasks;

namespace RawRabbit.Pipe.Middleware;

public class NoOpMiddleware : Middleware
{
	public override Task InvokeAsync(IPipeContext context, CancellationToken token = default)
	{
		return Task.FromResult(0);
	}
}