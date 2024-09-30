using System;

namespace RawRabbit.Serialization
{
	public interface ISerializer
	{
		string ContentType { get; }
		byte[] Serialize(object obj);
		object Deserialize(Type type, ReadOnlyMemory<byte>? bytes);
		TType Deserialize<TType>(ReadOnlyMemory<byte>? bytes);
	}
}
