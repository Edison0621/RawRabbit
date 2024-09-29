using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using RawRabbit.Instantiation;
// ReSharper disable All
#pragma warning disable CS0067 // Event is never used

namespace RawRabbit.PerformanceTest
{
	public class RpcBenchmarks
	{
		private IBusClient _busClient;
		private Request _request;
		private Respond _respond;
		public event EventHandler MessageReceived;
		public delegate void MessageReceivedEventHandler(EventHandler e);

		[Setup]
		public void Setup()
		{
			this._busClient = RawRabbitFactory.CreateSingleton();
			this._request = new Request();
			this._respond = new Respond();
			this._busClient.RespondAsync<Request,Respond>(message =>
				Task.FromResult(this._respond)
			);
			this._busClient.RespondAsync<Request, Respond>(message =>
				Task.FromResult(this._respond),
				ctx => ctx.UseRespondConfiguration(cfg => cfg
					.Consume(c => c
						.WithRoutingKey("custom_key"))
					.FromDeclaredQueue(q => q
						.WithName("custom_queue")
						.WithAutoDelete())
					.OnDeclaredExchange(e => e
						.WithName("custom_exchange")
						.WithAutoDelete()))
			);
		}

		[Cleanup]
		public void Cleanup()
		{
			this._busClient.DeleteQueueAsync<Request>();
			(this._busClient as IDisposable).Dispose();
		}

		[Benchmark]
		public async Task DirectRpc()
		{
			await this._busClient.RequestAsync<Request, Respond>(this._request, ctx => ctx
				.UseRequestConfiguration(cfg => cfg
					.PublishRequest(p => p
						.WithProperties(prop => prop.DeliveryMode = 1)))
			);
		}

		[Benchmark]
		public async Task NormalRpc()
		{
			await this._busClient.RequestAsync<Request, Respond>(this._request, ctx => ctx
				.UseRequestConfiguration(cfg => cfg
					.PublishRequest(p => p
						.OnDeclaredExchange(e => e
							.WithName("custom_exchange")
							.WithAutoDelete())
						.WithRoutingKey("custom_key")
						.WithProperties(prop => prop.DeliveryMode = 1))
					.ConsumeResponse(r => r
						.Consume(c => c
							.WithRoutingKey("response_key"))
						.FromDeclaredQueue(q => q
							.WithName("response_queue")
							.WithAutoDelete())
						.OnDeclaredExchange(e => e
							.WithName("response_exchange")
							.WithAutoDelete()
						)
					)
				)
			);
		}

		public class Request { }
		public class Respond { }
	}
}
