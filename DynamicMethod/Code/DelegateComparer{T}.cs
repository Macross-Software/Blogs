using System;
using System.Collections.Generic;

namespace Code
{
	public class DelegateComparer<T> : IComparer<T>
	{
		private readonly Func<T, T, int> _CompareFunc;

		public DelegateComparer(Func<T, T, int> compareFunc)
		{
#if DEBUG
			_CompareFunc = compareFunc ?? throw new ArgumentNullException(nameof(compareFunc));
#else
			_CompareFunc = compareFunc;
#endif
		}

		public int Compare(T x, T y)
		{
			return !SortComparerFactory.TryEnsureValidReferences(x, y, out int referenceComparisonResult)
				? referenceComparisonResult
				: _CompareFunc.Invoke(x, y);
		}
	}
}
