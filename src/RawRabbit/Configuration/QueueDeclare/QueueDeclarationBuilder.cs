using RawRabbit.Common;

namespace RawRabbit.Configuration.Queue
{
	public class QueueDeclarationBuilder : IQueueDeclarationBuilder
	{
		public QueueDeclaration Declaration { get;}

		public QueueDeclarationBuilder(QueueDeclaration initialQueue = null)
		{
			this.Declaration = initialQueue ?? QueueDeclaration.Default;
		}

		public IQueueDeclarationBuilder WithName(string queueName)
		{
			Truncator.Truncate(ref queueName);
			this.Declaration.Name = queueName;
			return this;
		}

		public IQueueDeclarationBuilder WithNameSuffix(string suffix)
		{
			this.WithName($"{this.Declaration.Name}_{suffix}");
			return this;
		}

		public IQueueDeclarationBuilder WithAutoDelete(bool autoDelete = true)
		{
			this.Declaration.AutoDelete = autoDelete;
			return this;
		}

		public IQueueDeclarationBuilder WithDurability(bool durable = true)
		{
			this.Declaration.Durable = durable;
			return this;
		}

		public IQueueDeclarationBuilder WithExclusivity(bool exclusive = true)
		{
			this.Declaration.Exclusive = exclusive;
			return this;
		}

		public IQueueDeclarationBuilder WithArgument(string key, object value)
		{
			this.Declaration.Arguments.Add(key, value);
			return this;
		}
	}
}
