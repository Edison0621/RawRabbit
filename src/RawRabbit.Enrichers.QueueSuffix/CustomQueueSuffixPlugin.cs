using RawRabbit.Enrichers.QueueSuffix;
using RawRabbit.Instantiation;

namespace RawRabbit;

public static class CustomQueueSuffixPlugin
{
	public static IClientBuilder UseCustomQueueSuffix(this IClientBuilder builder, string suffix = null)
	{
		QueueSuffixOptions options =  new()
		{
			_customSuffixFunc = context => suffix,
			_activeFunc = context => context.GetCustomQueueSuffixActivated(),
			_contextSuffixOverrideFunc = context => context.GetCustomQueueSuffix()
		};
		builder.UseQueueSuffix(options);

		return builder;
	}
}