using System.Threading.Tasks;
using Autofac;
using Autofac.Core;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.DependencyInjection.Autofac;
using RawRabbit.Instantiation;
using RawRabbit.IntegrationTests.TestMessages;
using Xunit;
// ReSharper disable All
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace RawRabbit.IntegrationTests.DependencyInjection
{
	public class AutofacTests
	{
		[Fact]
		public async Task Should_Be_Able_To_Resolve_Client_From_Autofac()
		{
			/* Setup */
			ContainerBuilder builder = new ContainerBuilder();
			builder.RegisterRawRabbit();
			IContainer container = builder.Build();

			/* Test */
			IBusClient client = container.Resolve<IBusClient>();
			IResourceDisposer disposer = container.Resolve<IResourceDisposer>();

			/* Assert */
			disposer.Dispose();
			Assert.True(true);
		}

		[Fact]
		public async Task Should_Be_Able_To_Publish_Message_From_Resolved_Client()
		{
			/* Setup */
			ContainerBuilder builder = new ContainerBuilder();
			builder.RegisterRawRabbit();
			IContainer container = builder.Build();

			/* Test */
			IBusClient client = container.Resolve<IBusClient>();
			await client.PublishAsync(new BasicMessage());
			await client.DeleteExchangeAsync<BasicMessage>();
			IResourceDisposer disposer = container.Resolve<IResourceDisposer>();

			/* Assert */
			disposer.Dispose();
			Assert.True(true);
		}

		[Fact]
		public async Task Should_Honor_Client_Configuration()
		{
			/* Setup */
			ContainerBuilder builder = new ContainerBuilder();
			RawRabbitConfiguration config = RawRabbitConfiguration.Local;
			config.VirtualHost = "/foo";

			/* Test */
			await Assert.ThrowsAsync<DependencyResolutionException>(async () =>
			{
				builder.RegisterRawRabbit(new RawRabbitOptions
				{
					ClientConfiguration = config
				});
				IContainer container = builder.Build();
				IBusClient client = container.Resolve<IBusClient>();
				await client.CreateChannelAsync();
			});
			

			/* Assert */
			Assert.True(true);
		}

		[Fact]
		public async Task Should_Be_Able_To_Resolve_Client_With_Plugins_From_Autofac()
		{
			/* Setup */
			ContainerBuilder builder = new ContainerBuilder();
			builder.RegisterRawRabbit(new RawRabbitOptions
			{
				Plugins = p => p.UseStateMachine()
			});
			IContainer container = builder.Build();

			/* Test */
			IBusClient client = container.Resolve<IBusClient>();
			IResourceDisposer disposer = container.Resolve<IResourceDisposer>();

			/* Assert */
			disposer.Dispose();
			Assert.True(true);
		}
	}
}
