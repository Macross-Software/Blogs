using System;
using System.Collections.Generic;
using System.Linq;

using BenchmarkDotNet.Attributes;

using Code;

namespace Benchmarks
{
	[MemoryDiagnoser]
	public class SortProviderBenchmarks
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
				SortDirection = SortDirection.Descending,
				SortField = "License"
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
			=> _Things = Thing.GenerateThings(NumberOfItems);

		[Benchmark(Baseline = true)]
		public void CodeBaseline()
		{
			IEnumerable<Thing> Results = _Things
				.OrderBy(i => i.Text)
				.ThenByDescending(i => i.Flag)
				.OrderBy(i => i.Day)
				.ThenByDescending(
					i => i is SwampThing swampThing
						? swampThing.License
						: i is SpaceThing spaceThing
							? spaceThing.License
							: null)
				.OrderBy(i => i.Id);

			if (Results.ToArray().Length != NumberOfItems)
				throw new InvalidOperationException();
		}

		[Benchmark]
		public void ReflectionSortProvider()
		{
			IEnumerable<Thing> Results = _Things.Sort(
				new ReflectionSortComparerFactory(),
				s_SortCriteria);

			if (Results.ToArray().Length != NumberOfItems)
				throw new InvalidOperationException();
		}

		[Benchmark]
		public void CachedReflectionSortProvider()
		{
			IEnumerable<Thing> Results = _Things.Sort(
				new CachedReflectionSortComparerFactory(),
				s_SortCriteria);

			if (Results.ToArray().Length != NumberOfItems)
				throw new InvalidOperationException();
		}

		[Benchmark]
		public void DynamicMethod()
		{
			IEnumerable<Thing> Results = _Things.Sort(
				new DynamicMethodSortComparerFactory(),
				s_SortCriteria);

			if (Results.ToArray().Length != NumberOfItems)
				throw new InvalidOperationException();
		}
	}
}