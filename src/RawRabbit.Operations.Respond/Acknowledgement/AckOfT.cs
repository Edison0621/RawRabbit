﻿namespace RawRabbit.Operations.Respond.Acknowledgement
{
	public class Ack<TResponse> : TypedAcknowlegement<TResponse>
	{
		public TResponse Response { get; set; }

		public Ack(TResponse response)
		{
			this.Response = response;
		}

		public override Common.Acknowledgement AsUntyped()
		{
			return new Ack { Response = this.Response };
		}
	}
}
