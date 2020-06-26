using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Code
{
	public class BaseTypeDynamicMethodSortComparerFactory : SortComparerFactory
	{
		public override IComparer<T> BuildSortComparer<T>(SortCriteria sortCriteria)
#if DEBUG
		{
			return sortCriteria == null
				? throw new ArgumentNullException(nameof(sortCriteria))
				: new DelegateComparer<T>(SortComparerFactory<T>.BuildComparerFunc(sortCriteria));
		}
#else
			// Turn off CA1062 in release for performance.
#pragma warning disable CA1062 // Validate arguments of public methods
			=> new DelegateComparer<T>(SortComparerFactory<T>.BuildComparerFunc(sortCriteria));
#pragma warning restore CA1062 // Validate arguments of public methods
#endif

		private static class SortComparerFactory<T>
		{
			private static readonly ConcurrentDictionary<string, Func<T, T, int>> s_PropertyComparerCache = new ConcurrentDictionary<string, Func<T, T, int>>();
			private static readonly Type s_SourceType = typeof(T);

			public static Func<T, T, int> BuildComparerFunc(SortCriteria sortCriteria)
			{
				string sortField = sortCriteria.SortField;

				if (!s_PropertyComparerCache.TryGetValue(sortField, out Func<T, T, int> propertyComparer))
				{
					propertyComparer = DynamicMethodSortComparerFactory.SortComparerFactory<T>.BuildPropertyComparer(s_SourceType, s_SourceType, sortField);
					s_PropertyComparerCache.TryAdd(sortField, propertyComparer);
				}

				return propertyComparer;
			}
		}
	}
}
