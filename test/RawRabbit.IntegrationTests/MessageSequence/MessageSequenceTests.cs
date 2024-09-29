using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RawRabbit.Configuration;
using RawRabbit.Enrichers.GlobalExecutionId;
using RawRabbit.Enrichers.MessageContext;
using RawRabbit.Enrichers.MessageContext.Context;
using RawRabbit.Instantiation;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.Operations.MessageSequence;
using RawRabbit.Operations.MessageSequence.Model;
using Xunit;

namespace RawRabbit.IntegrationTests.MessageSequence
{
	public class MessageSequenceTests
	{
		[Fact]
		public async Task Should_Create_Simple_Chain_Of_One_Send_And_Final_Receive()
		{	
			/* Setup */
			using (Instantiation.Disposable.BusClient client = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
			{
				Plugins = p => p
					.UseGlobalExecutionId()
					.UseStateMachine()
			}))
			{
				await client.SubscribeAsync<BasicRequest, MessageContext>((request, context) =>
					client.PublishAsync(new BasicResponse())
				);

				/* Test */
				MessageSequence<BasicResponse> chain = client.ExecuteSequence(c => c
						.PublishAsync<BasicRequest>()
						.Complete<BasicResponse>()
				);

				await chain.Task;

				/* Assert */
				Assert.True(true, "Received Response");
			}
		}

		[Fact]
		public async Task Should_Create_Chain_With_Publish_When_And_Complete()
		{
			/* Setup */
			using (Instantiation.Disposable.BusClient client = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
			{
				Plugins = p => p
					.UseMessageContext<MessageContext>()
					.UseStateMachine()
					.UseGlobalExecutionId()
			}))
			{
				TaskCompletionSource<SecondMessage> secondTcs = new TaskCompletionSource<SecondMessage>();
				await client.SubscribeAsync<FirstMessage, MessageContext>((request, context) =>
							client.PublishAsync(new SecondMessage())
				);
				await client.SubscribeAsync<SecondMessage, MessageContext>((request, context) =>
							client.PublishAsync(new ThirdMessage())
				);

				/* Test */
				MessageSequence<ThirdMessage> chain = client.ExecuteSequence(c => c
						.PublishAsync<FirstMessage>()
						.When<SecondMessage, MessageContext>((message, context) =>
						{
							secondTcs.TrySetResult(message);
							return Task.FromResult(0);
						})
						.Complete<ThirdMessage>()
				);

				await chain.Task;
				await secondTcs.Task;

				/* Assert */
				Assert.NotNull(secondTcs.Task.Result);
				Assert.True(true, "Received Response");
			}
		}

		[Fact]
		public async Task Should_Abort_Execution_If_Configured_To()
		{
			/* Setup */
			using (Instantiation.Disposable.BusClient client = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
			{
				Plugins = p => p
					.UseStateMachine()
					.UseGlobalExecutionId()
			}))
			{
				TaskCompletionSource<SecondMessage> secondTcs = new TaskCompletionSource<SecondMessage>();
				await client.SubscribeAsync<FirstMessage, MessageContext>((request, context) =>
							client.PublishAsync(new SecondMessage())
				);

				/* Test */
				MessageSequence<ThirdMessage> chain = client.ExecuteSequence(c => c
						.PublishAsync<FirstMessage>()
						.When<SecondMessage, MessageContext>((message, context) =>
						{
							secondTcs.TrySetResult(message);
							return Task.FromResult(0);
						}, it => it.AbortsExecution())
						.Complete<ThirdMessage>()
				);

				await chain.Task;
				await secondTcs.Task;

				/* Assert */
				Assert.NotNull(secondTcs.Task.Result);
				Assert.True(chain.Aborted);
			}
		}

		[Fact]
		public async Task Should_Execute_Sequence_With_Multiple_Whens()
		{
			/* Setup */
			using (Instantiation.Disposable.BusClient client = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
			{
				Plugins = p => p
					.UseStateMachine()
					.UseGlobalExecutionId()
			}))
			{
				TaskCompletionSource<SecondMessage> secondTcs = new TaskCompletionSource<SecondMessage>();
				TaskCompletionSource<ThirdMessage> thirdTsc = new TaskCompletionSource<ThirdMessage>();
				TaskCompletionSource<ForthMessage> forthTcs = new TaskCompletionSource<ForthMessage>();
				await client.SubscribeAsync<FirstMessage, MessageContext>((request, context) =>
							client.PublishAsync(new SecondMessage())
				);
				await client.SubscribeAsync<SecondMessage, MessageContext>((request, context) =>
							client.PublishAsync(new ThirdMessage())
				);
				await client.SubscribeAsync<ThirdMessage, MessageContext>((request, context) =>
							client.PublishAsync(new ForthMessage())
				);
				await client.SubscribeAsync<ForthMessage, MessageContext>((request, context) =>
							client.PublishAsync(new FifthMessage())
				);

				/* Test */
				MessageSequence<FifthMessage> chain = client.ExecuteSequence(c => c
						.PublishAsync<FirstMessage>()
						.When<SecondMessage, MessageContext>((message, context) =>
						{
							secondTcs.TrySetResult(message);
							return Task.FromResult(0);
						})
						.When<ThirdMessage, MessageContext>((message, context) =>
						{
							thirdTsc.TrySetResult(message);
							return Task.FromResult(0);
						})
						.When<ForthMessage, MessageContext>((message, context) =>
						{
							forthTcs.TrySetResult(message);
							return Task.FromResult(0);
						})
						.Complete<FifthMessage>()
				);

				await chain.Task;
				await secondTcs.Task;
				await thirdTsc.Task;
				await forthTcs.Task;

				/* Assert */
				Assert.NotNull(secondTcs.Task.Result);
				Assert.True(true, "Received Response");
			}
		}

		[Fact]
		public async Task Should_Work_For_Concurrent_Sequences()
		{
			/* Setup */
			using (Instantiation.Disposable.BusClient client = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
			{
				Plugins = p => p
					.UseStateMachine()
					.UseGlobalExecutionId()
			}))
			{
				TaskCompletionSource<SecondMessage> secondTcs = new TaskCompletionSource<SecondMessage>();
				await client.SubscribeAsync<FirstMessage, MessageContext>((request, context) =>
							client.PublishAsync(new SecondMessage())
				);
				await client.SubscribeAsync<SecondMessage, MessageContext>((request, context) =>
							client.PublishAsync(new ThirdMessage())
				);

				const int numberOfSequences = 10;
				List<MessageSequence<ThirdMessage>> sequences = new List<MessageSequence<ThirdMessage>>();
				for (int i = 0; i < numberOfSequences; i++)
				{
					MessageSequence<ThirdMessage> seq = client.ExecuteSequence(c => c
							.PublishAsync<FirstMessage>()
							.When<SecondMessage, MessageContext>((message, context) =>
							{
								secondTcs.TrySetResult(message);
								return Task.FromResult(0);
							})
							.Complete<ThirdMessage>()
					);
					sequences.Add(seq);
				}
				Task.WaitAll(sequences.Select(s => s.Task).ToArray<Task>());

				/* Test */

				/* Assert */
				Assert.All(sequences, s => Assert.False(s.Aborted));
			}
		}

		[Fact]
		public async Task Should_Not_Invoke_Handler_If_Previous_Mandatory_Handler_Not_Invoked()
		{
			/* Setup */
			using (Instantiation.Disposable.BusClient client = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
			{
				Plugins = p => p
					.UseStateMachine()
					.UseGlobalExecutionId()
			}))
			{
				TaskCompletionSource<FirstMessage> firstTcs = new TaskCompletionSource<FirstMessage>();
				TaskCompletionSource<SecondMessage> secondTcs = new TaskCompletionSource<SecondMessage>();
				TaskCompletionSource<ThirdMessage> thirdTcs = new TaskCompletionSource<ThirdMessage>();
				await client.SubscribeAsync<FirstMessage, MessageContext>((request, context) =>
				{
					firstTcs.TrySetResult(request);
					return client.PublishAsync(new ThirdMessage());
				});

				/* Test */
				client.ExecuteSequence(c => c
					.PublishAsync<FirstMessage>()
					.When<SecondMessage, MessageContext>((message, context) =>
						{
							secondTcs.TrySetResult(message);
							return Task.FromResult(0);
						})
					.When<ThirdMessage, MessageContext>((message, context) =>
						{
							thirdTcs.TrySetResult(message);
							return Task.FromResult(0);
						})
					.Complete<ForthMessage>()
				);

				await firstTcs.Task;
				Task.WaitAll(new Task[] {secondTcs.Task, thirdTcs.Task}, TimeSpan.FromMilliseconds(400));
				secondTcs.Task.Wait(TimeSpan.FromMilliseconds(400));

				/* Assert */
				Assert.False(secondTcs.Task.IsCompleted);
				Assert.False(thirdTcs.Task.IsCompleted);
			}
		}

		[Fact]
		public async Task Should_Honor_Timeout()
		{
			/* Setup */
			RawRabbitConfiguration cfg = RawRabbitConfiguration.Local;
			cfg.RequestTimeout = TimeSpan.FromMilliseconds(200);
			using (Instantiation.Disposable.BusClient client = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
			{
				Plugins = p => p
					.UseStateMachine()
					.UseGlobalExecutionId(),
				ClientConfiguration = cfg
			}))
			{
				/* Test */
				MessageSequence<SecondMessage> chain = client.ExecuteSequence(c => c
					.PublishAsync<FirstMessage>()
					.Complete<SecondMessage>()
				);

				/* Assert */
				await Assert.ThrowsAsync<TimeoutException>(async () => await chain.Task);
			}
		}

		[Fact]
		public async Task Should_Forward_Message_Context_In_When_Message_Handler()
		{
			/* Setup */
			using(Instantiation.Disposable.BusClient client = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
			{
				Plugins = p => p
					.UseMessageContext<MessageContext>()
					.UseContextForwarding()
					.UseGlobalExecutionId()
			}))
			using (Instantiation.Disposable.BusClient sequenceClient = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
			{
				Plugins = p => p
					.UseStateMachine()
					.UseMessageContext(context => new MessageContext { GlobalRequestId = Guid.NewGuid()})
					.UseContextForwarding()
					.UseGlobalExecutionId()
			}))
			{
				TaskCompletionSource<MessageContext> firstTcs = new TaskCompletionSource<MessageContext>();
				TaskCompletionSource<MessageContext> secondTcs = new TaskCompletionSource<MessageContext>();
				await client.SubscribeAsync<FirstMessage, MessageContext>((request, context) =>
					sequenceClient.PublishAsync(new SecondMessage())
				);
				await client.SubscribeAsync<ThirdMessage, MessageContext>(async (request, context) =>
				{
					secondTcs.TrySetResult(context);
					await sequenceClient.PublishAsync(new ForthMessage());
				});

				/* Test */
				sequenceClient.ExecuteSequence(c => c
					.PublishAsync<FirstMessage>()
					.When<SecondMessage, MessageContext>(async (message, context) =>
					{
						firstTcs.TrySetResult(context);
						await sequenceClient.PublishAsync(new ThirdMessage());
					})
					.Complete<ForthMessage>()
				);

				await firstTcs.Task;
				await secondTcs.Task;

				/* Assert */
				Assert.Equal(firstTcs.Task.Result.GlobalRequestId, secondTcs.Task.Result.GlobalRequestId);
			}
		}

		[Fact]
		public async Task Should_Work_With_Generic_Messages()
		{
			using (Instantiation.Disposable.BusClient client = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
			{
				Plugins = p => p
					.UseStateMachine()
					.UseMessageContext(context => new MessageContext {GlobalRequestId = Guid.NewGuid()})
					.UseContextForwarding()
					.UseGlobalExecutionId()
			}))
			{
				/* Setup */
				GenericMessage<Guid> guidMsg = new GenericMessage<Guid>{ Prop = Guid.NewGuid()};
				await client.SubscribeAsync<GenericMessage<string>>(message =>
					client.PublishAsync(guidMsg)
				);

				/* Test */
				MessageSequence<GenericMessage<Guid>> sequence = client.ExecuteSequence(s => s
					.PublishAsync(new GenericMessage<string>())
					.Complete<GenericMessage<Guid>>()
				);

				/* Assert */
				Assert.Equal(sequence.Task.Result.Prop, guidMsg.Prop);
			}
		}

		[Fact]
		public async Task Should_Support_Chained_Message_Sequences()
		{
			RawRabbitOptions sequenceOptions = new RawRabbitOptions
			{
				Plugins = p => p
					.UseStateMachine()
					.UseMessageContext(context => new MessageContext {GlobalRequestId = Guid.NewGuid()})
					.UseContextForwarding()
					.UseGlobalExecutionId()
			};
			using (Instantiation.Disposable.BusClient serviceA = RawRabbitFactory.CreateTestClient(sequenceOptions))
			using (Instantiation.Disposable.BusClient serviceB = RawRabbitFactory.CreateTestClient(sequenceOptions))
			using (Instantiation.Disposable.BusClient serviceC = RawRabbitFactory.CreateTestClient(sequenceOptions))
			{
				/* Setup */
				await serviceB.SubscribeAsync<FirstMessage>(async message =>
					{
						MessageSequence<ThirdMessage> nestedSequence = serviceB.ExecuteSequence(s => s
							.PublishAsync(new SecondMessage())
							.Complete<ThirdMessage>());
						await nestedSequence.Task;
						await serviceB.PublishAsync(new ForthMessage());
					}
				);
				await serviceC.SubscribeAsync<SecondMessage>(message => serviceC.PublishAsync(new ThirdMessage()));

				/* Test */
				MessageSequence<ForthMessage> mainSequence = serviceA.ExecuteSequence(s => s
					.PublishAsync(new FirstMessage())
					.Complete<ForthMessage>()
				);
				await mainSequence.Task;

				/* Assert */
				Assert.True(true, "Shoud complete sequence");
			}
		}
	}
}
