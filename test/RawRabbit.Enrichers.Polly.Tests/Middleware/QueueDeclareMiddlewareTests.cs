using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Polly;
using RabbitMQ.Client.Exceptions;
using RawRabbit.Common;
using RawRabbit.Configuration.Queue;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using Xunit;
using QueueDeclareMiddleware = RawRabbit.Enrichers.Polly.Middleware.QueueDeclareMiddleware;

namespace RawRabbit.Enrichers.Polly.Tests.Middleware
{
	public class QueueDeclareMiddlewareTests
	{
		[Fact]
		public async Task Should_Invoke_Queue_Declare_Policy_With_Correct_Context()
		{
			Mock<ITopologyProvider> topology = new();
			QueueDeclaration queueDeclaration = new();
			bool policyCalled = false;
			Context capturedContext = null;

			topology
				.SetupSequence(t => t.DeclareQueueAsync(queueDeclaration))
				.Throws(new OperationInterruptedException(null))
				.Returns(Task.CompletedTask);

			PipeContext context = new()
			{
				Properties = new Dictionary<string, object>
				{
					{PipeKey.QueueDeclaration, queueDeclaration}
				}
			};

			context.UsePolicy(Policy
				.Handle<OperationInterruptedException>()
				.RetryAsync((exception, retryCount, pollyContext) =>
				{
					policyCalled = true;
					capturedContext = pollyContext;
				}), PolicyKeys.QueueDeclare);
			QueueDeclareMiddleware middleware = new(topology.Object) {Next = new NoOpMiddleware()};

			/* Test */
			await middleware.InvokeAsync(context);

			/* Assert */
			Assert.True(policyCalled, "Should call policy");
			Assert.Equal(context, capturedContext[RetryKey.PipeContext]);
			Assert.Equal(queueDeclaration, capturedContext[RetryKey.QueueDeclaration]);
			Assert.Equal(topology.Object, capturedContext[RetryKey.TopologyProvider]);
		}
	}
}
