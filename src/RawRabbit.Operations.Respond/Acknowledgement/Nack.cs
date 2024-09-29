using RawRabbit.Common;

namespace RawRabbit.Operations.Respond.Acknowledgement
{
	public class Nack<TResponse> : TypedAcknowlegement<TResponse>
	{
		public bool Requeue { get; set; }

		public Nack(bool requeue = true)
		{
			this.Requeue = requeue;
		}

		public override Common.Acknowledgement AsUntyped()
		{
			return new Nack(this.Requeue);
		}
	}
}