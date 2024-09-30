using System;
using RawRabbit.DependencyInjection;
using RawRabbit.Pipe;

namespace RawRabbit.Instantiation;

public interface IClientBuilder
{
	void Register(Action<IPipeBuilder> pipe, Action<IDependencyRegister> ioc = null);
}

public class ClientBuilder : IClientBuilder
{
	public Action<IPipeBuilder> PipeBuilderAction { get; set; }
	public Action<IDependencyRegister> DependencyInjection { get; set; }

	public ClientBuilder()
	{
		this.PipeBuilderAction = _ => { };
		this.DependencyInjection = _ => { };
	}

	public void Register(Action<IPipeBuilder> pipe, Action<IDependencyRegister> ioc)
	{
		this.PipeBuilderAction += pipe;
		this.DependencyInjection += ioc;
	}
}
