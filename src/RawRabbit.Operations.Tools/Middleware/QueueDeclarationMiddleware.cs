using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Configuration.Queue;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Tools.Middleware;

public class QueueDeclarationOptions
{
	public Func <IPipeContext, QueueDeclaration> QueueDeclarationFunc { get; set; }
	public Action<IPipeContext, QueueDeclaration> SaveToContext { get; set;  }
}

public class QueueDeclarationMiddleware : Pipe.Middleware.Middleware
{
	protected readonly IQueueConfigurationFactory _cfgFactory;
	protected readonly Func<IPipeContext, QueueDeclaration> _queueDeclarationFunc;
	protected readonly Action<IPipeContext, QueueDeclaration> _saveToContextAction;

	public QueueDeclarationMiddleware(IQueueConfigurationFactory cfgFactory, QueueDeclarationOptions options = null)
	{
		this._cfgFactory = cfgFactory;
		this._queueDeclarationFunc = options?.QueueDeclarationFunc ?? (ctx =>ctx.GetQueueDeclaration());
		this._saveToContextAction = options?.SaveToContext ?? ((ctx, declaration) =>ctx.Properties.TryAdd(PipeKey.QueueDeclaration, declaration));
	}

	public override Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
	{
		QueueDeclaration queueDeclaration = this.GetQueueDeclaration(context);
		this.SaveToContext(context, queueDeclaration);
		return this.Next.InvokeAsync(context, token);
	}

	protected virtual QueueDeclaration GetQueueDeclaration(IPipeContext context)
	{
		QueueDeclaration declaration = this._queueDeclarationFunc?.Invoke(context);
		if(declaration != null)
		{
			return declaration;
		}
		Type messageType = context.GetMessageType();
		return this._cfgFactory.Create(messageType);
	}

	protected virtual void SaveToContext(IPipeContext context, QueueDeclaration declaration)
	{
		this._saveToContextAction?.Invoke(context, declaration);
	}
}