using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Serialization;

namespace RawRabbit.Pipe.Middleware;

public class HeaderSerializationOptions
{
	public Predicate<IPipeContext> ExecutePredicate { get; set; }
	public Func<IPipeContext, IBasicProperties> BasicPropsFunc { get; set; }
	public Func<IPipeContext, object> RetrieveItemFunc { get; set; }
	public Func<IPipeContext, object> CreateItemFunc { get; set; }
	public Func<IPipeContext, string> HeaderKeyFunc { get; set; }
}

public class HeaderSerializationMiddleware : StagedMiddleware
{
	protected readonly ISerializer _serializer;
	protected readonly Func<IPipeContext, IBasicProperties> _basicPropsFunc;
	protected readonly Func<IPipeContext, object> _retrieveItemFunc;
	protected readonly Func<IPipeContext, object> _createItemFunc;
	protected readonly Predicate<IPipeContext> _executePredicate;
	protected readonly Func<IPipeContext, string> _headerKeyFunc;

	public HeaderSerializationMiddleware(ISerializer serializer, HeaderSerializationOptions options = null)
	{
		this._serializer = serializer;
		this._executePredicate = options?.ExecutePredicate ?? (_ => true);
		this._basicPropsFunc = options?.BasicPropsFunc ?? (context => context.GetBasicProperties());
		this._retrieveItemFunc = options?.RetrieveItemFunc ?? (_ => null);
		this._createItemFunc = options?.CreateItemFunc ?? (_ => null);
		this._createItemFunc = options?.CreateItemFunc ?? (_ => null);
		this._headerKeyFunc = options?.HeaderKeyFunc ?? (_ => null);
	}

	public override async Task InvokeAsync(IPipeContext context, CancellationToken token = default)
	{
		if (!this.ShouldExecute(context))
		{
			await this.Next.InvokeAsync(context, token);
			return;
		}
		IBasicProperties properties = this.GetBasicProperties(context);
		string headerKey = this.GetHeaderKey(context);
		if (properties.Headers != null && properties.Headers.ContainsKey(headerKey))
		{
			await this.Next.InvokeAsync(context, token);
			return;
		}

		object item = this.GetHeaderItem(context) ?? this.CreateHeaderItem(context);
		byte[] serializedItem = this.SerializeItem(item);
		properties.Headers.TryAdd(headerKey, serializedItem);

		await this.Next.InvokeAsync(context, token);
	}

	protected virtual bool ShouldExecute(IPipeContext context)
	{
		return this._executePredicate.Invoke(context);
	}

	protected virtual IBasicProperties GetBasicProperties(IPipeContext context)
	{
		return this._basicPropsFunc?.Invoke(context);
	}

	protected virtual object GetHeaderItem(IPipeContext context)
	{
		return this._retrieveItemFunc?.Invoke(context);
	}

	protected virtual object CreateHeaderItem(IPipeContext context)
	{
		return this._createItemFunc?.Invoke(context);
	}

	protected virtual byte[] SerializeItem(object item)
	{
		return this._serializer.Serialize(item);
	}

	protected virtual string GetHeaderKey(IPipeContext context)
	{
		return this._headerKeyFunc?.Invoke(context);
	}

	public override string StageMarker => Pipe.StageMarker.BasicPropertiesCreated;
}
