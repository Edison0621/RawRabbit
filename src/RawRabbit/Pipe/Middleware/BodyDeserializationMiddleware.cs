using System;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Logging;
using RawRabbit.Serialization;

namespace RawRabbit.Pipe.Middleware;

public class MessageDeserializationOptions
{
	public Func<IPipeContext, Type> BodyTypeFunc { get; set; }
	public Func<IPipeContext, string> BodyContentTypeFunc { get; set; }
	public Func<IPipeContext, bool> ActivateContentTypeCheck{ get; set; }
	public Func<IPipeContext, ReadOnlyMemory<byte>?> BodyFunc { get; set; }
	public Action<IPipeContext, object> PersistAction { get; set; }
}

public class BodyDeserializationMiddleware : Middleware
{
	protected readonly ISerializer _serializer;
	protected readonly Func<IPipeContext, Type> _messageTypeFunc;
	protected readonly Func<IPipeContext, ReadOnlyMemory<byte>?> _bodyBytesFunc;
	protected Func<IPipeContext, string> BodyContentTypeFunc { get; set; }
	protected Func<IPipeContext, bool> ActivateContentTypeCheck { get; set; }
	protected readonly Action<IPipeContext, object> _persistAction;
	private readonly ILog _logger = LogProvider.For<BodyDeserializationMiddleware>();

	public BodyDeserializationMiddleware(ISerializer serializer, MessageDeserializationOptions options = null)
	{
		this._serializer = serializer;
		this._messageTypeFunc = options?.BodyTypeFunc ?? (context => context.GetMessageType());
		this._bodyBytesFunc = options?.BodyFunc ?? (context =>context.GetDeliveryEventArgs()?.Body);
		this._persistAction = options?.PersistAction ?? ((context, msg) => context.Properties.TryAdd(PipeKey.Message, msg));
		this.BodyContentTypeFunc = options?.BodyContentTypeFunc ?? (context => context.GetDeliveryEventArgs()?.BasicProperties.ContentType);
		this.ActivateContentTypeCheck = options?.ActivateContentTypeCheck ?? (context => context.GetContentTypeCheckActivated());
	}

	public override Task InvokeAsync(IPipeContext context, CancellationToken token = default)
	{
		if (this.ContentTypeCheckActivated(context))
		{
			string msgContentType = this.GetMessageContentType(context);
			if (!this.CanSerializeMessage(msgContentType))
			{
				throw new SerializationException($"Registered serializer supports {this._serializer.ContentType}, received message uses {msgContentType}.");
			}
		}
		object message = this.GetMessage(context);
		this.SaveInContext(context, message);
		return this.Next.InvokeAsync(context, token);
	}

	protected virtual bool ContentTypeCheckActivated(IPipeContext context)
	{
		return this.ActivateContentTypeCheck?.Invoke(context) ?? false;
	}

	protected virtual bool CanSerializeMessage(string msgContentType)
	{
		if (string.IsNullOrEmpty(msgContentType))
		{
			this._logger.Debug("Received message has no content type defined. Assuming it can be processed.");
			return true;
		}
		return string.Equals(msgContentType, this._serializer.ContentType, StringComparison.CurrentCultureIgnoreCase);
	}

	protected virtual string GetMessageContentType(IPipeContext context)
	{
		return this.BodyContentTypeFunc?.Invoke(context);
	}

	protected virtual object GetMessage(IPipeContext context)
	{
		ReadOnlyMemory<byte>? bodyBytes = this.GetBodyBytes(context);
		Type messageType = this.GetMessageType(context);
		return this._serializer.Deserialize(messageType, bodyBytes);
	}

	protected virtual Type GetMessageType(IPipeContext context)
	{
		Type msgType = this._messageTypeFunc(context);
		if (msgType == null)
		{
			this._logger.Warn("Unable to find message type in Pipe context.");
		}
		return msgType;
	}

	protected virtual ReadOnlyMemory<byte>? GetBodyBytes(IPipeContext context)
	{
		ReadOnlyMemory<byte>? bodyBytes = this._bodyBytesFunc(context);
		if (bodyBytes == null)
		{
			this._logger.Warn("Unable to find Body (bytes) in Pipe context");
		}
		return bodyBytes;
	}

	protected virtual void SaveInContext(IPipeContext context, object message)
	{
		if (this._persistAction == null)
		{
			this._logger.Warn("No persist action defined. Message will not be saved in Pipe context.");
		}

		this._persistAction?.Invoke(context, message);
	}
}

public static class BodyDeserializationMiddlewareExtensions
{
	private const string ContentTypeCheck = "Deserialization:ContentType:Check";

	public static TPipeContext UseContentTypeCheck<TPipeContext>(this TPipeContext context, bool check = true) where TPipeContext : IPipeContext
	{
		context.Properties.TryAdd(ContentTypeCheck, check);
		return context;
	}

	public static bool GetContentTypeCheckActivated(this IPipeContext context)
	{
		return context.Get<bool>(ContentTypeCheck);
	}
}