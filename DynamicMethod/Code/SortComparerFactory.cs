using System.Collections.Generic;
#if NETSTANDARD2_1
using System.Diagnostics.CodeAnalysis;
#endif

namespace Code
{
	public abstract class SortComparerFactory
	{
		internal static bool TryEnsureValidValues(bool xHasValue, bool yHasValue, out int comparisonResult)
		{
			if (!xHasValue && !yHasValue)
			{
				comparisonResult = 0;
				return false;
			}

			if (!xHasValue)
			{
				comparisonResult = -1;
				return false;
			}

			if (!yHasValue)
			{
				comparisonResult = 1;
				return false;
			}

			comparisonResult = 0;
			return true;
		}

#if NETSTANDARD2_0
		internal static bool TryEnsureValidReferences(object? x, object? y, out int comparisonResult)
#else
		internal static bool TryEnsureValidReferences([NotNullWhen(true)] object? x, [NotNullWhen(true)] object? y, out int comparisonResult)
#endif
		{
			bool xIsNull = x is null;
			bool yIsNull = y is null;

			if (xIsNull && yIsNull)
			{
				comparisonResult = 0;
				return false;
			}

			if (xIsNull)
			{
				comparisonResult = -1;
				return false;
			}

			if (yIsNull)
			{
				comparisonResult = 1;
				return false;
			}

			comparisonResult = 0;
			return true;
		}

		public abstract IComparer<T> BuildSortComparer<T>(SortCriteria sortCriteria);
	}
}
