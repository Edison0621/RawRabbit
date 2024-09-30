using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit;

public class BusClient : IBusClient
{
	private readonly IPipeBuilderFactory _pipeBuilderFactory;
	private readonly IPipeContextFactory _contextFactory;

	public BusClient(IPipeBuilderFactory pipeBuilderFactory, IPipeContextFactory contextFactory, IChannelFactory factory)
	{
		this._pipeBuilderFactory = pipeBuilderFactory;
		this._contextFactory = contextFactory;
	}

	public async Task<IPipeContext> InvokeAsync(Action<IPipeBuilder> pipeCfg, Action<IPipeContext> contextCfg = null, CancellationToken token = default(CancellationToken))
	{
		Middleware pipe = this._pipeBuilderFactory.Create(pipeCfg);
		IPipeContext context = this._contextFactory.CreateContext();
		contextCfg?.Invoke(context);
		await pipe.InvokeAsync(context, token);
		return context;
	}
}