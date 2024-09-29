using System;
using System.Threading.Tasks;
using Stateless;

namespace RawRabbit.Operations.StateMachine
{
	public abstract class StateMachineBase
	{
		public abstract Task TriggerAsync(object trigger);
		public abstract Task TriggerAsync<TPayload>(object trigger, TPayload payload);
		public abstract Model GetDto();
	}

	public abstract class StateMachineBase<TState, TTrigger, TModel> : StateMachineBase where TModel : Model<TState>
	{
		protected readonly TModel _model;
		protected readonly StateMachine<TState, TTrigger> _stateMachine;

		protected StateMachineBase(TModel model = null)
		{
			this._model = (model == null || model.Id == Guid.Empty) ? this.Initialize() : model;
			this._stateMachine = new StateMachine<TState, TTrigger>(() => this._model.State, s => this._model.State = s);
			this.ConfigureState(this._stateMachine);
		}

		protected abstract void ConfigureState(StateMachine<TState, TTrigger> machine);

		public abstract TModel Initialize();

		public override Task TriggerAsync(object trigger)
		{
			return this._stateMachine.FireAsync((TTrigger) trigger);
		}

		public override Task TriggerAsync<TPayload>(object trigger, TPayload payload)
		{
			StateMachine<TState, TTrigger>.TriggerWithParameters<TPayload> paramTrigger = new StateMachine<TState, TTrigger>.TriggerWithParameters<TPayload>((TTrigger)trigger);
			return this._stateMachine.FireAsync(paramTrigger, payload);
		}
		
		public override Model GetDto()
		{
			return this._model;
		}
	}
}