using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Consumer;
using RawRabbit.Logging;
using RawRabbit.Operations.Request.Context;
using RawRabbit.Operations.Request.Core;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Request.Middleware
{
	public class ResponseConsumerOptions
	{
		public Action<IPipeBuilder> ResponseReceived { get; set; }
		public Func<IPipeContext, ConsumerConfiguration> ResponseConfigFunc { get; set; }
		public Func<IPipeContext, string> CorrelationIdFunc { get; set; }
		public Func<IPipeContext, bool> UseDedicatedConsumer { get; set; }
	}

	public class ResponseConsumeMiddleware : Pipe.Middleware.Middleware
	{
		protected static readonly ConcurrentDictionary<IAsyncBasicConsumer, ConcurrentDictionary<string, TaskCompletionSource<BasicDeliverEventArgs>>> AllResponses =
			new ConcurrentDictionary<IAsyncBasicConsumer, ConcurrentDictionary<string, TaskCompletionSource<BasicDeliverEventArgs>>>();
		
		protected readonly IConsumerFactory _consumerFactory;
		protected readonly Pipe.Middleware.Middleware _responsePipe;
		private readonly ILog _logger = LogProvider.For<ResponseConsumeMiddleware>();
		protected readonly Func<IPipeContext, ConsumerConfiguration> _responseConfigFunc;
		protected readonly Func<IPipeContext, string> _correlationidFunc;
		protected readonly Func<IPipeContext, bool> _dedicatedConsumerFunc;

		public ResponseConsumeMiddleware(IConsumerFactory consumerFactory, IPipeBuilderFactory factory, ResponseConsumerOptions options)
		{
			this._responseConfigFunc = options?.ResponseConfigFunc ?? (context => context.GetResponseConfiguration());
			this._correlationidFunc = options?.CorrelationIdFunc ?? (context => context.GetBasicProperties()?.CorrelationId);
			this._dedicatedConsumerFunc = options?.UseDedicatedConsumer ?? (context => context.GetDedicatedResponseConsumer());
			this._consumerFactory = consumerFactory;
			this._responsePipe = factory.Create(options?.ResponseReceived);
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
		{
			ConsumerConfiguration respondCfg = this.GetResponseConfig(context);
			string correlationId = this.GetCorrelationid(context);
			bool dedicatedConsumer = this.GetDedicatedConsumer(context);
			TaskCompletionSource<BasicDeliverEventArgs> responseTsc = new TaskCompletionSource<BasicDeliverEventArgs>();

			IAsyncBasicConsumer consumer;
			if (dedicatedConsumer)
			{
				consumer = await this._consumerFactory.CreateConsumerAsync(token: token);
				await this._consumerFactory.ConfigureConsumeAsync(consumer, respondCfg.Consume);
			}
			else
			{
				consumer = await this._consumerFactory.GetConfiguredConsumerAsync(respondCfg.Consume, token: token);
			}

			ConcurrentDictionary<string, TaskCompletionSource<BasicDeliverEventArgs>> responses = AllResponses.GetOrAdd(consumer, c =>
				{
					ConcurrentDictionary<string, TaskCompletionSource<BasicDeliverEventArgs>> pendings = new ConcurrentDictionary<string, TaskCompletionSource<BasicDeliverEventArgs>>();
					c.OnMessage((sender, args) =>
					{
						if (!pendings.TryRemove(args.BasicProperties.CorrelationId, out TaskCompletionSource<BasicDeliverEventArgs> tsc))
							return Task.CompletedTask;

						tsc.TrySetResult(args);
						return Task.CompletedTask;
					});
					return pendings;
				}
			);
			context.Properties.Add(PipeKey.Consumer, consumer);
			responses.TryAdd(correlationId, responseTsc);
			await this.Next.InvokeAsync(context, token);
			token.Register(() => responseTsc.TrySetCanceled());
			await responseTsc.Task;
			this._logger.Info("Message '{messageId}' for correlation id '{correlationId}' received.", responseTsc.Task.Result.BasicProperties.MessageId, correlationId);
			if (dedicatedConsumer)
			{
				this._logger.Info("Disposing dedicated consumer on queue {queueName}", respondCfg.Consume.QueueName);
				consumer.Channel?.Dispose();
				AllResponses.TryRemove(consumer, out _);
			}
			context.Properties.Add(PipeKey.DeliveryEventArgs, responseTsc.Task.Result);
			try
			{
				await this._responsePipe.InvokeAsync(context, token);
			}
			catch (Exception e)
			{
				this._logger.Error(e, "Response pipe for message '{messageId}' executed unsuccessfully.", responseTsc.Task.Result.BasicProperties.MessageId);
				throw;
			}
		}

		protected virtual ConsumerConfiguration GetResponseConfig(IPipeContext context)
		{
			return this._responseConfigFunc?.Invoke(context);
		}

		protected virtual string GetCorrelationid(IPipeContext context)
		{
			return this._correlationidFunc?.Invoke(context);
		}

		protected virtual bool GetDedicatedConsumer(IPipeContext context)
		{
			return this._dedicatedConsumerFunc?.Invoke(context) ?? false;
		}
	}

	public static class ResposeConsumerMiddlewareExtensions
	{
		private const string DedicatedResponseConsumer = "Request:DedicatedResponseConsumer";

		/// <summary>
		/// Use with caution!
		/// 
		/// Instruct the Request operation to create a unique consumer for
		/// the response queue. The consumer will be cancelled once the
		/// response message is received.
		/// </summary>
		public static IRequestContext UseDedicatedResponseConsumer(this IRequestContext context, bool useDedicated = true)
		{
			context.Properties.AddOrReplace(DedicatedResponseConsumer, useDedicated);
			return context;
		}

		public static bool GetDedicatedResponseConsumer(this IPipeContext context)
		{
			return context.Get(DedicatedResponseConsumer, false);
		}
	}
}
