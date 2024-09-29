using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RawRabbit.Channel;
using RawRabbit.Configuration;
using RawRabbit.Exceptions;
using Xunit;

namespace RawRabbit.Tests.Channel
{
	public class ChannelFactoryTests
	{
		[Fact]
		public async Task Should_Throw_Exception_If_Connection_Is_Closed_By_Application()
		{
			/* Setup */
			Mock<IConnectionFactory> connectionFactroy = new Mock<IConnectionFactory>();
			Mock<IConnection> connection = new Mock<IConnection>();
			connectionFactroy
				.Setup(c => c.CreateConnection(
					It.IsAny<List<string>>()))
				.Returns(connection.Object);
			connection
				.Setup(c => c.IsOpen)
				.Returns(false);
			connection
				.Setup(c => c.CloseReason)
				.Returns(new ShutdownEventArgs(ShutdownInitiator.Application, 0, string.Empty));
			ChannelFactory channelFactory = new ChannelFactory(connectionFactroy.Object, RawRabbitConfiguration.Local);

			/* Test */
			/* Assert */
			try
			{
				await channelFactory.CreateChannelAsync();
				Assert.True(false, $"Connection is closed by Application, expected {nameof(ChannelAvailabilityException)}.");
			}
			catch (ChannelAvailabilityException e)
			{
				Assert.True(true, e.Message);
			}
		}

		[Fact]
		public async Task Should_Throw_Exception_If_Connection_Is_Closed_By_Lib_But_Is_Not_Recoverable()
		{
			/* Setup */
			Mock<IConnectionFactory> connectionFactroy = new Mock<IConnectionFactory>();
			Mock<IConnection> connection = new Mock<IConnection>();
			connectionFactroy
				.Setup(c => c.CreateConnection(
					It.IsAny<List<string>>()))
				.Returns(connection.Object);
			connection
				.Setup(c => c.IsOpen)
				.Returns(false);
			connection
				.Setup(c => c.CloseReason)
				.Returns(new ShutdownEventArgs(ShutdownInitiator.Library, 0, string.Empty));
			ChannelFactory channelFactory = new ChannelFactory(connectionFactroy.Object, RawRabbitConfiguration.Local);

			/* Test */
			/* Assert */
			try
			{
				await channelFactory.CreateChannelAsync();
				Assert.True(false, $"Connection is closed by Application, expected {nameof(ChannelAvailabilityException)}.");
			}
			catch (ChannelAvailabilityException e)
			{
				Assert.True(true, e.Message);
			}
		}

		[Fact]
		public async Task Should_Return_Channel_From_Connection()
		{
			/* Setup */
			Mock<IModel> channel = new Mock<IModel>();
			Mock<IConnectionFactory> connectionFactroy = new Mock<IConnectionFactory>();
			Mock<IConnection> connection = new Mock<IConnection>();
			connectionFactroy
				.Setup(c => c.CreateConnection(
					It.IsAny<List<string>>()))
				.Returns(connection.Object);
			connection
				.Setup(c => c.CreateModel())
				.Returns(channel.Object);
			connection
				.Setup(c => c.IsOpen)
				.Returns(true);
			ChannelFactory channelFactory = new ChannelFactory(connectionFactroy.Object, RawRabbitConfiguration.Local);

			/* Test */
			IModel retrievedChannel = await channelFactory.CreateChannelAsync();

			/* Assert */
			Assert.Equal(channel.Object, retrievedChannel);
		}

		[Fact]
		public async Task Should_Wait_For_Connection_To_Recover_Before_Returning_Channel()
		{
			/* Setup */
			Mock<IModel> channel = new Mock<IModel>();
			Mock<IConnectionFactory> connectionFactroy = new Mock<IConnectionFactory>();
			Mock<IConnection> connection = new Mock<IConnection>();
			Mock<IRecoverable> recoverable = connection.As<IRecoverable>();
			connectionFactroy
				.Setup(c => c.CreateConnection(
					It.IsAny<List<string>>()))
				.Returns(connection.Object);
			connection
				.Setup(c => c.CreateModel())
				.Returns(channel.Object);
			connection
				.Setup(c => c.IsOpen)
				.Returns(false);
			ChannelFactory channelFactory = new ChannelFactory(connectionFactroy.Object, RawRabbitConfiguration.Local);

			/* Test */
			/* Assert */
			Task<IModel> channelTask = channelFactory.CreateChannelAsync();
			channelTask.Wait(TimeSpan.FromMilliseconds(30));
			Assert.False(channelTask.IsCompleted);

			recoverable.Raise(r => r.Recovery += null, null, null);
			await channelTask;

			Assert.Equal(channel.Object, channelTask.Result);
		}
	}
}
