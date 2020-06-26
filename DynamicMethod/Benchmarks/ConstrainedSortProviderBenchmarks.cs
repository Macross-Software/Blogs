using System;
using System.Collections.Generic;
using System.Linq;

using BenchmarkDotNet.Attributes;

using Code;

namespace Benchmarks
{
	[MemoryDiagnoser]
	public class ConstrainedSortProviderBenchmarks
	{
		private static readonly SortCriteria[] s_SortCriteria = new SortCriteria[]
		{
			new SortCriteria
			{
				SortDirection = SortDirection.Ascending,
				SortField = "Text"
			},
			new SortCriteria
			{
				SortDirection = SortDirection.Descending,
				SortField = "Flag"
			},
			new SortCriteria
			{
				SortDirection = SortDirection.Ascending,
				SortField = "Day"
			},
			new SortCriteria
			{
				SortDirection = SortDirection.Ascending,
				SortField = "Id"
			},
		};

		private List<Thing>? _Things;

		[Params(100, 5000)]
		public int NumberOfItems { get; set; }

		[GlobalSetup]
		public void GlobalSetup()
			=> _Things = Thing.GenerateThings(NumberOfItems, false);

		[Benchmark(Baseline = true)]
		public void CodeBaseline()
		{
			IEnumerable<Thing> Results = _Things
				.OrderBy(i => i.Text)
				.ThenByDescending(i => i.Flag)
				.OrderBy(i => i.Day)
				.OrderBy(i => i.Id);

			if (Results.ToArray().Length != NumberOfItems)
				throw new InvalidOperationException();
		}

		[Benchmark]
		public void BaseTypeDynamicMethod()
		{
			IEnumerable<Thing> Results = _Things.Sort(
				new BaseTypeDynamicMethodSortComparerFactory(),
				s_SortCriteria);

			if (Results.ToArray().Length != NumberOfItems)
				throw new InvalidOperationException();
		}
	}
}