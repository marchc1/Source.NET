// #define LOGGED_EMIT_ENABLE

using Source.Common;
using Source.Common.Mathematics;

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Source.Common
{
	/// <summary>
	/// Define how to build IL methods for types.
	/// </summary>
	public interface IFieldAccessor
	{
		public string Name { get; }
		public Type DeclaringType { get; }
		public Type FieldType { get; }
		public int Length { get; }

		public T GetValue<T>(object instance);
		public bool SetValue<T>(object instance, in T value);
		public void CopyFrom<T>(object instance, Span<T> target);
		public void CopyTo<T>(object instance, Span<T> target);
	}

	file static class ILCast<From, To>
	{
		public delegate void DynamicCastFn(in From from, out To to);
		public static readonly DynamicCastFn Cast;


		static ILCast() {
			DynamicMethod method = new DynamicMethod($"ILCast<{typeof(From)}, {typeof(To)}", typeof(void), [typeof(From).MakeByRefType(), typeof(To).MakeByRefType()]);
			ILGenerator generator = method.GetILGenerator();

			generator.Emit(OpCodes.Ldarg_1);
			generator.Emit(OpCodes.Ldarg_0);

			// Try to implicitly cast.
			MethodInfo? implicitCheck = ILAssembler.TryGetImplicitConversion(typeof(From), typeof(To));
			if (implicitCheck != null) {
				generator.Emit(OpCodes.Ldobj, typeof(From));
				generator.Emit(OpCodes.Call, implicitCheck);
				generator.Emit(OpCodes.Stobj);
				goto compile;
			}

			// Generic value type conversion.
			Type from = typeof(From), to = typeof(To);
			if (from.IsPrimitive && to.IsPrimitive) {
				var convOp = ILAssembler.GetConvOpcode(from, to, out OpCode code, out _);
				if (convOp) {
					generator.Emit(OpCodes.Ldobj, from);
					generator.Emit(code);
					generator.Emit(OpCodes.Stobj, to);
					goto compile;
				}
				else
					throw new Exception();
			}


			// Generic reference type conversion.
			if (!from.IsValueType && !to.IsValueType) {
				generator.Emit(OpCodes.Ldind_Ref);
				if (to != typeof(object))
					generator.Emit(OpCodes.Castclass, to);
				generator.Emit(OpCodes.Stind_Ref);
				goto compile;
			}

			throw new NotImplementedException("You should never get to this point!");
		compile:
			generator.Emit(OpCodes.Ret);
			Cast = method.CreateDelegate<DynamicCastFn>();
		}
	}

	file static class ILAccess<T>
	{
		public delegate T GetFn(object instance);
		public delegate void SetFn(object instance, in T value);
		static readonly ConditionalWeakTable<DynamicAccessor, GetFn> getFns = [];
		static readonly ConditionalWeakTable<DynamicAccessor, SetFn> setFns = [];

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static GetFn Get(DynamicAccessor accessor) {
			if (getFns.TryGetValue(accessor, out GetFn? fn))
				return fn;

			DynamicMethod method = new DynamicMethod($"ILAccess<{typeof(T).Name}>_GetFn", typeof(T), [typeof(object)]);
			ILGenerator il = method.GetILGenerator();

			il.LoggedEmit(OpCodes.Ldarg_0);
			if (accessor.TargetType != typeof(object))
				il.LoggedEmit(OpCodes.Castclass, accessor.TargetType);
			for (int i = 0, c = accessor.Members.Count; i < c; i++) {
				MemberInfo member = accessor.Members[i];
				switch (member) {
					case FieldInfo field:
						if (i == c - 1)
							il.LoggedEmit(OpCodes.Ldfld, field);
						else if (field.FieldType.IsValueType)
							il.LoggedEmit(OpCodes.Ldflda, field);
						else
							il.LoggedEmit(OpCodes.Ldfld, field);
						break;
					case IndexInfo index:
						switch (index.Behavior) {
							case IndexInfoBehavior.GenericArrayType:
								MethodInfo getter = index.Container.GetMethod("get_Item", (BindingFlags)~0, [typeof(int)]) ?? index.Container.GetMethod("Get", (BindingFlags)~0, [typeof(int)]) ?? throw new Exception();
								il.LoggedEmit(OpCodes.Ldc_I4, index.Index);
								if (getter.IsVirtual)
									il.LoggedEmit(OpCodes.Callvirt, getter);
								else
									il.LoggedEmit(OpCodes.Call, getter);
								break;
							case IndexInfoBehavior.InlineArray:
								if (index.Index > 0) {
									// Push the index
									il.LoggedEmit(OpCodes.Ldc_I4, index.Index);
									// Push the size of one element
									il.LoggedEmit(OpCodes.Sizeof, index.ElementType);
									// Multiply: index * sizeof(element)
									il.LoggedEmit(OpCodes.Mul);
									// Add to the base address
									il.LoggedEmit(OpCodes.Add);
								}

								if (i == c - 1) {
									if (index.ElementType.IsValueType && !index.ElementType.IsPrimitive) il.LoggedEmit(OpCodes.Ldobj, index.ElementType);
									else if (index.ElementType == typeof(bool)) il.LoggedEmit(OpCodes.Ldind_I1);
									else if (index.ElementType == typeof(sbyte)) il.LoggedEmit(OpCodes.Ldind_I1);
									else if (index.ElementType == typeof(byte)) il.LoggedEmit(OpCodes.Ldind_U1);
									else if (index.ElementType == typeof(short)) il.LoggedEmit(OpCodes.Ldind_I2);
									else if (index.ElementType == typeof(ushort)) il.LoggedEmit(OpCodes.Ldind_U2);
									else if (index.ElementType == typeof(int)) il.LoggedEmit(OpCodes.Ldind_I4);
									else if (index.ElementType == typeof(uint)) il.LoggedEmit(OpCodes.Ldind_U4);
									else if (index.ElementType == typeof(ulong)) il.LoggedEmit(OpCodes.Ldind_I8);
									else if (index.ElementType == typeof(long)) il.LoggedEmit(OpCodes.Ldind_I8);
									else if (index.ElementType == typeof(float)) il.LoggedEmit(OpCodes.Ldind_R4);
									else if (index.ElementType == typeof(double)) il.LoggedEmit(OpCodes.Ldind_R8);
									else if (index.ElementType.IsClass) {
										il.LoggedEmit(OpCodes.Ldind_Ref);
									}
									else throw new NotSupportedException($"Unsupported element type: {index.ElementType}");
								}

								break;

							default: throw new NotImplementedException($"Unsupported IndexInfoBehavior value '{index.Behavior}' (implement me!)");
						}
						break;
					default: throw new NotImplementedException("Cannot support the current member info");
				}
			}

			MethodInfo? implicitCheck = ILAssembler.TryGetImplicitConversion(accessor.StoringType, typeof(T));
			if (implicitCheck != null) {
				il.LoggedEmit(OpCodes.Ldobj, accessor.StoringType);
				il.LoggedEmit(OpCodes.Call, implicitCheck);
				il.LoggedEmit(OpCodes.Stobj);
			}
			else if (ILAssembler.GetConvOpcode(accessor.StoringType, typeof(T), out OpCode convCode, out _))
				il.LoggedEmit(convCode);

			else if (typeof(T) != accessor.StoringType)
				il.LoggedEmit(OpCodes.Castclass, typeof(T));

			il.LoggedEmit(OpCodes.Ret);

			getFns.Add(accessor, fn = method.CreateDelegate<GetFn>());
			return fn;
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static SetFn? Set(DynamicAccessor accessor) {
			if (setFns.TryGetValue(accessor, out SetFn? fn))
				return fn;

			DynamicMethod method = new DynamicMethod($"ILAccess<{typeof(T).Name}>_SetFn", typeof(void), [typeof(object), typeof(T).MakeByRefType()]);
			ILGenerator il = method.GetILGenerator();

			il.LoggedEmit(OpCodes.Ldarg_0);
			il.LoggedEmit(OpCodes.Castclass, accessor.Members[0]!.DeclaringType!);

			for (int i = 0, c = accessor.Members.Count - 1; i < c; i++) {
				MemberInfo member = accessor.Members[i];
				switch (member) {
					case FieldInfo field:
						il.LoggedEmit(OpCodes.Ldflda, field);

						break;
					default: throw new NotImplementedException("Cannot support the current member info");
				}
			}

			switch (accessor.Members[^1]) {
				case FieldInfo field:
					LoadValue(accessor, il);
					PerformAutocast(accessor, il);
					il.LoggedEmit(OpCodes.Stfld, field);
					break;
				case IndexInfo index:
					switch (index.Behavior) {
						case IndexInfoBehavior.InlineArray:
							if (index.Index > 0) {
								// Push the index
								il.LoggedEmit(OpCodes.Ldc_I4, index.Index);
								// Push the size of one element
								il.LoggedEmit(OpCodes.Sizeof, index.ElementType);
								// Multiply: index * sizeof(element)
								il.LoggedEmit(OpCodes.Mul);
								// Add to the base address
								il.LoggedEmit(OpCodes.Add);
							}

							LoadValue(accessor, il);
							PerformAutocast(accessor, il);

							if (index.ElementType.IsValueType && !index.ElementType.IsPrimitive)
								il.LoggedEmit(OpCodes.Stobj, index.ElementType);
							else if (index.ElementType == typeof(bool)) il.LoggedEmit(OpCodes.Stind_I1);
							else if (index.ElementType == typeof(sbyte)) il.LoggedEmit(OpCodes.Stind_I1);
							else if (index.ElementType == typeof(byte)) il.LoggedEmit(OpCodes.Stind_I1);
							else if (index.ElementType == typeof(short)) il.LoggedEmit(OpCodes.Stind_I2);
							else if (index.ElementType == typeof(ushort)) il.LoggedEmit(OpCodes.Stind_I2);
							else if (index.ElementType == typeof(int)) il.LoggedEmit(OpCodes.Stind_I4);
							else if (index.ElementType == typeof(uint)) il.LoggedEmit(OpCodes.Stind_I4);
							else if (index.ElementType == typeof(ulong)) il.LoggedEmit(OpCodes.Stind_I8);
							else if (index.ElementType == typeof(long)) il.LoggedEmit(OpCodes.Stind_I8);
							else if (index.ElementType == typeof(float)) il.LoggedEmit(OpCodes.Stind_R4);
							else if (index.ElementType == typeof(double)) il.LoggedEmit(OpCodes.Stind_R8);
							else if (index.ElementType.IsClass) il.LoggedEmit(OpCodes.Stind_Ref);
							else throw new NotSupportedException($"Unsupported element type: {index.ElementType}");

							break;
					}
					break;
				default: throw new NotImplementedException("Cannot support the current member info");
			}
			il.LoggedEmit(OpCodes.Ret);

			setFns.Add(accessor, fn = method.CreateDelegate<SetFn>());
			return fn;
		}

		private static void LoadValue(DynamicAccessor accessor, ILGenerator il) {
			il.LoggedEmit(OpCodes.Ldarg_1);
		}

		static void PerformAutocast(DynamicAccessor accessor, ILGenerator il) {
			MethodInfo? implicitCheck = ILAssembler.TryGetImplicitConversion(typeof(T), accessor.StoringType);
			if (implicitCheck != null) {
				il.LoggedEmit(OpCodes.Ldobj, typeof(T));
				il.LoggedEmit(OpCodes.Call, implicitCheck);
			}
			else if (
				ILAssembler.GetLoadOpcode(typeof(T), out OpCode loadCode)
				&& ILAssembler.GetConvOpcode(typeof(T), accessor.StoringType, out OpCode convCode, out bool unsignedInput)
				) {
				if (unsignedInput && (convCode == OpCodes.Conv_R4 || convCode == OpCodes.Conv_R8))
					il.LoggedEmit(OpCodes.Conv_R_Un);
				else
					il.LoggedEmit(loadCode);
				il.LoggedEmit(convCode);
			}
			else {
				if (typeof(T).IsValueType)
					il.LoggedEmit(OpCodes.Ldobj, typeof(T));
				else
					il.LoggedEmit(OpCodes.Ldind_Ref);
			}
		}
	}
	public class DynamicArrayInfo(Type containedType, Func<int> length, bool isList = false)
	{
		public Type ContainedType => containedType;
		public int MaxLength => length();
		public bool IsList => isList;
	}

	public static class DynamicArrayHelp
	{
		public static readonly FrozenDictionary<Type, DynamicArrayInfo> AcceptableTypes = new Dictionary<Type, DynamicArrayInfo>() {
			{ typeof(Vector3), new(typeof(float), () => 3) },
			{ typeof(QAngle), new(typeof(float), () => 3) },
		}.ToFrozenDictionary();
	}

	public class DynamicArrayAccessor : DynamicAccessor
	{
		public readonly DynamicArrayIndexAccessor?[] ArrayIndexers;
		public readonly DynamicArrayInfo Info;

		public override int Length => Info.MaxLength;
		public override void CopyFrom<T>(object instanceFrom, Span<T> target) {
			for (int i = 0; i < Math.Min(Length, target.Length); i++)
				SetValue(instanceFrom, in target[i]);
		}
		public override void CopyTo<T>(object instanceFrom, Span<T> target) {
			for (int i = 0; i < Math.Min(Length, target.Length); i++)
				target[i] = AtIndex(i)!.GetValue<T>(instanceFrom);
		}
		public DynamicArrayAccessor(Type targetType, ReadOnlySpan<char> expression, int isList = -1) : base(targetType, expression) {
			var arrayAttr = StoringType.GetCustomAttribute<InlineArrayAttribute>();
			if (arrayAttr != null) {
				Info = new(StoringType.GetGenericArguments()[0], () => arrayAttr.Length);
			}
			else if (isList > 0) {
				Info = new(StoringType.GetGenericArguments()[0], () => isList, true);
			}
			else {
				if (!DynamicArrayHelp.AcceptableTypes.TryGetValue(StoringType, out Info!))
					throw new Exception("Uh oh, we need a type def override");
			}

			ArrayIndexers = new DynamicArrayIndexAccessor?[Info.MaxLength];
		}

		public override DynamicAccessor? AtIndex(int index) {
			if (index < 0)
				return null;

			if (index >= Info.MaxLength)
				return null;

			return ArrayIndexers[index] ??= new(this, index);
		}
	}

	public class DynamicArrayIndexAccessor : DynamicAccessor
	{
		public readonly DynamicArrayAccessor BaseArrayAccessor;
		public readonly bool IsAVectorElement;

		public override int Index { get; }

		public DynamicArrayIndexAccessor(DynamicArrayAccessor baseArray, int index, bool isVectorElem = false) : base(baseArray.TargetType, $"{baseArray.Name}[{Math.Abs(index)}]") {
			BaseArrayAccessor = baseArray;
			Index = Math.Abs(index);
			IsAVectorElement = isVectorElem;
		}
	}

	public enum IndexInfoBehavior
	{
		DirectElement,
		InlineArray,
		GenericArrayType
	}

	public sealed class IndexInfo : MemberInfo
	{
		Type container;
		MemberInfo containerMember;
		int index;
		public Type Container => container;
		public MemberInfo ContainerMember => containerMember;
		public IndexInfo(MemberInfo containedField, int index) {
			containerMember = containedField;
			container = containedField switch {
				FieldInfo i => i.FieldType,
				PropertyInfo i => i.PropertyType,
				IndexInfo i => i.DeclaringType,
				_ => throw new NotImplementedException()
			};
			this.index = index;
		}

		public override string ToString() {
			return $"{container}[{index}]";
		}
		Type? insideType;
		IndexInfoBehavior behavior;
		[MemberNotNull(nameof(insideType))]
		void Process() {
			insideType = container.GetElementType();
			if (insideType != null) {
				behavior = IndexInfoBehavior.DirectElement;
				return;
			}

			insideType = container.GetCustomAttribute<InlineArrayAttribute>() == null ? null : container.GetFields()[0].FieldType;
			if (insideType != null) {
				behavior = IndexInfoBehavior.InlineArray;
				return;
			}

			insideType = container.GetGenericArguments().FirstOrDefault();
			if (insideType != null) {
				behavior = IndexInfoBehavior.GenericArrayType;
				return;
			}

			if (DynamicArrayHelp.AcceptableTypes.TryGetValue(container, out var info)) {
				behavior = IndexInfoBehavior.InlineArray;
				insideType = info!.ContainedType;
				return;
			}

			throw new Exception("Uh oh!");
		}
		public int Index => index;
		public Type ElementType { get { Process(); return insideType; } }
		public IndexInfoBehavior Behavior { get { Process(); return behavior; } }
		public override Type DeclaringType => container;
		public override MemberTypes MemberType => MemberTypes.Custom;
		public override string Name => throw new NotImplementedException();
		public override Type ReflectedType => container;
		public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();
		public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new NotImplementedException();
		public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();
	}

	public class DynamicAccessor : IFieldAccessor
	{
		public readonly List<MemberInfo> Members = [];

		/// <summary>
		/// How many items
		/// </summary>
		public virtual int Length => 1;
		/// <summary>
		/// Get a dynamic accessor at an index (on default accessors this is a no-op)
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public virtual DynamicAccessor? AtIndex(int index) => this;

		internal string FirstName { get; set; } // testing function

		Type? buildingTargetType;
		/// <summary>
		/// The object target.
		/// </summary>
		public readonly Type TargetType;
		/// <summary>
		/// The final field/property target.
		/// </summary>
		public readonly Type StoringType;

		public virtual int Index => -1;

		public string Name { get; }
		Type IFieldAccessor.DeclaringType => TargetType;
		Type IFieldAccessor.FieldType => StoringType;

		void HandleIndex(ReadOnlySpan<char> index) {
			Members.Add(new IndexInfo(Members.Last(), int.Parse(index)));
		}
		void HandleFieldProp(ReadOnlySpan<char> index) {
			if (index.IsEmpty)
				return;

			MemberInfo[] memberInfos = buildingTargetType!.GetMember(new string(index), BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
			foreach (var member in memberInfos) {
				switch (member) {
					case FieldInfo field:
						Members.Add(field);
						buildingTargetType = field.FieldType;
						return;
					case PropertyInfo prop:
						Members.Add(prop);
						buildingTargetType = prop.PropertyType;
						return;
				}
			}

			throw new KeyNotFoundException($"Cannot find an appropriate member named '{index}' in the current target type '{buildingTargetType.Name}'. Ensure naming is correct.");
		}
		public DynamicAccessor(Type targetType, ReadOnlySpan<char> expression, ReadOnlySpan<char> name) : this(targetType, expression) {
			Name = new(name);
		}
		public DynamicAccessor(Type targetType, ReadOnlySpan<char> expression) {
			Name = new(expression);

			TargetType = targetType;
			buildingTargetType = targetType;
			int lastPeriod = 0;
			for (int i = 0; i < expression.Length + 1; i++) {
				// Final chance to handle a string index
				if (i == expression.Length) {
					ReadOnlySpan<char> index = expression[lastPeriod..i];
					HandleFieldProp(index);
					break;
				}
				char c = expression[i];
				if (c == '.') {
					ReadOnlySpan<char> index = expression[lastPeriod..i];
					HandleFieldProp(index);
					lastPeriod = i + 1; // advance past the period for string reading
				}

				if (c == '[') {
					ReadOnlySpan<char> index = expression[lastPeriod..i];
					HandleFieldProp(index);
					// Start reading until ]
					string work = "";
					while (expression[++i] != ']')
						work += expression[i];
					HandleIndex(work);
					lastPeriod = i + 1; // advance past the period for string reading
				}
			}

			StoringType = Members.Last() switch {
				FieldInfo f => f.FieldType,
				PropertyInfo p => p.PropertyType,
				IndexInfo i => i.ElementType,
				_ => throw new Exception()
			};
			FirstName = Members[0].Name;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public T GetValue<T>(object instance) => ILAccess<T>.Get(this)(instance);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool SetValue<T>(object instance, in T value) {
			var fn = ILAccess<T>.Set(this);
			if (fn == null)
				return false;
			fn(instance, in value);
			return true;
		}

		public virtual void CopyFrom<T>(object instanceFrom, Span<T> target) {
			SetValue<T>(instanceFrom, target.Length == 0 ? default : target[0]);
		}
		public virtual void CopyTo<T>(object instanceFrom, Span<T> target) {
			if (target.Length <= 0)
				return;
			target[0] = GetValue<T>(instanceFrom);
		}
	}

	public static class ILAssembler
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LoggedEmit(this ILGenerator il, OpCode code) {
#if LOGGED_EMIT_ENABLE
			Console.WriteLine(code);
#endif
			il.Emit(code);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LoggedEmit(this ILGenerator il, OpCode code, FieldInfo field) {
#if LOGGED_EMIT_ENABLE
			Console.WriteLine($"{code} {field}");
#endif
			il.Emit(code, field);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LoggedEmit(this ILGenerator il, OpCode code, MethodInfo method) {
#if LOGGED_EMIT_ENABLE
			Console.WriteLine($"{code} {method}");
#endif
			il.Emit(code, method);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LoggedEmit(this ILGenerator il, OpCode code, Type type) {
#if LOGGED_EMIT_ENABLE
			Console.WriteLine($"{code} {type}");
#endif
			il.Emit(code, type);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void LoggedEmit(this ILGenerator il, OpCode code, int v) {
#if LOGGED_EMIT_ENABLE
			Console.WriteLine($"{code} {v}");
#endif
			il.Emit(code, v);
		}
		public static MethodInfo? TryGetImplicitConversion(Type baseType, Type targetType) {
			return baseType.GetMethods(BindingFlags.Public | BindingFlags.Static)
				.Where(mi => mi.Name == "op_Implicit" && mi.ReturnType == targetType)
				.FirstOrDefault(mi => {
					ParameterInfo? pi = mi.GetParameters().FirstOrDefault();
					return pi != null && pi.ParameterType == baseType;
				})
				??
				targetType.GetMethods(BindingFlags.Public | BindingFlags.Static)
				.Where(mi => mi.Name == "op_Implicit" && mi.ReturnType == targetType)
				.FirstOrDefault(mi => {
					ParameterInfo? pi = mi.GetParameters().FirstOrDefault();
					return pi != null && pi.ParameterType == baseType;
				})
				;
		}

		public static bool GetLoadOpcode(Type from, out OpCode opcode) {
			return (opcode = Type.GetTypeCode(from) switch {
				TypeCode.Byte => OpCodes.Ldind_U1,
				TypeCode.UInt16 => OpCodes.Ldind_U2,
				TypeCode.UInt32 => OpCodes.Ldind_U4,
				TypeCode.UInt64 => OpCodes.Ldind_I8,
				TypeCode.SByte => OpCodes.Ldind_I1,
				TypeCode.Int16 => OpCodes.Ldind_I2,
				TypeCode.Int32 => OpCodes.Ldind_I4,
				TypeCode.Int64 => OpCodes.Ldind_I8,
				TypeCode.Single => OpCodes.Ldind_R4,
				TypeCode.Double => OpCodes.Ldind_R8,
				_ => OpCodes.Nop
			}) != OpCodes.Nop;
		}
		public static bool GetConvOpcode(Type from, Type to, out OpCode opcode, out bool isUnsignedInput) {
			opcode = OpCodes.Nop;
			var fromcode = Type.GetTypeCode(from);
			isUnsignedInput = fromcode switch {
				TypeCode.Boolean or TypeCode.Byte or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64 => true,
				_ => false
			};
			if (!from.IsPrimitive || !to.IsPrimitive)
				return false;

			if (from == to)
				return false;

			var code = Type.GetTypeCode(to);

			switch (fromcode) {
				default:
					opcode = code switch {
						TypeCode.SByte => OpCodes.Conv_I1,
						TypeCode.Byte => OpCodes.Conv_U1,
						TypeCode.Int16 => OpCodes.Conv_I2,
						TypeCode.UInt16 => OpCodes.Conv_U2,
						TypeCode.Int32 => OpCodes.Conv_I4,
						TypeCode.UInt32 => OpCodes.Conv_U4,
						TypeCode.Int64 => OpCodes.Conv_I8,
						TypeCode.UInt64 => OpCodes.Conv_U8,
						TypeCode.Single => OpCodes.Conv_R4,
						TypeCode.Double => OpCodes.Conv_R8,
						_ => OpCodes.Nop
					};
					break;
			}
			return opcode != OpCodes.Nop;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void DynamicCast<From, To>(in From from, out To to) => ILCast<From, To>.Cast(in from, out to);
	}
}

namespace Source
{
	public static class FIELD<T>
	{
		public static DynamicAccessor OF(ReadOnlySpan<char> expression) => new(typeof(T), expression);
		public static DynamicAccessor OF_NAMED(ReadOnlySpan<char> expression, ReadOnlySpan<char> name) => new(typeof(T), expression, name);
		public static DynamicArrayAccessor OF_ARRAY(ReadOnlySpan<char> expression) => new(typeof(T), expression);
		public static DynamicArrayIndexAccessor OF_ARRAYINDEX(ReadOnlySpan<char> expression, int index = 0) => new(OF_ARRAY(expression), index);
		public static DynamicArrayIndexAccessor OF_VECTORELEM(ReadOnlySpan<char> expression, int index) => new(OF_ARRAY(expression), index, isVectorElem: true);
		public static DynamicArrayAccessor OF_LIST(ReadOnlySpan<char> expression, int max) => new(typeof(T), expression, isList: max);
	}
}
