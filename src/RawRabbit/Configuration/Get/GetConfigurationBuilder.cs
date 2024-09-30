namespace RawRabbit.Configuration.Get;

public class GetConfigurationBuilder : IGetConfigurationBuilder
{
	public GetConfiguration Configuration { get; }

	public GetConfigurationBuilder(GetConfiguration config = null)
	{
		this.Configuration = config ?? new GetConfiguration();
	}
	public IGetConfigurationBuilder FromQueue(string queueName)
	{
		this.Configuration.QueueName = queueName;
		return this;
	}

	public IGetConfigurationBuilder WithNoAck(bool noAck = true)
	{
		return this.WithAutoAck(noAck);
	}

	public IGetConfigurationBuilder WithAutoAck(bool autoAck = true)
	{
		this.Configuration.AutoAck = autoAck;
		return this;
	}
}