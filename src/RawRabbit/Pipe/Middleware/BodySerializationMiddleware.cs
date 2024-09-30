using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Serialization;

namespace RawRabbit.Pipe.Middleware;

public class MessageSerializationOptions
{
	public Func<IPipeContext, object> MessageFunc { get; set; }
	public Action<IPipeContext, byte[]> PersistAction { get; set; }
}

public class BodySerializationMiddleware : Middleware
{
	protected readonly Func<IPipeContext, object> _msgFunc;
	protected readonly Action<IPipeContext, byte[]> _persistAction;
	private readonly ISerializer _serializer;

	public BodySerializationMiddleware(ISerializer serializer, MessageSerializationOptions options = null)
	{
		this._serializer = serializer;
		this._msgFunc = options?.MessageFunc ?? (context => context.GetMessage());
		this._persistAction = options?.PersistAction ?? ((c, s) => c.Properties.TryAdd(PipeKey.SerializedMessage, s));
	}

	public override Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
	{
		object message = this.GetMessage(context);
		byte[] serialized = this.SerializeMessage(message);
		this.AddSerializedMessageToContext(context, serialized);
		return this.Next.InvokeAsync(context, token);
	}

	protected virtual object GetMessage(IPipeContext context)
	{
		return this._msgFunc(context);
	}

	protected virtual byte[] SerializeMessage(object message)
	{
		return this._serializer.Serialize(message);
	}

	protected virtual void AddSerializedMessageToContext(IPipeContext context, byte[] serialized)
	{
		this._persistAction?.Invoke(context, serialized);
	}
}