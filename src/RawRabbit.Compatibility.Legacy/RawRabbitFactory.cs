using System;
using RawRabbit.Compatibility.Legacy.Configuration;
using RawRabbit.DependencyInjection;
using RawRabbit.Enrichers.MessageContext;
using RawRabbit.Enrichers.MessageContext.Context;
using RawRabbit.Instantiation;
using RawRabbitConfiguration = RawRabbit.Configuration.RawRabbitConfiguration;

namespace RawRabbit.Compatibility.Legacy;

public class RawRabbitFactory
{
	public static IBusClient<TMessageContext> CreateClient<TMessageContext>(RawRabbitOptions options = null)
		where TMessageContext : IMessageContext
	{
		options = options ?? new RawRabbitOptions();
		options.DependencyInjection = options.DependencyInjection ?? (_ => { });
		options.DependencyInjection += register => register.AddSingleton<IConfigurationEvaluator, ConfigurationEvaluator>();
		options.ClientConfiguration = options.ClientConfiguration ?? RawRabbitConfiguration.Local;
		options.Plugins = options.Plugins ?? (_ => { });
		options.Plugins += builder => builder
			.UseMessageContext(_ => new MessageContext { GlobalRequestId = Guid.NewGuid() })
			.UseContextForwarding();
		SimpleDependencyInjection simpleIoc = new();
		Instantiation.Disposable.BusClient client = Instantiation.RawRabbitFactory.CreateSingleton(options, simpleIoc, _ => simpleIoc);
		return new BusClient<TMessageContext>(client, simpleIoc.GetService<IConfigurationEvaluator>());
	}

	public static IBusClient CreateClient(RawRabbitOptions options = null)
	{
		options = options ?? new RawRabbitOptions();
		options.DependencyInjection = options.DependencyInjection ?? (_ => { });
		options.DependencyInjection += register => register.AddSingleton<IConfigurationEvaluator, ConfigurationEvaluator>();
		options.ClientConfiguration = options.ClientConfiguration ?? RawRabbitConfiguration.Local;
		options.Plugins = options.Plugins ?? (_ => { });
		options.Plugins += builder => builder
			.UseMessageContext(_ => new MessageContext {GlobalRequestId = Guid.NewGuid()})
			.UseContextForwarding();
		SimpleDependencyInjection simpleIoc = new();
		Instantiation.Disposable.BusClient client = Instantiation.RawRabbitFactory.CreateSingleton(options, simpleIoc, _ => simpleIoc);
		return new BusClient(client, simpleIoc.GetService<IConfigurationEvaluator>());
	}
}