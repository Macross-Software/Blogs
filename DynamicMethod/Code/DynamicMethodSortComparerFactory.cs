using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;

namespace Code
{
	public class DynamicMethodSortComparerFactory : SortComparerFactory
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

		internal static class SortComparerFactory<T>
		{
			private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<Type, ConcurrentDictionary<string, Func<T, T, int>>>> s_PropertyComparerCache = new ConcurrentDictionary<Type, ConcurrentDictionary<Type, ConcurrentDictionary<string, Func<T, T, int>>>>();
			private static readonly Type s_SourceType = typeof(T);

			public static Func<T, T, int> BuildComparerFunc(SortCriteria sortCriteria)
			{
				string sortField = sortCriteria.SortField;

				return (x, y) =>
				{
					Type typeX = x!.GetType();
					if (!s_PropertyComparerCache.TryGetValue(typeX, out ConcurrentDictionary<Type, ConcurrentDictionary<string, Func<T, T, int>>> subCache))
					{
						subCache = new ConcurrentDictionary<Type, ConcurrentDictionary<string, Func<T, T, int>>>();
						s_PropertyComparerCache.TryAdd(typeX, subCache);
					}

					Type typeY = y!.GetType();
					if (!subCache.TryGetValue(typeY, out ConcurrentDictionary<string, Func<T, T, int>> functionCache))
					{
						functionCache = new ConcurrentDictionary<string, Func<T, T, int>>();
						subCache.TryAdd(typeY, functionCache);
					}

					if (!functionCache.TryGetValue(sortField, out Func<T, T, int> propertyComparer))
					{
						propertyComparer = BuildPropertyComparer(typeX, typeY, sortField);
						functionCache.TryAdd(sortField, propertyComparer);
					}

					return propertyComparer(x, y);
				};
			}

			internal static Func<T, T, int> BuildPropertyComparer(Type typeX, Type typeY, string sortField)
			{
				MethodInfo? propertyX = SortComparerReflectionHelper.FindPropertyGetMethod(typeX, sortField);
				MethodInfo? propertyY = SortComparerReflectionHelper.FindPropertyGetMethod(typeY, sortField);

				if (propertyX == null && propertyY == null)
					return (x, y) => 0;

				if (propertyX == null || propertyY == null)
					return BuildOneSidedComparer((propertyX ?? propertyY)!, propertyX != null, sortField);

				Type targetType = SortComparerReflectionHelper.ResolveTargetType(propertyX.ReturnType);
				if (targetType != SortComparerReflectionHelper.ResolveTargetType(propertyY.ReturnType))
				{
					return BuildDefaultComparer(propertyX, propertyY, sortField);
				}

				bool isEnum = targetType.IsEnum;
				if (isEnum)
					targetType = typeof(long);

				MethodInfo? compareToMethodInfo = SortComparerReflectionHelper.FindCompareToMethodInfo(targetType);
				return compareToMethodInfo == null
					? BuildDefaultComparer(propertyX, propertyY, sortField)
					: !targetType.IsValueType
						? BuildReferenceTypePropertyComparer(propertyX, propertyY, sortField, targetType, compareToMethodInfo)
						: BuildValueTypePropertyComparer(propertyX, propertyY, sortField, typeof(Nullable<>).MakeGenericType(targetType), targetType, compareToMethodInfo, isEnum);
			}

			/// <remarks>
			/// This is used when a property is defined on one side of the comparison but not the other.
			/// Case A: PropertyX == null && PropertyY != null
			/// Case B: PropertyX != null && PropertyY == null
			/// The goal here is to return 0 if the found property is null or !HasValue, otherwise return -1 for CaseA, 1 for CaseB.
			/// Built using MethodsToDecompile.DefaultOneSidedReferenceComparer &amp; MethodsToDecompile.DefaultOneSidedValueComparer.
			/// </remarks>
			private static Func<T, T, int> BuildOneSidedComparer(
				MethodInfo property,
				bool isPropertyX,
				string sortField)
			{
				DynamicMethod compareMethod = new DynamicMethod($"{property.ReturnType.Name}.{isPropertyX}.{sortField}$OneSideComparer", typeof(int), new[] { s_SourceType, s_SourceType }, true);
				ILGenerator generator = compareMethod.GetILGenerator();

				Label returnNonNullResult = generator.DefineLabel();

				Type targetUnderlyingType = SortComparerReflectionHelper.ResolveTargetType(property.ReturnType);

				if (property.ReturnType.IsValueType)
				{
					Type targetNullableType = typeof(Nullable<>).MakeGenericType(targetUnderlyingType);

					SortComparerReflectionHelper.NullableTypeInfo nullableTypeInfo = SortComparerReflectionHelper.FindNullableTypeInfo(targetNullableType, targetUnderlyingType);

					/*
						.locals init (
							[0] valuetype [netstandard]System.Nullable`1<int32> valueX
						)
					*/
					generator.DeclareLocal(targetUnderlyingType);

					/*
						// if (!new int?(x.Id).HasValue)
					*/
					ReadValueTypeIntoFirstLocal(property, isPropertyX, targetNullableType, false, nullableTypeInfo.TargetNullableTypeCtor, generator);

					/*
						ldloca.s 0
						call instance bool valuetype [netstandard]System.Nullable`1<int32>::get_HasValue()
						brtrue.s returnNonNullResult
					*/
					generator.Emit(OpCodes.Ldloca_S, 0);
					generator.Emit(OpCodes.Call, nullableTypeInfo.TargetNullableTypeHasValue);
					generator.Emit(OpCodes.Brtrue_S, returnNonNullResult);

					/*
						// return 0;
						ldc.i4.0
						ret
					*/
					generator.Emit(OpCodes.Ldc_I4_0);
					generator.Emit(OpCodes.Ret);

					generator.MarkLabel(returnNonNullResult);

					/*
						// return 1;
						ldc.i4.1 <- return -1 if !isPropertyX
					*/
					if (isPropertyX)
						generator.Emit(OpCodes.Ldc_I4_1);
					else
						generator.Emit(OpCodes.Ldc_I4_M1);
				}
				else
				{
					/*
						// if (x.Text == null)
						ldarg.0
						callvirt instance string Code.Thing::get_Text()
						brtrue.s returnNonNullResult
					*/

					if (isPropertyX)
						generator.Emit(OpCodes.Ldarg_0);
					else
						generator.Emit(OpCodes.Ldarg_1);
					if (property.DeclaringType != s_SourceType)
						generator.Emit(OpCodes.Castclass, property.DeclaringType);
					generator.Emit(OpCodes.Callvirt, property);
					generator.Emit(OpCodes.Brtrue_S, returnNonNullResult);

					/*
						// return 0;
						ldc.i4.0
						ret
					*/
					generator.Emit(OpCodes.Ldc_I4_0);
					generator.Emit(OpCodes.Ret);

					generator.MarkLabel(returnNonNullResult);

					/*
						// return -1; <-- return 1 if isPropertyX
						ldc.i4.m1
					*/
					if (isPropertyX)
						generator.Emit(OpCodes.Ldc_I4_1);
					else
						generator.Emit(OpCodes.Ldc_I4_M1);
				}

				generator.Emit(OpCodes.Ret);

				return (Func<T, T, int>)compareMethod.CreateDelegate(typeof(Func<T, T, int>));
			}

			/// <remarks>
			/// The default comparer is used when PropertyX.ReturnType &amp; PropertyY.ReturnType are not the same or when a strongly typed CompareTo method could be found.
			/// This is basically fallback to the default Linq object behavior.
			/// Built using MethodsToDecompile.DefaultComparer.
			/// </remarks>
			private static Func<T, T, int> BuildDefaultComparer(
				MethodInfo? propertyX,
				MethodInfo? propertyY,
				string sortField)
			{
				DynamicMethod compareMethod = new DynamicMethod($"{propertyX?.ReturnType.Name}.{propertyY?.ReturnType.Name}.{sortField}$DefaultComparer", typeof(int), new[] { s_SourceType, s_SourceType }, true);
				ILGenerator generator = compareMethod.GetILGenerator();

				/*
					.locals init (
						[0] object? valueX,
						[1] object? valueY
					)
				*/
				generator.DeclareLocal(typeof(object));
				generator.DeclareLocal(typeof(object));

				if (propertyX == null)
				{
					/*
						// object? valueX = null;
						ldnull
						stloc.0
					*/
					generator.Emit(OpCodes.Ldnull);
					generator.Emit(OpCodes.Stloc_0);
				}
				else
				{
					/*
						// object? valueX = ((SwampThing)x).SwampName;
						ldarg.0
						castclass Code.SwampThing <-- cast to derived type if property doesn't exist on base
						callvirt instance string Code.SwampThing::get_SwampName()
						stloc.0
					*/
					generator.Emit(OpCodes.Ldarg_0);
					if (propertyX.DeclaringType != s_SourceType)
						generator.Emit(OpCodes.Castclass, propertyX.DeclaringType);
					generator.Emit(OpCodes.Callvirt, propertyX);
					if (propertyX.ReturnType.IsValueType)
						generator.Emit(OpCodes.Box, propertyX.ReturnType);
					generator.Emit(OpCodes.Stloc_0);
				}

				if (propertyY == null)
				{
					/*
						// object? valueY = null;
						ldnull
						stloc.1
					*/
					generator.Emit(OpCodes.Ldnull);
					generator.Emit(OpCodes.Stloc_1);
				}
				else
				{
					/*
						// object? valueY = ((SpaceThing)y).StarshipName;
						ldarg.1
						castclass Code.SpaceThing <-- cast to derived type if property doesn't exist on base
						callvirt instance string Code.SpaceThing::get_StarshipName()
						stloc.1
					*/
					generator.Emit(OpCodes.Ldarg_1);
					if (propertyY.DeclaringType != s_SourceType)
						generator.Emit(OpCodes.Castclass, propertyY.DeclaringType);
					generator.Emit(OpCodes.Callvirt, propertyY);
					if (propertyY.ReturnType.IsValueType)
						generator.Emit(OpCodes.Box, propertyY.ReturnType);
					generator.Emit(OpCodes.Stloc_1);
				}

				/*
					// return Comparer<object>.Default.Compare(text, y2);
					call class [netstandard]System.Collections.Generic.Comparer`1<!0> class [netstandard]System.Collections.Generic.Comparer`1<object>::get_Default()
					ldloc.0
					ldloc.1
					callvirt instance int32 class [netstandard]System.Collections.Generic.Comparer`1<object>::Compare(!0, !0)
					ret
				*/

				generator.Emit(OpCodes.Call, SortComparerReflectionHelper.DefaultComparerDefaultMethodInfo);
				generator.Emit(OpCodes.Ldloc_0);
				generator.Emit(OpCodes.Ldloc_1);
				generator.Emit(OpCodes.Callvirt, SortComparerReflectionHelper.DefaultComparerCompareMethodInfo);

				generator.Emit(OpCodes.Ret);

				return (Func<T, T, int>)compareMethod.CreateDelegate(typeof(Func<T, T, int>));
			}

			/// <remarks>
			/// Compare two reference types. For example, string to string.
			/// Built using MethodsToDecompile.CompareReferenceType.
			/// </remarks>
			private static Func<T, T, int> BuildReferenceTypePropertyComparer(
				MethodInfo? propertyX,
				MethodInfo? propertyY,
				string sortField,
				Type targetReferenceType,
				MethodInfo? compareToMethod)
			{
				DynamicMethod compareMethod = new DynamicMethod($"{propertyX?.ReturnType.Name}.{propertyY?.ReturnType.Name}.{sortField}$ReferenceComparer", typeof(int), new[] { s_SourceType, s_SourceType }, true);
				ILGenerator generator = compareMethod.GetILGenerator();

				Label executeComparisonLabel = generator.DefineLabel();

				/*
					.locals init (
						[0] string valueX, <- referenceType or object when no property getter
						[1] string valueY, <- referenceType or object when no property getter
						[2] int32 refCheck,
					)
				*/
				generator.DeclareLocal(targetReferenceType ?? typeof(object));
				generator.DeclareLocal(targetReferenceType ?? typeof(object));
				generator.DeclareLocal(typeof(int));

				if (propertyX == null)
				{
					/*
						// string? valueX = null;
						ldnull
						stloc.0
					*/
					generator.Emit(OpCodes.Ldnull);
					generator.Emit(OpCodes.Stloc_0);
				}
				else
				{
					/*
						// string? valueX = ((SwampThing)x).SwampName;
						ldarg.0
						castclass Code.SwampThing <-- cast to derived type if property doesn't exist on base
						callvirt instance string Code.SwampThing::get_SwampName()
						stloc.0
					*/
					generator.Emit(OpCodes.Ldarg_0);
					if (propertyX.DeclaringType != s_SourceType)
						generator.Emit(OpCodes.Castclass, propertyX.DeclaringType);
					generator.Emit(OpCodes.Callvirt, propertyX);
					generator.Emit(OpCodes.Stloc_0);
				}

				if (propertyY == null)
				{
					/*
						// string? valueY = null;
						ldnull
						stloc.1
					*/
					generator.Emit(OpCodes.Ldnull);
					generator.Emit(OpCodes.Stloc_1);
				}
				else
				{
					/*
						// string? valueY = ((SpaceThing)y).StarshipName;
						ldarg.1
						castclass Code.SpaceThing <-- cast to derived type if property doesn't exist on base
						callvirt instance string Code.SpaceThing::get_StarshipName()
						stloc.1
					*/
					generator.Emit(OpCodes.Ldarg_1);
					if (propertyY.DeclaringType != s_SourceType)
						generator.Emit(OpCodes.Castclass, propertyY.DeclaringType);
					generator.Emit(OpCodes.Callvirt, propertyY);
					generator.Emit(OpCodes.Stloc_1);
				}

				/*
					// if (!ReflectionHelper.TryCompareReferences(valueX, valueY, out int comparisonResult))
					ldloc.0
					ldloc.1
					ldloca.s 2
					call bool Code.ReflectionHelper::TryCompareReferences(object, object, int32&)
					brtrue.s executeComparisonLabel

					// return comparisonResult;
					ldloc.2
					ret
				 */
				generator.Emit(OpCodes.Ldloc_0);
				generator.Emit(OpCodes.Ldloc_1);
				generator.Emit(OpCodes.Ldloca_S, 2);
				generator.Emit(OpCodes.Call, SortComparerReflectionHelper.TryEnsureValidReferencesMethodInfo);
				generator.Emit(OpCodes.Brtrue_S, executeComparisonLabel);

				generator.Emit(OpCodes.Ldloc_2);
				generator.Emit(OpCodes.Ret);

				generator.MarkLabel(executeComparisonLabel);

				if (targetReferenceType == null)
				{
					// If we got here it means property wasn't defined on each type. We should never execute this code, TryEnsureValidReferences should handle this.
					generator.Emit(OpCodes.Ldc_I4_0);
					generator.Emit(OpCodes.Ret);
				}
				else
				{
					/*
						// return valueX.CompareTo(valueY);
						ldloc.0
						ldloc.1
						callvirt instance int32 [netstandard]System.String::CompareTo(string)
					*/
					generator.Emit(OpCodes.Ldloc_0);
					generator.Emit(OpCodes.Ldloc_1);
					generator.Emit(OpCodes.Callvirt, compareToMethod);
				}

				generator.Emit(OpCodes.Ret);

				return (Func<T, T, int>)compareMethod.CreateDelegate(typeof(Func<T, T, int>));
			}

			/// <remarks>
			/// Compare two value types. For example, int to int. Mixed nullable is allowed, for example int? to int. Comparison is always done using <see cref="Nullable{T}"/>.
			/// Enums are converted to long and compared that way.
			/// Built using MethodsToDecompile.CompareValueType, MethodsToDecompile.CompareValueBaseType, MethodsToDecompile.CompareValueTypeNoGetter, &amp; MethodsToDecompile.CompareEnumType.
			/// </remarks>
			private static Func<T, T, int> BuildValueTypePropertyComparer(
				MethodInfo? propertyX,
				MethodInfo? propertyY,
				string sortField,
				Type targetNullableType,
				Type targetUnderlyingType,
				MethodInfo? compareToMethod,
				bool isEnum)
			{
				SortComparerReflectionHelper.NullableTypeInfo nullableTypeInfo = SortComparerReflectionHelper.FindNullableTypeInfo(targetNullableType, targetUnderlyingType);

				DynamicMethod compareMethod = new DynamicMethod($"{propertyX?.ReturnType.Name}.{propertyY?.ReturnType.Name}.{sortField}$Comparer", typeof(int), new[] { s_SourceType, s_SourceType }, true);
				ILGenerator generator = compareMethod.GetILGenerator();

				Label executeComparisonLabel = generator.DefineLabel();

				/*
					.locals init (
						[0] valuetype [netstandard]System.Nullable`1<int32> valueX, <- Nullable<valueType>
						[1] valuetype [netstandard]System.Nullable`1<int32> valueY, <- Nullable<valueType>
						[2] int32 refCheck,
						[3] int32 <- valueType
					)
				*/
				generator.DeclareLocal(targetNullableType);
				generator.DeclareLocal(targetNullableType);
				generator.DeclareLocal(typeof(int));
				generator.DeclareLocal(targetUnderlyingType);

				ReadValueTypeIntoFirstLocal(propertyX, true, targetNullableType, isEnum, nullableTypeInfo.TargetNullableTypeCtor, generator);

				if (propertyY == null)
				{
					/*
						// int? valueY = null;
						ldloca.s 1
						initobj valuetype [netstandard]System.Nullable`1<int32>
					*/
					generator.Emit(OpCodes.Ldloca_S, 1);
					generator.Emit(OpCodes.Initobj, targetNullableType);
				}
				else if (!isEnum && propertyY.ReturnType == targetNullableType)
				{
					/*
						// int? valueY = ((SpaceThing)y).License;
						ldarg.1
						castclass Code.SpaceThing <-- cast to derived type if property doesn't exist on base
						callvirt instance valuetype [netstandard]System.Nullable`1<int32> Code.SpaceThing::get_License()
						stloc.1
					*/
					generator.Emit(OpCodes.Ldarg_1);
					if (propertyY.DeclaringType != s_SourceType)
						generator.Emit(OpCodes.Castclass, propertyY.DeclaringType);
					generator.Emit(OpCodes.Callvirt, propertyY);
					generator.Emit(OpCodes.Stloc_1);
				}
				else
				{
					/*
						// int? valueY = ((SwampThing)y).License;
						ldloca.s 1
						ldarg.1
						castclass Code.SwampThing
						callvirt instance int32 Code.SwampThing::get_License()
						conv.i8 <- cast enums to long
						call instance void valuetype [netstandard]System.Nullable`1<int32>::.ctor(!0)
					*/
					generator.Emit(OpCodes.Ldloca_S, 1);
					generator.Emit(OpCodes.Ldarg_1);
					if (propertyY.DeclaringType != s_SourceType)
						generator.Emit(OpCodes.Castclass, propertyY.DeclaringType);
					generator.Emit(OpCodes.Callvirt, propertyY);
					if (isEnum)
						generator.Emit(OpCodes.Conv_I8);
					generator.Emit(OpCodes.Call, nullableTypeInfo.TargetNullableTypeCtor);
				}

				/*
					// if (!ReflectionHelper.TryEnsureValidValues(valueX.HasValue, valueY.HasValue, out int comparisonResult))
					ldloca.s 0
					call instance bool valuetype [netstandard]System.Nullable`1<int32>::get_HasValue()
					ldloca.s 1
					call instance bool valuetype [netstandard]System.Nullable`1<int32>::get_HasValue()
					ldloca.s 2
					call bool Code.ReflectionHelper::TryEnsureValidValues(bool, bool, int32&)
					brtrue.s executeComparisonLabel

					// return comparisonResult;
					ldloc.2
					ret
				*/

				generator.Emit(OpCodes.Ldloca_S, 0);
				generator.Emit(OpCodes.Call, nullableTypeInfo.TargetNullableTypeHasValue);
				generator.Emit(OpCodes.Ldloca_S, 1);
				generator.Emit(OpCodes.Call, nullableTypeInfo.TargetNullableTypeHasValue);
				generator.Emit(OpCodes.Ldloca_S, 2);
				generator.Emit(OpCodes.Call, SortComparerReflectionHelper.TryEnsureValidValuesMethodInfo);
				generator.Emit(OpCodes.Brtrue_S, executeComparisonLabel);
				generator.Emit(OpCodes.Ldloc_2);
				generator.Emit(OpCodes.Ret);

				generator.MarkLabel(executeComparisonLabel);

				/*
					// return num.Value.CompareTo(license.Value);
					ldloca.s 0
					call instance !0 valuetype [netstandard]System.Nullable`1<int32>::get_Value()
					stloc.3
					ldloca.s 3
					ldloca.s 1
					call instance !0 valuetype [netstandard]System.Nullable`1<int32>::get_Value()
					call instance int32 [netstandard]System.Int32::CompareTo(int32)

					ret
				*/
				generator.Emit(OpCodes.Ldloca_S, 0);
				generator.Emit(OpCodes.Call, nullableTypeInfo.TargetNullableTypeValue);
				generator.Emit(OpCodes.Stloc_3);
				generator.Emit(OpCodes.Ldloca_S, 3);
				generator.Emit(OpCodes.Ldloca_S, 1);
				generator.Emit(OpCodes.Call, nullableTypeInfo.TargetNullableTypeValue);
				generator.Emit(OpCodes.Call, compareToMethod);

				generator.Emit(OpCodes.Ret);

				return (Func<T, T, int>)compareMethod.CreateDelegate(typeof(Func<T, T, int>));
			}

			/// <remarks>
			/// This will call the getter referenced by <paramref name="property"/> and store it into the first local variable in the scope.
			/// Normally the getter refers to PropertyX (the left) but in the case of BuildOneSidedComparer it could also be PropertyY (the right).
			/// </remarks>
			private static void ReadValueTypeIntoFirstLocal(MethodInfo? property, bool isPropertyX, Type targetNullableType, bool isEnum, ConstructorInfo targetNullableTypeCtor, ILGenerator generator)
			{
				if (property == null)
				{
					/*
						// int? valueX = null;
						ldloca.s 0
						initobj valuetype [netstandard]System.Nullable`1<int32>
					*/
					generator.Emit(OpCodes.Ldloca_S, 0);
					generator.Emit(OpCodes.Initobj, targetNullableType);
				}
				else if (!isEnum && property.ReturnType == targetNullableType)
				{
					/*
						// int? valueX = ((SpaceThing)x).License;
						ldarg.0
						castclass Code.SpaceThing <-- cast to derived type if property doesn't exist on base
						callvirt instance valuetype [netstandard]System.Nullable`1<int32> Code.SpaceThing::get_License()
						stloc.0
					*/
					if (isPropertyX)
						generator.Emit(OpCodes.Ldarg_0);
					else
						generator.Emit(OpCodes.Ldarg_1);
					if (property.DeclaringType != s_SourceType)
						generator.Emit(OpCodes.Castclass, property.DeclaringType);
					generator.Emit(OpCodes.Callvirt, property);
					generator.Emit(OpCodes.Stloc_0);
				}
				else
				{
					/*
						int? valueX = ((SwampThing)x).License;
						ldloca.s 0
						ldarg.0
						castclass Code.SwampThing <-- cast to derived type if property doesn't exist on base
						callvirt instance int32 Code.SwampThing::get_License()
						conv.i8 <- cast enums to long
						call instance void valuetype [netstandard]System.Nullable`1<int32>::.ctor(!0) <-- if type isn't nullable, make one
					*/
					generator.Emit(OpCodes.Ldloca_S, 0);
					if (isPropertyX)
						generator.Emit(OpCodes.Ldarg_0);
					else
						generator.Emit(OpCodes.Ldarg_1);
					if (property.DeclaringType != s_SourceType)
						generator.Emit(OpCodes.Castclass, property.DeclaringType);
					generator.Emit(OpCodes.Callvirt, property);
					if (isEnum)
						generator.Emit(OpCodes.Conv_I8);
					generator.Emit(OpCodes.Call, targetNullableTypeCtor);
				}
			}
		}
	}
}
