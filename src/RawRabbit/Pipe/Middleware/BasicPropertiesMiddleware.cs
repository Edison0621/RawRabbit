using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Serialization;

namespace RawRabbit.Pipe.Middleware
{
	public class BasicPropertiesOptions
	{
		public Action<IPipeContext, IBasicProperties> PropertyModier { get; set; }
		public Func<IPipeContext, IBasicProperties> GetOrCreatePropsFunc { get; set; }
		public Action<IPipeContext, IBasicProperties> PostCreateAction { get; set; }
	}

	public class BasicPropertiesMiddleware : Middleware
	{
		protected readonly ISerializer _serializer;
		protected readonly Func<IPipeContext, IBasicProperties> _getOrCreatePropsFunc;
		protected readonly Action<IPipeContext, IBasicProperties> _propertyModifier;
		protected readonly Action<IPipeContext, IBasicProperties> _postCreateAction;

		public BasicPropertiesMiddleware(ISerializer serializer, BasicPropertiesOptions options = null)
		{
			this._serializer = serializer;
			this._propertyModifier = options?.PropertyModier ?? ((ctx, props) => ctx.Get<Action<IBasicProperties>>(PipeKey.BasicPropertyModifier)?.Invoke(props));
			this._postCreateAction = options?.PostCreateAction;
			this._getOrCreatePropsFunc = options?.GetOrCreatePropsFunc ?? (ctx => ctx.GetBasicProperties() ?? new BasicProperties
			{
				MessageId = Guid.NewGuid().ToString(),
				Headers = new Dictionary<string, object>(),
				Persistent = ctx.GetClientConfiguration().PersistentDeliveryMode,
				ContentType = this._serializer.ContentType
			});
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
		{
			IBasicProperties props = this.GetOrCreateBasicProperties(context);
			this.ModifyBasicProperties(context, props);
			this.InvokePostCreateAction(context, props);
			context.Properties.TryAdd(PipeKey.BasicProperties, props);
			return this.Next.InvokeAsync(context, token);
		}

		protected virtual void ModifyBasicProperties(IPipeContext context, IBasicProperties props)
		{
			this._propertyModifier?.Invoke(context, props);
		}

		protected virtual void InvokePostCreateAction(IPipeContext context, IBasicProperties props)
		{
			this._postCreateAction?.Invoke(context, props);
		}

		protected virtual IBasicProperties GetOrCreateBasicProperties(IPipeContext context)
		{
			return this._getOrCreatePropsFunc(context);
		}
	}
}
