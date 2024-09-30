using System;
using System.Linq;
using System.Reflection;
using RawRabbit.Serialization;
using ZeroFormatter;

namespace RawRabbit.Enrichers.ZeroFormatter;

internal class ZeroFormatterSerializerWorker : ISerializer
{
	public string ContentType => "application/x-zeroformatter";
	private readonly MethodInfo _deserializeType;
	private readonly MethodInfo _serializeType;

	public ZeroFormatterSerializerWorker()
	{
		this._deserializeType = typeof(ZeroFormatterSerializer)
			.GetMethod(nameof(ZeroFormatterSerializer.Deserialize), new[] { typeof(byte[]) });
		this._serializeType = typeof(ZeroFormatterSerializer)
			.GetMethods()
			.FirstOrDefault(s => s.Name == nameof(ZeroFormatterSerializer.Serialize) && s.ReturnType == typeof(byte[]));
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
		return ZeroFormatterSerializer.Deserialize<TType>(bytes?.ToArray());
	}
}