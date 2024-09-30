using RawRabbit.Instantiation;
using RawRabbit.Serialization;

namespace RawRabbit.Enrichers.ZeroFormatter;

public static class ZeroFormatterPlugin
{
	/// <summary>
	/// Replaces the default serializer with ZeroFormatter.
	/// </summary>
	public static IClientBuilder UseZeroFormatter(this IClientBuilder builder)
	{
		builder.Register(
			pipe: _ => { },
			ioc: di => di.AddSingleton<ISerializer, ZeroFormatterSerializerWorker>());
		return builder;
	}
}