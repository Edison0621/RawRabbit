using System;
using RawRabbit.Configuration.Exchange;

namespace RawRabbit.Enrichers.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class ExchangeAttribute : Attribute
{
	internal bool? _nullableDurability;
	internal bool? _nullableAutoDelete;

	public string Name { get; set; }
	public ExchangeType Type { get; set; }
	public bool Durable { set { this._nullableDurability = value; } }
	public bool AutoDelete { set { this._nullableAutoDelete = value; } }
}