using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Channel.Abstraction;

namespace RawRabbit.Pipe.Middleware;

public class ChannelCreationOptions
{
	public Predicate<IPipeContext> CreatePredicate { get; set; }
	public Action<IPipeContext, IChannel> PostExecuteAction { get; set; }
	public Func<IChannelFactory, CancellationToken, Task<IChannel>> CreateFunc { get; set; }
}

public class ChannelCreationMiddleware : Middleware
{
	protected readonly IChannelFactory _channelFactory;
	protected readonly Predicate<IPipeContext> _createPredicate;
	protected readonly Func<IChannelFactory, CancellationToken, Task<IChannel>> _createFunc;
	protected readonly Action<IPipeContext, IChannel> _postExecuteAction;

	public ChannelCreationMiddleware(IChannelFactory channelFactory, ChannelCreationOptions options = null)
	{
		this._channelFactory = channelFactory;
		this._createPredicate = options?.CreatePredicate ?? (context => !context.Properties.ContainsKey(PipeKey.Channel));
		this._createFunc = options?.CreateFunc ?? ((factory, token) => factory.CreateChannelAsync(token));
		this._postExecuteAction = options?.PostExecuteAction;
	}

	public override async Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
	{
		if (this.ShouldCreateChannel(context))
		{
			IChannel channel = await this.GetOrCreateChannelAsync(this._channelFactory, token);
			context.Properties.TryAdd(PipeKey.Channel, channel);
			this._postExecuteAction?.Invoke(context, channel);
		}

		await this.Next.InvokeAsync(context, token);

	}

	protected virtual Task<IChannel> GetOrCreateChannelAsync(IChannelFactory factory, CancellationToken token)
	{
		return this._createFunc(factory, token);
	}

	protected virtual bool ShouldCreateChannel(IPipeContext context)
	{
		return this._createPredicate(context);
	}
}