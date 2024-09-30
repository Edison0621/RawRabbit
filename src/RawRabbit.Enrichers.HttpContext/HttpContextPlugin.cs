using Microsoft.AspNetCore.Http;
using RawRabbit.Enrichers.HttpContext;
using RawRabbit.Instantiation;

namespace RawRabbit;

public static class HttpContextPlugin
{
	public static IClientBuilder UseHttpContext(this IClientBuilder builder)
	{
		builder.Register(
			p => p.Use<AspNetCoreHttpContextMiddleware>(),
			p => p.AddSingleton<IHttpContextAccessor, HttpContextAccessor>()
		);
		return builder;
	}
}