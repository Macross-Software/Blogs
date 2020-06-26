using System.Collections.Generic;

namespace Code
{
	public static class MethodsToDecompile
	{
		public static int CompareValueType(Thing x, Thing y)
		{
			int? valueX = ((SwampThing)x).License;
			int? valueY = ((SpaceThing)y).License;

			if (!SortComparerFactory.TryEnsureValidValues(valueX.HasValue, valueY.HasValue, out int valueComparisonResult))
				return valueComparisonResult;

			return valueX.Value.CompareTo(valueY.Value);
		}

		public static int CompareValueBaseType(Thing x, Thing y)
		{
			int? valueX = x.Id;
			int? valueY = y.Id;

			if (!SortComparerFactory.TryEnsureValidValues(valueX.HasValue, valueY.HasValue, out int valueComparisonResult))
				return valueComparisonResult;

			return valueX.Value.CompareTo(valueY.Value);
		}

		public static int CompareValueTypeNoGetter()
		{
			int? valueX = null;
			int? valueY = null;

			if (!SortComparerFactory.TryEnsureValidValues(valueX.HasValue, valueY.HasValue, out int valueComparisonResult))
				return valueComparisonResult;

			return valueX.Value.CompareTo(valueY.Value);
		}

		public static int CompareEnumType(Thing x, Thing y)
		{
			long? valueX = (long)x.Day;
			long? valueY = (long)y.Day;

			if (!SortComparerFactory.TryEnsureValidValues(valueX.HasValue, valueY.HasValue, out int valueComparisonResult))
				return valueComparisonResult;

			return valueX.Value.CompareTo(valueY);
		}

		public static int CompareReferenceType(Thing x)
		{
			string? valueX = x.Text;
			string? valueY = null;

			if (!SortComparerFactory.TryEnsureValidReferences(valueX, valueY, out int referenceComparisonResult))
				return referenceComparisonResult;

			return valueX.CompareTo(valueY);
		}

		public static int DefaultComparer(Thing x, Thing y)
		{
			object? valueX = ((SwampThing)x).Value;
			object? valueY = ((SpaceThing)y).Value;

			return Comparer<object>.Default.Compare(valueX, valueY);
		}

		public static int DefaultOneSidedReferenceComparer(Thing x)
		{
			object? valueX = x.Text;

			if (valueX is null)
				return 0;
			return -1;
		}

		public static int DefaultOneSidedValueComparer(Thing x)
		{
			int? valueX = x.Id;

			if (!valueX.HasValue)
				return 0;
			return 1;
		}
	}
}
