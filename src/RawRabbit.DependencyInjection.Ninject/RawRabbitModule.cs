using Ninject;
using Ninject.Modules;
using RawRabbit.Instantiation;

namespace RawRabbit.DependencyInjection.Ninject
{
	public class RawRabbitModule : NinjectModule
	{
		public override void Load()
		{
			this.Kernel?.Bind<IDependencyResolver>()
				.ToMethod(context => new NinjectAdapter(context));

			this.Kernel?.Bind<IInstanceFactory>()
				.ToMethod(context => RawRabbitFactory.CreateInstanceFactory(context.Kernel.Get<RawRabbitOptions>()))
				.InSingletonScope();

			this.Kernel?.Bind<IBusClient>()
				.ToMethod(context => context.Kernel.Get<IInstanceFactory>().Create());
		}
	}
}
