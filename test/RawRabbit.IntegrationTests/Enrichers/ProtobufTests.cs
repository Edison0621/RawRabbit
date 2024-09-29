﻿using System;
using System.Threading.Tasks;
using ProtoBuf;
using RawRabbit.Exceptions;
using RawRabbit.Instantiation;
using RawRabbit.Pipe.Middleware;
using Xunit;
// ReSharper disable All
#pragma warning disable CS1587 // XML comment is not placed on a valid language element

namespace RawRabbit.IntegrationTests.Enrichers
{
	public class ProtobufTests
	{
		[Fact]
		public async Task Should_Deliver_And_Reiieve_Messages_Serialized_With_Protobuf()
		{
			using (Instantiation.Disposable.BusClient client = RawRabbitFactory.CreateTestClient(new RawRabbitOptions{ Plugins = p => p.UseProtobuf() }))
			{
				/** Setup **/
				TaskCompletionSource<ProtoMessage> tcs = new TaskCompletionSource<ProtoMessage>();
				ProtoMessage message = new ProtoMessage
				{
					Member = "Straight into bytes",
					Id = Guid.NewGuid()
				};
				await client.SubscribeAsync<ProtoMessage>(msg =>
				{
					tcs.TrySetResult(msg);
					return Task.CompletedTask;
				});

				/** Test **/
				await client.PublishAsync(message);
				await tcs.Task;

				/** Assert **/
				Assert.Equal(tcs.Task.Result.Id, message.Id);
				Assert.Equal(tcs.Task.Result.Member, message.Member);
			}
		}

		[Fact]
		public async Task Should_Perform_Rpc_With_Messages_Serialized_With_Protobuf()
		{
			using (Instantiation.Disposable.BusClient client = RawRabbitFactory.CreateTestClient(new RawRabbitOptions {Plugins = p => p.UseProtobuf()}))
			{
				/* Setup */
				ProtoResponse response = new ProtoResponse {Id = Guid.NewGuid()};
				await client.RespondAsync<ProtoRequest, ProtoResponse>(request => Task.FromResult(response));

				/* Test */
				ProtoResponse received = await client.RequestAsync<ProtoRequest, ProtoResponse>(new ProtoRequest());

				/* Assert */
				Assert.Equal(received.Id, response.Id);
			}
		}

		[Fact]
		public async Task Should_Publish_Message_To_Error_Exchange_If_Serializer_Mismatch()
		{
			using (Instantiation.Disposable.BusClient protobufClient = RawRabbitFactory.CreateTestClient(new RawRabbitOptions { Plugins = p => p.UseProtobuf() }))
			using (Instantiation.Disposable.BusClient jsonClient = RawRabbitFactory.CreateTestClient())
			{
				/** Setup **/
				bool handlerInvoked = false;
				TaskCompletionSource<ProtoMessage> tcs = new TaskCompletionSource<ProtoMessage>();
				ProtoMessage message = new ProtoMessage
				{
					Member = "Straight into bytes",
					Id = Guid.NewGuid()
				};
				await jsonClient.SubscribeAsync<ProtoMessage>(msg =>
				{
					handlerInvoked = true; // Should never get here
					return Task.CompletedTask;
				});
				await protobufClient.SubscribeAsync<ProtoMessage>(msg =>
				{
					tcs.TrySetResult(msg);
					return Task.CompletedTask;
				}, ctx => ctx.UseSubscribeConfiguration(cfg => cfg
					.FromDeclaredQueue(q => q.WithName("error_queue"))
					.OnDeclaredExchange(e => e.WithName("default_error_exchange"))
				));

				/** Test **/
				await protobufClient.PublishAsync(message);
				await tcs.Task;

				/** Assert **/
				Assert.False(handlerInvoked);
				Assert.Equal(tcs.Task.Result.Id, message.Id);
				Assert.Equal(tcs.Task.Result.Member, message.Member);
			}
		}

		[Fact]
		public async Task Should_Throw_Exception_If_Responder_Can_Not_Deserialize_Request_And_Content_Type_Check_Is_Activated()
		{
			using (Instantiation.Disposable.BusClient protobufClient = RawRabbitFactory.CreateTestClient(new RawRabbitOptions { Plugins = p => p.UseProtobuf() }))
			using (Instantiation.Disposable.BusClient jsonClient = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				await jsonClient.RespondAsync<ProtoRequest, ProtoResponse>(request =>
					Task.FromResult(new ProtoResponse())
				);

				/* Test */
				/* Assert */
				MessageHandlerException e = await Assert.ThrowsAsync<MessageHandlerException>(() =>
					protobufClient.RequestAsync<ProtoRequest, ProtoResponse>(new ProtoRequest {  Id = Guid.NewGuid()}
				, ctx => ctx.UseContentTypeCheck()));
			}
		}
	}

	[ProtoContract]
	public class ProtoMessage
	{
		[ProtoMember(1)]
		public Guid Id { get; set; }

		[ProtoMember(2)]
		public string Member { get; set; }
	}

	[ProtoContract]
	public class ProtoRequest
	{
		[ProtoMember(1)]
		public Guid Id { get; set; }
	}

	[ProtoContract]
	public class ProtoResponse
	{
		[ProtoMember(1)]
		public Guid Id { get; set; }
	}
}
