﻿using System;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using RawRabbit.IntegrationTests.TestMessages;
using Xunit;

namespace RawRabbit.IntegrationTests.PublishAndSubscribe;

public class MandatoryCallbackTests
{
	[Fact]
	public async Task Should_Invoke_Mandatory_Callback_If_Message_Is_Undelivered()
	{
		using Instantiation.Disposable.BusClient publisher = RawRabbitFactory.CreateTestClient();
		/* Setup */
		TaskCompletionSource<BasicReturnEventArgs> callbackTcs = new();

		/* Test */
		await publisher.PublishAsync(new BasicMessage {Prop = "Hello, world!"}, ctx => ctx
			.UsePublishConfiguration(cfg => cfg
				.WithReturnCallback(args =>
				{
					callbackTcs.TrySetResult(args);
				})
			));
		await callbackTcs.Task;

		/* Assert */
		Assert.True(true);
	}

	[Fact]
	public async Task Should_Not_Invoke_Mandatory_Callback_If_Message_Is_Undelivered()
	{
		using Instantiation.Disposable.BusClient publisher = RawRabbitFactory.CreateTestClient();
		using Instantiation.Disposable.BusClient subscriber = RawRabbitFactory.CreateTestClient();
		/* Setup */
		TaskCompletionSource<BasicMessage> deliveredTcs = new();
		TaskCompletionSource<BasicReturnEventArgs> callbackTcs = new();
		await subscriber.SubscribeAsync<BasicMessage>(message =>
		{
			deliveredTcs.TrySetResult(message);
			return Task.FromResult(0);
		});

		/* Test */
		await publisher.PublishAsync(new BasicMessage {Prop = "Hello, world!"}, ctx => ctx
			.UsePublishConfiguration(cfg => cfg
				.WithReturnCallback(args =>
				{
					callbackTcs.TrySetResult(args);
				})
			));
		await deliveredTcs.Task;
		callbackTcs.Task.Wait(TimeSpan.FromMilliseconds(200));

		/* Assert */
		Assert.False(callbackTcs.Task.IsCompleted);
	}
}
