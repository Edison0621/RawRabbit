using System;
using RabbitMQ.Client;

namespace RawRabbit.IntegrationTests
{
	public class IntegrationTestBase : IDisposable
	{
		protected IModel TestChannel => this._testChannel.Value;
		private readonly Lazy<IModel> _testChannel;
		private IConnection _connection;
		
		public IntegrationTestBase()
		{
			this._testChannel = new Lazy<IModel>(() =>
			{
				this._connection = new ConnectionFactory { HostName = "localhost" }.CreateConnection();
				return this._connection.CreateModel();
			});
		}

		public virtual void Dispose()
		{
			this.TestChannel?.Dispose();
			this._connection?.Dispose();
		}
	}
}
