using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using RawRabbit.Common;
using RawRabbit.Exceptions;
using RawRabbit.Logging;
using RawRabbit.Operations.Request.Core;
using RawRabbit.Pipe;
using RawRabbit.Serialization;

namespace RawRabbit.Operations.Request.Middleware
{
	public class ResponderExceptionOptions
	{
		public Func<IPipeContext, object> MessageFunc { get; set; }
		public Func<ExceptionInformation, IPipeContext, Task> HandlerFunc { get; set; }
		public Func<IPipeContext, Type> ResponseTypeFunc { get; set; }
		public Func<IPipeContext, BasicDeliverEventArgs> DeliveryArgsFunc { get; set; }
	}

	public class ResponderExceptionMiddleware : Pipe.Middleware.Middleware
	{
		private readonly ISerializer _serializer;
		protected Func<IPipeContext, object> _exceptionInfoFunc;
		protected readonly Func<ExceptionInformation, IPipeContext, Task> _handlerFunc;
		private readonly ILog _logger = LogProvider.For<ResponderExceptionMiddleware>();
		protected readonly Func<IPipeContext, Type> _responseTypeFunc;
		private readonly Func<IPipeContext, BasicDeliverEventArgs> _deliveryArgFunc;

		public ResponderExceptionMiddleware(ISerializer serializer, ResponderExceptionOptions options = null)
		{
			this._serializer = serializer;
			this._exceptionInfoFunc = options?.MessageFunc ?? (context => context.GetResponseMessage());
			this._handlerFunc = options?.HandlerFunc;
			this._deliveryArgFunc = options?.DeliveryArgsFunc ?? (context => context.GetDeliveryEventArgs());
			this._responseTypeFunc = options?.ResponseTypeFunc ?? (context =>
			{
				string type = this.GetDeliverEventArgs(context)?.BasicProperties.Type;
				return !string.IsNullOrWhiteSpace(type) ? Type.GetType(type, false) : typeof(object);
			});
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token = new CancellationToken())
		{
			Type responseType = this.GetResponseType(context);
			if (responseType == typeof(ExceptionInformation))
			{
				ExceptionInformation exceptionInfo = this.GetExceptionInfo(context);
				return this.HandleRespondException(exceptionInfo, context);
			}
			return this.Next.InvokeAsync(context, token);
		}

		protected virtual BasicDeliverEventArgs GetDeliverEventArgs(IPipeContext context)
		{
			return this._deliveryArgFunc?.Invoke(context);
		}

		protected virtual Type GetResponseType(IPipeContext context)
		{
			return this._responseTypeFunc?.Invoke(context);
		}

		protected virtual ReadOnlyMemory<byte> GetMessageBody(IPipeContext context)
		{
			BasicDeliverEventArgs deliveryArgs = this.GetDeliverEventArgs(context);
			return deliveryArgs?.Body ?? new ReadOnlyMemory<byte>();
		}

		protected virtual ExceptionInformation GetExceptionInfo(IPipeContext context)
		{
			ReadOnlyMemory<byte> body = this.GetMessageBody(context);
			try
			{
				return this._serializer.Deserialize<ExceptionInformation>(body);
			}
			catch (Exception e)
			{
				return new ExceptionInformation
				{
					Message =
						$"An unhandled exception was thrown by the responder, but the requesting client was unable to deserialize exception info. {Encoding.UTF8.GetString(body.ToArray())}.",
					InnerMessage = e.Message,
					StackTrace = e.StackTrace,
					ExceptionType = e.GetType().Name
				};
			}
		}

		protected virtual Task HandleRespondException(ExceptionInformation exceptionInfo, IPipeContext context)
		{
			this._logger.Info("An unhandled exception occured when remote tried to handle request.\n  Message: {exceptionMessage}\n  Stack Trace: {stackTrace}", exceptionInfo.Message, exceptionInfo.StackTrace);

			if (this._handlerFunc != null)
			{
				return this._handlerFunc(exceptionInfo, context);
			}

			MessageHandlerException exception = new MessageHandlerException(exceptionInfo.Message)
			{
				InnerExceptionType = exceptionInfo.ExceptionType,
				InnerStackTrace = exceptionInfo.StackTrace,
				InnerMessage = exceptionInfo.InnerMessage
			};
			return TaskUtil.FromException(exception);
		}
	}
}
