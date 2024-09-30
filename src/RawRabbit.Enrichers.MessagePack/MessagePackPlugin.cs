﻿using RawRabbit.Instantiation;
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
			pipe: p => { },
			ioc: di => di.AddSingleton<ISerializer, MessagePackSerializerWorker>(resolver => new MessagePackSerializerWorker()));
		return builder;
	}
}
