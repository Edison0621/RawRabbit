using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Queue;
using RawRabbit.Logging;

namespace RawRabbit.Pipe.Middleware
{
	public class QueueDeclareOptions
	{
		public Func<IPipeContext, QueueDeclaration> QueueDeclarationFunc { get; set; }
	}

	public class QueueDeclareMiddleware : Middleware
	{
		protected readonly Func<IPipeContext, QueueDeclaration> _queueDeclareFunc;
		protected readonly ITopologyProvider _topology;
		private readonly ILog _logger = LogProvider.For<QueueDeclareMiddleware>();

		public QueueDeclareMiddleware(ITopologyProvider topology, QueueDeclareOptions options = null )
		{
			this._topology = topology;
			this._queueDeclareFunc = options?.QueueDeclarationFunc ?? (context => context.GetQueueDeclaration());
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token = default (CancellationToken))
		{
			QueueDeclaration queue = this.GetQueueDeclaration(context);

			if (queue != null)
			{
				this._logger.Debug("Declaring queue '{queueName}'.", queue.Name);
				await this.DeclareQueueAsync(queue, context, token);
			}
			else
			{
				this._logger.Info("Queue will not be declaired: no queue declaration found in context.");
			}

			await this.Next.InvokeAsync(context, token);
		}

		protected virtual QueueDeclaration GetQueueDeclaration(IPipeContext context)
		{
			return this._queueDeclareFunc(context);
		}

		protected virtual Task DeclareQueueAsync(QueueDeclaration queue, IPipeContext context, CancellationToken token)
		{
			return this._topology.DeclareQueueAsync(queue);
		}
	}
}
