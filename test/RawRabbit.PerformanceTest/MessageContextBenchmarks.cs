using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using RawRabbit.Enrichers.MessageContext;
using RawRabbit.Instantiation;
// ReSharper disable All
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

namespace RawRabbit.PerformanceTest
{
	public class MessageContextBenchmarks
	{
		private IBusClient _withoutContext;
		private Task _completedTask;
		private MessageA _messageA;
		private IBusClient _withContext;
		private MessageB _messageB;
		public event EventHandler MessageReceived;
		public delegate void MessageReceivedEventHandler(EventHandler e);

		//[Setup]
		public void Setup()
		{
			this._withoutContext = RawRabbitFactory.CreateSingleton();
			this._withContext = RawRabbitFactory.CreateSingleton(new RawRabbitOptions
			{
				Plugins = p => p.UseMessageContext<MessageContext>()
			});
			this._completedTask = Task.FromResult(0);
			this._messageA = new MessageA();
			this._messageB = new MessageB();
			this._withoutContext.SubscribeAsync<MessageA>(message =>
			{
				this.MessageReceived(message, EventArgs.Empty);
				return this._completedTask;
			});
			this._withContext.SubscribeAsync<MessageB, MessageContext>((message, context) =>
			{
				this.MessageReceived(message, EventArgs.Empty);
				return this._completedTask;
			});
		}

		//[Cleanup]
		public void Cleanup()
		{
			this._withoutContext.DeleteQueueAsync<MessageA>();
			this._withoutContext.DeleteQueueAsync<MessageB>();
			(this._withoutContext as IDisposable).Dispose();
			(this._withContext as IDisposable).Dispose();
		}

		[Benchmark]
		public async Task MessageContext_FromFactory()
		{
			TaskCompletionSource<Message> msgTsc = new TaskCompletionSource<Message>();

			EventHandler onMessageReceived = (sender, args) => { msgTsc.TrySetResult(sender as Message); };
			this.MessageReceived += onMessageReceived;

			this._withContext.PublishAsync(this._messageB);
			await msgTsc.Task;
			this.MessageReceived -= onMessageReceived;
		}

		[Benchmark]
		public async Task MessageContext_None()
		{
			TaskCompletionSource<Message> msgTsc = new TaskCompletionSource<Message>();

			EventHandler onMessageReceived = (sender, args) => { msgTsc.TrySetResult(sender as Message); };
			this.MessageReceived += onMessageReceived;

			this._withoutContext.PublishAsync(this._messageA);
			await msgTsc.Task;
			this.MessageReceived -= onMessageReceived;
		}


		public class MessageA { }
		public class MessageB { }
		public class MessageContext { }
	}
}
