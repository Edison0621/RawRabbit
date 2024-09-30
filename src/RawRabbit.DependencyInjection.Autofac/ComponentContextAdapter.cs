using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Autofac.Core;

namespace RawRabbit.DependencyInjection.Autofac;

public class ComponentContextAdapter : IDependencyResolver
{
	public static ComponentContextAdapter Create(IComponentContext context)
	{
		return new ComponentContextAdapter(context);
	}

	private readonly IComponentContext _context;

	public ComponentContextAdapter(IComponentContext context)
	{
		this._context = context;
	}

	public TService GetService<TService>(params object[] additional)
	{
		return (TService)this.GetService(typeof(TService), additional);
	}

	public object GetService(Type serviceType, params object[] additional)
	{
		List<Parameter> parameters = additional
			.Select(a => new TypedParameter(a.GetType(), a))
			.ToList<Parameter>();
		return this._context.Resolve(serviceType, parameters);
	}
}