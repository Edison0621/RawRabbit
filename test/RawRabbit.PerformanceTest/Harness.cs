using System;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Xunit;

namespace RawRabbit.PerformanceTest
{
	public class Harness
	{
		[Fact]
		public void PubSubBenchmarks()
		{
			Summary result = BenchmarkRunner.Run<PubSubBenchmarks>();
			Assert.NotEqual(TimeSpan.Zero, result.TotalTime);
		}

		[Fact]
		public void RpcBenchmarks()
		{
			Summary result = BenchmarkRunner.Run<RpcBenchmarks>();
			Assert.NotEqual(TimeSpan.Zero, result.TotalTime);
		}

		[Fact]
		public void MessageContextBenchmarks()
		{
			Summary result = BenchmarkRunner.Run<MessageContextBenchmarks>();
			Assert.NotEqual(TimeSpan.Zero, result.TotalTime);
		}
	}
}
