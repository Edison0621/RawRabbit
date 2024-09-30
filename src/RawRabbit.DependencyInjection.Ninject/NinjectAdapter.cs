using System;
using System.Linq;
using Ninject;
using Ninject.Activation;
using Ninject.Parameters;

namespace RawRabbit.DependencyInjection.Ninject;

public class NinjectAdapter : IDependencyResolver
{
	private readonly IContext _context;

	public NinjectAdapter(IContext context)
	{
		this._context = context;
	}

	public TService GetService<TService>(params object[] additional)
	{
		return (TService)this.GetService(typeof(TService), additional);
	}

	public object GetService(Type serviceType, params object[] additional)
	{
		IParameter[] args = additional
			.Select(a => new TypeMatchingConstructorArgument(a.GetType(), (context, target) => a))
			.ToArray<IParameter>();
		return this._context.Kernel.Get(serviceType, args);
	}
}