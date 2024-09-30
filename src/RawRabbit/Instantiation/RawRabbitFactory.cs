using System;
using RawRabbit.DependencyInjection;

namespace RawRabbit.Instantiation;

public class RawRabbitFactory
{
	public static Disposable.BusClient CreateSingleton(RawRabbitOptions options = null)
	{
		SimpleDependencyInjection ioc = new();
		return CreateSingleton(options, ioc, register => ioc);
	}

	public static Disposable.BusClient CreateSingleton(RawRabbitOptions options, IDependencyRegister register, Func<IDependencyRegister, IDependencyResolver> resolverFunc)
	{
		InstanceFactory factory = CreateInstanceFactory(options, register, resolverFunc);
		return new Disposable.BusClient(factory);
	}

	public static InstanceFactory CreateInstanceFactory(RawRabbitOptions options = null)
	{
		SimpleDependencyInjection ioc = new();
		return CreateInstanceFactory(options, ioc, register => ioc);
	}

	public static InstanceFactory CreateInstanceFactory(RawRabbitOptions options, IDependencyRegister register, Func<IDependencyRegister, IDependencyResolver> resolverFunc)
	{
		register.AddRawRabbit(options);
		IDependencyResolver resolver = resolverFunc(register);
		return new InstanceFactory(resolver);
	}
}