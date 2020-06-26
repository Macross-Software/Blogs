using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace Code
{
	internal static class SortComparerReflectionHelper
	{
		public class NullableTypeInfo
		{
			public ConstructorInfo TargetNullableTypeCtor { get; }

			public MethodInfo TargetNullableTypeHasValue { get; }

			public MethodInfo TargetNullableTypeValue { get; }

			public NullableTypeInfo(Type targetNullableType, Type targetUnderlyingType)
			{
				TargetNullableTypeCtor = targetNullableType.GetConstructor(new Type[] { targetUnderlyingType });
				TargetNullableTypeHasValue = targetNullableType.GetProperty("HasValue", BindingFlags.Instance | BindingFlags.Public).GetMethod;
				TargetNullableTypeValue = targetNullableType.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public).GetMethod;
			}
		}

		private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, MethodInfo?>> s_PropertyGetMethodCache = new ConcurrentDictionary<Type, ConcurrentDictionary<string, MethodInfo?>>();
		private static readonly ConcurrentDictionary<Type, MethodInfo?> s_CompareToMethodInfoCache = new ConcurrentDictionary<Type, MethodInfo?>();
		private static readonly ConcurrentDictionary<Type, NullableTypeInfo> s_NullableTypeInfoCache = new ConcurrentDictionary<Type, NullableTypeInfo>();

		public static MethodInfo DefaultComparerDefaultMethodInfo { get; } = typeof(Comparer<object>).GetProperty("Default", BindingFlags.Static | BindingFlags.Public).GetMethod;

		public static MethodInfo DefaultComparerCompareMethodInfo { get; } = typeof(Comparer<object>).GetMethod("Compare", BindingFlags.Instance | BindingFlags.Public);

		public static MethodInfo TryEnsureValidReferencesMethodInfo { get; } = typeof(SortComparerFactory).GetMethod(nameof(SortComparerFactory.TryEnsureValidReferences), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

		public static MethodInfo TryEnsureValidValuesMethodInfo { get; } = typeof(SortComparerFactory).GetMethod(nameof(SortComparerFactory.TryEnsureValidValues), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

		public static MethodInfo? FindPropertyGetMethod(Type type, string propertyName)
		{
			if (!s_PropertyGetMethodCache.TryGetValue(type, out ConcurrentDictionary<string, MethodInfo?> getMethodCache))
			{
				getMethodCache = new ConcurrentDictionary<string, MethodInfo?>(StringComparer.OrdinalIgnoreCase);
				s_PropertyGetMethodCache.TryAdd(type, getMethodCache);
			}

			if (!getMethodCache.TryGetValue(propertyName, out MethodInfo? getMethod))
			{
				getMethod = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase)?.GetMethod;
				getMethodCache.TryAdd(propertyName, getMethod);
			}

			return getMethod;
		}

		public static MethodInfo? FindCompareToMethodInfo(Type propertyType)
		{
			if (!s_CompareToMethodInfoCache.TryGetValue(propertyType, out MethodInfo? compareToMethod))
			{
				Type? underlyingNullableType = Nullable.GetUnderlyingType(propertyType);

				foreach (MethodInfo method in (underlyingNullableType ?? propertyType).GetMethods(BindingFlags.Instance | BindingFlags.Public))
				{
					if (method.Name != "CompareTo")
						continue;

					ParameterInfo[] methodParams = method.GetParameters();
					if ((methodParams?.Length ?? 0) != 1)
						continue;

					if (methodParams![0].ParameterType == propertyType)
					{
						compareToMethod = method;
						break;
					}
				}

				s_CompareToMethodInfoCache.TryAdd(propertyType, compareToMethod);
			}

			return compareToMethod;
		}

		public static Type ResolveTargetType(Type propertyType)
		{
			if (propertyType.IsValueType)
				propertyType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

			return propertyType;
		}

		public static NullableTypeInfo FindNullableTypeInfo(Type targetNullableType, Type targetUnderlyingType)
		{
			if (!s_NullableTypeInfoCache.TryGetValue(targetNullableType, out NullableTypeInfo nullableTypeInfo))
			{
				nullableTypeInfo = new NullableTypeInfo(targetNullableType, targetUnderlyingType);
				s_NullableTypeInfoCache.TryAdd(targetNullableType, nullableTypeInfo);
			}

			return nullableTypeInfo;
		}
	}
}
