using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RawRabbit.Instantiation;
using RawRabbit.IntegrationTests.StateMachine.Generic;
using RawRabbit.Operations.StateMachine;
using Xunit;
// ReSharper disable All

namespace RawRabbit.IntegrationTests.StateMachine
{
	public class StateMachineTests
	{
		[Fact]
		public async Task Should_Complete_Generic_Task()
		{
			using (Instantiation.Disposable.BusClient processOwner = RawRabbitFactory.CreateTestClient(new RawRabbitOptions { Plugins = p => p.UseStateMachine() }))
			using (Instantiation.Disposable.BusClient worker = RawRabbitFactory.CreateTestClient())
			using (Instantiation.Disposable.BusClient initiator = RawRabbitFactory.CreateTestClient())
			using (Instantiation.Disposable.BusClient observer = RawRabbitFactory.CreateTestClient())
			{
				TaskCompletionSource<ProcessCompeted> tsc = new TaskCompletionSource<ProcessCompeted>();
				List<ProcessUpdated> updates = new List<ProcessUpdated>();
				await processOwner.RegisterStateMachineAsync<ProcessTriggers>();
				await observer.SubscribeAsync<ProcessCompeted>(competed =>
				{
					tsc.TrySetResult(competed);
					return Task.FromResult(0);
				});
				await observer.SubscribeAsync<ProcessUpdated>(updated =>
				{
					updates.Add(updated);
					return Task.FromResult(0);
				});
				await worker.SubscribeAsync<TaskCreated>(async msg =>
				{
					await worker.PublishAsync(new StartTask
					{
						Assignee = "Luke Skyworker",
						TaskId = msg.TaskId
					});
					await Task.Delay(TimeSpan.FromMilliseconds(30));
					await worker.PublishAsync(new PauseTask
					{
						TaskId = msg.TaskId,
						Reason = "Need to repair 3CPO first."
					});
					await Task.Delay(TimeSpan.FromMilliseconds(30));
					await worker.PublishAsync(new ResumeTask
					{
						TaskId = msg.TaskId,
						Message = "Back to work"
					});
					await Task.Delay(TimeSpan.FromMilliseconds(30));
					await worker.PublishAsync(new CompleteTask
					{
						TaskId = msg.TaskId
					});
				});
				await initiator.PublishAsync(new CreateTask
				{
					Name = "Destroy Death Star.",
					DeadLine = DateTime.Today.AddDays(2)
				});
				await tsc.Task;
				Assert.True(true);
			}
		}
	}
}
