using Microsoft.VisualStudio.TestTools.UnitTesting;

using Code;

namespace Tests
{
	[TestClass]
	public class ReflectionSortComparerFactoryTests : SortComparerFactoryTests
	{
		protected override SortComparerFactory SortComparerFactory { get; } = new ReflectionSortComparerFactory();
	}
}