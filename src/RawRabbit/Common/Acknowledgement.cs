namespace RawRabbit.Common
{
	public abstract class Acknowledgement { }

	public class Ack : Acknowledgement
	{ }

	public class Nack : Acknowledgement
	{
		public bool Requeue { get; set; }

		public Nack(bool requeue = true)
		{
			this.Requeue = requeue;
		}
	}

	public class Reject : Acknowledgement
	{
		public bool Requeue { get; set; }

		public Reject(bool requeue = true)
		{
			this.Requeue = requeue;
		}
	}
}
