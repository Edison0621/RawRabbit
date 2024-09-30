using System;

namespace RawRabbit.Enrichers.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class QueueAttribute : Attribute
{
	internal bool? _nullableDurability;
	internal bool? _nullableExclusitivy;
	internal bool? _nullableAutoDelete;

	public string Name { get; set; }

	public bool Durable
	{
		get { return this._nullableDurability.GetValueOrDefault(); }
		set { this._nullableDurability = value; }
	}

	public bool Exclusive
	{
		get { return this._nullableExclusitivy.GetValueOrDefault(); }
		set { this._nullableExclusitivy = value; }
	}

	public bool AutoDelete
	{
		get { return this._nullableAutoDelete.GetValueOrDefault(); }
		set { this._nullableAutoDelete = value; }
	}
	public int MessageTtl { get; set; }
	public byte MaxPriority { get; set; }
	public string DeadLeterExchange { get; set; }
	public string Mode { get; set; }
}