using RawRabbit.Common;

namespace RawRabbit.Operations.Respond.Acknowledgement;

public class Reject<TResponse> : TypedAcknowlegement<TResponse>
{
	public bool Requeue { get; set; }

	public Reject(bool requeue = true)
	{
		this.Requeue = requeue;
	}

	public override Common.Acknowledgement AsUntyped()
	{
		return new Reject(this.Requeue);
	}
}