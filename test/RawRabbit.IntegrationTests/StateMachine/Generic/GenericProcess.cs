using System;
using System.Threading.Tasks;
using RawRabbit.Operations.StateMachine;
using Stateless;

namespace RawRabbit.IntegrationTests.StateMachine.Generic
{
	public class GenericProcess : StateMachineBase<State, Trigger, GenericProcessModel>
	{
		private readonly IBusClient _client;
		private StateMachine<State, Trigger>.TriggerWithParameters<string> _cancel;
		private StateMachine<State, Trigger>.TriggerWithParameters<string> _pause;

		public GenericProcess(IBusClient client, GenericProcessModel model = null) : base(model)
		{
			this._client = client;
		}

		protected override void ConfigureState(StateMachine<State, Trigger> process)
		{
			this._cancel = process.SetTriggerParameters<string>(Trigger.Cancel);
			this._pause = process.SetTriggerParameters<string>(Trigger.Pausing);

			process
				.Configure(State.Created)
				.PermitIf(Trigger.Start, State.InProgress, this.IsAssigned)
				.Permit(Trigger.Cancel, State.Aborted);

			process
				.Configure(State.InProgress)
				.OnEntryAsync(() => this.SendUpdateMessage())
				.Permit(Trigger.Completion, State.Completed)
				.Permit(Trigger.Pausing, State.Paused)
				.Permit(Trigger.Cancel, State.Aborted);

			process
				.Configure(State.Paused)
				.OnEntryFromAsync(this._pause, this.SendUpdateMessage)
				.PermitIf(Trigger.Resuming, State.InProgress, this.IsAssigned)
				.Permit(Trigger.Cancel, State.Aborted);

			process
				.Configure(State.Aborted)
				.OnEntryFromAsync(this._cancel, this.SendAbortMessage);

			process
				.Configure(State.Completed)
				.OnEntryAsync(this.SendCompletionMessage);
		}

		private Task SendCompletionMessage()
		{
			return this._client.PublishAsync(new ProcessCompeted
			{
				TaskId = this._model.Id
			});
		}

		private Task SendAbortMessage(string reason)
		{
			return this._client.PublishAsync(new ProcessAborted
			{
				TaskId = this._model.Id,
				Reason = reason
			});
		}

		private bool IsAssigned()
		{
			return !string.IsNullOrWhiteSpace(this._model.Assignee);
		}

		private Task SendUpdateMessage(string message = null)
		{
			return this._client.PublishAsync(new ProcessUpdated
			{
				TaskId = this._model.Id,
				State = this._model.State,
				Assignee = this._model.Assignee,
				Message = message
			});
		}

		public Task StartAsync(string assignee)
		{
			this._model.Assignee = assignee;
			return this._stateMachine.FireAsync(Trigger.Start);
		}

		public Task PauseAsync(string reason)
		{
			return this._stateMachine.FireAsync(this._pause, reason);
		}

		public Task ResumeAsync()
		{
			return this._stateMachine.FireAsync(Trigger.Resuming);
		}

		public void Abort(string reason)
		{
			this._stateMachine.Fire(this._cancel, reason);
		}

		public Task CreateAsync(string process, DateTime deadline)
		{
			this._model.Name = process;
			this._model.Deadline = deadline;
			this._model.Id = Guid.NewGuid();
			return this._client.PublishAsync(new TaskCreated
			{
				Name = process,
				TaskId = this._model.Id
			});
		}

		public Task CompleteAsync()
		{
			return this._stateMachine.FireAsync(Trigger.Completion);
		}

		public override GenericProcessModel Initialize()
		{
			return new GenericProcessModel
			{
				State = State.Created,
				Id = Guid.NewGuid()
			};
		}
	}
}
