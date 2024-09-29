using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using RawRabbit.Configuration;

namespace RawRabbit.Common
{
	public class ConnectionStringParser
	{
		private static readonly Regex _mainRegex = new Regex(@"((?<username>.*):(?<password>.*)@)?(?<hosts>[^\/\?:]*)(:(?<port>[^\/\?]*))?(?<vhost>\/[^\?]*)?(\?(?<parameters>.*))?");
		private static readonly Regex _parametersRegex = new Regex(@"(?<name>[^?=&]+)=(?<value>[^&]*)?");
		private static readonly RawRabbitConfiguration _defaults = RawRabbitConfiguration.Local;

		public static RawRabbitConfiguration Parse(string connectionString)
		{
			Match mainMatch = _mainRegex.Match(connectionString);
			int port = _defaults.Port;
			if (RegexMatchGroupIsNonEmpty(mainMatch, "port"))
			{
				string suppliedPort = mainMatch.Groups["port"].Value;
				if (!int.TryParse(suppliedPort, out port))
				{
					throw new FormatException($"The supplied port '{suppliedPort}' in the connection string is not a number");
				}
			}

			RawRabbitConfiguration cfg = new RawRabbitConfiguration
			{
				Username = RegexMatchGroupIsNonEmpty(mainMatch, "username") ? mainMatch.Groups["username"].Value : _defaults.Username,
				Password = RegexMatchGroupIsNonEmpty(mainMatch, "password") ? mainMatch.Groups["password"].Value : _defaults.Password,
				Hostnames = mainMatch.Groups["hosts"].Value.Split(',').ToList(),
				Port = port,
				VirtualHost = ExctractVirutalHost(mainMatch)
			};

			MatchCollection parametersMatches = _parametersRegex.Matches(mainMatch.Groups["parameters"].Value);
			foreach (Match match in parametersMatches)
			{
				string name = match.Groups["name"].Value.ToLower();
				string val = match.Groups["value"].Value.ToLower();
				PropertyInfo propertyInfo = cfg
					.GetType()
					.GetTypeInfo()
					.GetProperty(name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

				if (propertyInfo == null)
				{
					throw new ArgumentException($"No configuration property named '{name}'");
				}

				if (propertyInfo.PropertyType == typeof (TimeSpan))
				{
					TimeSpan convertedValue = TimeSpan.FromSeconds(int.Parse(val));
					propertyInfo.SetValue(cfg, convertedValue, null);
				}
				else
				{
					propertyInfo.SetValue(cfg, Convert.ChangeType(val, propertyInfo.PropertyType), null);
				}
			}

			return cfg;
		}

		private static string ExctractVirutalHost(Match mainMatch)
		{
			string vhost = RegexMatchGroupIsNonEmpty(mainMatch, "vhost") ? mainMatch.Groups["vhost"].Value : _defaults.VirtualHost;
			return string.Equals(vhost, _defaults.VirtualHost, StringComparison.CurrentCultureIgnoreCase)
				? vhost
				: vhost.Substring(1);
		}

		private static bool RegexMatchGroupIsNonEmpty(Match match, string groupName)
		{
			return match.Groups[groupName].Success && match.Groups[groupName].Length > 0;
		}
	}
}
