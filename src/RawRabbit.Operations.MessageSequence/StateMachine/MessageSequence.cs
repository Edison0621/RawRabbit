using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.Logging;
using RawRabbit.Operations.MessageSequence.Configuration;
using RawRabbit.Operations.MessageSequence.Configuration.Abstraction;
using RawRabbit.Operations.MessageSequence.Model;
using RawRabbit.Operations.MessageSequence.Trigger;
using RawRabbit.Operations.StateMachine;
using RawRabbit.Operations.StateMachine.Trigger;
using RawRabbit.Pipe;
using RawRabbit.Subscription;
using Stateless;

namespace RawRabbit.Operations.MessageSequence.StateMachine
{
	public class MessageSequence : StateMachineBase<SequenceState, Type, SequenceModel>,
			IMessageChainPublisher, IMessageSequenceBuilder
	{
		private readonly IBusClient _client;
		private readonly INamingConventions _naming;
		private readonly RawRabbitConfiguration _clientCfg;
		private Action _fireAction;
		private readonly TriggerConfigurer _triggerConfigurer;
		private readonly Queue<StepDefinition> _stepDefinitions;
		private readonly List<ISubscription> _subscriptions;
		private readonly ILog _logger = LogProvider.For<MessageSequence>();
		private IChannel _channel;

		public MessageSequence(IBusClient client, INamingConventions naming, RawRabbitConfiguration clientCfg, SequenceModel model = null) : base(model)
		{
			this._client = client;
			this._naming = naming;
			this._clientCfg = clientCfg;
			this._triggerConfigurer = new TriggerConfigurer();
			this._stepDefinitions = new Queue<StepDefinition>();
			this._subscriptions = new List<ISubscription>();
		}

		protected override void ConfigureState(StateMachine<SequenceState, Type> machine)
		{
			machine
				.Configure(SequenceState.Active)
				.Permit(typeof(CancelSequence), SequenceState.Canceled);
		}

		public override SequenceModel Initialize()
		{
			return new SequenceModel
			{
				State = SequenceState.Created,
				Id = Guid.NewGuid(),
				Completed = new List<ExecutionResult>(),
				Skipped = new List<ExecutionResult>()
			};
		}

		public IMessageSequenceBuilder PublishAsync<TMessage>(TMessage message = default(TMessage), Guid globalMessageId = new Guid()) where TMessage : new()
		{
			if (globalMessageId != Guid.Empty)
			{
				this._logger.Info("Setting Global Message Id to {globalMessageId}", globalMessageId);
				this._model.Id = globalMessageId;
			}
			return this.PublishAsync(message, context => { });
		}

		public IMessageSequenceBuilder PublishAsync<TMessage>(TMessage message, Action<IPipeContext> context, CancellationToken ct = new CancellationToken())
			where TMessage : new()
		{
			this._logger.Info("Initializing Message Sequence that starts with {messageType}.", typeof(TMessage).Name);

			StateMachine<SequenceState, Type>.TriggerWithParameters<TMessage> entryTrigger = this._stateMachine.SetTriggerParameters<TMessage>(typeof(TMessage));

			this._stateMachine
				.Configure(SequenceState.Created)
				.Permit(typeof(TMessage), SequenceState.Active);

			this._stateMachine
				.Configure(SequenceState.Active)
				.OnEntryFromAsync(entryTrigger, msg => this._client.PublishAsync(msg, c =>
				{
					c.Properties.Add(Enrichers.GlobalExecutionId.PipeKey.GlobalExecutionId, this._model.Id.ToString());
					context?.Invoke(c);
				}, ct));

			this._fireAction = () => this._stateMachine.FireAsync(entryTrigger, message);
			return this;
		}

		public IMessageSequenceBuilder When<TMessage, TMessageContext>(Func<TMessage, TMessageContext, Task> func, Action<IStepOptionBuilder> options = null)
		{
			StepOptionBuilder optionBuilder = new StepOptionBuilder();
			options?.Invoke(optionBuilder);
			this._stepDefinitions.Enqueue(new StepDefinition
			{
				Type = typeof(TMessage),
				AbortsExecution = optionBuilder.Configuration.AbortsExecution,
				Optional =  optionBuilder.Configuration.Optional
			});

			StateMachine<SequenceState, Type>.TriggerWithParameters<MessageAndContext<TMessage, TMessageContext>> trigger = this._stateMachine.SetTriggerParameters<MessageAndContext<TMessage, TMessageContext>>(typeof(TMessage));

			this._stateMachine
				.Configure(SequenceState.Active)
				.InternalTransitionAsync(trigger, async (message, transition) =>
				{
					this._logger.Debug("Received message of type {messageType} for sequence {sequenceId}.", transition.Trigger.Name, this._model.Id);
					bool matchFound = false;
					do
					{
						if (this._stepDefinitions.Peek() == null)
						{
							this._logger.Info("No matching steps found for sequence. Perhaps {messageType} isn't a registered message for sequence {sequenceId}.", transition.Trigger.Name, this._model.Id);
							return;
						}
						StepDefinition step = this._stepDefinitions.Dequeue();
						if (step.Type != typeof(TMessage))
						{
							if (step.Optional)
							{
								this._logger.Info("The step for {optionalMessageType} is optional. Skipping, as received message is of type {currentMessageType}.", step.Type.Name, typeof(TMessage).Name);
								this._model.Skipped.Add(new ExecutionResult
								{
									Type = step.Type,
									Time = DateTime.Now
								});
							}
							else
							{
								this._logger.Info("The step for {messageType} is mandatory. Current message, {currentMessageType} will be dismissed.", step.Type.Name, typeof(TMessage).Name);
								return;
							}
						}
						else
						{
							matchFound = true;
						}
					} while (!matchFound);

					this._logger.Debug("Invoking message handler for {messageType}", typeof(TMessage).Name);
					await func(message.Message, message.Context);
					this._model.Completed.Add(new ExecutionResult
					{
						Type = typeof(TMessage),
						Time = DateTime.Now
					});
					if (optionBuilder.Configuration.AbortsExecution)
					{
						if (this._stateMachine.PermittedTriggers.Contains(typeof(CancelSequence)))
						{
							this._stateMachine.Fire(typeof(CancelSequence));
						}
					}
				});

			this._triggerConfigurer
				.FromMessage<MessageSequence,TMessage, TMessageContext>(
					(msg, ctx) => this._model.Id,
					(sequence, message, ctx) => this._stateMachine.FireAsync(trigger, new MessageAndContext<TMessage, TMessageContext> {Context = ctx, Message = message}),
					cfg => cfg
						.FromDeclaredQueue(q => q
							.WithNameSuffix(this._model.Id.ToString())
							.WithExclusivity()
							.WithAutoDelete())
						.Consume(c => c.WithRoutingKey($"{this._naming.RoutingKeyConvention(typeof(TMessage))}.{this._model.Id}")
					)
				);
			return this;
		}

		MessageSequence<TMessage> IMessageSequenceBuilder.Complete<TMessage>()
		{
			TaskCompletionSource<TMessage> tsc = new TaskCompletionSource<TMessage>();
			MessageSequence<TMessage> sequence = new MessageSequence<TMessage>
			{
				Task = tsc.Task
			};

			this._stateMachine
				.Configure(SequenceState.Active)
				.Permit(typeof(TMessage), SequenceState.Completed);

			this._stateMachine
				.Configure(SequenceState.Active)
				.OnExit(() =>
				{
					this._logger.Debug("Disposing subscriptions for Message Sequence '{sequenceId}'.", this._model.Id);
					foreach (ISubscription subscription in this._subscriptions)
					{
						subscription.Dispose();
					}

					this._channel.Dispose();
				});

			StateMachine<SequenceState, Type>.TriggerWithParameters<TMessage> trigger = this._stateMachine.SetTriggerParameters<TMessage>(typeof(TMessage));
			this._stateMachine
				.Configure(SequenceState.Completed)
				.OnEntryFrom(trigger, message =>
				{
					this._logger.Info("Sequence {sequenceId} completed with message '{messageType}'.", this._model.Id, typeof(TMessage).Name);
					sequence.Completed = this._model.Completed;
					sequence.Skipped = this._model.Skipped;
					tsc.TrySetResult(message);
				});

			this._stateMachine
				.Configure(SequenceState.Canceled)
				.OnEntry(() =>
				{
					sequence.Completed = this._model.Completed;
					sequence.Skipped = this._model.Skipped;
					sequence.Aborted = true;
					tsc.TrySetResult(default(TMessage));
				});

			this._triggerConfigurer
				.FromMessage<MessageSequence, TMessage>(
					message => this._model.Id,
					(s, message) => this._stateMachine.Fire(trigger, message),
					cfg => cfg
						.FromDeclaredQueue(q => q
							.WithNameSuffix(this._model.Id.ToString())
							.WithExclusivity()
							.WithAutoDelete())
						.Consume(c => c.WithRoutingKey($"{this._naming.RoutingKeyConvention(typeof(TMessage))}.{this._model.Id}")
					)
				);

			this._channel = this._client.CreateChannelAsync().GetAwaiter().GetResult();

			foreach (TriggerConfiguration triggerCfg in this._triggerConfigurer.TriggerConfiguration)
			{
				triggerCfg.Context += context =>
				{
					context.Properties.Add(StateMachineKey.ModelId, this._model.Id);
					context.Properties.Add(StateMachineKey.Machine, this);
					CollectionExtensions.TryAdd(context.Properties, PipeKey.Channel, this._channel);
				};
				IPipeContext ctx = this._client.InvokeAsync(triggerCfg.Pipe, triggerCfg.Context).GetAwaiter().GetResult();
				this._subscriptions.Add(ctx.GetSubscription());
			}

			Timer requestTimer = null;
			requestTimer = new Timer(state =>
			{
				// ReSharper disable once AccessToModifiedClosure
				requestTimer?.Dispose();
				tsc.TrySetException(new TimeoutException(
					$"Unable to complete sequence {this._model.Id} in {this._clientCfg.RequestTimeout:g}. Operation Timed out."));
				if (this._stateMachine.PermittedTriggers.Contains(typeof(CancelSequence)))
				{
					this._stateMachine.Fire(typeof(CancelSequence));
				}
			}, null, this._clientCfg.RequestTimeout, new TimeSpan(-1));

			this._fireAction();

			return sequence;
		}

		private sealed class CancelSequence { }

		// Temp class until Stateless supports multiple trigger args
		private sealed class MessageAndContext<TMessage, TContext>
		{
			public TMessage Message { get; set; }
			public TContext Context { get; set; }
		}
	}

	public enum SequenceState
	{
		Created,
		Active,
		Completed,
		Canceled
	}
}
