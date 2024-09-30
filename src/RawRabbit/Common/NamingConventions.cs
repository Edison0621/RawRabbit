using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace RawRabbit.Common;

public interface INamingConventions
{
	Func<Type, string> ExchangeNamingConvention { get; set; }
	Func<Type, string> QueueNamingConvention { get; set; }
	Func<Type, string> RoutingKeyConvention { get; set; }
	Func<string> ErrorExchangeNamingConvention { get; set; }
	Func<TimeSpan,string> RetryLaterExchangeConvention { get; set; }
	Func<string, TimeSpan,string> RetryLaterQueueNameConvention { get; set; }
	Func<Type, string> SubscriberQueueSuffix { get; set; }
}

public sealed class NamingConventions : INamingConventions
{
	private readonly ConcurrentDictionary<Type, int> _subscriberCounter;
	private readonly string _applicationName;
	private const string IisWorkerProcessName = "w3wp";
	private static readonly Regex _dllRegex = new(@"(?<ApplicationName>[^\\]*).dll", RegexOptions.Compiled);
	private static readonly Regex _consoleOrServiceRegex = new(@"(?<ApplicationName>[^\\]*).exe", RegexOptions.Compiled);
	private static readonly Regex _iisHostedAppRegexVer1 = new(@"-ap\s\\""(?<ApplicationName>[^\\]+)");
	private static readonly Regex _iisHostedAppRegexVer2 = new(@"\\\\apppools\\\\(?<ApplicationName>[^\\]+)");

	public Func<Type, string> ExchangeNamingConvention { get; set; }
	public Func<Type, string> QueueNamingConvention { get; set; }
	public Func<Type, string> RoutingKeyConvention { get; set; }
	public Func<string> ErrorExchangeNamingConvention { get; set; }
	public Func<TimeSpan, string> RetryLaterExchangeConvention { get; set; }
	public Func<string, TimeSpan, string> RetryLaterQueueNameConvention { get; set; }
	public Func<Type, string> SubscriberQueueSuffix { get; set; }

	public NamingConventions()
	{
		this._subscriberCounter = new ConcurrentDictionary<Type,int>();
		this._applicationName = GetApplicationName(string.Join(" ", Environment.GetCommandLineArgs()));

		this.ExchangeNamingConvention = type => type?.Namespace?.ToLower() ?? string.Empty;
		this.QueueNamingConvention = type => CreateShortAfqn(type);
		this.RoutingKeyConvention = type => CreateShortAfqn(type);
		this.ErrorExchangeNamingConvention = () => "default_error_exchange";
		this.SubscriberQueueSuffix = this.GetSubscriberQueueSuffix;
		this.RetryLaterExchangeConvention = _ => "default_retry_later_exchange";
		this.RetryLaterQueueNameConvention = (exchange, span) => $"retry_for_{exchange.Replace(".","_")}_in_{span.TotalMilliseconds}_ms";
	}

	private string GetSubscriberQueueSuffix(Type messageType)
	{
		StringBuilder sb = new(this._applicationName);

		this._subscriberCounter.AddOrUpdate(
			key: messageType,
			addValueFactory: _ =>
			{
				int next = 0;
				return next;
			},
			updateValueFactory:(_, i) =>
			{
				int next = i+1;
				sb.Append($"_{next}");
				return next;
			});

		return sb.ToString();
	}

	public static string GetApplicationName(params string[] commandLine)
	{
		Match match = _consoleOrServiceRegex.Match(commandLine.FirstOrDefault() ?? string.Empty);
		string applicationName = string.Empty;

		if (match.Success && match.Groups["ApplicationName"].Value != IisWorkerProcessName)
		{
			applicationName = match.Groups["ApplicationName"].Value;
			if (applicationName.EndsWith(".vshost", StringComparison.Ordinal))
				applicationName = applicationName.Remove(applicationName.Length - ".vshost".Length);
		}
		else
		{
			match = _iisHostedAppRegexVer1.Match(commandLine.FirstOrDefault() ?? string.Empty);
			if (match.Success)
			{
				applicationName = match.Groups["ApplicationName"].Value;
			}
			else
			{
				match = _iisHostedAppRegexVer2.Match(commandLine.FirstOrDefault() ?? string.Empty);
				if (match.Success)
				{
					applicationName = match.Groups["ApplicationName"].Value;
				}
				else
				{
					int index = commandLine.Length > 1 ? 1 : 0;
					if (_dllRegex.IsMatch(commandLine[index]))
					{
						applicationName = _dllRegex.Match(commandLine[index]).Groups["ApplicationName"].Value;
					}
				}
			}
		}
			
		return applicationName.Replace(".","_").ToLower();
	}

	private static string CreateShortAfqn(Type type, string path = "", string delimiter = ".")
	{
		string t = $"{path}{(string.IsNullOrEmpty(path) ? string.Empty : delimiter)}{GetNonGenericTypeName(type)}";

		if (type.GetTypeInfo().IsGenericType)
		{
			t += "[";
			t = type.GenericTypeArguments.Aggregate(t, (current, argument) => CreateShortAfqn(argument, current, current.EndsWith("[", StringComparison.Ordinal) ? string.Empty : ","));
			t += "]";
		}

		return (t.Length > 254
			? string.Concat("...", t.Substring(t.Length - 250))
			: t).ToLowerInvariant();
	}

	public static string GetNonGenericTypeName(Type type)
	{
		string[] name = !type.GetTypeInfo().IsGenericType
			? new[] { type.Name }
			: type.Name.Split('`');

		return name[0];
	}
}
