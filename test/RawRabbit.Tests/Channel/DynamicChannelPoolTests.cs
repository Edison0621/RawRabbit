using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RawRabbit.Channel;
using Xunit;

namespace RawRabbit.Tests.Channel
{
	public class DynamicChannelPoolTests
	{
		[Fact]
		public async Task Should_Be_Able_To_Add_And_Use_Channels()
		{
			/* Setup */
			List<Mock<IModel>> channels = new List<Mock<IModel>> {new Mock<IModel>(), new Mock<IModel>(), new Mock<IModel>()};
			foreach (Mock<IModel> channel in channels)
			{
				channel
					.Setup(c => c.IsClosed)
					.Returns(false);
			}
			DynamicChannelPool pool = new DynamicChannelPool();
			pool.Add(channels.Select(c => c.Object));

			/* Test */
			IModel firstChannel = await pool.GetAsync();
			IModel secondChannel = await pool.GetAsync();
			IModel thirdChannel = await pool.GetAsync();

			/* Assert */
			Assert.Equal(firstChannel, channels[0].Object);
			Assert.Equal(secondChannel, channels[1].Object);
			Assert.Equal(thirdChannel, channels[2].Object);
		}

		[Fact]
		public void Should_Not_Throw_Exception_If_Trying_To_Remove_Channel_Not_In_Pool()
		{
			/* Setup */
			DynamicChannelPool pool = new DynamicChannelPool();
			Mock<IModel> channel = new Mock<IModel>();

			/* Test */
			pool.Remove(channel.Object);

			/* Assert */
			Assert.True(true, "Successfully remove a channel not in the pool");
		}

		[Fact]
		public async Task Should_Remove_Channels_Based_On_Count()
		{
			/* Setup */
			List<Mock<IModel>> channels = new List<Mock<IModel>>
			{
				new Mock<IModel>{ Name = "First" },
				new Mock<IModel>{ Name = "Second"},
				new Mock<IModel>{ Name = "Third"}
			};
			foreach (Mock<IModel> channel in channels)
			{
				channel
					.Setup(c => c.IsOpen)
					.Returns(true);
			}
			DynamicChannelPool pool = new DynamicChannelPool(channels.Select(c => c.Object));

			/* Test */
			pool.Remove(2);
			IModel firstChannel = await pool.GetAsync();
			IModel secondChannel = await pool.GetAsync();
			IModel thirdChannel = await pool.GetAsync();

			/* Assert */
			Assert.Equal(firstChannel, channels[2].Object);
			Assert.Equal(secondChannel, channels[2].Object);
			Assert.Equal(thirdChannel, channels[2].Object);
		}
	}
}
