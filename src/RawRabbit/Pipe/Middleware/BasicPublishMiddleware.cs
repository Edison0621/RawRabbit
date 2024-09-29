using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Logging;

namespace RawRabbit.Pipe.Middleware
{
	public class BasicPublishOptions
	{
		public Func<IPipeContext, IModel> ChannelFunc { get; set; }
		public Func<IPipeContext, string> ExchangeNameFunc { get; set; }
		public Func<IPipeContext, string> RoutingKeyFunc { get; set; }
		public Func<IPipeContext, bool> MandatoryFunc { get; set; }
		public Func<IPipeContext, IBasicProperties> BasicPropsFunc { get; set; }
		public Func<IPipeContext, byte[]> BodyFunc { get; set; }
	}

	public class BasicPublishMiddleware : Middleware
	{
		protected readonly IExclusiveLock _exclusive;
		protected readonly Func<IPipeContext, IModel> _channelFunc;
		protected readonly Func<IPipeContext, string> _exchangeNameFunc;
		protected readonly Func<IPipeContext, string> _routingKeyFunc;
		protected readonly Func<IPipeContext, bool> _mandatoryFunc;
		protected readonly Func<IPipeContext, IBasicProperties> _basicPropsFunc;
		protected readonly Func<IPipeContext, byte[]> _bodyFunc;
		private ILog _logger = LogProvider.For<BasicPublishMiddleware>();

		public BasicPublishMiddleware(IExclusiveLock exclusive, BasicPublishOptions options = null)
		{
			this._exclusive = exclusive;
			this._channelFunc = options?.ChannelFunc ?? (context => context.GetTransientChannel());
			this._exchangeNameFunc = options?.ExchangeNameFunc ?? (c => c.GetBasicPublishConfiguration()?.ExchangeName);
			this._routingKeyFunc = options?.RoutingKeyFunc ?? (c => c.GetBasicPublishConfiguration()?.RoutingKey);
			this._mandatoryFunc = options?.MandatoryFunc ?? (c => c.GetBasicPublishConfiguration()?.Mandatory ?? false);
			this._basicPropsFunc = options?.BasicPropsFunc ?? (c => c.GetBasicProperties());
			this._bodyFunc = options?.BodyFunc ?? (c => c.GetBasicPublishConfiguration()?.Body);
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
		{
			IModel channel = this.GetOrCreateChannel(context);
			string exchangeName = this.GetExchangeName(context);
			string routingKey = this.GetRoutingKey(context);
			bool mandatory = this.GetMandatoryOptions(context);
			IBasicProperties basicProps = this.GetBasicProps(context);
			byte[] body = this.GetMessageBody(context);

			this._logger.Info("Performing basic publish with routing key {routingKey} on exchange {exchangeName}.", routingKey, exchangeName);

			this.ExclusiveExecute(channel, c => this.BasicPublish(
						channel: c,
						exchange: exchangeName,
						routingKey: routingKey,
						mandatory: mandatory,
						basicProps: basicProps,
						body: body,
						context: context
					), token
			);

			await this.Next.InvokeAsync(context, token);
		}

		protected virtual void BasicPublish(IModel channel, string exchange, string routingKey, bool mandatory, IBasicProperties basicProps, byte[] body, IPipeContext context)
		{
			channel.BasicPublish(
				exchange: exchange,
				routingKey: routingKey,
				mandatory: mandatory,
				basicProperties: basicProps,
				body: body
			);
		}

		protected virtual void ExclusiveExecute(IModel channel, Action<IModel> action, CancellationToken token)
		{
			this._exclusive.Execute(channel, action, token);
		}

		protected virtual byte[] GetMessageBody(IPipeContext context)
		{
			byte[] body = this._bodyFunc(context);
			if (body == null)
			{
				this._logger.Warn("No body found in the Pipe context.");
			}
			return body;
		}

		protected virtual IBasicProperties GetBasicProps(IPipeContext context)
		{
			IBasicProperties props = this._basicPropsFunc(context);
			if (props == null)
			{
				this._logger.Warn("No basic properties found in the Pipe context.");
			}
			return props;
		}

		protected virtual bool GetMandatoryOptions(IPipeContext context)
		{
			return this._mandatoryFunc(context);
		}

		protected virtual string GetRoutingKey(IPipeContext context)
		{
			string routingKey = this._routingKeyFunc(context);
			if (routingKey == null)
			{
				this._logger.Warn("No routing key found in the Pipe context.");
			}
			return routingKey;
		}

		protected virtual string GetExchangeName(IPipeContext context)
		{
			string exchange = this._exchangeNameFunc(context);
			if (exchange == null)
			{
				this._logger.Warn("No exchange name found in the Pipe context.");
			}
			return exchange;
		}

		protected virtual IModel GetOrCreateChannel(IPipeContext context)
		{
			IModel channel = this._channelFunc(context);
			if (channel == null)
			{
				this._logger.Warn("No channel to perform publish found.");
			}
			return channel;
		}
	}
}
