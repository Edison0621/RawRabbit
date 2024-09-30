using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using RabbitMQ.Client;
using RawRabbit.Instantiation;
// ReSharper disable All
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

namespace RawRabbit.PerformanceTest
{
	public class PubSubBenchmarks
	{
		private IBusClient _busClient;
		private Task _completedTask;
		private Message _message;
		public event EventHandler MessageReceived;
		public delegate void MessageReceivedEventHandler(EventHandler e);

		//[Setup]
		public void Setup()
		{
			this._busClient = RawRabbitFactory.CreateSingleton();
			this._completedTask = Task.FromResult(0);
			this._message = new Message();
			this._busClient.SubscribeAsync<Message>(message =>
			{
				this.MessageReceived(message, EventArgs.Empty);
				return this._completedTask;
			});
		}

		//[Cleanup]
		public void Cleanup()
		{
			this._busClient.DeleteQueueAsync<Message>();
			(this._busClient as IDisposable).Dispose();
		}

		[Benchmark]
		public async Task ConsumerAcknowledgements_Off()
		{
			TaskCompletionSource<Message> msgTsc = new TaskCompletionSource<Message>();

			EventHandler onMessageReceived = (sender, args) => { msgTsc.TrySetResult(sender as Message); };
			this.MessageReceived += onMessageReceived;

			this._busClient.PublishAsync(this._message, ctx => ctx.UsePublishAcknowledge(false));
			await msgTsc.Task;
			this.MessageReceived -= onMessageReceived;
		}

		[Benchmark]
		public async Task ConsumerAcknowledgements_On()
		{
			TaskCompletionSource<Message> msgTsc = new TaskCompletionSource<Message>();

			EventHandler onMessageReceived = (sender, args) => { msgTsc.TrySetResult(sender as Message); };
			this.MessageReceived += onMessageReceived;

			this._busClient.PublishAsync(this._message);
			await msgTsc.Task;
			this.MessageReceived -= onMessageReceived;
		}

		[Benchmark]
		public async Task DeliveryMode_NonPersistant()
		{
			TaskCompletionSource<Message> msgTsc = new TaskCompletionSource<Message>();

			EventHandler onMessageReceived = (sender, args) => { msgTsc.TrySetResult(sender as Message); };
			this.MessageReceived += onMessageReceived;

			this._busClient.PublishAsync(this._message, ctx => ctx
				.UsePublishConfiguration(cfg => cfg
					.WithProperties(p => p.DeliveryMode = DeliveryModes.Transient))
			);
			await msgTsc.Task;
			this.MessageReceived -= onMessageReceived;
		}

		[Benchmark]
		public async Task DeliveryMode_Persistant()
		{
			TaskCompletionSource<Message> msgTsc = new TaskCompletionSource<Message>();

			EventHandler onMessageReceived = (sender, args) => { msgTsc.TrySetResult(sender as Message); };
			this.MessageReceived += onMessageReceived;

			this._busClient.PublishAsync(this._message, ctx => ctx
				.UsePublishConfiguration(cfg => cfg
					.WithProperties(p => p.DeliveryMode = DeliveryModes.Persistent))
			);
			await msgTsc.Task;
			this.MessageReceived -= onMessageReceived;
		}
	}

	public class Message { }
}
