﻿// ReSharper disable All
using System.Threading;

#if NET451
using System.Runtime.Remoting.Messaging;
#endif

namespace RawRabbit.Enrichers.GlobalExecutionId.Dependencies
{
	public class GlobalExecutionIdRepository
	{
#if NETSTANDARD1_5
		private static readonly AsyncLocal<string> _globalExecutionId = new AsyncLocal<string>();
#elif NET451
		protected const string GlobalExecutionId = "RawRabbit:GlobalExecutionId";
#endif
		
		public static string Get()
		{
#if NETSTANDARD1_5
			return _globalExecutionId?.Value;
#elif NET451
			return CallContext.LogicalGetData(GlobalExecutionId) as string;
#endif
		}

		public static void Set(string id)
		{
#if NETSTANDARD1_5
			_globalExecutionId.Value = id;
#elif NET451
			CallContext.LogicalSetData(GlobalExecutionId, id);
#endif
		}
	}
}
