using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using RabbitMQ.Client;
using RawRabbit.Configuration;

namespace RawRabbit.Enrichers.Polly.Services
{
	public class ChannelFactory : Channel.ChannelFactory
	{
		protected readonly Policy _createChannelPolicy;
		protected readonly Policy _connectPolicy;
		protected readonly Policy _getConnectionPolicy;

		public ChannelFactory(IConnectionFactory connectionFactory, RawRabbitConfiguration config, ConnectionPolicies policies = null)
			: base(connectionFactory, config)
		{
			this._createChannelPolicy = policies?.CreateChannel ?? Policy.NoOpAsync();
			this._connectPolicy = policies?.Connect ?? Policy.NoOpAsync();
			this._getConnectionPolicy = policies?.GetConnection ?? Policy.NoOpAsync();
		}

		public override Task ConnectAsync(CancellationToken token = default(CancellationToken))
		{
			return this._connectPolicy.ExecuteAsync(
				action: ct => base.ConnectAsync(ct),
				contextData: new Dictionary<string, object>
				{
					[RetryKey.ConnectionFactory] = this._connectionFactory,
					[RetryKey.ClientConfiguration] = this._clientConfig
				},
				cancellationToken: token
			);
		}

		protected override Task<IConnection> GetConnectionAsync(CancellationToken token = default(CancellationToken))
		{
			return this._getConnectionPolicy.ExecuteAsync(
				action: ct => base.GetConnectionAsync(ct),
				contextData: new Dictionary<string, object>
				{
					[RetryKey.ConnectionFactory] = this._connectionFactory,
					[RetryKey.ClientConfiguration] = this._clientConfig
				},
				cancellationToken: token
			);
		}

		public override Task<IChannel> CreateChannelAsync(CancellationToken token = default(CancellationToken))
		{
			return this._createChannelPolicy.ExecuteAsync(
				action: ct => base.CreateChannelAsync(ct),
				contextData: new Dictionary<string, object>
				{
					[RetryKey.ConnectionFactory] = this._connectionFactory,
					[RetryKey.ClientConfiguration] = this._clientConfig
				},
				cancellationToken: token
			);
		}
	}

	public class ConnectionPolicies
	{
		/// <summary>
		/// Used whenever 'CreateChannelAsync' is called.
		/// Expects an async policy.
		/// </summary>
		public Policy CreateChannel { get; set; }

		/// <summary>
		/// Used whenever an existing connection is retrieved.
		/// </summary>
		public Policy GetConnection { get; set; }

		/// <summary>
		/// Used when establishing the initial connection
		/// </summary>
		public Policy Connect { get; set; }
	}
}
