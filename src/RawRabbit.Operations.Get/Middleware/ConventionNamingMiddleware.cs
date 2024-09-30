using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Get;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Get.Middleware;

public class ConventionNamingOptions
{
	public Func<IPipeContext, GetConfiguration> GetConfigFunc { get; set; }
	public Func<IPipeContext, Type> MessageTypeFunc { get; set; }
}

public class ConventionNamingMiddleware : Pipe.Middleware.Middleware
{
	private readonly INamingConventions _conventions;
	protected readonly Func<IPipeContext, GetConfiguration> _getConfigFunc;
	protected readonly Func<IPipeContext, Type> _messageTypeFunc;

	public ConventionNamingMiddleware(INamingConventions conventions, ConventionNamingOptions options = null)
	{
		this._conventions = conventions;
		this._getConfigFunc = options?.GetConfigFunc ?? (context => context.GetGetConfiguration());
		this._messageTypeFunc = options?.MessageTypeFunc ?? (context => context.GetMessageType());
	}

	public override Task InvokeAsync(IPipeContext context, CancellationToken token = default)
	{
		GetConfiguration config = this.GetGetConfiguration(context);
		if (!string.IsNullOrWhiteSpace(config.QueueName))
		{
			return this.Next.InvokeAsync(context, token);
		}

		Type messageType = this.GetMessageType(context);
		config.QueueName = this._conventions.QueueNamingConvention(messageType);
		return this.Next.InvokeAsync(context, token);
	}

	protected virtual GetConfiguration GetGetConfiguration(IPipeContext context)
	{
		return this._getConfigFunc(context);
	}

	protected virtual Type GetMessageType(IPipeContext context)
	{
		return this._messageTypeFunc(context);
	}
}