using System.Collections.Generic;

namespace RawRabbit.Configuration.Queue;

public class QueueDeclaration
{
	public string Name { get; set; }
	public bool Durable { get; set; }
	public bool Exclusive { get; set; }
	public bool AutoDelete { get; set; }
	public Dictionary<string, object> Arguments { get; set; }

	public QueueDeclaration()
	{
		this.Arguments = new Dictionary<string, object>();
	}

	public QueueDeclaration(GeneralQueueConfiguration cfg) : this()
	{
		this.Durable = cfg.Durable;
		this.AutoDelete = cfg.AutoDelete;
		this.Exclusive = cfg.Exclusive;
	}

	public static QueueDeclaration Default => new();
}
