using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Configuration.Consume;

namespace RawRabbit.Consumer
{
	public interface IConsumerFactory
	{
		Task<IAsyncBasicConsumer> GetConsumerAsync(ConsumeConfiguration cfg, IChannel channel = null, CancellationToken token = default(CancellationToken));
		Task<IAsyncBasicConsumer> CreateConsumerAsync(IChannel channel = null, CancellationToken token = default(CancellationToken));
		Task<IAsyncBasicConsumer> ConfigureConsumeAsync(IAsyncBasicConsumer consumer, ConsumeConfiguration cfg);
		Task<IAsyncBasicConsumer> GetConfiguredConsumerAsync(ConsumeConfiguration cfg, IChannel channel = null, CancellationToken token = default(CancellationToken));
	}
}
