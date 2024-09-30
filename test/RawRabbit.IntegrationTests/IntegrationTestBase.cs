using System;
using RabbitMQ.Client;

namespace RawRabbit.IntegrationTests;

public class IntegrationTestBase : IDisposable
{
	protected IChannel TestChannel => this._testChannel.Value;
	private readonly Lazy<IChannel> _testChannel;
	private IConnection _connection;
		
	public IntegrationTestBase()
	{
		this._testChannel = new Lazy<IChannel>( () =>
		{
			this._connection = new ConnectionFactory { HostName = "localhost" }.CreateConnectionAsync().GetAwaiter().GetResult();
			return this._connection.CreateChannelAsync().GetAwaiter().GetResult();
		});
	}

	public virtual void Dispose()
	{
		this.TestChannel?.Dispose();
		this._connection?.Dispose();
	}
}
