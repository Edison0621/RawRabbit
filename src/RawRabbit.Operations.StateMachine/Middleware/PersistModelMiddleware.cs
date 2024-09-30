using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Operations.StateMachine.Core;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.StateMachine.Middleware;

public class PersistModelMiddleware : Pipe.Middleware.Middleware
{
	private readonly IStateMachineActivator _stateMachineRepo;

	public PersistModelMiddleware(IStateMachineActivator stateMachineRepo)
	{
		this._stateMachineRepo = stateMachineRepo;
	}

	public override async Task InvokeAsync(IPipeContext context, CancellationToken token = default)
	{
		StateMachineBase machine = context.GetStateMachine();
		await this._stateMachineRepo.PersistAsync(machine);
		await this.Next.InvokeAsync(context, token);
	}
}