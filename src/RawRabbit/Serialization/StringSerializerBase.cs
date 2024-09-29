using System;
using System.Text;

namespace RawRabbit.Serialization
{
	public abstract class StringSerializerBase : ISerializer
	{
		public abstract string ContentType { get; }
		public abstract object Deserialize(Type type, string serialized);
		public abstract string SerializeToString(object obj);

		public byte[] Serialize(object obj)
		{
			string serialized = this.SerializeToString(obj);
			return this.ConvertToBytes(serialized);
		}

		public object Deserialize(Type type, byte[] bytes)
		{
			if (bytes == null)
			{
				return null;
			}
			string serialized = this.ConvertToString(bytes);
			return this.Deserialize(type, serialized);
		}

		public TType Deserialize<TType>(byte[] bytes)
		{
			string serialized = this.ConvertToString(bytes);
			return (TType)this.Deserialize(typeof(TType), serialized);
		}

		protected virtual byte[] ConvertToBytes(string serialzed)
		{
			return Encoding.UTF8.GetBytes(serialzed);
		}

		protected virtual string ConvertToString(byte[] bytes)
		{
			return Encoding.UTF8.GetString(bytes);
		}
	}
}
