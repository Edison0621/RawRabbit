using System;
using System.Linq;
using System.Text.RegularExpressions;
using RawRabbit.Instantiation;

namespace RawRabbit.Enrichers.QueueSuffix;

public static  class ApplicationQueueSuffixPlugin
{
	private const string IisWorkerProcessName = "w3wp";
	private static readonly Regex _dllRegex = new(@"(?<ApplicationName>[^\\]*).dll", RegexOptions.Compiled);
	private static readonly Regex _consoleOrServiceRegex = new(@"(?<ApplicationName>[^\\]*).exe", RegexOptions.Compiled);
	private static readonly Regex _iisHostedAppRegexVer1 = new(@"-ap\s\\""(?<ApplicationName>[^\\]+)");
	private static readonly Regex _iisHostedAppRegexVer2 = new(@"\\\\apppools\\\\(?<ApplicationName>[^\\]+)");

	public static IClientBuilder UseApplicationQueueSuffix(this IClientBuilder builder)
	{
		string[] commandLine =  Environment.GetCommandLineArgs();
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

		string name =  applicationName.Replace(".", "_").ToLower();
		builder.UseQueueSuffix(new QueueSuffixOptions
		{
			_customSuffixFunc = context => name,
			_activeFunc = context => context.GetApplicationSuffixFlag()
		});
		return builder;
	}
}