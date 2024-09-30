using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Configuration.Consume;
using RawRabbit.Configuration.Queue;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.QueueSuffix;

public class QueueSuffixMiddleware : StagedMiddleware
{
	protected readonly Func<IPipeContext, bool> _activatedFlagFunc;
	protected readonly Func<IPipeContext, QueueDeclaration> _queueDeclareFunc;
	protected readonly Func<IPipeContext, string> _customSuffixFunc;
	protected readonly Func<IPipeContext, ConsumeConfiguration> _consumeCfgFunc;
	protected readonly Action<QueueDeclaration, string> _appendSuffixAction;
	protected readonly Func<string, bool> _skipSuffixFunc;
	protected readonly Func<IPipeContext, string> _contextSuffixOverride;

	public override string StageMarker => Pipe.StageMarker.ConsumeConfigured;

	public QueueSuffixMiddleware(QueueSuffixOptions options = null)
	{
		this._activatedFlagFunc = options?._activeFunc ?? (context => context.GetCustomQueueSuffixActivated());
		this._customSuffixFunc = options?._customSuffixFunc ?? (context => context.GetCustomQueueSuffix());
		this._queueDeclareFunc = options?._queueDeclareFunc ?? (context => context.GetQueueDeclaration());
		this._appendSuffixAction = options?._appendSuffixAction ?? ((queue, suffix) => queue.Name = $"{queue.Name}_{suffix}");
		this._consumeCfgFunc = options?._consumeConfigFunc ?? (context => context.GetConsumeConfiguration());
		this._skipSuffixFunc = options?._skipSuffixFunc ?? (string.IsNullOrWhiteSpace);
		this._contextSuffixOverride = options?._contextSuffixOverrideFunc ?? (context => null);
	}

	public override Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
	{
		bool activated = this.GetActivatedFlag(context);
		if (!activated)
		{
			return this.Next.InvokeAsync(context, token);
		}
		QueueDeclaration declaration = this.GetQueueDeclaration(context);
		if (declaration == null)
		{
			return this.Next.InvokeAsync(context, token);
		}
		string suffix = this.GetCustomQueueSuffix(context);
		if (this.SkipSuffix(suffix))
		{
			return this.Next.InvokeAsync(context, token);
		}

		this.AppendSuffix(declaration, suffix);
		ConsumeConfiguration consumeConfig = this.GetConsumeConfig(context);
		this.AlignConsumerConfig(consumeConfig, declaration);
		return this.Next.InvokeAsync(context, token);
	}

	protected virtual bool SkipSuffix(string suffix)
	{
		return this._skipSuffixFunc.Invoke(suffix);
	}

	protected virtual void AlignConsumerConfig(ConsumeConfiguration consumeConfig, QueueDeclaration declaration)
	{
		if (consumeConfig == null)
		{
			return;
		}
		consumeConfig.QueueName = declaration.Name;
	}

	protected virtual bool GetActivatedFlag(IPipeContext context)
	{
		return this._activatedFlagFunc.Invoke(context);
	}

	protected virtual QueueDeclaration GetQueueDeclaration(IPipeContext context)
	{
		return this._queueDeclareFunc?.Invoke(context);
	}

	protected virtual string GetCustomQueueSuffix(IPipeContext context)
	{
		string suffixOverride = this.GetContextSuffixOverride(context);
		if (!string.IsNullOrWhiteSpace(suffixOverride))
		{
			return suffixOverride;
		}
		return this._customSuffixFunc?.Invoke(context);
	}

	protected virtual string GetContextSuffixOverride(IPipeContext context)
	{
		return this._contextSuffixOverride?.Invoke(context);
	}

	protected virtual void AppendSuffix(QueueDeclaration queue, string suffix)
	{
		this._appendSuffixAction?.Invoke(queue, suffix);
	}

	protected virtual ConsumeConfiguration GetConsumeConfig(IPipeContext context)
	{
		return this._consumeCfgFunc(context);
	}
}