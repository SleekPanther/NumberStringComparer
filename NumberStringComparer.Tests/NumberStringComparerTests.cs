using NumberStringComparer;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace NumberStringComparer.Tests {
	public class NumberStringComparerTests {
		private static readonly IReadOnlyList<string> list = new List<string>().AsReadOnly();

		[Fact]
		public void NumberStringComparerType_Tests() {
			Assert.True(NumberStringComparer<string>.IsValidType(typeof(string)));
			Assert.True(NumberStringComparer<int>.IsValidType(typeof(int)));
			Assert.True(NumberStringComparer<double>.IsValidType(typeof(double)));
			Assert.True(NumberStringComparer<float>.IsValidType(typeof(float)));
			Assert.True(NumberStringComparer<decimal>.IsValidType(typeof(decimal)));
			Assert.True(NumberStringComparer<long>.IsValidType(typeof(long)));
			Assert.True(NumberStringComparer<short>.IsValidType(typeof(short)));

			Assert.False(NumberStringComparer<short>.IsValidType(typeof(DateTime)));

			Assert.True(NumberStringComparer<KeyValuePair<string, IReadOnlyList<string>>>.IsValidType(typeof(KeyValuePair<string, IReadOnlyList<string>>)));
			Assert.True(NumberStringComparer<KeyValuePair<int, IReadOnlyList<string>>>.IsValidType(typeof(KeyValuePair<int, IReadOnlyList<string>>)));
			Assert.True(NumberStringComparer<KeyValuePair<double, IReadOnlyList<string>>>.IsValidType(typeof(KeyValuePair<double, IReadOnlyList<string>>)));
			Assert.True(NumberStringComparer<KeyValuePair<float, IReadOnlyList<string>>>.IsValidType(typeof(KeyValuePair<float, IReadOnlyList<string>>)));
			Assert.True(NumberStringComparer<KeyValuePair<decimal, IReadOnlyList<string>>>.IsValidType(typeof(KeyValuePair<decimal, IReadOnlyList<string>>)));
			Assert.True(NumberStringComparer<KeyValuePair<long, IReadOnlyList<string>>>.IsValidType(typeof(KeyValuePair<long, IReadOnlyList<string>>)));
			Assert.True(NumberStringComparer<KeyValuePair<short, IReadOnlyList<string>>>.IsValidType(typeof(KeyValuePair<short, IReadOnlyList<string>>)));

			Assert.False(NumberStringComparer<KeyValuePair<DateTime, IReadOnlyList<string>>>.IsValidType(typeof(KeyValuePair<DateTime, IReadOnlyList<string>>)));
		}

		[Fact]
		public void NumberStringParse_Tests() {
			var num1 = NumberStringComparer<string>.NumberString<string>.Parse("1");
			Assert.Equal(1, num1.Number);
			Assert.Equal("1", num1.Text);

			var num2 = NumberStringComparer<int>.NumberString<int>.Parse(1);
			Assert.Equal(1, num2.Number);
			Assert.Equal("1", num2.Text);

			var num3 = NumberStringComparer<int>.NumberString<int>.Parse("a");
			Assert.Null(num3.Number);
			Assert.Equal("a", num3.Text);

			var ex = Record.Exception(() => NumberStringComparer<KeyValuePair<string, IReadOnlyList<string>>>
				.NumberString<KeyValuePair<string, IReadOnlyList<string>>>.Parse(new KeyValuePair<string, IReadOnlyList<string>>("1", new List<string>() { "1", })));
			Assert.Null(ex);

			var ex2 = Record.Exception(() => NumberStringComparer<DateTime>
				.NumberString<DateTime>.Parse(new DateTime(2023, 1, 1)));
			Assert.NotNull(ex2);
			Assert.IsType<InvalidOperationException>(ex2);

			var num4 = NumberStringComparer<DateTime>
				.NumberString<DateTime>.Parse(new DateTime(2023, 1, 1), nameof(DateTime.Year));
			Assert.Equal(2023, num4.Number);
			Assert.Equal("2023", num4.Text);
		}

		[Fact]
		public void NumberStringToString_Tests() {
			var num4 = new NumberStringComparer<string>.NumberString<string>(1, "1");
			Assert.Equal("1 (1)", num4.ToString());

			var num5 = new NumberStringComparer<string>.NumberString<string>(null, "a");
			Assert.Equal("null (a)", num5.ToString());

			var num6 = new NumberStringComparer<string>.NumberString<string>(null, null);
			Assert.Equal("null (null)", num6.ToString());
		}

		[Theory]
		[MemberData(nameof(NumberStringGetParts_TestData))]
		public void NumberStringGetParts_Tests(string[] values, string[][] expected) {
			for(int i = 0; i < values.Length; i++) {
				Assert.Equal(expected[i], NumberStringComparer<string>.NumberString<string>.GetParts(values[i]));
			}
		}
		public static object[][] NumberStringGetParts_TestData = new[] {
			new object[]{
				new[] {
					"22",
					"a",
					"1,2, ",
					"1,2,3",
					"1,2,3, 4 ,",
					"1,2,3, 4 , a, ",
					"22AA",
					"22AAa",
					"22AAa1111",
					"22AA1aa",
					"",
					"1",
					"10",
					"AA1",
					"3",
					"AA11",
					"AA11aa",
				},
				new string[][]{
					new []{ "22", },
					new []{ "a", },
					new []{ "1", "2", },
					new []{ "1", "2", "3", },
					new []{ "1", "2", "3", "4", },
					new []{ "1", "2", "3", "4", "a", },
					new []{ "22", "AA", },
					new []{ "22", "AAa", },
					new []{ "22", "AAa", "1111", },
					new []{ "22", "AA", "1", "aa", },
					new string [0],
					new []{ "1", },
					new []{ "10", },
					new []{ "AA", "1", },
					new []{ "3", },
					new []{ "AA", "11", },
					new []{ "AA", "11", "aa", },
					new []{ "", },
				},
			},
		};

		[Fact]
		public void NumberStringComparerSort_Tests() {
			var dict = new Dictionary<string, IReadOnlyList<string>>() {
				{ "A", list },
				{ "c", list },
				{ "X", list },
				{ "11", list },
				{ "1", list },
				{ "12", list },
				{ "120", list },
				{ "3", list },
			};
			var sorted = dict.ToList();
			sorted.Sort(NumberStringComparer<KeyValuePair<string, IReadOnlyList<string>>>.GetComparer());
			string[] expectedDictionary1 = new[] {
				"1",
				"3",
				"11",
				"12",
				"120",
				"A",
				"c",
				"X",
			};
			Assert.Equal(expectedDictionary1, sorted.Select(kvp => kvp.Key));

			var dict2 = new Dictionary<string, string>() {
				{"22", "22" },
				{"a", "a" },
				{"22AA", "22AA" },
				{"22AA1", "22AA1" },
				{"1", "1" },
				{"10", "10" },
				{"AA1", "AA1" },
				{"3", "3" },
				{"1,2,3", "1,2,3" },
				{"1,2", "1,2" },
				{"1, a, ", "1,2" },
			};
			var sorted2 = dict2.ToList();
			sorted2.Sort(NumberStringComparer<KeyValuePair<string, string>>.GetComparer());
			string[] expectedDictionary2 = new[] {
				"1",
				"1,2",
				"1,2,3",
				"1, a, ",
				"3",
				"10",
				"22",
				"22AA",
				"22AA1",
				"a",
				"AA1",
			};
			Assert.Equal(expectedDictionary2, sorted2.Select(kvp => kvp.Key));

			var list1 = new List<string>() {
				"a",
				"x",
				"yy",
				"yya",
				"yyz",
				"yyb",
				"1",
				"2",
				"3",
				"4",
				"11",
				"22",
				"33",
				"44",
				"111",
				"10",
				"1ab",
				"2ab",
				"3abb",
				"4ab",
				"4Ab",
			};
			list1.Sort(NumberStringComparer<string>.GetComparer());
			string[] expectedList = new[] {
				"1",
				"1ab",
				"2",
				"2ab",
				"3",
				"3abb",
				"4",
				"4ab",
				"4Ab",
				"10",
				"11",
				"22",
				"33",
				"44",
				"111",
				"a",
				"x",
				"yy",
				"yya",
				"yyb",
				"yyz",
			};
			Assert.Equal(expectedList, list1);

			string[] array1 = new[] {
				"a",
				"x",
				"yy",
				"yya",
				"yyz",
				"yyb",
				"1",
				"2",
				"3",
				"4",
				"11",
				"22",
				"33",
				"44",
				"111",
				"10",
				"1ab",
				"2ab",
				"3abb",
				"4ab",
				"4Ab",
			};
			Array.Sort(array1, NumberStringComparer<string>.GetComparer());
			string[] expectedArray = new[] {
				"1",
				"1ab",
				"2",
				"2ab",
				"3",
				"3abb",
				"4",
				"4ab",
				"4Ab",
				"10",
				"11",
				"22",
				"33",
				"44",
				"111",
				"a",
				"x",
				"yy",
				"yya",
				"yyb",
				"yyz",
			};
			Assert.Equal(expectedArray, array1);
		}

		[Fact]
		public void NumberStringComparerObjectComparerException_Tests() {
			var exWrongDateTimeComparerMethod = Record.Exception(() => Array.Sort(new[] { new DateTime(), }, NumberStringComparer<DateTime>.GetComparer()));
			Assert.NotNull(exWrongDateTimeComparerMethod);
			Assert.IsType<InvalidOperationException>(exWrongDateTimeComparerMethod);

			var exEmptyPropertyNameDateTimeObjectComparer = Record.Exception(() => Array.Sort(new[] { new DateTime(), }, NumberStringComparer<DateTime>.GetObjectComparer(null)));
			Assert.NotNull(exEmptyPropertyNameDateTimeObjectComparer);
			Assert.IsType<ArgumentNullException>(exEmptyPropertyNameDateTimeObjectComparer);

			var exInvalidPropertyDateTimeObjectComparer = Record.Exception(() => Array.Sort(new[] { new DateTime(), }, NumberStringComparer<DateTime>.GetObjectComparer("FakeProperty")));
			Assert.NotNull(exInvalidPropertyDateTimeObjectComparer);
			Assert.IsType<InvalidOperationException>(exInvalidPropertyDateTimeObjectComparer);

			var exWrongObjectComparer = Record.Exception(() => Array.Sort(new[] { string.Empty, }, NumberStringComparer<string>.GetObjectComparer("FakeProperty")));
			Assert.NotNull(exWrongObjectComparer);
			Assert.IsType<InvalidOperationException>(exWrongObjectComparer);
		}

		[Fact]
		public void NumberStringComparerObjectComparison_Tests() {
			var dateTimeObjects = new[] {
				new DateTime(2023, 1, 1),
				new DateTime(2023, 3, 1),
				new DateTime(2023, 2, 1),
			};
			var expectedDateTimeObjects = new[]
			{
				new DateTime(2023, 1, 1),
				new DateTime(2023, 2, 1),
				new DateTime(2023, 3, 1),
			};
			Array.Sort(dateTimeObjects, NumberStringComparer<DateTime>.GetObjectComparer(nameof(DateTime.Month)));
			Assert.Equal(expectedDateTimeObjects, dateTimeObjects);


			var dateTimeCompoundSortObjects = new[] {
				new DateTime(2023, 3, 1),
				new DateTime(2023, 2, 1),
				new DateTime(2023, 2, 3),
				new DateTime(2023, 2, 1),
				new DateTime(2023, 1, 1),
			};
			var expectedDateTimeCompoundSortObjects = new[] {
				new DateTime(2023, 1, 1),
				new DateTime(2023, 2, 1),
				new DateTime(2023, 2, 1),
				new DateTime(2023, 2, 3),
				new DateTime(2023, 3, 1),
			};
			dateTimeCompoundSortObjects = dateTimeCompoundSortObjects
				.OrderBy(d => d, NumberStringComparer<DateTime>.GetObjectComparer(nameof(DateTime.Month)))
				.ThenBy(d => d, NumberStringComparer<DateTime>.GetObjectComparer(nameof(DateTime.Day)))
				.ToArray();
			Assert.Equal(expectedDateTimeCompoundSortObjects, dateTimeCompoundSortObjects);
		}
	}
}
