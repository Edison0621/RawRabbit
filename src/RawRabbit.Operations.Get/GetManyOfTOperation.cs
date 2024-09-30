using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Configuration.Get;
using RawRabbit.Operations.Get.Model;
using RawRabbit.Pipe;

namespace RawRabbit;

public static class GetManyOfTOperation
{
	public static async Task<Ackable<List<Ackable<TMessage>>>> GetManyAsync<TMessage>(this IBusClient busClient, int batchSize, Action<IGetConfigurationBuilder> config = null, CancellationToken token = default(CancellationToken))
	{
		IChannel channel = await busClient.CreateChannelAsync(token:token);
		List<Ackable<TMessage>> result = new();

		while (result.Count < batchSize)
		{
			Ackable<TMessage> ackableMessage = await busClient.GetAsync<TMessage>(config, c => { CollectionExtensions.TryAdd(c.Properties, PipeKey.Channel, channel); }, token);
			if (ackableMessage.Content == null)
			{
				break;
			}
			result.Add(ackableMessage);
		}

		return new Ackable<List<Ackable<TMessage>>>(
			result,
			result.FirstOrDefault()?._channel,
			list => list.Where(a => !a.Acknowledged).SelectMany(a => a.DeliveryTags).ToArray()
		);
	}
}