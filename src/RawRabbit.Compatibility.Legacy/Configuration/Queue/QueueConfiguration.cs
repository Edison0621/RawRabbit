using System.Collections.Generic;
using RawRabbit.Configuration;

namespace RawRabbit.Compatibility.Legacy.Configuration.Queue
{
	public class QueueConfiguration
	{
		public string FullQueueName
		{
			get
			{
				string fullQueueName =  string.IsNullOrEmpty(this.NameSuffix)
					? this.QueueName
					: $"{this.QueueName}_{this.NameSuffix}";

				return fullQueueName.Length > 254
					? string.Concat("...", fullQueueName.Substring(fullQueueName.Length - 250))
					: fullQueueName;
			}
		}

		public string QueueName { get; set; }
		public string NameSuffix { get; set; }
		public bool Durable { get; set; }
		public bool Exclusive { get; set; }
		public bool AutoDelete { get; set; }
		public Dictionary<string, object> Arguments { get; set; }
		public bool AssumeInitialized { get; set; }

		public QueueConfiguration()
		{
			this.Arguments = new Dictionary<string, object>();
		}

		public QueueConfiguration(GeneralQueueConfiguration cfg) : this()
		{
			this.Durable = cfg.Durable;
			this.AutoDelete = cfg.AutoDelete;
			this.Exclusive = cfg.Exclusive;
		}

		public static QueueConfiguration Default => new QueueConfiguration();
	}
}
