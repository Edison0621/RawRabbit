using System;
using System.Collections.Generic;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.StateMachine.Trigger;

public class TriggerConfigurer
{
	public List<TriggerConfiguration> TriggerConfiguration { get; set; }
		
	public TriggerConfigurer()
	{
		this.TriggerConfiguration = new List<TriggerConfiguration>();
	}

	public TriggerConfigurer From(Action<IPipeBuilder> pipe, Action<IPipeContext> context)
	{
		this.TriggerConfiguration.Add(new TriggerConfiguration
		{
			Pipe = pipe,
			Context = context
		});
		return this;
	}
}

public class TriggerConfiguration
{
	public Action<IPipeBuilder> Pipe { get; set; }
	public Action<IPipeContext> Context { get; set; }
}