﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RawRabbit.Channel;
using Xunit;

namespace RawRabbit.Tests.Channel;

public class DynamicChannelPoolTests
{
	[Fact]
	public async Task Should_Be_Able_To_Add_And_Use_Channels()
	{
		/* Setup */
		List<Mock<IChannel>> channels = [new(), new(), new()];
		foreach (Mock<IChannel> channel in channels)
		{
			channel
				.Setup(c => c.IsClosed)
				.Returns(false);
		}
		DynamicChannelPool pool = new();
		pool.Add(channels.Select(c => c.Object));

		/* Test */
		IChannel firstChannel = await pool.GetAsync();
		IChannel secondChannel = await pool.GetAsync();
		IChannel thirdChannel = await pool.GetAsync();

		/* Assert */
		Assert.Equal(firstChannel, channels[0].Object);
		Assert.Equal(secondChannel, channels[1].Object);
		Assert.Equal(thirdChannel, channels[2].Object);
	}

	[Fact]
	public void Should_Not_Throw_Exception_If_Trying_To_Remove_Channel_Not_In_Pool()
	{
		/* Setup */
		DynamicChannelPool pool = new();
		Mock<IChannel> channel = new();

		/* Test */
		pool.Remove(channel.Object);

		/* Assert */
		Assert.True(true, "Successfully remove a channel not in the pool");
	}

	[Fact]
	public async Task Should_Remove_Channels_Based_On_Count()
	{
		/* Setup */
		List<Mock<IChannel>> channels =
		[
			new() { Name = "First" },
			new() { Name = "Second" },
			new() { Name = "Third" }
		];
		foreach (Mock<IChannel> channel in channels)
		{
			channel
				.Setup(c => c.IsOpen)
				.Returns(true);
		}
		DynamicChannelPool pool = new(channels.Select(c => c.Object));

		/* Test */
		pool.Remove(2);
		IChannel firstChannel = await pool.GetAsync();
		IChannel secondChannel = await pool.GetAsync();
		IChannel thirdChannel = await pool.GetAsync();

		/* Assert */
		Assert.Equal(firstChannel, channels[2].Object);
		Assert.Equal(secondChannel, channels[2].Object);
		Assert.Equal(thirdChannel, channels[2].Object);
	}
}
