using System.Linq;

using Code;

namespace System.Collections.Generic
{
	public static class Extensions
	{
		public static IEnumerable<T> Sort<T>(this IEnumerable<T> items, SortComparerFactory sortComparerFactory, IEnumerable<SortCriteria> sortCriteria)
		{
			if (items == null)
				throw new ArgumentNullException(nameof(items));
			if (sortComparerFactory == null)
				throw new ArgumentNullException(nameof(sortComparerFactory));

			IOrderedEnumerable<T>? query = null;

			foreach (SortCriteria criteria in sortCriteria ?? Array.Empty<SortCriteria>())
			{
				IComparer<T> sortComparer = sortComparerFactory.BuildSortComparer<T>(criteria);

				if (query == null)
				{
					query = criteria.SortDirection == SortDirection.Ascending
						? items.OrderBy(i => i, sortComparer)
						: items.OrderByDescending(i => i, sortComparer);
				}
				else
				{
					query = criteria.SortDirection == SortDirection.Ascending
						? query.ThenBy(i => i, sortComparer)
						: query.ThenByDescending(i => i, sortComparer);
				}
			}

			return query ?? items;
		}
	}
}
