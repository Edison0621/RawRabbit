using System;
using Autofac;
using Autofac.Features.ResolveAnything;
using RawRabbit.Instantiation;

namespace RawRabbit.DependencyInjection.Autofac
{
	public static class ContainerBuilderExtension
	{
		private const string RawRabbit = "RawRabbit";

		public static ContainerBuilder RegisterRawRabbit(this ContainerBuilder builder, RawRabbitOptions options = null)
		{
			// ReSharper disable once PossibleNullReferenceException
			builder.RegisterSource(new AnyConcreteTypeNotAlreadyRegisteredSource(type => type.Namespace.StartsWith(RawRabbit, StringComparison.Ordinal)));
			ContainerBuilderAdapter adapter = new ContainerBuilderAdapter(builder);
			adapter.AddRawRabbit(options);
			return builder;
		}
	}
}
