using System.Threading;

namespace RawRabbit.Enrichers.GlobalExecutionId.Dependencies
{
	public class GlobalExecutionIdRepository
	{
		private static readonly AsyncLocal<string> _globalExecutionId = new AsyncLocal<string>();

		public static string Get()
		{
			return _globalExecutionId?.Value;
		}

		public static void Set(string id)
		{
			_globalExecutionId.Value = id;
		}
	}
}
