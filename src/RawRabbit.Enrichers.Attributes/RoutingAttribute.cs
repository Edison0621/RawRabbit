using System;

namespace RawRabbit.Enrichers.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class RoutingAttribute : Attribute
{
	internal bool? _nullableAutoAck;

	public string RoutingKey { get; set; }
	public ushort PrefetchCount { get; set; }
	[Obsolete("Property name changed. Use 'WithAutoAck' instead.")]
	public bool NoAck { get { return this._nullableAutoAck.GetValueOrDefault(); } set { this._nullableAutoAck = value; } }
	public bool AutoAck { get { return this._nullableAutoAck.GetValueOrDefault(); } set { this._nullableAutoAck = value; } }
}