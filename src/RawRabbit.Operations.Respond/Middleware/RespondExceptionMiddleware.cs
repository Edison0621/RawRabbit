using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Common;
using RawRabbit.Configuration.Consume;
using RawRabbit.Exceptions;
using RawRabbit.Operations.Respond.Core;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Operations.Respond.Middleware
{
	public class RespondExceptionOptions
	{
		public Func<IPipeContext, BasicDeliverEventArgs> DeliveryArgsFunc { get; set; }
		public Func<IPipeContext, ConsumeConfiguration> ConsumeConfigFunc { get; set; }
		public Action<IPipeContext, ExceptionInformation> SaveAction { get; set; }
		public Action<IPipeBuilder> InnerPipe { get; set; }
	}

	public class RespondExceptionMiddleware : ExceptionHandlingMiddleware
	{
		protected readonly Func<IPipeContext, BasicDeliverEventArgs> _deliveryArgsFunc;
		protected readonly Func<IPipeContext, ConsumeConfiguration> _consumeConfigFunc;
		protected Func<IPipeContext, IModel> _channelFunc;
		protected readonly Action<IPipeContext, ExceptionInformation> _saveAction;

		public RespondExceptionMiddleware(IPipeBuilderFactory factory, RespondExceptionOptions options = null)
			: base(factory, new ExceptionHandlingOptions { InnerPipe = options?.InnerPipe })
		{
			this._deliveryArgsFunc = options?.DeliveryArgsFunc ?? (context => context.GetDeliveryEventArgs());
			this._consumeConfigFunc = options?.ConsumeConfigFunc ?? (context => context.GetConsumeConfiguration());
			this._saveAction = options?.SaveAction ?? ((context, information) => context.Properties.TryAdd(RespondKey.ResponseMessage, information));
		}

		protected override Task OnExceptionAsync(Exception exception, IPipeContext context, CancellationToken token)
		{
			Exception innerException = UnwrapInnerException(exception);
			BasicDeliverEventArgs args = this.GetDeliveryArgs(context);
			ConsumeConfiguration cfg = this.GetConsumeConfiguration(context);
			this.AddAcknowledgementToContext(context, cfg);
			ExceptionInformation exceptionInfo = this.CreateExceptionInformation(innerException, args, cfg, context);
			this.SaveInContext(context, exceptionInfo);
			return this.Next.InvokeAsync(context, token);
		}

		protected virtual void AddAcknowledgementToContext(IPipeContext context, ConsumeConfiguration cfg)
		{
			if (cfg.AutoAck)
			{
				return;
			}
			context.Properties.TryAdd(PipeKey.MessageAcknowledgement, new Ack());
		}

		protected virtual BasicDeliverEventArgs GetDeliveryArgs(IPipeContext context)
		{
			return this._deliveryArgsFunc(context);
		}

		protected virtual ConsumeConfiguration GetConsumeConfiguration(IPipeContext context)
		{
			return this._consumeConfigFunc(context);
		}

		protected virtual ExceptionInformation CreateExceptionInformation(Exception exception, BasicDeliverEventArgs args, ConsumeConfiguration cfg, IPipeContext context)
		{
			return new ExceptionInformation
			{
				Message = $"An unhandled exception was thrown when consuming a message\n  MessageId: {args.BasicProperties.MessageId}\n  Queue: '{cfg.QueueName}'\n  Exchange: '{cfg.ExchangeName}'\nSee inner exception for more details.",
				ExceptionType = exception.GetType().FullName,
				StackTrace = exception.StackTrace,
				InnerMessage = exception.Message
			};
		}

		protected virtual void SaveInContext(IPipeContext context, ExceptionInformation info)
		{
			this._saveAction?.Invoke(context, info);
		}
	}
}
