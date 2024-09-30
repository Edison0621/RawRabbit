using RawRabbit.Instantiation;
using RawRabbit.Serialization;

namespace RawRabbit.Enrichers.MessagePack;

public static class MessagePackPlugin
{
	/// <summary>
	/// Replaces the default serializer with MessagePack.
	/// </summary>
	public static IClientBuilder UseMessagePack(this IClientBuilder builder)
	{
		builder.Register(
			pipe: _ => { },
			ioc: di => di.AddSingleton<ISerializer, MessagePackSerializerWorker>(_ => new MessagePackSerializerWorker()));
		return builder;
	}
}
