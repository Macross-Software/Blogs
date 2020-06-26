using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace Code
{
	public class CachedReflectionSortComparerFactory : SortComparerFactory
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
			private static readonly ConcurrentDictionary<string, Func<T, T, int>> s_ComparerCache = new ConcurrentDictionary<string, Func<T, T, int>>();

			public static Func<T, T, int> BuildComparerFunc(SortCriteria sortCriteria)
			{
				string sortField = sortCriteria.SortField;

				if (!SortComparerFactory<T>.s_ComparerCache.TryGetValue(sortField, out Func<T, T, int> comparerFunc))
				{
					comparerFunc = (x, y) =>
					{
						object? valueX = SortComparerReflectionHelper.FindPropertyGetMethod(x!.GetType(), sortField)?.Invoke(x, null);
						object? valueY = SortComparerReflectionHelper.FindPropertyGetMethod(y!.GetType(), sortField)?.Invoke(y, null);

						if (!TryEnsureValidReferences(valueX, valueY, out int referenceComparisonResult))
							return referenceComparisonResult;

						Type targetType = SortComparerReflectionHelper.ResolveTargetType(valueX.GetType());
						if (targetType == SortComparerReflectionHelper.ResolveTargetType(valueY.GetType()))
						{
							MethodInfo? compareToMethodInfo = SortComparerReflectionHelper.FindCompareToMethodInfo(targetType);
							if (compareToMethodInfo != null)
								return (int)compareToMethodInfo.Invoke(valueX, new object[] { valueY });
						}

						return Comparer<object>.Default.Compare(valueX, valueY);
					};
					SortComparerFactory<T>.s_ComparerCache.TryAdd(sortField, comparerFunc);
				}

				return comparerFunc;
			}
		}
	}
}
