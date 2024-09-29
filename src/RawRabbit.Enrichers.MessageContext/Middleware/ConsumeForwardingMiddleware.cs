using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Enrichers.MessageContext.Dependencies;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.MessageContext.Middleware
{
	public class ConsumeForwardingMiddleware : StagedMiddleware
	{
		private readonly IMessageContextRepository _repo;

		public ConsumeForwardingMiddleware(IMessageContextRepository repo)
		{
			this._repo = repo;
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
		{
			object messageContext = context.GetMessageContext();
			if (messageContext != null)
			{
				this._repo.Set(messageContext);
			}
			return this.Next.InvokeAsync(context, token);
		}

		public override string StageMarker => "MessageContextDeserialized";
	}
}
