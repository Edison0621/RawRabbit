using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Logging;
using RawRabbit.Operations.Respond.Core;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Respond.Middleware;

public class ReplyToExtractionOptions
{
	public Func<IPipeContext, BasicDeliverEventArgs> DeliveryArgsFunc { get; set; }
	public Func<BasicDeliverEventArgs, PublicationAddress> ReplyToFunc { get; set; }
	public Action<IPipeContext, PublicationAddress> ContextSaveAction { get; set; }
}

public class ReplyToExtractionMiddleware : Pipe.Middleware.Middleware
{
	protected readonly Func<IPipeContext, BasicDeliverEventArgs> _deliveryArgsFunc;
	protected readonly Func<BasicDeliverEventArgs, PublicationAddress> _replyToFunc;
	protected readonly Action<IPipeContext, PublicationAddress> _contextSaveAction;
	private readonly ILog _logger = LogProvider.For<ReplyToExtractionMiddleware>();

	public ReplyToExtractionMiddleware(ReplyToExtractionOptions options = null)
	{
		this._contextSaveAction = options?.ContextSaveAction ?? ((ctx, addr) => ctx.Properties.Add(RespondKey.PublicationAddress, addr));
		this._deliveryArgsFunc = options?.DeliveryArgsFunc ?? (ctx => ctx.GetDeliveryEventArgs());
		this._replyToFunc = options?.ReplyToFunc ?? (args =>
			args.BasicProperties.ReplyToAddress ?? new PublicationAddress(ExchangeType.Direct, string.Empty, args.BasicProperties.ReplyTo));
	}

	public override Task InvokeAsync(IPipeContext context, CancellationToken token = default)
	{
		BasicDeliverEventArgs args = this.GetDeliveryArgs(context);
		PublicationAddress replyTo = this.GetReplyTo(args);
		this.SaveInContext(context, replyTo);
		return this.Next.InvokeAsync(context, token);
	}

	protected virtual BasicDeliverEventArgs GetDeliveryArgs(IPipeContext context)
	{
		BasicDeliverEventArgs args = this._deliveryArgsFunc(context);
		if (args == null)
		{
			this._logger.Warn("Delivery args not found in Pipe context.");
		}
		return args;
	}

	protected virtual PublicationAddress GetReplyTo(BasicDeliverEventArgs args)
	{
		PublicationAddress replyTo = this._replyToFunc(args);
		if (replyTo == null)
		{
			this._logger.Warn("Reply to address not found in Pipe context.");
		}
		else
		{
			//TODO args.BasicProperties.ReplyTo = replyTo.RoutingKey;
			this._logger.Info("Using reply address with exchange {exchangeName} and routing key '{routingKey}'", replyTo.ExchangeName, replyTo.RoutingKey);
		}
		return replyTo;
	}

	protected virtual void SaveInContext(IPipeContext context, PublicationAddress replyTo)
	{
		if (this._contextSaveAction == null)
		{
			this._logger.Warn("No context save action found. Reply to address will not be saved.");
		}

		this._contextSaveAction?.Invoke(context, replyTo);
	}
}