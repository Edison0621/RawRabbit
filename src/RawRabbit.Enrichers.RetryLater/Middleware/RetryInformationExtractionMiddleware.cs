using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using RawRabbit.Common;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Middleware;

public class RetryInformationExtractionOptions
{
	public Func<IPipeContext, BasicDeliverEventArgs> DeliveryArgsFunc { get; set; }
}

public class RetryInformationExtractionMiddleware : StagedMiddleware
{
	private readonly IRetryInformationProvider _retryProvider;
	protected readonly Func<IPipeContext, BasicDeliverEventArgs> _deliveryArgsFunc;
	public override string StageMarker => Pipe.StageMarker.MessageReceived;

	public RetryInformationExtractionMiddleware(IRetryInformationProvider retryProvider, RetryInformationExtractionOptions options = null)
	{
		this._retryProvider = retryProvider;
		this._deliveryArgsFunc = options?.DeliveryArgsFunc ?? (context => context.GetDeliveryEventArgs());
	}

	public override Task InvokeAsync(IPipeContext context, CancellationToken token = default)
	{
		RetryInformation retryInfo = this.GetRetryInformation(context);
		this.AddToPipeContext(context, retryInfo);
		return this.Next.InvokeAsync(context, token);
	}

	protected virtual BasicDeliverEventArgs GetDeliveryEventArgs(IPipeContext context)
	{
		return this._deliveryArgsFunc?.Invoke(context);
	}

	protected virtual RetryInformation GetRetryInformation(IPipeContext context)
	{
		BasicDeliverEventArgs devlieryArgs = this.GetDeliveryEventArgs(context);
		return this._retryProvider.Get(devlieryArgs);
	}

	protected virtual void AddToPipeContext(IPipeContext context, RetryInformation retryInfo)
	{
		context.AddRetryInformation(retryInfo);
	}
}