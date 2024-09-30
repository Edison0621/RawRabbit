using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using RawRabbit.Configuration;
using RawRabbit.Enrichers.Polly.Services;
using Xunit;

namespace RawRabbit.Enrichers.Polly.Tests.Services
{
	public class ChannelFactoryTests
	{
		[Fact]
		public async Task Should_Use_Connect_Policy_When_Connecting_To_Broker()
		{
			/* Setup */
			Mock<IConnection> connection = new();
			Mock<IConnectionFactory> connectionFactory = new();
			connectionFactory
				.Setup(f => f.CreateConnectionAsync(CancellationToken.None).GetAwaiter().GetResult())
				.Returns(connection.Object);
			connectionFactory
				.SetupSequence(c => c.CreateConnectionAsync(
						It.IsAny<List<string>>(),CancellationToken.None
					).GetAwaiter().GetResult())
				.Throws(new BrokerUnreachableException(new Exception()))
				.Throws(new BrokerUnreachableException(new Exception()))
				.Throws(new BrokerUnreachableException(new Exception()))
				.Returns(connection.Object);

			AsyncRetryPolicy policy = Policy
				.Handle<BrokerUnreachableException>()
				.WaitAndRetryAsync(new[]
				{
					TimeSpan.FromMilliseconds(1),
					TimeSpan.FromMilliseconds(2),
					TimeSpan.FromMilliseconds(4),
					TimeSpan.FromMilliseconds(8),
					TimeSpan.FromMilliseconds(16)
				});

			ChannelFactory factory = new(connectionFactory.Object, RawRabbitConfiguration.Local, new ConnectionPolicies{ Connect = policy});

			/* Test */
			/* Assert */
			await factory.ConnectAsync();
		}

		[Fact]
		public async Task Should_Use_Create_Channel_Policy_When_Creaing_Channels()
		{
			/* Setup */
			Mock<IChannel> channel = new();
			Mock<IConnection> connection = new();
			Mock<IConnectionFactory> connectionFactory = new();
			connectionFactory
				.Setup(f => f.CreateConnectionAsync(CancellationToken.None).GetAwaiter().GetResult())
				.Returns(connection.Object);
			connectionFactory
				.Setup(c => c.CreateConnectionAsync(
					It.IsAny<List<string>>(), CancellationToken.None
				).GetAwaiter().GetResult())
				.Returns(connection.Object);
			connection
				.Setup(c => c.IsOpen)
				.Returns(true);
			connection
				.SetupSequence(c => c.CreateChannelAsync(It.IsAny<ushort?>(), CancellationToken.None).GetAwaiter().GetResult())
				.Throws(new TimeoutException())
				.Throws(new TimeoutException())
				.Returns(channel.Object);

			AsyncRetryPolicy policy = Policy
				.Handle<TimeoutException>()
				.WaitAndRetryAsync(new[]
				{
					TimeSpan.FromMilliseconds(1),
					TimeSpan.FromMilliseconds(2),
					TimeSpan.FromMilliseconds(4),
					TimeSpan.FromMilliseconds(8),
					TimeSpan.FromMilliseconds(16)
				});

			ChannelFactory factory = new(connectionFactory.Object, RawRabbitConfiguration.Local, new ConnectionPolicies { CreateChannel = policy });

			/* Test */
			IChannel retrievedChannel = await  factory.CreateChannelAsync();

			/* Assert */
			Assert.Equal(channel.Object, retrievedChannel);
		}
	}
}
