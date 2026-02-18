global using static Game.Shared.PredictionCopy;

using Source;
using Source.Common;

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Game.Shared;

public enum PredictionCopyType
{
	Everything,
	NonNetworkedOnly,
	NetworkedOnly
}

public enum PredictionCopyRelationship
{
	DataFrameToObject,
	ObjectToDataFrame,

	// Will these even be used?
	DataFrameToDataFrame,
	ObjectToObject
}

public delegate void FN_FIELD_COMPARE(ReadOnlySpan<char> classname, ReadOnlySpan<char> fieldname, ReadOnlySpan<char> fieldtype, bool networked, bool noterrorchecked, bool differs, bool withintolerance, ReadOnlySpan<byte> value);

public ref struct PredictionIO
{
	IDynamicAccessor? dynaccess;
	object? dynowner;

	TypeDescription? typedesc;
	DataFrame? dataowner;

	public static PredictionIO FromDynamicAccessor(IDynamicAccessor accessor, object owner)
		=> new() { dynaccess = accessor, dynowner = owner };

	public static PredictionIO FromDataMap(TypeDescription typedesc, DataFrame dataowner)
		=> new() { typedesc = typedesc, dataowner = dataowner };

	public T? Get<T>(int offset = 0) {
		if (dynaccess != null)
			return dynaccess.AtIndex(offset).GetValue<T>(dynowner!);
		else if (dataowner != null)
			return dataowner.Get<T>(typedesc!, offset);
		else throw new NullReferenceException();
	}

	public void Set<T>(T? value, int offset = 0) {
		if (dynaccess != null)
			dynaccess.AtIndex(offset).SetValue<T>(dynowner!, value!);
		else if (dataowner != null)
			dataowner.Set<T>(typedesc!, value, offset);
		else throw new NullReferenceException();
	}
}

public ref struct PredictionCopy
{
	public const bool PC_DATA_NORMAL = false;
	public const bool PC_DATA_PACKED = true;

	public readonly PredictionCopyType Type;
	public readonly DataFrame Dest_DataFrame;
	public readonly DataFrame Src_DataFrame;
	public readonly object? Dest_Object;
	public readonly object? Src_Object;
	public readonly PredictionCopyRelationship Relationship;
	public readonly bool ErrorCheck;
	public readonly bool ReportErrors;
	public readonly bool PerformCopy;
	public readonly bool DescribeFields;
	public readonly FN_FIELD_COMPARE? Func;

	public int ErrorCount;

	DataMap? CurrentMap;
	TypeDescription? WatchField;
	TypeDescription? CurrentField;
	ReadOnlySpan<char> CurrentClassName;


	public PredictionCopy(PredictionCopyType type, DataFrame dest, object src,
		bool countErrors = false, bool reportErrors = false, bool performCopy = true, bool describeFields = false, FN_FIELD_COMPARE? func = null) {
		Type = type;
		Dest_DataFrame = dest;
		Src_Object = src;
		Relationship = PredictionCopyRelationship.ObjectToDataFrame;

		ErrorCheck = countErrors;
		ReportErrors = reportErrors;
		PerformCopy = performCopy;
		DescribeFields = describeFields;
		Func = func;
	}

	public PredictionCopy(PredictionCopyType type, object dest, DataFrame src,
		bool countErrors = false, bool reportErrors = false, bool performCopy = true, bool describeFields = false, FN_FIELD_COMPARE? func = null) {
		Type = type;
		Dest_Object = dest;
		Src_DataFrame = src;
		Relationship = PredictionCopyRelationship.DataFrameToObject;

		ErrorCheck = countErrors;
		ReportErrors = reportErrors;
		PerformCopy = performCopy;
		DescribeFields = describeFields;
		Func = func;
	}

	public PredictionCopy(PredictionCopyType type, DataFrame dest, DataFrame src,
		bool countErrors = false, bool reportErrors = false, bool performCopy = true, bool describeFields = false, FN_FIELD_COMPARE? func = null) {
		Type = type;
		Dest_DataFrame = dest;
		Src_DataFrame = src;
		Relationship = PredictionCopyRelationship.DataFrameToDataFrame;

		ErrorCheck = countErrors;
		ReportErrors = reportErrors;
		PerformCopy = performCopy;
		DescribeFields = describeFields;
		Func = func;
	}

	public PredictionCopy(PredictionCopyType type, object dest, object src,
		bool countErrors = false, bool reportErrors = false, bool performCopy = true, bool describeFields = false, FN_FIELD_COMPARE? func = null) {
		Type = type;
		Dest_Object = dest;
		Src_Object = src;
		Relationship = PredictionCopyRelationship.ObjectToObject;

		ErrorCheck = countErrors;
		ReportErrors = reportErrors;
		PerformCopy = performCopy;
		DescribeFields = describeFields;
		Func = func;
	}



	static TypeDescription? FindFieldByName_R(ReadOnlySpan<char> fieldname, DataMap? dmap) {
		if (dmap == null)
			throw new NullReferenceException("Do not pass a null DataMap into FindFieldByName.");

		int c = dmap.DataNumFields;
		for (int i = 0; i < c; i++) {
			TypeDescription td = dmap.DataDesc[i];
			if (td.FieldType == FieldType.Void)
				continue;

			if (td.FieldType == FieldType.Embedded) {
				// TODO:  this will only find the first subclass with the variable of the specified name
				//  At some point we might want to support multiple levels of overriding automatically
				TypeDescription? ret = FindFieldByName_R(fieldname, td.TD!);
				if (ret != null)
					return ret;
			}

			if (0 == stricmp(td.FieldName, fieldname))
				return td;
		}

		if (dmap.BaseMap != null)
			return FindFieldByName_R(fieldname, dmap.BaseMap);

		return null;
	}
	public static TypeDescription? FindFieldByName(ReadOnlySpan<char> fieldname, DataMap? dmap) {
		return FindFieldByName_R(fieldname, dmap);
	}

	static int g_nChainCount = 1;
	public static void ValidateChains_R(DataMap dmap) {

	}

	bool ShouldReport;
	bool ShouldDescribe;

	public enum DiffType
	{
		Differs,
		Identical,
		WithinTolerance
	}

	public bool CanCheck() => (CurrentField!.Flags & FieldTypeDescFlags.NoErrorCheck) == 0;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	DiffType BASIC_COMPARE<T>(in PredictionIO output, in PredictionIO input, int count) where T : IEquatable<T> {
		if (!ErrorCheck) return DiffType.Differs;

		if (CanCheck()) {
			for (int i = 0; i < count; i++) {
				T? op = output.Get<T>(i);
				T? ip = input.Get<T>(i);
				if ((op == null && ip == null) || (op != null && op.Equals(ip)))
					continue;

				return DiffType.Differs;
			}
		}

		return DiffType.Identical;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	DiffType BASIC_COMPARE<T>(in PredictionIO output, in PredictionIO input, int count, COMPARE_FUNC<T> fn) {
		if (!ErrorCheck) return DiffType.Differs;

		if (CanCheck()) {
			for (int i = 0; i < count; i++) {
				T? op = output.Get<T>(i);
				T? ip = input.Get<T>(i);
				DiffType dt = fn(op, ip);
				if (dt == DiffType.Identical)
					continue;

				return DiffType.Differs;
			}
		}

		return DiffType.Identical;
	}

	delegate DiffType COMPARE_FUNC<T>(T? o, T? i);
	delegate DiffType COMPARE_FUNC_TOL<T>(bool usetolerance, float tolerance, T? o, T? i);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	DiffType BASIC_COMPARE_TOLERANCE<T>(in PredictionIO output, in PredictionIO input, int count, COMPARE_FUNC_TOL<T> fn) where T : IEquatable<T> {
		if (!ErrorCheck) return DiffType.Differs;

		DiffType retval = DiffType.Identical;
		if (CanCheck()) {
			float tolerance = CurrentField.FieldTolerance;
			Assert(tolerance >= 0.0f);
			bool usetolerance = tolerance > 0.0f;

			for (int i = 0; i < count; i++) {
				T? op = output.Get<T>(i);
				T? ip = input.Get<T>(i);
				if ((op == null && ip == null) || (op != null && op.Equals(ip)))
					continue;

				DiffType dt = fn(usetolerance, tolerance, op, ip);
				if (dt == DiffType.Identical)
					continue;
				else if (dt == DiffType.WithinTolerance) {
					retval = DiffType.WithinTolerance;
					continue;
				}

				return DiffType.Differs;
			}
		}

		return retval;
	}

	DiffType CompareShort(in PredictionIO output, in PredictionIO input, int count) => BASIC_COMPARE<short>(in output, in input, count);
	DiffType CompareInt(in PredictionIO output, in PredictionIO input, int count) => BASIC_COMPARE<int>(in output, in input, count);
	DiffType CompareByte(in PredictionIO output, in PredictionIO input, int count) => BASIC_COMPARE<byte>(in output, in input, count);
	DiffType CompareBool(in PredictionIO output, in PredictionIO input, int count) => BASIC_COMPARE<bool>(in output, in input, count);
	DiffType CompareFloat(in PredictionIO output, in PredictionIO input, int count) => BASIC_COMPARE_TOLERANCE<float>(in output, in input, count, static (usetolerance, tolerance, op, ip) => {
		if (usetolerance && MathF.Abs(op - ip) <= tolerance)
			return DiffType.WithinTolerance;
		return DiffType.Differs;
	});
	DiffType CompareString(in PredictionIO output, in PredictionIO input) {
		if (!ErrorCheck) return DiffType.Differs;

		int i = 0;
		if (CanCheck())
			while (true) {
				char oc = output.Get<char>(i);
				char ic = input.Get<char>(i);
				if (oc == '\0' || ic == '\0')
					return oc == ic ? DiffType.Identical : DiffType.Differs;
				if (oc != ic)
					return DiffType.Differs;
				i++;
			}

		return DiffType.Identical;
	}
	DiffType CompareVector(in PredictionIO output, in PredictionIO input) => CompareVector(output, input, 1);
	DiffType CompareVector(in PredictionIO output, in PredictionIO input, int count) => BASIC_COMPARE_TOLERANCE<Vector3>(in output, in input, count, static (usetolerance, tolerance, op, ip) => {
		var delta = op - ip;
		if (usetolerance && MathF.Abs(delta.X) <= tolerance && MathF.Abs(delta.Y) <= tolerance && MathF.Abs(delta.Z) <= tolerance)
			return DiffType.WithinTolerance;
		return DiffType.Differs;
	});
	DiffType CompareQuaternion(in PredictionIO output, in PredictionIO input) => CompareQuaternion(output, input, 1);
	DiffType CompareQuaternion(in PredictionIO output, in PredictionIO input, int count) => BASIC_COMPARE_TOLERANCE<Quaternion>(in output, in input, count, static (usetolerance, tolerance, op, ip) => {
		var delta = op - ip;
		if (usetolerance && MathF.Abs(delta.X) <= tolerance && MathF.Abs(delta.Y) <= tolerance && MathF.Abs(delta.Z) <= tolerance && MathF.Abs(delta.W) <= tolerance)
			return DiffType.WithinTolerance;
		return DiffType.Differs;
	});
	DiffType CompareColor(in PredictionIO output, in PredictionIO input, int count) => BASIC_COMPARE<Color>(in output, in input, count);
	DiffType CompareEHandle(in PredictionIO output, in PredictionIO input, int count) => BASIC_COMPARE<EHANDLE>(in output, in input, count, static (ov, iv) => ov.Get() == iv.Get() ? DiffType.Identical : DiffType.Differs);



	void CopyShort(DiffType dt, in PredictionIO output, in PredictionIO input, int count) {
		if (!PerformCopy) return;
		if (dt == DiffType.Identical) return;
		for (int i = 0; i < count; i++) output.Set<short>(input.Get<short>(i), i);
	}
	void CopyInt(DiffType dt, in PredictionIO output, in PredictionIO input, int count) {
		if (!PerformCopy) return;
		if (dt == DiffType.Identical) return;
		for (int i = 0; i < count; i++) output.Set<int>(input.Get<int>(i), i);
	}
	void CopyByte(DiffType dt, in PredictionIO output, in PredictionIO input, int count) {
		if (!PerformCopy) return;
		if (dt == DiffType.Identical) return;
		for (int i = 0; i < count; i++) output.Set<byte>(input.Get<byte>(i), i);
	}
	void CopyBool(DiffType dt, in PredictionIO output, in PredictionIO input, int count) {
		if (!PerformCopy) return;
		if (dt == DiffType.Identical) return;
		for (int i = 0; i < count; i++) output.Set<bool>(input.Get<bool>(i), i);
	}
	void CopyFloat(DiffType dt, in PredictionIO output, in PredictionIO input, int count) {
		if (!PerformCopy) return;
		if (dt == DiffType.Identical) return;
		for (int i = 0; i < count; i++) output.Set<float>(input.Get<float>(i), i);
	}
	void CopyString(DiffType dt, in PredictionIO output, in PredictionIO input) {
		if (!PerformCopy) return;
		if (dt == DiffType.Identical) return;

		int i = 0;
		while (true) {
			char ic = input.Get<char>(i);
			output.Set<char>(ic, i);
			if (ic == '\0')
				break;
			
			i++;
		}
	}
	void CopyVector(DiffType dt, in PredictionIO output, in PredictionIO input) => CopyVector(dt, output, input, 1);
	void CopyVector(DiffType dt, in PredictionIO output, in PredictionIO input, int count) {
		if (!PerformCopy) return;
		if (dt == DiffType.Identical) return;
		for (int i = 0; i < count; i++) output.Set<Vector3>(input.Get<Vector3>(i), i);
	}
	void CopyQuaternion(DiffType dt, in PredictionIO output, in PredictionIO input) => CopyQuaternion(dt, output, input, 1);
	void CopyQuaternion(DiffType dt, in PredictionIO output, in PredictionIO input, int count) {
		if (!PerformCopy) return;
		if (dt == DiffType.Identical) return;
		for (int i = 0; i < count; i++) output.Set<Quaternion>(input.Get<Quaternion>(i), i);
	}
	void CopyColor(DiffType dt, in PredictionIO output, in PredictionIO input, int count) {
		if (!PerformCopy) return;
		if (dt == DiffType.Identical) return;
		for (int i = 0; i < count; i++) output.Set<Color>(input.Get<Color>(i), i);
	}
	void CopyEHandle(DiffType dt, in PredictionIO output, in PredictionIO input, int count) {
		if (!PerformCopy) return;
		if (dt == DiffType.Identical) return;
		for (int i = 0; i < count; i++) {
			BaseHandle handle = output.Get<BaseHandle>(i);
			handle.Index = input.Get<BaseHandle>(i)!.Index;
			output.Set(handle, i);
		}
	}

	void CopyFields(int chaincount, DataMap pRootMap, TypeDescription[] pFields) {
		int i;
		FieldTypeDescFlags flags;
		int fieldCount = pFields.Length;
		int fieldSize;

		CurrentMap = pRootMap;
		if (CurrentClassName.IsEmpty)
			CurrentClassName = pRootMap.DataClassName;

		for (i = 0; i < fieldCount; i++) {
			CurrentField = pFields[i];
			flags = CurrentField.Flags;

			// Mark any subchains first
			if (CurrentField.OverrideField != null)
				CurrentField.OverrideField.OverrideCount = chaincount;

			// Skip this field?
			if (CurrentField.OverrideCount == chaincount)
				continue;

			// Always recurse into embeddeds
			if (CurrentField.FieldType != FieldType.Embedded) {
				// Don't copy fields that are private to server or client
				if ((flags & FieldTypeDescFlags.Private) != 0)
					continue;

				// For PC_NON_NETWORKED_ONLYs skip any fields that are present in the network send tables
				if (Type == PredictionCopyType.NonNetworkedOnly && (flags & FieldTypeDescFlags.InSendTable) != 0)
					continue;

				// For PC_NETWORKED_ONLYs skip any fields that are not present in the network send tables
				if (Type == PredictionCopyType.NetworkedOnly && (flags & FieldTypeDescFlags.InSendTable) == 0)
					continue;
			}

			PredictionIO pOutputData;
			PredictionIO pInputData;

			switch (Relationship) {
				case PredictionCopyRelationship.DataFrameToObject:
					pInputData = PredictionIO.FromDataMap(CurrentField, Src_DataFrame);
					pOutputData = PredictionIO.FromDynamicAccessor(CurrentField.FieldAccessor, Dest_Object!);
					break;
				case PredictionCopyRelationship.ObjectToDataFrame:
					pInputData = PredictionIO.FromDynamicAccessor(CurrentField.FieldAccessor, Src_Object!);
					pOutputData = PredictionIO.FromDataMap(CurrentField, Dest_DataFrame!);
					break;
				case PredictionCopyRelationship.DataFrameToDataFrame:
					pInputData = PredictionIO.FromDataMap(CurrentField, Src_DataFrame);
					pOutputData = PredictionIO.FromDataMap(CurrentField, Dest_DataFrame!);
					break;
				case PredictionCopyRelationship.ObjectToObject:
					pInputData = PredictionIO.FromDynamicAccessor(CurrentField.FieldAccessor, Src_Object!);
					pOutputData = PredictionIO.FromDynamicAccessor(CurrentField.FieldAccessor, Dest_Object!);
					break;
				default:
					throw new Exception();
			}

			// Assume we can report
			ShouldReport = ReportErrors;
			ShouldDescribe = true;
			fieldSize = CurrentField.FieldSize;

			bool bShouldWatch = WatchField == CurrentField;

			DiffType difftype;

			switch (CurrentField.FieldType) {
				case FieldType.Embedded: {
						// todo
					}
					break;
				case FieldType.Float: {
						difftype = CompareFloat(pOutputData, pInputData, fieldSize);
						CopyFloat(difftype, pOutputData, pInputData, fieldSize);
						// if (ErrorCheck && ShouldDescribe) DescribeFloat(difftype, pOutputData, pInputData, fieldSize);
						// if (bShouldWatch) WatchFloat(difftype, pOutputData, pInputData, fieldSize);
					}
					break;

				case FieldType.Time:
				case FieldType.Tick:
					Assert(false);
					break;

				case FieldType.String: {
						difftype = CompareString(pOutputData, pInputData);
						CopyString(difftype, pOutputData, pInputData);
						// if (ErrorCheck && ShouldDescribe) DescribeString(difftype, pOutputData, pInputData);
						// if (bShouldWatch) WatchString(difftype, pOutputData, pInputData);
					}
					break;

				case FieldType.ModelIndex:
				case FieldType.ModelName:
				case FieldType.SoundName:
				case FieldType.Custom:
				case FieldType.ClassPtr:
				case FieldType.EDict:
				case FieldType.PositionVector:
					Assert(false);
					break;

				case FieldType.Vector: {
						difftype = CompareVector(pOutputData, pInputData, fieldSize);
						CopyVector(difftype, pOutputData, pInputData, fieldSize);
						// if (ErrorCheck && ShouldDescribe) DescribeVector(difftype, pOutputData, pInputData, fieldSize);
						// if (bShouldWatch) WatchVector(difftype, pOutputData, pInputData, fieldSize);
					}
					break;

				case FieldType.Quaternion: {
						difftype = CompareQuaternion(pOutputData, pInputData, fieldSize);
						CopyQuaternion(difftype, pOutputData, pInputData, fieldSize);
						// if (ErrorCheck && ShouldDescribe) DescribeQuaternion(difftype, pOutputData, pInputData, fieldSize);
						// if (bShouldWatch) WatchQuaternion(difftype, pOutputData, pInputData, fieldSize);
					}
					break;

				case FieldType.Color32: {
						difftype = CompareColor(pOutputData, pInputData, fieldSize);
						CopyColor(difftype, pOutputData, pInputData, fieldSize);
						// if (ErrorCheck && ShouldDescribe) DescribeData(difftype, 4 * fieldSize, pOutputData, pInputData);
						// if (bShouldWatch) WatchData(difftype, 4 * fieldSize, pOutputData, pInputData);
					}
					break;

				case FieldType.Boolean: {
						difftype = CompareBool(pOutputData, pInputData, fieldSize);
						CopyBool(difftype, pOutputData, pInputData, fieldSize);
						// if (ErrorCheck && ShouldDescribe) DescribeBool(difftype, pOutputData, pInputData, fieldSize);
						// if (bShouldWatch) WatchBool(difftype, pOutputData, pInputData, fieldSize);
					}
					break;

				case FieldType.Integer: {
						difftype = CompareInt(pOutputData, pInputData, fieldSize);
						CopyInt(difftype, pOutputData, pInputData, fieldSize);
						// if (ErrorCheck && ShouldDescribe) DescribeInt(difftype, pOutputData, pInputData, fieldSize);
						// if (bShouldWatch) WatchInt(difftype, pOutputData, pInputData, fieldSize);
					}
					break;

				case FieldType.Short: {
						difftype = CompareShort(pOutputData, pInputData, fieldSize);
						CopyShort(difftype, pOutputData, pInputData, fieldSize);
						// if (ErrorCheck && ShouldDescribe) DescribeShort(difftype, pOutputData, pInputData, fieldSize);
						// if (bShouldWatch) WatchShort(difftype, pOutputData, pInputData, fieldSize);
					}
					break;

				case FieldType.Character: {
						difftype = CompareByte(pOutputData, pInputData, fieldSize);
						CopyByte(difftype, pOutputData, pInputData, fieldSize);
						// if (ErrorCheck && ShouldDescribe) DescribeInt(difftype, &valOut, &valIn, fieldSize);
						// if (bShouldWatch) WatchData(difftype, fieldSize, (pOutputData), pInputData);
					}
					break;
				case FieldType.EHandle: {
						difftype = CompareEHandle(pOutputData, pInputData, fieldSize);
						CopyEHandle(difftype, pOutputData, pInputData, fieldSize);
						// if (ErrorCheck && ShouldDescribe) DescribeEHandle(difftype, pOutputData, pInputData, fieldSize);
						// if (bShouldWatch) WatchEHandle(difftype, pOutputData, pInputData, fieldSize);
					}
					break;
				case FieldType.Function: {
						Assert(false);
					}
					break;
				case FieldType.Void: {
						// Don't do anything, it's an empty data description
					}
					break;
				default: {
						Warning("Bad field type\n");
						Assert(false);
					}
					break;
			}
		}

		CurrentClassName = null;
	}

	void TransferData_R(int chaincount, DataMap dmap) {
		CopyFields(chaincount, dmap, dmap.DataDesc);

		if (dmap.BaseMap != null) {
			TransferData_R(chaincount, dmap.BaseMap);
		}
	}

	public int TransferData(scoped ReadOnlySpan<char> operation, int entindex, DataMap? dmap) {
		Assert(dmap != null);
		++g_nChainCount;

		if (!dmap.ChainsValidated)
			ValidateChains_R(dmap);

		TransferData_R(g_nChainCount, dmap);

		return ErrorCount;
	}
}
