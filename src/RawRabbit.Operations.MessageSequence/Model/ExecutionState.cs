using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RawRabbit.Operations.MessageSequence.Model;

public class ExecutionState
{
	public List<ExecutionResult> Skipped { get; set; }
	public List<ExecutionResult> Completed { get; set; }
	public Guid GlobalRequestId { get; set; }
	public bool Aborted { get; set; }
	public List<Task> HandlerTasks { get; set; }

	public ExecutionState()
	{
		this.Skipped = new List<ExecutionResult>();
		this.Completed = new List<ExecutionResult>();
		this.HandlerTasks = new List<Task>();
	}
}