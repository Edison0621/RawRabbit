using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Configuration;
using RawRabbit.Exceptions;
using RawRabbit.Logging;

namespace RawRabbit.Channel;

public class ChannelFactory : IChannelFactory
{
	private readonly ILog _logger = LogProvider.For<ChannelFactory>();
	protected readonly IConnectionFactory _connectionFactory;
	protected readonly RawRabbitConfiguration _clientConfig;
	protected readonly ConcurrentBag<IChannel> _channels;
	protected IConnection _connection;

	public ChannelFactory(IConnectionFactory connectionFactory, RawRabbitConfiguration config)
	{
		this._connectionFactory = connectionFactory;
		this._clientConfig = config;
		this._channels = new ConcurrentBag<IChannel>();
	}

	public virtual async Task ConnectAsync(CancellationToken token = default)
	{
		try
		{
			this._logger.Debug("Creating a new connection for {hostNameCount} hosts.", this._clientConfig.Hostnames.Count);
			this._connection = await this._connectionFactory.CreateConnectionAsync(this._clientConfig.Hostnames, this._clientConfig.ClientProvidedName, token);
			this._connection.ConnectionShutdown += (_, args) => this._logger.Warn("Connection was shutdown by {Initiator}. ReplyText {ReplyText}", args.Initiator, args.ReplyText);
		}
		catch (BrokerUnreachableException e)
		{
			this._logger.Info("Unable to connect to broker", e);
			throw;
		}
	}

	public virtual async Task<IChannel> CreateChannelAsync(CancellationToken token = default)
	{
		IConnection connection = await this.GetConnectionAsync(token);
		token.ThrowIfCancellationRequested();
		IChannel channel = await connection.CreateChannelAsync(cancellationToken: token);
		this._channels.Add(channel);
		return channel;
	}

	protected virtual async Task<IConnection> GetConnectionAsync(CancellationToken token = default)
	{
		token.ThrowIfCancellationRequested();
		if (this._connection == null)
		{
			await this.ConnectAsync(token);
		}

		// ReSharper disable once PossibleNullReferenceException
		if (this._connection.IsOpen)
		{
			this._logger.Debug("Existing connection is open and will be used.");
			return this._connection;
		}

		this._logger.Info("The existing connection is not open.");

		if (this._connection.CloseReason is { Initiator: ShutdownInitiator.Application })
		{
			this._logger.Info("Connection is closed with Application as initiator. It will not be recovered.");
			this._connection.Dispose();
			throw new ChannelAvailabilityException("Closed connection initiated by the Application. A new connection will not be created, and no channel can be created.");
		}

		if (this._connection is not IRecoverable recoverable)
		{
			this._logger.Info("Connection is not recoverable");
			this._connection.Dispose();
			throw new ChannelAvailabilityException("The non recoverable connection is closed. A channel can not be created.");
		}

		this._logger.Debug("Connection is recoverable. Waiting for 'Recovery' event to be triggered. ");
		TaskCompletionSource<IConnection> recoverTcs = new();
		token.Register(() => recoverTcs.TrySetCanceled());

		EventHandler<EventArgs> completeTask = null;
		completeTask = (_, _) =>
		{
			if (recoverTcs.Task.IsCanceled)
			{
				return;
			}

			this._logger.Info("Connection has been recovered!");
			recoverTcs.TrySetResult(recoverable as IConnection);
			recoverable.Recovery -= completeTask;
		};

		recoverable.Recovery += completeTask;
		return await recoverTcs.Task;
	}

	public void Dispose()
	{
		foreach (IChannel channel in this._channels)
		{
			channel?.Dispose();
		}

		this._connection?.Dispose();
	}
}
