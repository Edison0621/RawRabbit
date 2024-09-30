﻿using System;
using Autofac;

namespace RawRabbit.DependencyInjection.Autofac;

public class ContainerBuilderAdapter : IDependencyRegister
{
	private readonly ContainerBuilder _builder;

	public ContainerBuilderAdapter(ContainerBuilder builder)
	{
		this._builder = builder;
	}

	public IDependencyRegister AddTransient<TService, TImplementation>(Func<IDependencyResolver, TImplementation> instanceCreator) where TService : class where TImplementation : class, TService
	{
		this._builder
			.Register<TImplementation>(context => instanceCreator(new ComponentContextAdapter(context.Resolve<IComponentContext>())))
			.As<TService>()
			.InstancePerDependency();
		return this;
	}

	public IDependencyRegister AddTransient<TService, TImplementation>() where TService : class where TImplementation : class, TService
	{
		this._builder
			.RegisterType<TImplementation>()
			.As<TService>()
			.InstancePerDependency();
		return this;
	}

	public IDependencyRegister AddSingleton<TService>(TService instance) where TService : class
	{
		this._builder
			.Register<TService>(_ => instance)
			.As<TService>()
			.SingleInstance();
		return this;
	}

	public IDependencyRegister AddSingleton<TService, TImplementation>(Func<IDependencyResolver, TService> instanceCreator) where TService : class where TImplementation : class, TService
	{
		this._builder
			.Register<TService>(context => instanceCreator(new ComponentContextAdapter(context.Resolve<IComponentContext>())))
			.As<TService>()
			.SingleInstance();
		return this;
	}

	public IDependencyRegister AddSingleton<TService, TImplementation>() where TService : class where TImplementation : class, TService
	{
		this._builder
			.RegisterType<TImplementation>()
			.As<TService>()
			.SingleInstance();
		return this;
	}
}