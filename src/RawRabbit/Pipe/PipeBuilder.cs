using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using RawRabbit.DependencyInjection;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Pipe;

public interface IPipeBuilder
{
	IPipeBuilder Use(Func<IPipeContext, Func<Task>, Task> handler);
	IPipeBuilder Use<TMiddleWare>(params object[] args) where TMiddleWare : Middleware.Middleware;
	IPipeBuilder Replace<TCurrent, TNew>(Predicate<object[]> predicate = null, Func<object[], object[]> argsFunc = null) where TCurrent: Middleware.Middleware where TNew : Middleware.Middleware;
	IPipeBuilder Replace<TCurrent, TNew>(Predicate<object[]> predicate = null, params object[] args) where TCurrent: Middleware.Middleware where TNew : Middleware.Middleware;
	IPipeBuilder Remove<TMiddleware>(Predicate<object[]> predicate = null) where TMiddleware : Middleware.Middleware;
}

public interface IExtendedPipeBuilder : IPipeBuilder
{
	Middleware.Middleware Build();
}

public class PipeBuilder : IExtendedPipeBuilder
{
	private readonly IDependencyResolver _resolver;
	protected readonly List<MiddlewareInfo> _pipe;
	private readonly Action<IPipeBuilder> _additional;

	public PipeBuilder(IDependencyResolver resolver)
	{
		this._resolver = resolver;
		this._additional = this._resolver.GetService<Action<IPipeBuilder>>() ?? (builder => {});
		this._pipe = new List<MiddlewareInfo>();
	}

	public IPipeBuilder Use(Func<IPipeContext, Func<Task>, Task> handler)
	{
		this.Use<UseHandlerMiddleware>(handler);
		return this;
	}

	public IPipeBuilder Use<TMiddleWare>(params object[] args) where TMiddleWare : Middleware.Middleware
	{
		this._pipe.Add(new MiddlewareInfo
		{
			Type = typeof(TMiddleWare),
			ConstructorArgs = args
		});
		return this;
	}

	public IPipeBuilder Replace<TCurrent, TNew>(Predicate<object[]> predicate = null, params object[] args) where TCurrent : Middleware.Middleware where TNew : Middleware.Middleware
	{
		return this.Replace<TCurrent, TNew>(predicate, oldArgs => args);
	}

	public IPipeBuilder Replace<TCurrent, TNew>(Predicate<object[]> predicate = null, Func<object[], object[]> argsFunc = null) where TCurrent : Middleware.Middleware where TNew : Middleware.Middleware
	{
		predicate = predicate ?? (objects => true);
		IEnumerable<MiddlewareInfo> matching = this._pipe.Where(c => c.Type == typeof(TCurrent) && predicate(c.ConstructorArgs));
		foreach (MiddlewareInfo middlewareInfo in matching)
		{
			object[] args = argsFunc?.Invoke(middlewareInfo.ConstructorArgs);
			middlewareInfo.Type = typeof(TNew);
			middlewareInfo.ConstructorArgs = args;
		}
		return this;
	}

	public IPipeBuilder Remove<TMiddleware>(Predicate<object[]> predicate = null) where TMiddleware : Middleware.Middleware
	{
		predicate = predicate ?? (objects => true);
		List<MiddlewareInfo> matching = this._pipe.Where(c => c.Type == typeof(TMiddleware) && predicate(c.ConstructorArgs)).ToList();
		foreach (MiddlewareInfo match in matching)
		{
			this._pipe.Remove(match);
		}
		return this;
	}

	public virtual Middleware.Middleware Build()
	{
		this._additional.Invoke(this);

		List<MiddlewareInfo> stagedMwInfo = this._pipe
			.Where(info => typeof(StagedMiddleware).GetTypeInfo().IsAssignableFrom(info.Type))
			.ToList();
		List<StagedMiddleware> stagedMiddleware = stagedMwInfo
			.Select(this.CreateInstance)
			.OfType<StagedMiddleware>()
			.ToList();

		List<Middleware.Middleware> sortedMws = new();
		foreach (MiddlewareInfo mwInfo in this._pipe)
		{
			if (typeof(StagedMiddleware).GetTypeInfo().IsAssignableFrom(mwInfo.Type))
			{
				continue;
			}

			Middleware.Middleware middleware = this.CreateInstance(mwInfo);
			sortedMws.Add(middleware);

			StageMarkerMiddleware stageMarkerMw = middleware as StageMarkerMiddleware;
			if (stageMarkerMw != null)
			{
				List<Middleware.Middleware> thisStageMws = stagedMiddleware
					.Where(mw => mw.StageMarker == stageMarkerMw._stage)
					.ToList<Middleware.Middleware>();
				sortedMws.AddRange(thisStageMws);
			}
		}

		return this.Build(sortedMws);
	}

	protected virtual Middleware.Middleware Build(IList<Middleware.Middleware> middlewares)
	{
		Middleware.Middleware next = new NoOpMiddleware();
		for (int i = middlewares.Count - 1; i >= 0; i--)
		{
			Middleware.Middleware cancellation = new CancellationMiddleware();
			Middleware.Middleware current = middlewares[i];
			current.Next = cancellation;
			cancellation.Next = next;
			next = current;
		}
		return next;
	}

	protected virtual Middleware.Middleware CreateInstance(MiddlewareInfo middlewareInfo)
	{
		return this._resolver.GetService(middlewareInfo.Type, middlewareInfo.ConstructorArgs) as Middleware.Middleware;
	}
}