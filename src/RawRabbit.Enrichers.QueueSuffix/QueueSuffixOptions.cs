using System;
using RawRabbit.Configuration.Consume;
using RawRabbit.Configuration.Queue;
using RawRabbit.Pipe;

namespace RawRabbit.Enrichers.QueueSuffix;

public class QueueSuffixOptions
{
	public Func<IPipeContext, QueueDeclaration> _queueDeclareFunc;
	public Func<IPipeContext, string> _customSuffixFunc;
	public Func<IPipeContext, string> _contextSuffixOverrideFunc;
	public Func<IPipeContext, bool> _activeFunc;
	public Func<string, bool> _skipSuffixFunc;
	public Func<IPipeContext, ConsumeConfiguration> _consumeConfigFunc;
	public Action<QueueDeclaration, string> _appendSuffixAction;
}