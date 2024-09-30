namespace RawRabbit.Configuration.Get;

public interface IGetConfigurationBuilder
{
	IGetConfigurationBuilder FromQueue(string queueName);
	IGetConfigurationBuilder WithAutoAck(bool autoAck = true);
}
