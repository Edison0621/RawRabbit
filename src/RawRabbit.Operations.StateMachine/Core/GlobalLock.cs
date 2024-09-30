﻿using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Logging;

namespace RawRabbit.Operations.StateMachine.Core;

public interface IGlobalLock
{
	Task ExecuteAsync(Guid modelId, Func<Task> handler, CancellationToken ct = default);
}

public class GlobalLock : IGlobalLock
{
	private readonly Func<Guid, Func<Task>, CancellationToken, Task> _exclusiveExecute;

	public GlobalLock(Func<Guid, Func<Task>, CancellationToken, Task> exclusiveExecute = null)
	{
		if (exclusiveExecute == null)
		{
			ProcessGlobalLock processLock = new();
			exclusiveExecute = (id, handler, ct) => processLock.ExecuteAsync(id, handler, ct);
		}

		this._exclusiveExecute = exclusiveExecute;
	}

	public Task ExecuteAsync(Guid modelId, Func<Task> handler, CancellationToken ct = new())
	{
		return this._exclusiveExecute(modelId, handler, ct);
	}
}

public class ProcessGlobalLock : IGlobalLock
{
	private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _semaphores;
	private readonly ILog _logger = LogProvider.For<ProcessGlobalLock>();

	public ProcessGlobalLock()
	{
		this._semaphores = new ConcurrentDictionary<Guid, SemaphoreSlim>();
	}
		
	public async Task ExecuteAsync(Guid modelId, Func<Task> handler, CancellationToken ct = default)
	{
		SemaphoreSlim semaphore = this._semaphores.GetOrAdd(modelId, _ => new SemaphoreSlim(1, 1));
		await semaphore.WaitAsync(ct);
		try
		{
			await handler();
		}
		catch (Exception e)
		{
			this._logger.Error(e, "Unhandled exception during execution under Global Lock");
		}
		finally
		{
			semaphore.Release();
		}
	}
}