using System;
using System.Threading.Tasks;
using RawRabbit.DependencyInjection;

namespace RawRabbit.Operations.StateMachine.Core;

public interface IStateMachineActivator
{
	Task<StateMachineBase> ActivateAsync(Guid id, Type stateMachineType);
	Task PersistAsync(StateMachineBase stateMachine);
}

public class StateMachineActivator : IStateMachineActivator
{
	private readonly IModelRepository _modelRepo;
	private readonly IDependencyResolver _resolver;

	public StateMachineActivator(IModelRepository modelRepo, IDependencyResolver resolver)
	{
		this._modelRepo = modelRepo;
		this._resolver = resolver;
	}

	public async Task<StateMachineBase> ActivateAsync(Guid id, Type stateMachineType)
	{
		Model model = await this._modelRepo.GetAsync(id);
		if (model != null)
		{
			StateMachineBase machine = this._resolver.GetService(stateMachineType, model) as StateMachineBase;
			return machine;
		}
		StateMachineBase newMachine = this._resolver.GetService(stateMachineType) as StateMachineBase;
		// ReSharper disable once PossibleNullReferenceException
		Model newModel = newMachine.GetDto();
		newModel.Id = id;
		await this._modelRepo.AddOrUpdateAsync(newModel);
		return newMachine;
	}
		
	public async Task PersistAsync(StateMachineBase stateMachine)
	{
		await this._modelRepo.AddOrUpdateAsync(stateMachine.GetDto());
	}
}