namespace RawRabbit.Operations.Get.Model
{
	public static class AckableExtensions
	{
		public static Ackable<TType> AsAckable<TType>(this Ackable<object> ackable)
		{
			return new Ackable<TType>((TType)ackable.Content, ackable._channel, type => ackable._deliveryTagFunc(type));
		}
	}
}