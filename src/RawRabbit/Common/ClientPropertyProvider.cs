using System;
using System.Collections.Generic;
using System.Reflection;
using RawRabbit.Configuration;

namespace RawRabbit.Common;

public interface IClientPropertyProvider
{
	IDictionary<string, object> GetClientProperties(RawRabbitConfiguration cfg = null);
}

public class ClientPropertyProvider : IClientPropertyProvider
{
	public IDictionary<string, object> GetClientProperties(RawRabbitConfiguration cfg = null)
	{
		Dictionary<string, object> props = new()
		{
			{ "product", "RawRabbit" },
			{ "version", typeof(IBusClient).GetTypeInfo().Assembly.GetName().Version?.ToString() },
			{ "platform", ".NET" },
			{ "client_directory", typeof(IBusClient).GetTypeInfo().Assembly.CodeBase},
			{ "client_server", Environment.MachineName },
		};

		if (cfg != null)
		{
			props.Add("request_timeout", cfg.RequestTimeout.ToString("g"));
			props.Add("broker_username", cfg.Username);
		}

		return props;
	}
}