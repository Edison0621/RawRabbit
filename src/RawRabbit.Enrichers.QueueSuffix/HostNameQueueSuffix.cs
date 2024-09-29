using System;
using RawRabbit.Instantiation;

namespace RawRabbit.Enrichers.QueueSuffix
{
	public static class HostNameQueueSuffix
	{
		public static IClientBuilder UseHostQueueSuffix(this IClientBuilder builder)
		{
			builder.UseQueueSuffix(new QueueSuffixOptions
			{
				_customSuffixFunc = context => Environment.MachineName.ToLower(),
				_activeFunc = context => context.GetHostnameQueueSuffixFlag()
			});

			return builder;
		}
	}
}
