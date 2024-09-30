using System;
using System.Linq;
using System.Reflection;
using MessagePack;
using RawRabbit.Serialization;

namespace RawRabbit.Enrichers.MessagePack;

internal class MessagePackSerializerWorker : ISerializer
{
	public string ContentType => "application/x-messagepack";
	private readonly MethodInfo _deserializeType;
	private readonly MethodInfo _serializeType;

	public MessagePackSerializerWorker(MessagePackFormat format)
	{
		Type tp = format == MessagePackFormat.Lz4Compression ? typeof(LZ4MessagePackSerializer) : typeof(MessagePackSerializer);

		this._deserializeType = tp
			.GetMethod(nameof(MessagePackSerializer.Deserialize), new[] { typeof(byte[]) });
		this._serializeType = tp
			.GetMethods()
			.FirstOrDefault(s => s.Name == nameof(MessagePackSerializer.Serialize) && s.ReturnType == typeof(byte[]));
	}

	public byte[] Serialize(object obj)
	{
		if (obj == null)
			throw new ArgumentNullException();

		return (byte[])this._serializeType
			.MakeGenericMethod(obj.GetType())
			.Invoke(null, new[] { obj });
	}

	public object Deserialize(Type type, ReadOnlyMemory<byte>? bytes)
	{
		return this._deserializeType.MakeGenericMethod(type)
			.Invoke(null, new object[] { bytes });
	}

	public TType Deserialize<TType>(ReadOnlyMemory<byte>? bytes)
	{
		return MessagePackSerializer.Deserialize<TType>(bytes?.ToArray());
	}
}