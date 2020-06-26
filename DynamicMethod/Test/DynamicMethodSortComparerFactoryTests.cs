using Microsoft.VisualStudio.TestTools.UnitTesting;

using Code;

namespace Tests
{
	[TestClass]
	public class DynamicMethodSortComparerFactoryTests : SortComparerFactoryTests
	{
		protected override SortComparerFactory SortComparerFactory { get; } = new DynamicMethodSortComparerFactory();
	}
}