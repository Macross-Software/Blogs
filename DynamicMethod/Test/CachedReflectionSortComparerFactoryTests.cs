using Microsoft.VisualStudio.TestTools.UnitTesting;

using Code;

namespace Tests
{
	[TestClass]
	public class CachedReflectionSortComparerFactoryTests : SortComparerFactoryTests
	{
		protected override SortComparerFactory SortComparerFactory { get; } = new CachedReflectionSortComparerFactory();
	}
}