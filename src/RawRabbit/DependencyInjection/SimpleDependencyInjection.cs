using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RawRabbit.DependencyInjection;

public class SimpleDependencyInjection : IDependencyRegister, IDependencyResolver
{
	private readonly Dictionary<Type, Func<IDependencyResolver, object>> _registrations = new();

	public IDependencyRegister AddTransient<TService, TImplementation>(Func<IDependencyResolver, TImplementation> instanceCreator) where TService : class where TImplementation : class, TService
	{
		if (this._registrations.ContainsKey(typeof(TService)))
		{
			this._registrations.Remove(typeof(TService));
		}

		this._registrations.Add(typeof(TService), instanceCreator);
		return this;
	}

	public IDependencyRegister AddTransient<TService, TImplementation>() where TImplementation : class, TService where TService : class
	{
		this.AddTransient<TService, TImplementation>(resolver => this.GetService(typeof(TImplementation)) as TImplementation);
		return this;
	}

	public IDependencyRegister AddSingleton<TService>(TService instance) where TService : class
	{
		this.AddTransient<TService, TService>(resolver => instance);
		return this;
	}

	public IDependencyRegister AddSingleton<TService, TImplementation>(Func<IDependencyResolver, TService> instanceCreator) where TImplementation : class, TService where TService : class
	{
		Lazy<TImplementation> lazy = new(() => (TImplementation)instanceCreator(this));
		this.AddTransient<TService,TImplementation>(resolver => lazy.Value);
		return this;
	}

	public IDependencyRegister AddSingleton<TService, TImplementation>() where TImplementation : class, TService where TService : class
	{
		Lazy<TImplementation> lazy = new(() => (TImplementation)this.CreateInstance(typeof(TImplementation), Enumerable.Empty<object>()));
		this.AddTransient<TService, TImplementation>(resolver => lazy.Value);
		return this;
	}

	public TService GetService<TService>(params object[] additional)
	{
		return (TService)this.GetService(typeof(TService), additional);
	}

	public object GetService(Type serviceType, params object[] additional)
	{
		Func<IDependencyResolver, object> creator;
		if (this._registrations.TryGetValue(serviceType, out creator))
		{
			return creator(this);
		}
		if (!serviceType.GetTypeInfo().IsAbstract)
		{
			return this.CreateInstance(serviceType, additional);
		}
		throw new InvalidOperationException("No registration for " + serviceType);
	}

	public bool TryGetService(Type serviceType, out object service, params object[] additional)
	{
		Func<IDependencyResolver, object> creator;
		if (this._registrations.TryGetValue(serviceType, out creator))
		{
			service = creator(this);
			return true;
		}
		if (!serviceType.GetTypeInfo().IsAbstract)
		{
			service = this.CreateInstance(serviceType, additional);
			return true;
		}
		service = null;
		return false;
	}

	private object CreateInstance(Type implementationType, IEnumerable<object> additional)
	{
		IEnumerable<Type> additionalTypes = additional.Select(a => a.GetType());
		ConstructorInfo[] ctors = implementationType
			.GetConstructors();
		ConstructorInfo ctor = ctors
			.Where(c => c.GetParameters().All(p => p.Attributes.HasFlag(ParameterAttributes.Optional) || additionalTypes.Contains(p.ParameterType) || this._registrations.Keys.Contains(p.ParameterType)))
			.OrderByDescending(c => c.GetParameters().Length)
			.FirstOrDefault();
		if (ctor == null)
		{
			throw new Exception($"Unable to find suitable constructor for {implementationType.Name}.");
		}
		object[] dependencies = ctor
			.GetParameters()
			.Select(parameter =>
			{
				if (additionalTypes.Contains(parameter.ParameterType))
				{
					return additional.First(a => a.GetType() == parameter.ParameterType);
				}
				object service;
				return this.TryGetService(parameter.ParameterType, out service) ? service : null;
			})
			.ToArray();
		return ctor.Invoke(dependencies);
	}
}