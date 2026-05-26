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
[MaxIterationCount(25)]
[Config(typeof(Config))]
public class NumberStringComparerBenchmarks
{
	private List<string> _mixedShort = null!;
	private List<string> _mixedLong = null!;
	private List<string> _pureNumbers = null!;
	private List<string> _alphanumeric = null!;
	private List<string> _commaSeparated = null!;
	private List<KeyValuePair<string, string>> _dictionary = null!;

	[GlobalSetup]
	public void Setup()
	{
		// From test: mixed numbers and strings (21 items)
		_mixedShort = new List<string> {
			"a", "x", "yy", "yya", "yyz", "yyb",
			"1", "2", "3", "4", "11", "22", "33", "44", "111", "10",
			"1ab", "2ab", "3abb", "4ab", "4Ab"
		};

		// Larger realistic dataset (10,000+ items with longer strings)
		_mixedLong = new List<string>();
		for (int i = 0; i < 2000; i++)
		{
			_mixedLong.Add(i.ToString());
			_mixedLong.Add((i * 10).ToString());
			_mixedLong.Add($"{i}ab");
			_mixedLong.Add($"item{i}");
			_mixedLong.Add($"test{i}data{i * 2}moretext{i * 3}");
		}
		// Add random strings with longer length
		for (int i = 0; i < 1000; i++)
		{
			_mixedLong.Add(GenerateRandomString(Random.Shared, 20, 50));
		}

		// Pure numeric strings (5,000 items)
		_pureNumbers = new List<string>();
		for (int i = 0; i < 2500; i++)
		{
			_pureNumbers.Add(i.ToString());
			_pureNumbers.Add((i * 100).ToString());
		}

		// Complex alphanumeric (8,000 items with longer strings)
		_alphanumeric = new List<string>();
		for (int i = 0; i < 2000; i++)
		{
			_alphanumeric.Add($"{i}AA");
			_alphanumeric.Add($"{i}AAa");
			_alphanumeric.Add($"{i}AAa{i * 10}BBB{i * 20}CCC");
			_alphanumeric.Add($"AA{i}BB{i * 2}CC{i * 3}");
		}

		// Comma-separated (6,000 items with longer sequences)
		_commaSeparated = new List<string>();
		for (int i = 0; i < 2000; i++)
		{
			_commaSeparated.Add($"{i},{i + 1}");
			_commaSeparated.Add($"{i},{i + 1},{i + 2},{i + 3},{i + 4}");
			_commaSeparated.Add($"{i}, a, b, c, d, e, f");
		}

		// Dictionary scenario (from tests)
		_dictionary = new List<KeyValuePair<string, string>>();
		foreach (var item in _mixedShort)
		{
			_dictionary.Add(new KeyValuePair<string, string>(item, item));
		}
	}

	private static string GenerateRandomString(Random random, int minLength, int maxLength)
	{
		int length = random.Next(minLength, maxLength);
		var chars = new char[length];
		for (int i = 0; i < length; i++)
		{
			// Mix of letters and numbers
			if (random.Next(2) == 0)
			{
				chars[i] = (char)random.Next('a', 'z' + 1);
			}
			else
			{
				chars[i] = (char)random.Next('0', '9' + 1);
			}
		}
		return new string(chars);
	}

	//// Mixed short list (21 items) - from actual test
	//[Benchmark(Baseline = true)]
	//public void Original_MixedShort()
	//{
	//	var list = new List<string>(_mixedShort);
	//	list.Sort(NumberStringComparerOriginal<string>.GetComparer());
	//}
	//[Benchmark]
	//public void Current_MixedShort()
	//{
	//	var list = new List<string>(_mixedShort);
	//	list.Sort(NumberStringComparer<string>.GetComparer());
	//}

	//// Pure numbers (5,000 items) - best case scenario
	//[Benchmark]
	//public void Original_PureNumbers() {
	//	var list = new List<string>(_pureNumbers);
	//	list.Sort(NumberStringComparerOriginal<string>.GetComparer());
	//}
	//[Benchmark]
	//public void Current_PureNumbers() {
	//	var list = new List<string>(_pureNumbers);
	//	list.Sort(NumberStringComparer<string>.GetComparer());
	//}

	//// Dictionary/KeyValuePair scenario
	//[Benchmark]
	//public void Original_Dictionary() {
	//	var list = new List<KeyValuePair<string, string>>(_dictionary);
	//	list.Sort(NumberStringComparerOriginal<KeyValuePair<string, string>>.GetComparer());
	//}
	//[Benchmark]
	//public void Current_Dictionary() {
	//	var list = new List<KeyValuePair<string, string>>(_dictionary);
	//	list.Sort(NumberStringComparer<KeyValuePair<string, string>>.GetComparer());
	//}


	// Mixed long list (11,000+ items) - realistic large dataset
	[Benchmark]
	public void Original_MixedLong()
	{
		var list = new List<string>(_mixedLong);
		list.Sort(NumberStringComparerOriginal<string>.GetComparer());
	}

	[Benchmark]
	public void Current_MixedLong()
	{
		var list = new List<string>(_mixedLong);
		list.Sort(NumberStringComparer<string>.GetComparer());
	}

	// Complex alphanumeric (8,000 items) - heavy parsing
	[Benchmark]
	public void Original_Alphanumeric()
	{
		var list = new List<string>(_alphanumeric);
		list.Sort(NumberStringComparerOriginal<string>.GetComparer());
	}
	[Benchmark]
	public void Current_Alphanumeric()
	{
		var list = new List<string>(_alphanumeric);
		list.Sort(NumberStringComparer<string>.GetComparer());
	}

	// Comma-separated (6,000 items) - special case
	[Benchmark]
	public void Original_CommaSeparated()
	{
		var list = new List<string>(_commaSeparated);
		list.Sort(NumberStringComparerOriginal<string>.GetComparer());
	}
	[Benchmark]
	public void Current_CommaSeparated()
	{
		var list = new List<string>(_commaSeparated);
		list.Sort(NumberStringComparer<string>.GetComparer());
	}


	private class Config : ManualConfig
	{
		public Config()
		{
			SummaryStyle = SummaryStyle.Default.WithRatioStyle(RatioStyle.Percentage);
		}
	}
}