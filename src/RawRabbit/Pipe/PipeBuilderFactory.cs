using System;
using System.Collections.Concurrent;
using RawRabbit.DependencyInjection;

namespace RawRabbit.Pipe;

public interface IPipeBuilderFactory
{
	IExtendedPipeBuilder Create();
	Middleware.Middleware Create(Action<IPipeBuilder> pipe);
}

public class PipeBuilderFactory : IPipeBuilderFactory
{
	private readonly IDependencyResolver _resolver;

	public PipeBuilderFactory(IDependencyResolver resolver)
	{
		this._resolver = resolver;
	}

	public IExtendedPipeBuilder Create()
	{
		return new PipeBuilder(this._resolver);
	}

	public Middleware.Middleware Create(Action<IPipeBuilder> pipe)
	{
		IExtendedPipeBuilder builder = this.Create();
		pipe(builder);
		return builder.Build();
	}
}

public class CachedPipeBuilderFactory : IPipeBuilderFactory
{
	private readonly PipeBuilderFactory _fallback;
	private readonly ConcurrentDictionary<Action<IPipeBuilder>, Middleware.Middleware> _pipeCache;

	public CachedPipeBuilderFactory(IDependencyResolver resolver)
	{
		this._fallback = new PipeBuilderFactory(resolver);
		this._pipeCache = new ConcurrentDictionary<Action<IPipeBuilder>, Middleware.Middleware>();
	}

	public IExtendedPipeBuilder Create()
	{
		return this._fallback.Create();
	}

	public Middleware.Middleware Create(Action<IPipeBuilder> pipe)
	{
		Middleware.Middleware result = this._pipeCache.GetOrAdd(pipe, action => this._fallback.Create(action));
		return result;
	}
}