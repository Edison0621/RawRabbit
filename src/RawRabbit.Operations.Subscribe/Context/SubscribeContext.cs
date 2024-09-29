using RawRabbit.Pipe;

namespace RawRabbit.Operations.Subscribe.Context
{
	public interface ISubscribeContext : IPipeContext { }

	public class SubscribeContext : PipeContext, ISubscribeContext
	{
		public SubscribeContext(IPipeContext context)
		{
			this.Properties = context.Properties;
		}
	}
}
