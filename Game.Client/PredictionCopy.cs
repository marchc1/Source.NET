using Game.Shared;

using Source;
using Source.Common;

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Game.Client;

public static class PredictionCopyImpl
{
	extension(ref PredictionCopy self)
	{
		#region ehandle methods
		public DiffType CompareEHandle(ReadOnlySpan<BaseHandle> output, ReadOnlySpan<BaseHandle> input, int count) => self.BASIC_COMPARE(output, input, count, static (in ov, in iv) => ov.Index == iv.Index ? DiffType.Identical : DiffType.Differs);
		public void CopyEHandle(DiffType dt, Span<BaseHandle> output, ReadOnlySpan<BaseHandle> input, int count) => self.BASIC_COPY(dt, output, input, count);
		public void DescribeEHandle(DiffType dt, Span<BaseHandle> outdata, ReadOnlySpan<BaseHandle> indata, int size) {
			if (!self.ErrorCheck) return;
			EHANDLE invalue = indata[0], outvalue = outdata[0];

			if (dt == DiffType.Differs)
				self.ReportFieldsDiffer($"EHandles differ (net) 0x{invalue.Index:X} (pred) 0x{outvalue.Index:X}\n");

#if CLIENT_DLL
			C_BaseEntity? ent = outvalue.Get();
			if (ent != null) {
				ReadOnlySpan<char> classname = ent.GetClassname();
				if (classname.IsStringEmpty)
					classname = ent.GetType().Name;

				self.DescribeFields(dt, $"EHandle (0x{outvalue.Index:X}->{classname})");
			}
			else
				self.DescribeFields(dt, "EHandle (NULL)");

#else
		DescribeFields(dt, $"EHandle (0x{outvalue.Index:X})");
#endif
		}

		public void WatchEHandle(DiffType dt, Span<BaseHandle> outdata, ReadOnlySpan<BaseHandle> indata, int size) {
			if (self.WatchField != self.CurrentField)
				return;
#if CLIENT_DLL
			C_BaseEntity? ent = ((EHANDLE)outdata[0]).Get();
			if (ent != null) {
				ReadOnlySpan<char> classname = ent.GetClassname();
				if (classname.IsStringEmpty)
					classname = ent.GetType().Name;

				self.WatchMsg($"EHandle (0x{outdata[0].Index:X}->{classname})");
			}
			else
				self.WatchMsg("EHandle (NULL)");
#else
		WatchMsg($"EHandle (0x{outdata[0].Index:X})");
#endif
		}
		#endregion
	}

	public static readonly List<object> ilConstants = new();
	public static int IL_AddConstant(object obj) {
		ilConstants.Add(obj);
		return ilConstants.Count - 1;
	}

	static readonly FieldInfo constantsField = typeof(PredictionCopyImpl).GetField(nameof(ilConstants))!;
	static readonly MethodInfo listIndexer = typeof(List<object>).GetProperty("Item")!.GetGetMethod()!;

	// PredictionCopy fields
	static readonly FieldInfo f_CurrentMap = typeof(PredictionCopy).GetField("CurrentMap")!;
	static readonly FieldInfo f_CurrentField = typeof(PredictionCopy).GetField("CurrentField")!;
	static readonly FieldInfo f_CurrentClassName = typeof(PredictionCopy).GetField("CurrentClassName")!;
	static readonly FieldInfo f_ShouldReport = typeof(PredictionCopy).GetField("ShouldReport")!;
	static readonly FieldInfo f_ShouldDescribe = typeof(PredictionCopy).GetField("ShouldDescribe")!;
	static readonly FieldInfo f_ReportErrors = typeof(PredictionCopy).GetField("ReportErrors")!;
	static readonly FieldInfo f_ErrorCheck = typeof(PredictionCopy).GetField("ErrorCheck")!;
	static readonly FieldInfo f_WatchField = typeof(PredictionCopy).GetField("WatchField")!;
	static readonly FieldInfo f_Type = typeof(PredictionCopy).GetField("Type")!;
	static readonly FieldInfo f_Src_Object = typeof(PredictionCopy).GetField("Src_Object")!;
	static readonly FieldInfo f_Dest_Object = typeof(PredictionCopy).GetField("Dest_Object")!;
	static readonly FieldInfo f_Src_DataFrame = typeof(PredictionCopy).GetField("Src_DataFrame")!;
	static readonly FieldInfo f_Dest_DataFrame = typeof(PredictionCopy).GetField("Dest_DataFrame")!;

	// MemoryMarshal.CreateSpan / CreateReadOnlySpan
	static MethodInfo GetCreateSpan(Type elementType) =>
		typeof(MemoryMarshal).GetMethod("CreateSpan")!.MakeGenericMethod(elementType);
	static MethodInfo GetCreateReadOnlySpan(Type elementType) =>
		typeof(MemoryMarshal).GetMethod("CreateReadOnlySpan")!.MakeGenericMethod(elementType);

	// Span<byte>.Slice(int, int)
	static readonly MethodInfo spanByteSlice = typeof(Span<byte>).GetMethod("Slice", [typeof(int), typeof(int)])!;

	// MemoryMarshal.Cast<byte, T> for Span and ReadOnlySpan
	static MethodInfo GetCastSpan(Type elementType) =>
		typeof(MemoryMarshal).GetMethods()
			.First(m => m.Name == "Cast"
				&& m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(Span<>))
			.MakeGenericMethod(typeof(byte), elementType);

	static MethodInfo GetCastReadOnlySpan(Type elementType) =>
		typeof(MemoryMarshal).GetMethods()
			.First(m => m.Name == "Cast"
				&& m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(ReadOnlySpan<>))
			.MakeGenericMethod(typeof(byte), elementType);

	static Type? GetElementType(FieldType ft) => ft switch {
		FieldType.Float => typeof(float),
		FieldType.Double => typeof(double),
		FieldType.Integer => typeof(int),
		FieldType.Short => typeof(short),
		FieldType.Character => typeof(byte),
		FieldType.StringCharacter => typeof(char),
		FieldType.Boolean => typeof(bool),
		FieldType.Vector => typeof(Vector3),
		FieldType.Quaternion => typeof(Quaternion),
		FieldType.Color32 => typeof(Color),
		FieldType.EHandle => typeof(BaseHandle),
		_ => null
	};

	static (string compare, string copy, string describe, string watch) GetMethodNames(FieldType ft) => ft switch {
		FieldType.Float => ("CompareFloat", "CopyFloat", "DescribeFloat", "WatchFloat"),
		FieldType.Double => ("CompareDouble", "CopyDouble", "DescribeDouble", "WatchDouble"),
		FieldType.Integer => ("CompareInt", "CopyInt", "DescribeInt", "WatchInt"),
		FieldType.Short => ("CompareShort", "CopyShort", "DescribeShort", "WatchShort"),
		FieldType.Character => ("CompareByte", "CopyByte", "DescribeByte", "WatchByte"),
		FieldType.StringCharacter => ("CompareChar", "CopyChar", "DescribeChar", "WatchChar"),
		FieldType.Boolean => ("CompareBool", "CopyBool", "DescribeBool", "WatchBool"),
		FieldType.Vector => ("CompareVector", "CopyVector", "DescribeVector", "WatchVector"),
		FieldType.Quaternion => ("CompareQuaternion", "CopyQuaternion", "DescribeQuaternion", "WatchQuaternion"),
		FieldType.Color32 => ("CompareColor", "CopyColor", "DescribeData", "WatchData"),
		FieldType.EHandle => ("CompareEHandle", "CopyEHandle", "DescribeEHandle", "WatchEHandle"),
		_ => throw new NotSupportedException($"No method names for {ft}")
	};

	static void EmitLoadConstant(ILGenerator il, int index, Type targetType) {
		il.LoggedEmit(OpCodes.Ldsfld, constantsField);
		il.LoggedEmit(OpCodes.Ldc_I4, index);
		il.LoggedEmit(OpCodes.Callvirt, listIndexer);
		il.LoggedEmit(OpCodes.Castclass, targetType);
	}

	static void EmitSetSelfFieldFromConstant(ILGenerator il, int index, Type targetType, FieldInfo selfField) {
		il.LoggedEmit(OpCodes.Ldarg_0); 
		EmitLoadConstant(il, index, targetType);
		il.LoggedEmit(OpCodes.Stfld, selfField);
	}

	static void EmitPreamble(ILGenerator il, DataMap pRootMap) {
		int mapIdx = IL_AddConstant(pRootMap);

		EmitSetSelfFieldFromConstant(il, mapIdx, typeof(DataMap), f_CurrentMap);

		il.LoggedEmit(OpCodes.Ldarg_0);
		EmitLoadConstant(il, mapIdx, typeof(DataMap));
		il.LoggedEmit(OpCodes.Ldfld, typeof(DataMap).GetField("DataClassName")!);
		il.LoggedEmit(OpCodes.Call, typeof(string).GetMethod("op_Implicit", [typeof(string)])!);
		il.LoggedEmit(OpCodes.Stfld, f_CurrentClassName);
	}

	static void EmitNetworkSkipChecks(ILGenerator il, TypeDescription field, Label continueLabel) {
		if (field.FieldType == FieldType.Embedded)
			return;

		if ((field.Flags & FieldTypeDescFlags.InSendTable) != 0) {
			Label pass = il.DefineLabel();
			il.LoggedEmit(OpCodes.Ldarg_0);
			il.LoggedEmit(OpCodes.Ldfld, f_Type);
			il.LoggedEmit(OpCodes.Ldc_I4, (int)PredictionCopyType.NonNetworkedOnly);
			il.LoggedEmit(OpCodes.Bne_Un, pass);
			il.LoggedEmit(OpCodes.Br, continueLabel);
			il.MarkLabel(pass);
		}
		else {
			Label pass = il.DefineLabel();
			il.LoggedEmit(OpCodes.Ldarg_0);
			il.LoggedEmit(OpCodes.Ldfld, f_Type);
			il.LoggedEmit(OpCodes.Ldc_I4, (int)PredictionCopyType.NetworkedOnly);
			il.LoggedEmit(OpCodes.Bne_Un, pass);
			il.LoggedEmit(OpCodes.Br, continueLabel);
			il.MarkLabel(pass);
		}
	}

	static void EmitSetReportState(ILGenerator il) {
		il.LoggedEmit(OpCodes.Ldarg_0);
		il.LoggedEmit(OpCodes.Ldarg_0);
		il.LoggedEmit(OpCodes.Ldfld, f_ReportErrors);
		il.LoggedEmit(OpCodes.Stfld, f_ShouldReport);

		il.LoggedEmit(OpCodes.Ldarg_0);
		il.LoggedEmit(OpCodes.Ldc_I4_1);
		il.LoggedEmit(OpCodes.Stfld, f_ShouldDescribe);
	}

	static Type ResolveActualFieldType(FieldInfo fieldInfo, Type elementType) {
		Type actualType = fieldInfo.FieldType;

		if (actualType.IsConstructedGenericType && actualType.GetGenericTypeDefinition() == typeof(Handle<>))
			actualType = typeof(BaseHandle);

		if (actualType.IsEnum)
			actualType = actualType.GetEnumUnderlyingType();

		return actualType;
	}

	static bool AreLayoutCompatible(Type a, Type b) {
		if (a == b) return true;
		return ManagedSizeOf(a) == ManagedSizeOf(b);
	}

	static bool IsNetworkArray(Type fieldType, [NotNullWhen(true)] out Type? arrayElementType) {
		if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(NetworkArray<>)) {
			arrayElementType = fieldType.GetGenericArguments()[0];
			return true;
		}
		arrayElementType = null;
		return false;
	}

	static void EmitConvertedReadOnlySpan(ILGenerator il, LocalBuilder typedObj, FieldInfo fieldInfo, Type actualType, Type elementType, int fieldSize) {
		if (fieldSize == 1) {
			LocalBuilder buf = il.DeclareLocal(elementType);

			il.LoggedEmit(OpCodes.Ldloc, typedObj);
			il.LoggedEmit(OpCodes.Ldfld, fieldInfo);

			EmitConversion(il, actualType, elementType);

			il.LoggedEmit(OpCodes.Stloc, buf);

			il.LoggedEmit(OpCodes.Ldloca, buf);
			il.LoggedEmit(OpCodes.Ldc_I4_1);
			il.LoggedEmit(OpCodes.Call, GetCreateReadOnlySpan(elementType));
		}
		else {
			Warning($"Cannot convert array field {fieldInfo.Name} ({actualType.Name}x{fieldSize} -> {elementType.Name}), using raw bytes\n");
			il.LoggedEmit(OpCodes.Ldloc, typedObj);
			il.LoggedEmit(OpCodes.Ldflda, fieldInfo);
			int byteCount = ManagedSizeOf(elementType) * fieldSize;
			il.LoggedEmit(OpCodes.Ldc_I4, byteCount);
			il.LoggedEmit(OpCodes.Call, GetCreateReadOnlySpan(typeof(byte)));
			il.LoggedEmit(OpCodes.Call, castROSpanOpen.MakeGenericMethod(typeof(byte), elementType));
		}
	}

	static void EmitConversion(ILGenerator il, Type from, Type to) {
		if (from == to) return;

		MethodInfo? implicitConv = ILAssembler.TryGetImplicitConversion(from, to);
		if (implicitConv != null) {
			il.LoggedEmit(OpCodes.Call, implicitConv);
			return;
		}

		Type actualFrom = from.IsEnum ? from.GetEnumUnderlyingType() : from;
		Type actualTo = to.IsEnum ? to.GetEnumUnderlyingType() : to;

		if (actualFrom.IsPrimitive && actualTo.IsPrimitive) {
			if (ILAssembler.GetConvOpcode(actualFrom, actualTo, out OpCode convCode, out bool isUnsigned)) {
				if (isUnsigned && (convCode == OpCodes.Conv_R4 || convCode == OpCodes.Conv_R8))
					il.LoggedEmit(OpCodes.Conv_R_Un);
				il.LoggedEmit(convCode);
				return;
			}
		}

		if (!from.IsValueType && !to.IsValueType) {
			il.LoggedEmit(OpCodes.Castclass, to);
			return;
		}
	}

	static void EmitObjectFieldAsReadOnlySpan(ILGenerator il, LocalBuilder typedObj, FieldInfo fieldInfo, Type elementType, int fieldSize) {
		Type actualType = ResolveActualFieldType(fieldInfo, elementType);

		if (IsNetworkArray(fieldInfo.FieldType, out Type? netArrayElemType)) {
			FieldInfo valueField = fieldInfo.FieldType.GetField("Value")!;
			il.LoggedEmit(OpCodes.Ldloc, typedObj);
			il.LoggedEmit(OpCodes.Ldflda, fieldInfo);
			il.LoggedEmit(OpCodes.Ldfld, valueField);
			il.LoggedEmit(OpCodes.Newobj, typeof(ReadOnlySpan<>).MakeGenericType(netArrayElemType).GetConstructor([netArrayElemType.MakeArrayType()])!);
			if (netArrayElemType != elementType) {
				il.LoggedEmit(OpCodes.Call, castROSpanOpen.MakeGenericMethod(netArrayElemType, elementType));
			}
			return;
		}

		if (actualType == elementType) {
			il.LoggedEmit(OpCodes.Ldloc, typedObj);
			il.LoggedEmit(OpCodes.Ldflda, fieldInfo);
			il.LoggedEmit(OpCodes.Ldc_I4, fieldSize);
			il.LoggedEmit(OpCodes.Call, GetCreateReadOnlySpan(elementType));
		}
		else if (AreLayoutCompatible(actualType, elementType)) {
			il.LoggedEmit(OpCodes.Ldloc, typedObj);
			il.LoggedEmit(OpCodes.Ldflda, fieldInfo);
			il.LoggedEmit(OpCodes.Ldc_I4, fieldSize);
			il.LoggedEmit(OpCodes.Call, GetCreateReadOnlySpan(actualType));
			il.LoggedEmit(OpCodes.Call, castROSpanOpen.MakeGenericMethod(actualType, elementType));
		}
		else {
			EmitConvertedReadOnlySpan(il, typedObj, fieldInfo, actualType, elementType, fieldSize);
		}
	}

	static void EmitObjectFieldAsSpan(ILGenerator il, LocalBuilder typedObj, FieldInfo fieldInfo, Type elementType, int fieldSize) {
		Type actualType = ResolveActualFieldType(fieldInfo, elementType);
		_pendingWriteBack = null;

		if (IsNetworkArray(fieldInfo.FieldType, out Type? netArrayElemType)) {
			FieldInfo valueField = fieldInfo.FieldType.GetField("Value")!;
			il.LoggedEmit(OpCodes.Ldloc, typedObj);
			il.LoggedEmit(OpCodes.Ldflda, fieldInfo);
			il.LoggedEmit(OpCodes.Ldfld, valueField);
			il.LoggedEmit(OpCodes.Newobj, typeof(Span<>).MakeGenericType(netArrayElemType).GetConstructor([netArrayElemType.MakeArrayType()])!);
			if (netArrayElemType != elementType) {
				il.LoggedEmit(OpCodes.Call, castSpanOpen.MakeGenericMethod(netArrayElemType, elementType));
			}
			return;
		}

		if (actualType == elementType) {
			il.LoggedEmit(OpCodes.Ldloc, typedObj);
			il.LoggedEmit(OpCodes.Ldflda, fieldInfo);
			il.LoggedEmit(OpCodes.Ldc_I4, fieldSize);
			il.LoggedEmit(OpCodes.Call, GetCreateSpan(elementType));
		}
		else if (AreLayoutCompatible(actualType, elementType)) {
			il.LoggedEmit(OpCodes.Ldloc, typedObj);
			il.LoggedEmit(OpCodes.Ldflda, fieldInfo);
			il.LoggedEmit(OpCodes.Ldc_I4, fieldSize);
			il.LoggedEmit(OpCodes.Call, GetCreateSpan(actualType));
			il.LoggedEmit(OpCodes.Call, castSpanOpen.MakeGenericMethod(actualType, elementType));
		}
		else if (fieldSize == 1) {
			LocalBuilder buf = il.DeclareLocal(elementType);

			il.LoggedEmit(OpCodes.Ldloc, typedObj);
			il.LoggedEmit(OpCodes.Ldfld, fieldInfo);
			EmitConversion(il, actualType, elementType);
			il.LoggedEmit(OpCodes.Stloc, buf);

			il.LoggedEmit(OpCodes.Ldloca, buf);
			il.LoggedEmit(OpCodes.Ldc_I4_1);
			il.LoggedEmit(OpCodes.Call, GetCreateSpan(elementType));

			_pendingWriteBack = () => {
				il.LoggedEmit(OpCodes.Ldloc, typedObj);
				il.LoggedEmit(OpCodes.Ldloc, buf);
				EmitConversion(il, elementType, actualType);
				il.LoggedEmit(OpCodes.Stfld, fieldInfo);
			};
		}
		else {
			Warning($"Cannot convert array field {fieldInfo.Name} ({actualType.Name}x{fieldSize} -> {elementType.Name}), using raw bytes\n");
			il.LoggedEmit(OpCodes.Ldloc, typedObj);
			il.LoggedEmit(OpCodes.Ldflda, fieldInfo);
			int byteCount = ManagedSizeOf(elementType) * fieldSize;
			il.LoggedEmit(OpCodes.Ldc_I4, byteCount);
			il.LoggedEmit(OpCodes.Call, GetCreateSpan(typeof(byte)));
			il.LoggedEmit(OpCodes.Call, castSpanOpen.MakeGenericMethod(typeof(byte), elementType));
		}
	}

	static Type CheckTypeAgainstEHANDLE(Type t) {
		if (t.IsConstructedGenericType && t.GetGenericTypeDefinition() == typeof(Handle<>))
			t = typeof(BaseHandle);
		return t;
	}

	static void EmitDataFrameAsReadOnlySpan(ILGenerator il, FieldInfo dataFrameField, TypeDescription field, Type elementType) {
		elementType = CheckTypeAgainstEHANDLE(elementType);
		int offset = (int)field.PackedOffset;
		int byteSize = ManagedSizeOf(elementType) * field.FieldSize;

		il.LoggedEmit(OpCodes.Ldarg_0);
		il.LoggedEmit(OpCodes.Ldflda, dataFrameField);
		il.LoggedEmit(OpCodes.Ldc_I4, offset);
		il.LoggedEmit(OpCodes.Ldc_I4, byteSize);
		il.LoggedEmit(OpCodes.Call, spanByteSlice);
		il.LoggedEmit(OpCodes.Call, GetCastSpan(elementType));
		var spanT = typeof(Span<>).MakeGenericType(elementType);
		var opImplicit = typeof(Span<>).MakeGenericType(elementType)
			.GetMethod("op_Implicit", [spanT]);
		if (opImplicit != null) {
			il.LoggedEmit(OpCodes.Call, opImplicit);
		}
	}

	static void EmitDataFrameAsSpan(ILGenerator il, FieldInfo dataFrameField, TypeDescription field, Type elementType) {
		elementType = CheckTypeAgainstEHANDLE(elementType);
		int offset = (int)field.PackedOffset;
		int byteSize = ManagedSizeOf(elementType) * field.FieldSize;

		il.LoggedEmit(OpCodes.Ldarg_0);
		il.LoggedEmit(OpCodes.Ldflda, dataFrameField);
		il.LoggedEmit(OpCodes.Ldc_I4, offset);
		il.LoggedEmit(OpCodes.Ldc_I4, byteSize);
		il.LoggedEmit(OpCodes.Call, spanByteSlice);
		il.LoggedEmit(OpCodes.Call, GetCastSpan(elementType));
	}

	[ThreadStatic] static Action? _pendingWriteBack;

	static void EmitFieldOps(
		ILGenerator il,
		TypeDescription field,
		Type elementType,
		Action emitOutputAsReadOnlySpan,
		Action emitInputAsReadOnlySpan,
		Action emitOutputAsSpan,
		Action emitInputAsSpan
	) {
		_pendingWriteBack = null;
		var (compareName, copyName, describeName, watchName) = GetMethodNames(field.FieldType);
		int fieldSize = field.FieldSize;

		var roSpanT = typeof(ReadOnlySpan<>).MakeGenericType(elementType);
		var spanT = typeof(Span<>).MakeGenericType(elementType);

		LocalBuilder locDiffType = il.DeclareLocal(typeof(DiffType));
		LocalBuilder locOutputRO = il.DeclareLocal(roSpanT);
		LocalBuilder locOutputRW = il.DeclareLocal(spanT);
		LocalBuilder locInputRO = il.DeclareLocal(roSpanT);

		emitOutputAsReadOnlySpan();
		il.LoggedEmit(OpCodes.Stloc, locOutputRO);

		emitInputAsReadOnlySpan();
		il.LoggedEmit(OpCodes.Stloc, locInputRO);

		emitOutputAsSpan();
		il.LoggedEmit(OpCodes.Stloc, locOutputRW);

		MethodInfo compareMethod;
		if (field.FieldType == FieldType.String) {
			compareMethod = typeof(PredictionCopy).GetMethod(compareName, [roSpanT, roSpanT])!;
		}
		else {
			compareMethod = typeof(PredictionCopy).GetMethod(compareName, [roSpanT, roSpanT, typeof(int)])!;
		}
		if (compareMethod == null)
			compareMethod = typeof(PredictionCopyImpl).GetMethod(compareName, [typeof(PredictionCopy).MakeByRefType(), roSpanT, roSpanT, typeof(int)])!;

		il.LoggedEmit(OpCodes.Ldarg_0);
		il.LoggedEmit(OpCodes.Ldloc, locOutputRO);
		il.LoggedEmit(OpCodes.Ldloc, locInputRO);
		if (field.FieldType != FieldType.String)
			il.LoggedEmit(OpCodes.Ldc_I4, fieldSize);
		il.LoggedEmit(OpCodes.Call, compareMethod);
		il.LoggedEmit(OpCodes.Stloc, locDiffType);

		MethodInfo copyMethod;
		if (field.FieldType == FieldType.String) {
			copyMethod = typeof(PredictionCopy).GetMethod(copyName, [typeof(DiffType), spanT, roSpanT])!;
		}
		else {
			copyMethod = typeof(PredictionCopy).GetMethod(copyName, [typeof(DiffType), spanT, roSpanT, typeof(int)])!;
		}
		if (copyMethod == null)
			copyMethod = typeof(PredictionCopyImpl).GetMethod(copyName, [typeof(PredictionCopy).MakeByRefType(), typeof(DiffType), spanT, roSpanT, typeof(int)])!;

		il.LoggedEmit(OpCodes.Ldarg_0);
		il.LoggedEmit(OpCodes.Ldloc, locDiffType);
		il.LoggedEmit(OpCodes.Ldloc, locOutputRW);
		il.LoggedEmit(OpCodes.Ldloc, locInputRO);
		if (field.FieldType != FieldType.String)
			il.LoggedEmit(OpCodes.Ldc_I4, fieldSize);
		il.LoggedEmit(OpCodes.Call, copyMethod);

		_pendingWriteBack?.Invoke();
		_pendingWriteBack = null;

		Label skipDescribe = il.DefineLabel();
		il.LoggedEmit(OpCodes.Ldarg_0);
		il.LoggedEmit(OpCodes.Ldfld, f_ErrorCheck);
		il.LoggedEmit(OpCodes.Brfalse, skipDescribe);
		il.LoggedEmit(OpCodes.Ldarg_0);
		il.LoggedEmit(OpCodes.Ldfld, f_ShouldDescribe);
		il.LoggedEmit(OpCodes.Brfalse, skipDescribe);

		MethodInfo describeMethod;
		bool describeIsData = (describeName == "DescribeData");
		bool describeIsString = (field.FieldType == FieldType.String);

		if (describeIsData) {
			describeMethod = typeof(PredictionCopy).GetMethod("DescribeData",
				[typeof(DiffType), typeof(int), typeof(Span<byte>), typeof(ReadOnlySpan<byte>)])!;

			il.LoggedEmit(OpCodes.Ldarg_0);
			il.LoggedEmit(OpCodes.Ldloc, locDiffType);
			if (field.FieldType == FieldType.Color32)
				il.LoggedEmit(OpCodes.Ldc_I4, 4 * fieldSize);
			else
				il.LoggedEmit(OpCodes.Ldc_I4, fieldSize);
			il.LoggedEmit(OpCodes.Ldloc, locOutputRW);
			il.LoggedEmit(OpCodes.Call, typeof(MemoryMarshal).GetMethod("AsBytes", [spanT])!.MakeGenericMethod(elementType));
			il.LoggedEmit(OpCodes.Ldloc, locInputRO);
			il.LoggedEmit(OpCodes.Call, typeof(MemoryMarshal).GetMethod("AsBytes", [roSpanT])!.MakeGenericMethod(elementType));
			il.LoggedEmit(OpCodes.Call, describeMethod);
		}
		else if (describeIsString) {
			describeMethod = typeof(PredictionCopy).GetMethod(describeName, [typeof(DiffType), spanT, roSpanT])!;
			il.LoggedEmit(OpCodes.Ldarg_0);
			il.LoggedEmit(OpCodes.Ldloc, locDiffType);
			il.LoggedEmit(OpCodes.Ldloc, locOutputRW);
			il.LoggedEmit(OpCodes.Ldloc, locInputRO);
			il.LoggedEmit(OpCodes.Call, describeMethod);
		}
		else {
			if ((field.FieldType == FieldType.Character || field.FieldType == FieldType.StringCharacter) && describeName == "DescribeInt") {
				var descSpanT = typeof(Span<int>);
				var descRoSpanT = typeof(ReadOnlySpan<int>);
				describeMethod = typeof(PredictionCopy).GetMethod("DescribeInt",
					[typeof(DiffType), descSpanT, descRoSpanT, typeof(int)])!;

				il.LoggedEmit(OpCodes.Ldarg_0);
				il.LoggedEmit(OpCodes.Ldloc, locDiffType);
				il.LoggedEmit(OpCodes.Ldloc, locOutputRW);
				il.LoggedEmit(OpCodes.Call, castSpanOpen.MakeGenericMethod(elementType, typeof(int)));
				il.LoggedEmit(OpCodes.Ldloc, locInputRO);
				il.LoggedEmit(OpCodes.Call, castROSpanOpen.MakeGenericMethod(elementType, typeof(int)));
				il.LoggedEmit(OpCodes.Ldc_I4, fieldSize);
				il.LoggedEmit(OpCodes.Call, describeMethod);
			}
			else {
				describeMethod = typeof(PredictionCopy).GetMethod(describeName,
					[typeof(DiffType), spanT, roSpanT, typeof(int)])!;

				if (describeMethod == null)
					describeMethod = typeof(PredictionCopyImpl).GetMethod(describeName, [typeof(PredictionCopy).MakeByRefType(), typeof(DiffType), spanT, roSpanT, typeof(int)])!;

				il.LoggedEmit(OpCodes.Ldarg_0);
				il.LoggedEmit(OpCodes.Ldloc, locDiffType);
				il.LoggedEmit(OpCodes.Ldloc, locOutputRW);
				il.LoggedEmit(OpCodes.Ldloc, locInputRO);
				il.LoggedEmit(OpCodes.Ldc_I4, fieldSize);
				il.LoggedEmit(OpCodes.Call, describeMethod);
			}
		}
		il.MarkLabel(skipDescribe);

		Label skipWatch = il.DefineLabel();
		il.LoggedEmit(OpCodes.Ldarg_0);
		il.LoggedEmit(OpCodes.Ldfld, f_WatchField);
		il.LoggedEmit(OpCodes.Ldarg_0);
		il.LoggedEmit(OpCodes.Ldfld, f_CurrentField);
		il.LoggedEmit(OpCodes.Bne_Un, skipWatch);

		bool watchIsData = (watchName == "WatchData");
		bool watchIsString = (field.FieldType == FieldType.String);

		if (watchIsData) {
			var watchMethod = typeof(PredictionCopy).GetMethod("WatchData",
				[typeof(DiffType), typeof(int), typeof(Span<byte>), typeof(ReadOnlySpan<byte>)])!;

			il.LoggedEmit(OpCodes.Ldarg_0);
			il.LoggedEmit(OpCodes.Ldloc, locDiffType);
			if (field.FieldType == FieldType.Color32)
				il.LoggedEmit(OpCodes.Ldc_I4, 4 * fieldSize);
			else
				il.LoggedEmit(OpCodes.Ldc_I4, fieldSize);
			il.LoggedEmit(OpCodes.Ldloc, locOutputRW);
			il.LoggedEmit(OpCodes.Call, asBytesSpanOpen.MakeGenericMethod(elementType));
			il.LoggedEmit(OpCodes.Ldloc, locInputRO);
			il.LoggedEmit(OpCodes.Call, asBytesROSpanOpen.MakeGenericMethod(elementType));
			il.LoggedEmit(OpCodes.Call, watchMethod);
		}
		else if (watchIsString) {
			var watchMethod = typeof(PredictionCopy).GetMethod(watchName, [typeof(DiffType), spanT, roSpanT])!;
			il.LoggedEmit(OpCodes.Ldarg_0);
			il.LoggedEmit(OpCodes.Ldloc, locDiffType);
			il.LoggedEmit(OpCodes.Ldloc, locOutputRW);
			il.LoggedEmit(OpCodes.Ldloc, locInputRO);
			il.LoggedEmit(OpCodes.Call, watchMethod);
		}
		else {
			var watchMethod = typeof(PredictionCopy).GetMethod(watchName,
				[typeof(DiffType), spanT, roSpanT, typeof(int)])!;

			if (watchMethod == null)
				watchMethod = typeof(PredictionCopyImpl).GetMethod(watchName, [typeof(PredictionCopy).MakeByRefType(), typeof(DiffType), spanT, roSpanT, typeof(int)])!;

			il.LoggedEmit(OpCodes.Ldarg_0);
			il.LoggedEmit(OpCodes.Ldloc, locDiffType);
			il.LoggedEmit(OpCodes.Ldloc, locOutputRW);
			il.LoggedEmit(OpCodes.Ldloc, locInputRO);
			il.LoggedEmit(OpCodes.Ldc_I4, fieldSize);
			il.LoggedEmit(OpCodes.Call, watchMethod);
		}
		il.MarkLabel(skipWatch);
	}

	static readonly MethodInfo castSpanOpen = typeof(MemoryMarshal).GetMethods()
	.First(m => m.Name == "Cast" && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(Span<>));

	static readonly MethodInfo castROSpanOpen = typeof(MemoryMarshal).GetMethods()
		.First(m => m.Name == "Cast" && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(ReadOnlySpan<>));

	static readonly MethodInfo asBytesSpanOpen = typeof(MemoryMarshal).GetMethods()
		.First(m => m.Name == "AsBytes" && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(Span<>));

	static readonly MethodInfo asBytesROSpanOpen = typeof(MemoryMarshal).GetMethods()
		.First(m => m.Name == "AsBytes" && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(ReadOnlySpan<>));

	public static void CopyFields_ObjectToDataFrame(int chaincount, DataMap pRootMap, TypeDescription[] pFields, ILGenerator il, LocalBuilder typedSrc) {
		EmitPreamble(il, pRootMap);

		for (int i = 0; i < pFields.Length; i++) {
			var field = pFields[i];

			if (field.OverrideField != null)
				field.OverrideField.OverrideCount = chaincount;
			if (field.OverrideCount == chaincount)
				continue;
			if (field.FieldType != FieldType.Embedded && (field.Flags & FieldTypeDescFlags.Private) != 0)
				continue;

			Label continueLabel = il.DefineLabel();

			int fieldIdx = IL_AddConstant(field);
			EmitSetSelfFieldFromConstant(il, fieldIdx, typeof(TypeDescription), f_CurrentField);

			EmitNetworkSkipChecks(il, field, continueLabel);

			EmitSetReportState(il);

			switch (field.FieldType) {
				case FieldType.Embedded: {
						LocalBuilder saveName = il.DeclareLocal(typeof(ReadOnlySpan<char>));
						LocalBuilder saveField = il.DeclareLocal(typeof(TypeDescription));
						LocalBuilder saveDestDF = il.DeclareLocal(typeof(Span<byte>));

						il.LoggedEmit(OpCodes.Ldarg_0);
						il.LoggedEmit(OpCodes.Ldfld, f_CurrentClassName);
						il.LoggedEmit(OpCodes.Stloc, saveName);
						il.LoggedEmit(OpCodes.Ldarg_0);
						il.LoggedEmit(OpCodes.Ldfld, f_CurrentField);
						il.LoggedEmit(OpCodes.Stloc, saveField);
						il.LoggedEmit(OpCodes.Ldarg_0);
						il.LoggedEmit(OpCodes.Ldfld, f_Dest_DataFrame);
						il.LoggedEmit(OpCodes.Stloc, saveDestDF);

						int tdIdx = IL_AddConstant(field.TD!);
						il.LoggedEmit(OpCodes.Ldarg_0);
						EmitLoadConstant(il, tdIdx, typeof(DataMap));
						il.LoggedEmit(OpCodes.Ldfld, typeof(DataMap).GetField("DataClassName")!);
						il.LoggedEmit(OpCodes.Stfld, f_CurrentClassName);

						il.LoggedEmit(OpCodes.Ldarg_0);
						il.LoggedEmit(OpCodes.Ldarg_0);
						il.LoggedEmit(OpCodes.Ldflda, f_Dest_DataFrame);
						il.LoggedEmit(OpCodes.Ldc_I4, (int)field.PackedOffset);
						il.LoggedEmit(OpCodes.Call, typeof(Span<byte>).GetMethod("Slice", [typeof(int)])!);
						il.LoggedEmit(OpCodes.Stfld, f_Dest_DataFrame);

						LocalBuilder embeddedSrc = il.DeclareLocal(field.FieldInfo.FieldType);
						il.LoggedEmit(OpCodes.Ldloc, typedSrc);
						il.LoggedEmit(OpCodes.Ldfld, field.FieldInfo);
						il.LoggedEmit(OpCodes.Stloc, embeddedSrc);

						CopyFields_ObjectToDataFrame(chaincount, field.TD!, field.TD!.DataDesc, il, embeddedSrc);

						il.LoggedEmit(OpCodes.Ldarg_0);
						il.LoggedEmit(OpCodes.Ldloc, saveName);
						il.LoggedEmit(OpCodes.Stfld, f_CurrentClassName);
						il.LoggedEmit(OpCodes.Ldarg_0);
						il.LoggedEmit(OpCodes.Ldloc, saveField);
						il.LoggedEmit(OpCodes.Stfld, f_CurrentField);
						il.LoggedEmit(OpCodes.Ldarg_0);
						il.LoggedEmit(OpCodes.Ldloc, saveDestDF);
						il.LoggedEmit(OpCodes.Stfld, f_Dest_DataFrame);
						break;
					}

				case FieldType.Void:
					break;

				case FieldType.Time:
				case FieldType.Tick:
				case FieldType.ModelIndex:
				case FieldType.ModelName:
				case FieldType.SoundName:
				case FieldType.Custom:
				case FieldType.ClassPtr:
				case FieldType.EDict:
				case FieldType.PositionVector:
				case FieldType.Function:
					break;

				case FieldType.String: {
						Type elemType = typeof(char);
						EmitFieldOps(il, field, elemType,
							() => EmitDataFrameAsReadOnlySpan(il, f_Dest_DataFrame, field, elemType),
							() => EmitObjectFieldAsReadOnlySpan(il, typedSrc, field.FieldInfo, elemType, field.FieldSize),
							() => EmitDataFrameAsSpan(il, f_Dest_DataFrame, field, elemType),
							() => { }
						);
						break;
					}

				default: {
						Type? elemType = GetElementType(field.FieldType);
						if (elemType == null) break;
						EmitFieldOps(il, field, elemType,
							() => EmitDataFrameAsReadOnlySpan(il, f_Dest_DataFrame, field, elemType),
							() => EmitObjectFieldAsReadOnlySpan(il, typedSrc, field.FieldInfo, elemType, field.FieldSize),
							() => EmitDataFrameAsSpan(il, f_Dest_DataFrame, field, elemType),
							() => { }
						);
						break;
					}
			}

			il.MarkLabel(continueLabel);
		}
	}

	public static void CopyFields_DataFrameToObject(int chaincount, DataMap pRootMap, TypeDescription[] pFields, ILGenerator il, LocalBuilder typedDest) {
		EmitPreamble(il, pRootMap);

		for (int i = 0; i < pFields.Length; i++) {
			var field = pFields[i];

			if (field.OverrideField != null)
				field.OverrideField.OverrideCount = chaincount;
			if (field.OverrideCount == chaincount)
				continue;
			if (field.FieldType != FieldType.Embedded && (field.Flags & FieldTypeDescFlags.Private) != 0)
				continue;

			Label continueLabel = il.DefineLabel();

			int fieldIdx = IL_AddConstant(field);
			EmitSetSelfFieldFromConstant(il, fieldIdx, typeof(TypeDescription), f_CurrentField);
			EmitNetworkSkipChecks(il, field, continueLabel);
			EmitSetReportState(il);

			switch (field.FieldType) {
				case FieldType.Embedded: {
						LocalBuilder saveName = il.DeclareLocal(typeof(ReadOnlySpan<char>));
						LocalBuilder saveField = il.DeclareLocal(typeof(TypeDescription));
						LocalBuilder saveSrcDF = il.DeclareLocal(typeof(Span<byte>));

						il.LoggedEmit(OpCodes.Ldarg_0);
						il.LoggedEmit(OpCodes.Ldfld, f_CurrentClassName);
						il.LoggedEmit(OpCodes.Stloc, saveName);
						il.LoggedEmit(OpCodes.Ldarg_0);
						il.LoggedEmit(OpCodes.Ldfld, f_CurrentField);
						il.LoggedEmit(OpCodes.Stloc, saveField);
						il.LoggedEmit(OpCodes.Ldarg_0);
						il.LoggedEmit(OpCodes.Ldfld, f_Src_DataFrame);
						il.LoggedEmit(OpCodes.Stloc, saveSrcDF);

						int tdIdx = IL_AddConstant(field.TD!);
						il.LoggedEmit(OpCodes.Ldarg_0);
						EmitLoadConstant(il, tdIdx, typeof(DataMap));
						il.LoggedEmit(OpCodes.Ldfld, typeof(DataMap).GetField("DataClassName")!);
						il.LoggedEmit(OpCodes.Stfld, f_CurrentClassName);

						il.LoggedEmit(OpCodes.Ldarg_0);
						il.LoggedEmit(OpCodes.Ldarg_0);
						il.LoggedEmit(OpCodes.Ldflda, f_Src_DataFrame);
						il.LoggedEmit(OpCodes.Ldc_I4, (int)field.PackedOffset);
						il.LoggedEmit(OpCodes.Call, typeof(Span<byte>).GetMethod("Slice", [typeof(int)])!);
						il.LoggedEmit(OpCodes.Stfld, f_Src_DataFrame);

						LocalBuilder embeddedDest = il.DeclareLocal(field.FieldInfo.FieldType);
						il.LoggedEmit(OpCodes.Ldloc, typedDest);
						il.LoggedEmit(OpCodes.Ldfld, field.FieldInfo);
						il.LoggedEmit(OpCodes.Stloc, embeddedDest);

						CopyFields_DataFrameToObject(chaincount, field.TD!, field.TD!.DataDesc, il, embeddedDest);

						il.LoggedEmit(OpCodes.Ldarg_0);
						il.LoggedEmit(OpCodes.Ldloc, saveName);
						il.LoggedEmit(OpCodes.Stfld, f_CurrentClassName);
						il.LoggedEmit(OpCodes.Ldarg_0);
						il.LoggedEmit(OpCodes.Ldloc, saveField);
						il.LoggedEmit(OpCodes.Stfld, f_CurrentField);
						il.LoggedEmit(OpCodes.Ldarg_0);
						il.LoggedEmit(OpCodes.Ldloc, saveSrcDF);
						il.LoggedEmit(OpCodes.Stfld, f_Src_DataFrame);
						break;
					}

				case FieldType.Void:
					break;

				case FieldType.Time:
				case FieldType.Tick:
				case FieldType.ModelIndex:
				case FieldType.ModelName:
				case FieldType.SoundName:
				case FieldType.Custom:
				case FieldType.ClassPtr:
				case FieldType.EDict:
				case FieldType.PositionVector:
				case FieldType.Function:
					break;

				case FieldType.String: {
						Type elemType = typeof(char);
						EmitFieldOps(il, field, elemType,
							() => EmitObjectFieldAsReadOnlySpan(il, typedDest, field.FieldInfo, elemType, field.FieldSize),
							() => EmitDataFrameAsReadOnlySpan(il, f_Src_DataFrame, field, elemType),
							() => EmitObjectFieldAsSpan(il, typedDest, field.FieldInfo, elemType, field.FieldSize),
							() => { }
						);
						break;
					}

				default: {
						Type? elemType = GetElementType(field.FieldType);
						if (elemType == null) break;

						EmitFieldOps(il, field, elemType,
							() => EmitObjectFieldAsReadOnlySpan(il, typedDest, field.FieldInfo, elemType, field.FieldSize),
							() => EmitDataFrameAsReadOnlySpan(il, f_Src_DataFrame, field, elemType),
							() => EmitObjectFieldAsSpan(il, typedDest, field.FieldInfo, elemType, field.FieldSize),
							() => { }
						);
						break;
					}
			}

			il.MarkLabel(continueLabel);
		}
	}

	public static void CopyFields_ObjectToObject(int chaincount, DataMap pRootMap, TypeDescription[] pFields, ILGenerator il, LocalBuilder typedSrc, LocalBuilder typedDest) {
		EmitPreamble(il, pRootMap);

		for (int i = 0; i < pFields.Length; i++) {
			var field = pFields[i];

			if (field.OverrideField != null)
				field.OverrideField.OverrideCount = chaincount;
			if (field.OverrideCount == chaincount)
				continue;
			if (field.FieldType != FieldType.Embedded && (field.Flags & FieldTypeDescFlags.Private) != 0)
				continue;

			Label continueLabel = il.DefineLabel();

			int fieldIdx = IL_AddConstant(field);
			EmitSetSelfFieldFromConstant(il, fieldIdx, typeof(TypeDescription), f_CurrentField);
			EmitNetworkSkipChecks(il, field, continueLabel);
			EmitSetReportState(il);

			switch (field.FieldType) {
				case FieldType.Embedded: {
						LocalBuilder saveName = il.DeclareLocal(typeof(ReadOnlySpan<char>));
						LocalBuilder saveField = il.DeclareLocal(typeof(TypeDescription));

						il.LoggedEmit(OpCodes.Ldarg_0);
						il.LoggedEmit(OpCodes.Ldfld, f_CurrentClassName);
						il.LoggedEmit(OpCodes.Stloc, saveName);
						il.LoggedEmit(OpCodes.Ldarg_0);
						il.LoggedEmit(OpCodes.Ldfld, f_CurrentField);
						il.LoggedEmit(OpCodes.Stloc, saveField);

						int tdIdx = IL_AddConstant(field.TD!);
						il.LoggedEmit(OpCodes.Ldarg_0);
						EmitLoadConstant(il, tdIdx, typeof(DataMap));
						il.LoggedEmit(OpCodes.Ldfld, typeof(DataMap).GetField("DataClassName")!);
						il.LoggedEmit(OpCodes.Stfld, f_CurrentClassName);

						LocalBuilder embeddedSrc = il.DeclareLocal(field.FieldInfo.FieldType);
						il.LoggedEmit(OpCodes.Ldloc, typedSrc);
						il.LoggedEmit(OpCodes.Ldfld, field.FieldInfo);
						il.LoggedEmit(OpCodes.Stloc, embeddedSrc);

						LocalBuilder embeddedDest = il.DeclareLocal(field.FieldInfo.FieldType);
						il.LoggedEmit(OpCodes.Ldloc, typedDest);
						il.LoggedEmit(OpCodes.Ldfld, field.FieldInfo);
						il.LoggedEmit(OpCodes.Stloc, embeddedDest);

						CopyFields_ObjectToObject(chaincount, field.TD!, field.TD!.DataDesc, il, embeddedSrc, embeddedDest);

						il.LoggedEmit(OpCodes.Ldarg_0);
						il.LoggedEmit(OpCodes.Ldloc, saveName);
						il.LoggedEmit(OpCodes.Stfld, f_CurrentClassName);
						il.LoggedEmit(OpCodes.Ldarg_0);
						il.LoggedEmit(OpCodes.Ldloc, saveField);
						il.LoggedEmit(OpCodes.Stfld, f_CurrentField);
						break;
					}

				case FieldType.Void:
					break;

				case FieldType.Time:
				case FieldType.Tick:
				case FieldType.ModelIndex:
				case FieldType.ModelName:
				case FieldType.SoundName:
				case FieldType.Custom:
				case FieldType.ClassPtr:
				case FieldType.EDict:
				case FieldType.PositionVector:
				case FieldType.Function:
					break;

				case FieldType.String: {
						Type elemType = typeof(char);
						EmitFieldOps(il, field, elemType,
							() => EmitObjectFieldAsReadOnlySpan(il, typedDest, field.FieldInfo, elemType, field.FieldSize),
							() => EmitObjectFieldAsReadOnlySpan(il, typedSrc, field.FieldInfo, elemType, field.FieldSize),
							() => EmitObjectFieldAsSpan(il, typedDest, field.FieldInfo, elemType, field.FieldSize),
							() => { }
						);
						break;
					}

				default: {
						Type? elemType = GetElementType(field.FieldType);
						if (elemType == null) break;

						EmitFieldOps(il, field, elemType,
							() => EmitObjectFieldAsReadOnlySpan(il, typedDest, field.FieldInfo, elemType, field.FieldSize),
							() => EmitObjectFieldAsReadOnlySpan(il, typedSrc, field.FieldInfo, elemType, field.FieldSize),
							() => EmitObjectFieldAsSpan(il, typedDest, field.FieldInfo, elemType, field.FieldSize),
							() => { }
						);
						break;
					}
			}

			il.MarkLabel(continueLabel);
		}
	}

	public static void CopyFields_DataFrameToDataFrame(int chaincount, DataMap pRootMap, TypeDescription[] pFields, ILGenerator il) {
		EmitPreamble(il, pRootMap);

		for (int i = 0; i < pFields.Length; i++) {
			var field = pFields[i];

			if (field.OverrideField != null)
				field.OverrideField.OverrideCount = chaincount;
			if (field.OverrideCount == chaincount)
				continue;
			if (field.FieldType != FieldType.Embedded && (field.Flags & FieldTypeDescFlags.Private) != 0)
				continue;

			Label continueLabel = il.DefineLabel();

			int fieldIdx = IL_AddConstant(field);
			EmitSetSelfFieldFromConstant(il, fieldIdx, typeof(TypeDescription), f_CurrentField);
			EmitNetworkSkipChecks(il, field, continueLabel);
			EmitSetReportState(il);

			switch (field.FieldType) {
				case FieldType.Embedded: {
						LocalBuilder saveName = il.DeclareLocal(typeof(ReadOnlySpan<char>));
						LocalBuilder saveField = il.DeclareLocal(typeof(TypeDescription));
						LocalBuilder saveSrcDF = il.DeclareLocal(typeof(Span<byte>));
						LocalBuilder saveDestDF = il.DeclareLocal(typeof(Span<byte>));

						il.LoggedEmit(OpCodes.Ldarg_0);
						il.LoggedEmit(OpCodes.Ldfld, f_CurrentClassName);
						il.LoggedEmit(OpCodes.Stloc, saveName);
						il.LoggedEmit(OpCodes.Ldarg_0);
						il.LoggedEmit(OpCodes.Ldfld, f_CurrentField);
						il.LoggedEmit(OpCodes.Stloc, saveField);
						il.LoggedEmit(OpCodes.Ldarg_0);
						il.LoggedEmit(OpCodes.Ldfld, f_Src_DataFrame);
						il.LoggedEmit(OpCodes.Stloc, saveSrcDF);
						il.LoggedEmit(OpCodes.Ldarg_0);
						il.LoggedEmit(OpCodes.Ldfld, f_Dest_DataFrame);
						il.LoggedEmit(OpCodes.Stloc, saveDestDF);

						int tdIdx = IL_AddConstant(field.TD!);
						il.LoggedEmit(OpCodes.Ldarg_0);
						EmitLoadConstant(il, tdIdx, typeof(DataMap));
						il.LoggedEmit(OpCodes.Ldfld, typeof(DataMap).GetField("DataClassName")!);
						il.LoggedEmit(OpCodes.Stfld, f_CurrentClassName);

						il.LoggedEmit(OpCodes.Ldarg_0);
						il.LoggedEmit(OpCodes.Ldarg_0);
						il.LoggedEmit(OpCodes.Ldflda, f_Src_DataFrame);
						il.LoggedEmit(OpCodes.Ldc_I4, (int)field.PackedOffset);
						il.LoggedEmit(OpCodes.Call, typeof(Span<byte>).GetMethod("Slice", [typeof(int)])!);
						il.LoggedEmit(OpCodes.Stfld, f_Src_DataFrame);

						il.LoggedEmit(OpCodes.Ldarg_0);
						il.LoggedEmit(OpCodes.Ldarg_0);
						il.LoggedEmit(OpCodes.Ldflda, f_Dest_DataFrame);
						il.LoggedEmit(OpCodes.Ldc_I4, (int)field.PackedOffset);
						il.LoggedEmit(OpCodes.Call, typeof(Span<byte>).GetMethod("Slice", [typeof(int)])!);
						il.LoggedEmit(OpCodes.Stfld, f_Dest_DataFrame);

						CopyFields_DataFrameToDataFrame(chaincount, field.TD!, field.TD!.DataDesc, il);

						il.LoggedEmit(OpCodes.Ldarg_0);
						il.LoggedEmit(OpCodes.Ldloc, saveName);
						il.LoggedEmit(OpCodes.Stfld, f_CurrentClassName);
						il.LoggedEmit(OpCodes.Ldarg_0);
						il.LoggedEmit(OpCodes.Ldloc, saveField);
						il.LoggedEmit(OpCodes.Stfld, f_CurrentField);
						il.LoggedEmit(OpCodes.Ldarg_0);
						il.LoggedEmit(OpCodes.Ldloc, saveSrcDF);
						il.LoggedEmit(OpCodes.Stfld, f_Src_DataFrame);
						il.LoggedEmit(OpCodes.Ldarg_0);
						il.LoggedEmit(OpCodes.Ldloc, saveDestDF);
						il.LoggedEmit(OpCodes.Stfld, f_Dest_DataFrame);
						break;
					}

				case FieldType.Void:
					break;

				case FieldType.Time:
				case FieldType.Tick:
				case FieldType.ModelIndex:
				case FieldType.ModelName:
				case FieldType.SoundName:
				case FieldType.Custom:
				case FieldType.ClassPtr:
				case FieldType.EDict:
				case FieldType.PositionVector:
				case FieldType.Function:
					break;

				case FieldType.String: {
						Type elemType = typeof(char);
						EmitFieldOps(il, field, elemType,
							() => EmitDataFrameAsReadOnlySpan(il, f_Dest_DataFrame, field, elemType),
							() => EmitDataFrameAsReadOnlySpan(il, f_Src_DataFrame, field, elemType),
							() => EmitDataFrameAsSpan(il, f_Dest_DataFrame, field, elemType),
							() => { }
						);
						break;
					}

				default: {
						Type? elemType = GetElementType(field.FieldType);
						if (elemType == null) break;

						EmitFieldOps(il, field, elemType,
							() => EmitDataFrameAsReadOnlySpan(il, f_Dest_DataFrame, field, elemType),
							() => EmitDataFrameAsReadOnlySpan(il, f_Src_DataFrame, field, elemType),
							() => EmitDataFrameAsSpan(il, f_Dest_DataFrame, field, elemType),
							() => { }
						);
						break;
					}
			}

			il.MarkLabel(continueLabel);
		}
	}

	static void EmitDebugLog(ILGenerator il, string message) {
		il.Emit(OpCodes.Ldstr, message);
		il.Emit(OpCodes.Call, typeof(Console).GetMethod("WriteLine", [typeof(string)])!);
	}

	public static void StartIL(PredictionCopyRelationship relationship, DataMap dmap, out DynamicMethod dynMethod, out ILGenerator il) {
		dynMethod = new DynamicMethod(
			$"{dmap.DataClassName}_{relationship}Fn",
			typeof(int),
			[typeof(PredictionCopy).MakeByRefType()]
		);
		il = dynMethod.GetILGenerator();
	}

	public static void EndIL(ILGenerator il) {
		il.LoggedEmit(OpCodes.Ldarg_0);
		il.LoggedEmit(OpCodes.Ldfld, typeof(PredictionCopy).GetField("ErrorCount")!);
		il.LoggedEmit(OpCodes.Ret);
	}

	#region transfer functions
	public static void Transfer_ObjectToObject_R(int chaincount, DataMap dmap, ILGenerator il, LocalBuilder typedSrc, LocalBuilder typedDest) {
		CopyFields_ObjectToObject(chaincount, dmap, dmap.DataDesc, il, typedSrc, typedDest);
		if (dmap.BaseMap != null)
			Transfer_ObjectToObject_R(chaincount, dmap.BaseMap, il, typedSrc, typedDest);
	}

	public static void Transfer_ObjectToDataFrame_R(int chaincount, DataMap dmap, ILGenerator il, LocalBuilder typedSrc) {
		CopyFields_ObjectToDataFrame(chaincount, dmap, dmap.DataDesc, il, typedSrc);
		if (dmap.BaseMap != null)
			Transfer_ObjectToDataFrame_R(chaincount, dmap.BaseMap, il, typedSrc);
	}

	public static void Transfer_DataFrameToObject_R(int chaincount, DataMap dmap, ILGenerator il, LocalBuilder typedDest) {
		CopyFields_DataFrameToObject(chaincount, dmap, dmap.DataDesc, il, typedDest);
		if (dmap.BaseMap != null)
			Transfer_DataFrameToObject_R(chaincount, dmap.BaseMap, il, typedDest);
	}

	public static void Transfer_DataFrameToDataFrame_R(int chaincount, DataMap dmap, ILGenerator il) {
		CopyFields_DataFrameToDataFrame(chaincount, dmap, dmap.DataDesc, il);
		if (dmap.BaseMap != null)
			Transfer_DataFrameToDataFrame_R(chaincount, dmap.BaseMap, il);
	}
	#endregion

	#region compile functions
	public static PredictionCopyFn_ObjectToObjectFn Compile_ObjectToObject(DataMap dmap) {
		StartIL(PredictionCopyRelationship.ObjectToObject, dmap, out DynamicMethod dynMethod, out ILGenerator il);

		LocalBuilder typedSrc = il.DeclareLocal(dmap.DataClassType);
		il.Emit(OpCodes.Ldarg_0);
		il.Emit(OpCodes.Ldfld, f_Src_Object);
		il.Emit(OpCodes.Castclass, dmap.DataClassType);
		il.Emit(OpCodes.Stloc, typedSrc);

		LocalBuilder typedDest = il.DeclareLocal(dmap.DataClassType);
		il.Emit(OpCodes.Ldarg_0);
		il.Emit(OpCodes.Ldfld, f_Dest_Object);
		il.Emit(OpCodes.Castclass, dmap.DataClassType);
		il.Emit(OpCodes.Stloc, typedDest);

		Transfer_ObjectToObject_R(PredictionCopy.g_nChainCount, dmap, il, typedSrc, typedDest);
		EndIL(il);
		return dynMethod.CreateDelegate<PredictionCopyFn_ObjectToObjectFn>();
	}
	public static PredictionCopyFn_ObjectToDataFrameFn Compile_ObjectToDataFrame(DataMap dmap) {
		StartIL(PredictionCopyRelationship.ObjectToDataFrame, dmap, out DynamicMethod dynMethod, out ILGenerator il);

		LocalBuilder typedSrc = il.DeclareLocal(dmap.DataClassType);
		il.Emit(OpCodes.Ldarg_0);
		il.Emit(OpCodes.Ldfld, f_Src_Object);
		il.Emit(OpCodes.Castclass, dmap.DataClassType);
		il.Emit(OpCodes.Stloc, typedSrc);

		Transfer_ObjectToDataFrame_R(PredictionCopy.g_nChainCount, dmap, il, typedSrc);
		EndIL(il);
		return dynMethod.CreateDelegate<PredictionCopyFn_ObjectToDataFrameFn>();
	}
	public static PredictionCopyFn_DataFrameToObjectFn Compile_DataFrameToObject(DataMap dmap) {
		StartIL(PredictionCopyRelationship.DataFrameToObject, dmap, out DynamicMethod dynMethod, out ILGenerator il);

		LocalBuilder typedDest = il.DeclareLocal(dmap.DataClassType);
		il.Emit(OpCodes.Ldarg_0);
		il.Emit(OpCodes.Ldfld, f_Dest_Object);
		il.Emit(OpCodes.Castclass, dmap.DataClassType);
		il.Emit(OpCodes.Stloc, typedDest);

		Transfer_DataFrameToObject_R(PredictionCopy.g_nChainCount, dmap, il, typedDest);
		EndIL(il);
		return dynMethod.CreateDelegate<PredictionCopyFn_DataFrameToObjectFn>();
	}
	public static PredictionCopyFn_DataFrameToDataFrameFn Compile_DataFrameToDataFrame(DataMap dmap) {
		StartIL(PredictionCopyRelationship.DataFrameToDataFrame, dmap, out var dynMethod, out var il);
		Transfer_DataFrameToDataFrame_R(PredictionCopy.g_nChainCount, dmap, il);
		EndIL(il);
		return dynMethod.CreateDelegate<PredictionCopyFn_DataFrameToDataFrameFn>();
	}
	#endregion

	static readonly Dictionary<Type, Func<int>> sizeFns = [];
	static int ManagedSizeOf(Type elementType) {
		if (sizeFns.TryGetValue(elementType, out var a))
			return a();

		a = sizeFns[elementType] = typeof(Unsafe).GetMethod("SizeOf")!
				.MakeGenericMethod(elementType)
				.CreateDelegate<Func<int>>();

		return a();
	}

	extension(ref PredictionCopy self)
	{
		public int TransferData(scoped ReadOnlySpan<char> operation, int entindex, DataMap? dmap) {
			Assert(dmap != null);
			++PredictionCopy.g_nChainCount;

			switch (self.Relationship) {
				case PredictionCopyRelationship.ObjectToObject:
					return (dmap.PredictionCopyFn_ObjectToObject ??= Compile_ObjectToObject(dmap))(ref self);
				case PredictionCopyRelationship.ObjectToDataFrame:
					return (dmap.PredictionCopyFn_ObjectToDataFrame ??= Compile_ObjectToDataFrame(dmap))(ref self);
				case PredictionCopyRelationship.DataFrameToObject:
					return (dmap.PredictionCopyFn_DataFrameToObject ??= Compile_DataFrameToObject(dmap))(ref self);
				case PredictionCopyRelationship.DataFrameToDataFrame:
					return (dmap.PredictionCopyFn_DataFrameToDataFrame ??= Compile_DataFrameToDataFrame(dmap))(ref self);
			}

			return 0;
		}
	}
}
