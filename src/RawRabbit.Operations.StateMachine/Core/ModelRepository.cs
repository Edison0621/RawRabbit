using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace RawRabbit.Operations.StateMachine.Core;

public interface IModelRepository
{
	Task<Model> GetAsync(Guid id);
	Task AddOrUpdateAsync(Model model);
}

public class ModelRepository : IModelRepository
{
	private readonly Func<Guid, Task<Model>> _get;
	private readonly Func<Model, Task> _addOrUpdate;

	public ModelRepository(
		Func<Guid, Task<Model>> get = null,
		Func<Model, Task> addOrUpdate = null)
	{
		this._get = get;
		this._addOrUpdate = addOrUpdate;
		if (this._get == null && this._addOrUpdate == null)
		{
			ConcurrentDictionary<Guid, Model> fallback = new();
			this._get = id =>
			{
				Model model;
				return fallback.TryGetValue(id, out model)
					? Task.FromResult(model)
					: Task.FromResult<Model>(null);
			};
			this._addOrUpdate = model =>
			{
				fallback.AddOrUpdate(model.Id, guid => model, (id, m) => model);
				return Task.FromResult(0);
			};
		}
	}

	public Task<Model> GetAsync(Guid id)
	{
		return this._get(id);
	}

	public Task AddOrUpdateAsync(Model model)
	{
		return this._addOrUpdate(model);
	}
}