﻿using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Configuration;
using RawRabbit.Exceptions;
using RawRabbit.Logging;

namespace RawRabbit.Channel
{
	public class ChannelFactory : IChannelFactory
	{
		private readonly ILog _logger = LogProvider.For<ChannelFactory>();
		protected readonly IConnectionFactory _connectionFactory;
		protected readonly RawRabbitConfiguration _clientConfig;
		protected readonly ConcurrentBag<IModel> _channels;
		protected IConnection _connection;

		public ChannelFactory(IConnectionFactory connectionFactory, RawRabbitConfiguration config)
		{
			this._connectionFactory = connectionFactory;
			this._clientConfig = config;
			this._channels = new ConcurrentBag<IModel>();
		}

		public virtual Task ConnectAsync(CancellationToken token = default(CancellationToken))
		{
			try
			{
				this._logger.Debug("Creating a new connection for {hostNameCount} hosts.", this._clientConfig.Hostnames.Count);
				this._connection = this._connectionFactory.CreateConnection(this._clientConfig.Hostnames, this._clientConfig.ClientProvidedName);
				this._connection.ConnectionShutdown += (sender, args) => this._logger.Warn("Connection was shutdown by {Initiator}. ReplyText {ReplyText}", args.Initiator, args.ReplyText);
			}
			catch (BrokerUnreachableException e)
			{
				this._logger.Info("Unable to connect to broker", e);
				throw;
			}
			return Task.FromResult(true);
		}

		public virtual async Task<IModel> CreateChannelAsync(CancellationToken token = default(CancellationToken))
		{
			IConnection connection = await this.GetConnectionAsync(token);
			token.ThrowIfCancellationRequested();
			IModel channel = connection.CreateModel();
			this._channels.Add(channel);
			return channel;
		}

		protected virtual async Task<IConnection> GetConnectionAsync(CancellationToken token = default(CancellationToken))
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

			if (this._connection.CloseReason != null && this._connection.CloseReason.Initiator == ShutdownInitiator.Application)
			{
				this._logger.Info("Connection is closed with Application as initiator. It will not be recovered.");
				this._connection.Dispose();
				throw new ChannelAvailabilityException("Closed connection initiated by the Application. A new connection will not be created, and no channel can be created.");
			}

			if (!(this._connection is IRecoverable recoverable))
			{
				this._logger.Info("Connection is not recoverable");
				this._connection.Dispose();
				throw new ChannelAvailabilityException("The non recoverable connection is closed. A channel can not be created.");
			}

			this._logger.Debug("Connection is recoverable. Waiting for 'Recovery' event to be triggered. ");
			TaskCompletionSource<IConnection> recoverTcs = new TaskCompletionSource<IConnection>();
			token.Register(() => recoverTcs.TrySetCanceled());

			EventHandler<EventArgs> completeTask = null;
			completeTask = (sender, args) =>
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
			foreach (IModel channel in this._channels)
			{
				channel?.Dispose();
			}

			this._connection?.Dispose();
		}
	}
}
