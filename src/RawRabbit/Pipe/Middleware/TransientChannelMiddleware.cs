using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Logging;

namespace RawRabbit.Pipe.Middleware;

public class TransientChannelMiddleware : Middleware
{
	protected readonly IChannelFactory _channelFactory;
	private readonly ILog _logger = LogProvider.For<TransientChannelMiddleware>();

	public TransientChannelMiddleware(IChannelFactory channelFactory)
	{
		this._channelFactory = channelFactory;
	}

	public override async Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
	{
		using (IChannel channel = await this.CreateChannelAsync(context, token))
		{
			this._logger.Debug("Adding channel {channelNumber} to Execution Context.", channel.ChannelNumber);
			context.Properties.Add(PipeKey.TransientChannel, channel);
			await this.Next.InvokeAsync(context, token);
		}
	}

	protected virtual Task<IChannel> CreateChannelAsync(IPipeContext context, CancellationToken ct)
	{
		return this._channelFactory.CreateChannelAsync(ct);
	}
}