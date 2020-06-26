using System;
using System.Collections.Generic;
using System.Reflection;

namespace Code
{
	public class ReflectionSortComparerFactory : SortComparerFactory
	{
		public override IComparer<T> BuildSortComparer<T>(SortCriteria sortCriteria)
#if DEBUG
		{
			return sortCriteria == null
				? throw new ArgumentNullException(nameof(sortCriteria))
				: new DelegateComparer<T>(BuildComparerFunc<T>(sortCriteria));
		}
#else
			// Turn off CA1062 in release for performance.
#pragma warning disable CA1062 // Validate arguments of public methods
			=> new DelegateComparer<T>(BuildComparerFunc<T>(sortCriteria));
#pragma warning restore CA1062 // Validate arguments of public methods
#endif

		private static Func<T, T, int> BuildComparerFunc<T>(SortCriteria sortCriteria)
		{
			string sortField = sortCriteria.SortField;

			return (x, y) =>
			{
				object? valueX = x!.GetType().GetProperty(sortField, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase)?.GetMethod.Invoke(x, null);
				object? valueY = y!.GetType().GetProperty(sortField, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase)?.GetMethod.Invoke(y, null);

				if (!TryEnsureValidReferences(valueX, valueY, out int referenceComparisonResult))
					return referenceComparisonResult;

				Type targetType = SortComparerReflectionHelper.ResolveTargetType(valueX.GetType());
				if (targetType == SortComparerReflectionHelper.ResolveTargetType(valueY.GetType()))
				{
					MethodInfo? CompareToTyped = null;
					foreach (MethodInfo method in targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public))
					{
						if (method.Name != "CompareTo")
							continue;

						ParameterInfo[] methodParams = method.GetParameters();
						if ((methodParams?.Length ?? 0) != 1)
							continue;

						if (methodParams![0].ParameterType == targetType)
						{
							CompareToTyped = method;
							break;
						}
					}

					if (CompareToTyped != null)
						return (int)CompareToTyped.Invoke(valueX, new object[] { valueY });
				}

				return Comparer<object>.Default.Compare(valueX, valueY);
			};
		}
	}
}
