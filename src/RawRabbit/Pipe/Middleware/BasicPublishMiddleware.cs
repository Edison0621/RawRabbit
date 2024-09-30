using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Logging;

namespace RawRabbit.Pipe.Middleware;

public class BasicPublishOptions
{
	public Func<IPipeContext, IChannel> ChannelFunc { get; set; }
	public Func<IPipeContext, string> ExchangeNameFunc { get; set; }
	public Func<IPipeContext, string> RoutingKeyFunc { get; set; }
	public Func<IPipeContext, bool> MandatoryFunc { get; set; }
	public Func<IPipeContext, BasicProperties> BasicPropsFunc { get; set; }
	public Func<IPipeContext, byte[]> BodyFunc { get; set; }
}

public class BasicPublishMiddleware : Middleware
{
	protected readonly IExclusiveLock _exclusive;
	protected readonly Func<IPipeContext, IChannel> _channelFunc;
	protected readonly Func<IPipeContext, string> _exchangeNameFunc;
	protected readonly Func<IPipeContext, string> _routingKeyFunc;
	protected readonly Func<IPipeContext, bool> _mandatoryFunc;
	protected readonly Func<IPipeContext, BasicProperties> _basicPropsFunc;
	protected readonly Func<IPipeContext, byte[]> _bodyFunc;
	private ILog _logger = LogProvider.For<BasicPublishMiddleware>();

	public BasicPublishMiddleware(IExclusiveLock exclusive, BasicPublishOptions options = null)
	{
		this._exclusive = exclusive;
		this._channelFunc = options?.ChannelFunc ?? (context => context.GetTransientChannel());
		this._exchangeNameFunc = options?.ExchangeNameFunc ?? (c => c.GetBasicPublishConfiguration()?.ExchangeName);
		this._routingKeyFunc = options?.RoutingKeyFunc ?? (c => c.GetBasicPublishConfiguration()?.RoutingKey);
		this._mandatoryFunc = options?.MandatoryFunc ?? (c => c.GetBasicPublishConfiguration()?.Mandatory ?? false);
		this._basicPropsFunc = options?.BasicPropsFunc ?? (c => c.GetBasicProperties());
		this._bodyFunc = options?.BodyFunc ?? (c => c.GetBasicPublishConfiguration()?.Body);
	}

	public override async Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
	{
		IChannel channel = this.GetOrCreateChannel(context);
		string exchangeName = this.GetExchangeName(context);
		string routingKey = this.GetRoutingKey(context);
		bool mandatory = this.GetMandatoryOptions(context);
		BasicProperties basicProps = this.GetBasicProps(context);
		byte[] body = this.GetMessageBody(context);

		this._logger.Info("Performing basic publish with routing key {routingKey} on exchange {exchangeName}.", routingKey, exchangeName);

		this.ExclusiveExecute(channel, async c => await this.BasicPublishAsync(
				channel: c,
				exchange: exchangeName,
				routingKey: routingKey,
				mandatory: mandatory,
				basicProps: basicProps,
				body: body,
				context: context
			), token
		);

		await this.Next.InvokeAsync(context, token);
	}

	protected virtual async Task BasicPublishAsync(IChannel channel, string exchange, string routingKey, bool mandatory, BasicProperties basicProps, ReadOnlyMemory<byte> body, IPipeContext context)
	{
		await channel.BasicPublishAsync(
			exchange: exchange,
			routingKey: routingKey,
			mandatory: mandatory,
			basicProperties: basicProps,
			body: body
		);
	}

	protected virtual void ExclusiveExecute(IChannel channel, Action<IChannel> action, CancellationToken token)
	{
		this._exclusive.Execute(channel, action, token);
	}

	protected virtual byte[] GetMessageBody(IPipeContext context)
	{
		byte[] body = this._bodyFunc(context);
		if (body == null)
		{
			this._logger.Warn("No body found in the Pipe context.");
		}
		return body;
	}

	protected virtual BasicProperties GetBasicProps(IPipeContext context)
	{
		BasicProperties props = this._basicPropsFunc(context);
		if (props == null)
		{
			this._logger.Warn("No basic properties found in the Pipe context.");
		}
		return props;
	}

	protected virtual bool GetMandatoryOptions(IPipeContext context)
	{
		return this._mandatoryFunc(context);
	}

	protected virtual string GetRoutingKey(IPipeContext context)
	{
		string routingKey = this._routingKeyFunc(context);
		if (routingKey == null)
		{
			this._logger.Warn("No routing key found in the Pipe context.");
		}
		return routingKey;
	}

	protected virtual string GetExchangeName(IPipeContext context)
	{
		string exchange = this._exchangeNameFunc(context);
		if (exchange == null)
		{
			this._logger.Warn("No exchange name found in the Pipe context.");
		}
		return exchange;
	}

	protected virtual IChannel GetOrCreateChannel(IPipeContext context)
	{
		IChannel channel = this._channelFunc(context);
		if (channel == null)
		{
			this._logger.Warn("No channel to perform publish found.");
		}
		return channel;
	}
}