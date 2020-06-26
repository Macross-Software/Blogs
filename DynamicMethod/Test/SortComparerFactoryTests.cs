#pragma warning disable SA1118 // Parameter should not span multiple lines
using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Code;

namespace Tests
{
	public abstract class SortComparerFactoryTests
	{
		protected abstract SortComparerFactory SortComparerFactory { get; }

		[TestMethod]
		public void SortProviderValueTypeTest()
		{
			List<Thing> Things = Thing.GenerateThings(10);

			IEnumerable<Thing> Results = Things.Sort(
				SortComparerFactory,
				new SortCriteria[]
				{
					new SortCriteria
					{
						SortDirection = SortDirection.Ascending,
						SortField = "Id"
					}
				});

			CollectionAssert.AreEqual(
				Things.OrderBy(i => i.Id).ToList(),
				Results.ToList());

			Results = Things.Sort(
				SortComparerFactory,
				new SortCriteria[]
				{
					new SortCriteria
					{
						SortDirection = SortDirection.Descending,
						SortField = "Id"
					}
				});

			CollectionAssert.AreEqual(
				Things.OrderByDescending(i => i.Id).ToList(),
				Results.ToList());
		}

		[TestMethod]
		public void SortProviderReferenceTypeTest()
		{
			List<Thing> Things = Thing.GenerateThings(10);

			IEnumerable<Thing> Results = Things.Sort(
				SortComparerFactory,
				new SortCriteria[]
				{
					new SortCriteria
					{
						SortDirection = SortDirection.Ascending,
						SortField = "Text"
					}
				});

			CollectionAssert.AreEqual(
				Things.OrderBy(i => i.Text).ToList(),
				Results.ToList());

			Results = Things.Sort(
				SortComparerFactory,
				new SortCriteria[]
				{
					new SortCriteria
					{
						SortDirection = SortDirection.Descending,
						SortField = "Text"
					}
				});

			CollectionAssert.AreEqual(
				Things.OrderByDescending(i => i.Text).ToList(),
				Results.ToList());
		}

		[TestMethod]
		public void SortProviderNullableTypeTest()
		{
			List<Thing> Things = Thing.GenerateThings(10);

			IEnumerable<Thing> Results = Things.Sort(
				SortComparerFactory,
				new SortCriteria[]
				{
					new SortCriteria
					{
						SortDirection = SortDirection.Ascending,
						SortField = "Flag"
					}
				});

			CollectionAssert.AreEqual(
				Things.OrderBy(i => i.Flag).ToList(),
				Results.ToList());

			Results = Things.Sort(
				SortComparerFactory,
				new SortCriteria[]
				{
					new SortCriteria
					{
						SortDirection = SortDirection.Descending,
						SortField = "Flag"
					}
				});

			CollectionAssert.AreEqual(
				Things.OrderByDescending(i => i.Flag).ToList(),
				Results.ToList());
		}

		[TestMethod]
		public void SortProviderEnumTypeTest()
		{
			List<Thing> Things = Thing.GenerateThings(10);

			IEnumerable<Thing> Results = Things.Sort(
				SortComparerFactory,
				new SortCriteria[]
				{
					new SortCriteria
					{
						SortDirection = SortDirection.Ascending,
						SortField = "Day"
					}
				});

			CollectionAssert.AreEqual(
				Things.OrderBy(i => i.Day).ToList(),
				Results.ToList());

			Results = Things.Sort(
				SortComparerFactory,
				new SortCriteria[]
				{
					new SortCriteria
					{
						SortDirection = SortDirection.Descending,
						SortField = "Day"
					}
				});

			CollectionAssert.AreEqual(
				Things.OrderByDescending(i => i.Day).ToList(),
				Results.ToList());
		}

		[TestMethod]
		public void SortProviderMixedTypeTest()
		{
			List<Thing> Things = Thing.GenerateThings(100);

			SortCriteria[] sortCriteria = new SortCriteria[]
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
					SortField = "Id"
				},
			};

			IEnumerable<Thing> Results = Things.Sort(
				SortComparerFactory,
				sortCriteria);

			CollectionAssert.AreEqual(
				Things.OrderBy(i => i.Text).ThenByDescending(i => i.Flag).ThenBy(i => i.Day).ThenByDescending(i => i.Id).ToList(),
				Results.ToList());
		}

		[TestMethod]
		public void SortProviderSwampThingTest()
		{
			List<Thing> Things = Thing.GenerateThings(10);

			IEnumerable<Thing> Results = Things.Sort(
				SortComparerFactory,
				new SortCriteria[]
				{
					new SortCriteria
					{
						SortDirection = SortDirection.Ascending,
						SortField = "SwampName"
					}
				});

			CollectionAssert.AreEqual(
				Things.OrderBy(i => (i as SwampThing)?.SwampName ?? null).ToList(),
				Results.ToList());

			Results = Things.Sort(
				SortComparerFactory,
				new SortCriteria[]
				{
					new SortCriteria
					{
						SortDirection = SortDirection.Descending,
						SortField = "SwampName"
					}
				});

			CollectionAssert.AreEqual(
				Things.OrderByDescending(i => (i as SwampThing)?.SwampName ?? null).ToList(),
				Results.ToList());
		}

		[TestMethod]
		public void SortProviderMismatchedTypesTest()
		{
			List<Thing> Things = Thing.GenerateThings(10);

			IEnumerable<Thing> Results = Things.Sort(
				SortComparerFactory,
				new SortCriteria[]
				{
					new SortCriteria
					{
						SortDirection = SortDirection.Ascending,
						SortField = "Value"
					}
				});

			try
			{
				Results.ToList();
				Assert.Fail();
			}
#pragma warning disable CA1031 // Do not catch general exception types
			catch (Exception exception)
#pragma warning restore CA1031 // Do not catch general exception types
			{
				bool FoundMatch = false;
				while (exception != null)
				{
					if (exception.Message == "Object must be of type Int64.")
					{
						FoundMatch = true;
						break;
					}
					exception = exception.InnerException;
				}
				if (!FoundMatch)
					Assert.Fail();
			}
		}

		[TestMethod]
		public void SortProviderOneSideNullableTypeTest()
		{
			List<Thing> Things = Thing.GenerateThings(10);

			IEnumerable<Thing> Results = Things.Sort(
				SortComparerFactory,
				new SortCriteria[]
				{
					new SortCriteria
					{
						SortDirection = SortDirection.Ascending,
						SortField = "License"
					}
				});

			CollectionAssert.AreEqual(
				Things.OrderBy(
					i => i is SwampThing swampThing
						? swampThing.License
						: i is SpaceThing spaceThing
							? spaceThing.License
							: null).ToList(),
				Results.ToList());

			Results = Things.Sort(
				SortComparerFactory,
				new SortCriteria[]
				{
					new SortCriteria
					{
						SortDirection = SortDirection.Descending,
						SortField = "License"
					}
				});

			CollectionAssert.AreEqual(
				Things.OrderByDescending(
					i => i is SwampThing swampThing
						? swampThing.License
						: i is SpaceThing spaceThing
							? spaceThing.License
							: null).ToList(),
				Results.ToList());
		}
	}
}
#pragma warning restore SA1118 // Parameter should not span multiple lines