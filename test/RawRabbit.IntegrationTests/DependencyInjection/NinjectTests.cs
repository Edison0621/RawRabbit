using System.Threading.Tasks;
using Ninject;
using RawRabbit.DependencyInjection.Ninject;
using RawRabbit.Instantiation;
using Xunit;
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace RawRabbit.IntegrationTests.DependencyInjection;

public class NinjectTests
{
	[Fact]
	public async Task Should_Be_Able_To_Resolve_Client_From_Ninject()
	{
		/* Setup */
		StandardKernel kernel = new();
		kernel.RegisterRawRabbit();
			
		/* Test */
		IBusClient client = kernel.Get<IBusClient>();
		IInstanceFactory instanceFactory = kernel.Get<IInstanceFactory>();

		/* Assert */
		(instanceFactory as InstanceFactory)?.Dispose();
		Assert.NotNull(client);
	}
}
