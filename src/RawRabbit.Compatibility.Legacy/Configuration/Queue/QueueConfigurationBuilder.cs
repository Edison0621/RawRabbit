namespace RawRabbit.Compatibility.Legacy.Configuration.Queue
{
	public class QueueConfigurationBuilder : IQueueConfigurationBuilder
	{
		public QueueConfiguration Configuration { get; }

		public QueueConfigurationBuilder(QueueConfiguration initialQueue = null)
		{
			this.Configuration = initialQueue ?? QueueConfiguration.Default;
		}

		public IQueueConfigurationBuilder WithName(string queueName)
		{
			this.Configuration.QueueName = queueName;
			return this;
		}

		public IQueueConfigurationBuilder WithNameSuffix(string suffix)
		{
			this.Configuration.NameSuffix = suffix;
			return this;
		}

		public IQueueConfigurationBuilder WithAutoDelete(bool autoDelete = true)
		{
			this.Configuration.AutoDelete = autoDelete;
			return this;
		}

		public IQueueConfigurationBuilder WithDurability(bool durable = true)
		{
			this.Configuration.Durable = durable;
			return this;
		}

		public IQueueConfigurationBuilder WithExclusivity(bool exclusive = true)
		{
			this.Configuration.Exclusive = exclusive;
			return this;
		}

		public IQueueConfigurationBuilder WithArgument(string key, object value)
		{
			this.Configuration.Arguments.Add(key, value);
			return this;
		}

		public IQueueConfigurationBuilder AssumeInitialized(bool asumption = true)
		{
			this.Configuration.AssumeInitialized = asumption;
			return this;
		}
	}
}
