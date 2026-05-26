using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using NumberStringComparer;
using NumberStringComparer.InitialPrototype;

BenchmarkRunner.Run<NumberStringComparerBenchmarks>();

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[MaxIterationCount(50)]
[Config(typeof(Config))]
public class NumberStringComparerBenchmarks
{
	private List<string> _testData = null!;

	[GlobalSetup]
	public void Setup()
	{
		_testData = new List<string> { "1", "10", "2", "20", "a", "b", "1a", "2b" };
	}

	[Benchmark(Baseline = true)]
	public void SortWithOriginal()
	{
		var list = new List<string>(_testData);
		list.Sort(NumberStringComparerOriginal<string>.GetComparer());
	}

	[Benchmark]
	public void SortWithCurrent()
	{
		var list = new List<string>(_testData);
		list.Sort(NumberStringComparer<string>.GetComparer());
	}
	
	private class Config : ManualConfig {
		public Config() {
			SummaryStyle = SummaryStyle.Default.WithRatioStyle(RatioStyle.Percentage);
		}
	}
}