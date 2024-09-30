using System.Collections.Generic;

namespace RawRabbit.Operations.StateMachine.Trigger;

public abstract class TriggerConfigurationCollection
{
	public List<TriggerConfiguration> GetTriggerConfiguration()
	{
		TriggerConfigurer configurer = new();
		this.ConfigureTriggers(configurer);
		return configurer.TriggerConfiguration;
	}

	public abstract void ConfigureTriggers(TriggerConfigurer trigger);
}