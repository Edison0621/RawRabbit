﻿using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Configuration.Consume;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit
{
	public static class CreateConsumerExtension
	{
		public static readonly Action<IPipeBuilder> ConsumerAction = pipe => pipe
			.Use<ConsumerCreationMiddleware>();

		public static async Task<IAsyncBasicConsumer> CreateConsumerAsync(this IBusClient client, ConsumeConfiguration config = null, CancellationToken ct = default(CancellationToken))
		{
			IPipeContext result = await client.InvokeAsync(ConsumerAction, context =>
			{
				context.Properties.Add(PipeKey.ConsumeConfiguration, config);
			},ct);
			return result.GetConsumer();
		}
	}
}
