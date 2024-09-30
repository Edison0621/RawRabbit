using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.NoOp;
using RabbitMQ.Client;
using RawRabbit.Configuration;

namespace RawRabbit.Enrichers.Polly.Services;

public class ChannelFactory : Channel.ChannelFactory
{
	protected readonly AsyncNoOpPolicy _createChannelPolicy;
	protected readonly AsyncNoOpPolicy _connectPolicy;
	protected readonly AsyncNoOpPolicy _getConnectionPolicy;

	public ChannelFactory(IConnectionFactory connectionFactory, RawRabbitConfiguration config, ConnectionPolicies policies = null)
		: base(connectionFactory, config)
	{
		this._createChannelPolicy = policies?.CreateChannel ?? Policy.NoOpAsync();
		this._connectPolicy = policies?.Connect ?? Policy.NoOpAsync();
		this._getConnectionPolicy = policies?.GetConnection ?? Policy.NoOpAsync();
	}

	public override Task ConnectAsync(CancellationToken token = default)
	{
		return this._connectPolicy.ExecuteAsync(
			action: ct => base.ConnectAsync(token),
			contextData: new Dictionary<string, object>
			{
				[RetryKey.ConnectionFactory] = this._connectionFactory,
				[RetryKey.ClientConfiguration] = this._clientConfig
			}
		);
	}

	protected override Task<IConnection> GetConnectionAsync(CancellationToken token = default)
	{
		return this._getConnectionPolicy.ExecuteAsync(
			action: ct => base.GetConnectionAsync(token),
			contextData: new Dictionary<string, object>
			{
				[RetryKey.ConnectionFactory] = this._connectionFactory,
				[RetryKey.ClientConfiguration] = this._clientConfig
			}
		);
	}

	public override Task<IChannel> CreateChannelAsync(CancellationToken token = default)
	{
		return this._createChannelPolicy.ExecuteAsync(
			action: ct => base.CreateChannelAsync(token),
			contextData: new Dictionary<string, object>
			{
				[RetryKey.ConnectionFactory] = this._connectionFactory,
				[RetryKey.ClientConfiguration] = this._clientConfig
			}
		);
	}
}

public class ConnectionPolicies
{
	/// <summary>
	/// Used whenever 'CreateChannelAsync' is called.
	/// Expects an async policy.
	/// </summary>
	public AsyncNoOpPolicy CreateChannel { get; set; }

	/// <summary>
	/// Used whenever an existing connection is retrieved.
	/// </summary>
	public AsyncNoOpPolicy GetConnection { get; set; }

	/// <summary>
	/// Used when establishing the initial connection
	/// </summary>
	public AsyncNoOpPolicy Connect { get; set; }
}
