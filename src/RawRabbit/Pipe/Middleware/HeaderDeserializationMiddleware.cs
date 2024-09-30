using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using RawRabbit.Logging;
using RawRabbit.Serialization;

namespace RawRabbit.Pipe.Middleware;

public class HeaderDeserializationOptions
{
	public Func<IPipeContext, BasicDeliverEventArgs> DeliveryArgsFunc { get; set; }
	public Func<IPipeContext, string> HeaderKeyFunc { get; set; }
	public Func<IPipeContext, Type> HeaderTypeFunc { get; set; }
	public Action<IPipeContext, object> ContextSaveAction { get; set; }
}

public class HeaderDeserializationMiddleware : StagedMiddleware
{
	protected readonly ISerializer _serializer;
	protected readonly Func<IPipeContext, BasicDeliverEventArgs> _deliveryArgsFunc;
	protected readonly Action<IPipeContext, object> _contextSaveAction;
	protected readonly Func<IPipeContext, string> _headerKeyFunc;
	protected readonly Func<IPipeContext, Type> _headerTypeFunc;
	private readonly ILog _logger = LogProvider.For<HeaderDeserializationMiddleware>();

	public HeaderDeserializationMiddleware(ISerializer serializer, HeaderDeserializationOptions options = null)
	{
		this._deliveryArgsFunc = options?.DeliveryArgsFunc ?? (context => context.GetDeliveryEventArgs());
		this._headerKeyFunc = options?.HeaderKeyFunc;
		this._contextSaveAction = options?.ContextSaveAction ?? ((context, item) => context.Properties.TryAdd(this._headerKeyFunc(context), item));
		this._headerTypeFunc = options?.HeaderTypeFunc ?? (_ =>typeof(object)) ;
		this._serializer = serializer;
	}

	public override async Task InvokeAsync(IPipeContext context, CancellationToken token = default)
	{
		object headerObject = this.GetHeaderObject(context);
		if (headerObject != null)
		{
			this.SaveInContext(context, headerObject);
		}
		await this.Next.InvokeAsync(context, token);
	}

	protected virtual void SaveInContext(IPipeContext context, object headerValue)
	{
		this._contextSaveAction?.Invoke(context, headerValue);
	}

	protected virtual object GetHeaderObject(IPipeContext context)
	{
		byte[] bytes = this.GetHeaderBytes(context);
		if (bytes == null)
		{
			return null;
		}
		Type type = this.GetHeaderType(context);
		return this._serializer.Deserialize(type, bytes);
	}

	protected virtual byte[] GetHeaderBytes(IPipeContext context)
	{
		string headerKey = this.GetHeaderKey(context);
		BasicDeliverEventArgs args = this.GetDeliveryArgs(context);
		if (string.IsNullOrEmpty(headerKey))
		{
			this._logger.Debug("Key {headerKey} not found.", headerKey);
			return null;
		}
		if (args == null)
		{
			this._logger.Debug("DeliveryEventArgs not found.");
			return null;
		}
		if (args.BasicProperties.Headers == null || !args.BasicProperties.Headers.ContainsKey(headerKey))
		{
			this._logger.Info("BasicProperties Header does not contain {headerKey}", headerKey);
			return null;
		}

		return args.BasicProperties.Headers.TryGetValue(headerKey, out object headerBytes)
			? headerBytes as byte[]
			: null;
	}

	protected virtual BasicDeliverEventArgs GetDeliveryArgs(IPipeContext context)
	{
		BasicDeliverEventArgs args = this._deliveryArgsFunc(context);
		if (args == null)
		{
			this._logger.Warn("Unable to extract delivery args from Pipe Context.");
		}
		return args;
	}

	protected virtual string GetHeaderKey(IPipeContext context)
	{
		string key = this._headerKeyFunc(context);
		if (key == null)
		{
			this._logger.Warn("Unable to extract header key from Pipe context.");
		}
		else
		{
			this._logger.Debug("Trying to extract {headerKey} from header", key);
		}
		return key;
	}

	protected virtual Type GetHeaderType(IPipeContext context)
	{
		Type type = this._headerTypeFunc(context);
		if (type == null)
		{
			this._logger.Warn("Unable to extract header type from Pipe context.");
		}
		else
		{
			this._logger.Debug("Header type extracted: '{headerType}'", type.Name);
		}
		return type;
	}

	public override string StageMarker => Pipe.StageMarker.MessageReceived;
}
