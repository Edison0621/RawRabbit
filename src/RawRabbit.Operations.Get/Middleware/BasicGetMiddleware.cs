using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Get.Middleware
{
	public class BasicGetOptions
	{
		public Func<IPipeContext, IModel> ChannelFunc { get; set; }
		public Func<IPipeContext, bool> AutoAckFunc { get; internal set; }
		public Action<IPipeContext, BasicGetResult> PostExecutionAction { get; set; }
		public Func<IPipeContext, string> QueueNameFunc { get; internal set; }
	}

	public class BasicGetMiddleware : Pipe.Middleware.Middleware
	{
		protected readonly Func<IPipeContext, IModel> _channelFunc;
		protected readonly Func<IPipeContext, string> _queueNameFunc;
		protected readonly Func<IPipeContext, bool> _autoAckFunc;
		protected readonly Action<IPipeContext, BasicGetResult> _postExecutionAction;

		public BasicGetMiddleware(BasicGetOptions options = null)
		{
			this._channelFunc = options?.ChannelFunc ?? (context => context.GetChannel());
			this._queueNameFunc = options?.QueueNameFunc ?? (context => context.GetGetConfiguration()?.QueueName);
			this._autoAckFunc = options?.AutoAckFunc ?? (context => context.GetGetConfiguration()?.AutoAck ?? false);
			this._postExecutionAction = options?.PostExecutionAction;
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
		{
			IModel channel = this.GetChannel(context);
			string queueNamme = this.GetQueueName(context);
			bool autoAck = this.GetAutoAck(context);
			BasicGetResult getResult = this.PerformBasicGet(channel, queueNamme, autoAck);
			context.Properties.TryAdd(GetPipeExtensions.BasicGetResult, getResult);
			this._postExecutionAction?.Invoke(context, getResult);
			return this.Next.InvokeAsync(context, token);
		}

		protected virtual BasicGetResult PerformBasicGet(IModel channel, string queueName, bool autoAck)
		{
			return channel.BasicGet(queueName, autoAck);
		}

		protected virtual bool GetAutoAck(IPipeContext context)
		{
			return this._autoAckFunc(context);
		}

		protected virtual string GetQueueName(IPipeContext context)
		{
			return this._queueNameFunc(context);
		}

		protected virtual IModel GetChannel(IPipeContext context)
		{
			return this._channelFunc(context);
		}
	}
}
