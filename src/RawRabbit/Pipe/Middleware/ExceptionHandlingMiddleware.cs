using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Logging;

namespace RawRabbit.Pipe.Middleware;

public class ExceptionHandlingOptions
{
	public Func<Exception, IPipeContext, CancellationToken, Task> HandlingFunc { get; set; }

	public Action<IPipeBuilder> InnerPipe { get; set; }
}

public class ExceptionHandlingMiddleware : Middleware
{
	protected readonly Func<Exception, IPipeContext, CancellationToken, Task> _handlingFunc;
	public readonly Middleware _innerPipe;
	private readonly ILog _logger = LogProvider.For<ExceptionHandlingMiddleware>();

	public ExceptionHandlingMiddleware(IPipeBuilderFactory factory, ExceptionHandlingOptions options = null)
	{
		this._handlingFunc = options?.HandlingFunc ?? ((_, _, _) => Task.FromResult(0));
		this._innerPipe = factory.Create(options?.InnerPipe);
	}

	public override async Task InvokeAsync(IPipeContext context, CancellationToken token = default)
	{
		try
		{
			await this._innerPipe.InvokeAsync(context, token);
			await this.Next.InvokeAsync(context, token);
		}
		catch (Exception e)
		{
			this._logger.Error(e, "Exception thrown. Will be handled by Exception Handler");
			await this.OnExceptionAsync(e, context, token);
		}
	}

	protected virtual Task OnExceptionAsync(Exception exception, IPipeContext context, CancellationToken token)
	{
		return this._handlingFunc(exception, context, token);
	}

	protected static Exception UnwrapInnerException(Exception exception)
	{
		if (exception is AggregateException && exception.InnerException != null)
		{
			// ReSharper disable once TailRecursiveCall
			return UnwrapInnerException(exception.InnerException);
		}
		return exception;
	}
}
