global using static Game.Shared.PredictionCopy;
#if CLIENT_DLL
using Game.Client;
#endif


using Source;
using Source.Common;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Text;

using static System.Runtime.InteropServices.JavaScript.JSType;

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

public delegate void FN_FIELD_COMPARE(ReadOnlySpan<char> classname, ReadOnlySpan<char> fieldname, ReadOnlySpan<char> fieldtype, bool networked, bool noterrorchecked, bool differs, bool withintolerance, ReadOnlySpan<char> value);

public ref struct PredictionIO
{
	IDynamicAccessor? dynaccess;
	object? dynowner;

	TypeDescription? typedesc;
	DataFrame? dataowner;

	public readonly bool UsesDynAccessor() => dynaccess != null;
	public readonly bool UsesDataMap() => typedesc != null;

	public readonly void GetDynAccessorVars(out IDynamicAccessor? dynaccess, out object? dynowner) {
		dynaccess = this.dynaccess;
		dynowner = this.dynowner;
	}

	public readonly void GetDataMapVars(out TypeDescription? typedesc, out DataFrame? dataowner) {
		typedesc = this.typedesc;
		dataowner = this.dataowner;
	}

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

public readonly ref struct EmbeddedSaveState
{
	public readonly TypeDescription? CurrentField;
	public readonly DataFrame Dest_DataFrame;
	public readonly DataFrame Src_DataFrame;
	public readonly object? Dest_Object;
	public readonly object? Src_Object;
	public readonly ReadOnlySpan<char> CurrentClassName;

	public EmbeddedSaveState(TypeDescription? field, DataFrame dest_dataframe, DataFrame src_dataframe, object? dest_object, object? src_object, ReadOnlySpan<char> classname) {
		CurrentField = field;
		Dest_DataFrame = dest_dataframe;
		Src_DataFrame = src_dataframe;
		Dest_Object = dest_object;
		Src_Object = src_object;
		CurrentClassName = classname;
	}
}

public ref struct PredictionCopy
{
	public const bool PC_DATA_NORMAL = false;
	public const bool PC_DATA_PACKED = true;

	public void LoadState(in EmbeddedSaveState state) {
		CurrentField = state.CurrentField;
		Dest_DataFrame = state.Dest_DataFrame;
		Src_DataFrame = state.Src_DataFrame;
		Dest_Object = state.Dest_Object;
		Src_Object = state.Src_Object;
		CurrentClassName = state.CurrentClassName;
	}
	public EmbeddedSaveState SaveState() {
		return new(CurrentField, Dest_DataFrame, Src_DataFrame, Dest_Object, Src_Object, CurrentClassName);
	}

	public readonly PredictionCopyType Type;
	public DataFrame Dest_DataFrame;
	public DataFrame Src_DataFrame;
	public object? Dest_Object;
	public object? Src_Object;
	public readonly PredictionCopyRelationship Relationship;
	public readonly bool ErrorCheck;
	public readonly bool ReportErrors;
	public readonly bool PerformCopy;
	public readonly bool ShouldDescribeFields;
	public readonly FN_FIELD_COMPARE? FieldCompareFunc;

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
		ShouldDescribeFields = describeFields;
		FieldCompareFunc = func;
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
		ShouldDescribeFields = describeFields;
		FieldCompareFunc = func;
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
		ShouldDescribeFields = describeFields;
		FieldCompareFunc = func;
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
		ShouldDescribeFields = describeFields;
		FieldCompareFunc = func;
	}

	public void DescribeFields(DiffType dt, ReadOnlySpan<char> txt) {
		if (!ShouldDescribe)
			return;

		if (FieldCompareFunc == null)
			return;

		Assert(CurrentMap != null);
		Assert(!CurrentClassName.IsEmpty);

		ReadOnlySpan<char> fieldname = "empty";
		FieldTypeDescFlags flags = 0;

		if (CurrentField != null) {
			flags = CurrentField.Flags;
			fieldname = CurrentField.FieldName != null ? CurrentField.FieldName : "NULL";
		}

		bool isnetworked = (flags & FieldTypeDescFlags.InSendTable) != 0;
		bool isnoterrorchecked = (flags & FieldTypeDescFlags.NoErrorCheck) != 0;

		FieldCompareFunc(
			CurrentClassName,
			fieldname,
			CurrentField?.FieldType.ToString(),
			isnetworked,
			isnoterrorchecked,
			dt != DiffType.Identical,
			dt == DiffType.WithinTolerance,
			txt
		);

		ShouldDescribe = false;
	}
	public void ReportFieldsDiffer(ReadOnlySpan<char> txt) {
		++ErrorCount;

		if (!ShouldReport)
			return;

		if (ShouldDescribeFields && FieldCompareFunc != null)
			return;

		Assert(CurrentMap != null);
		Assert(!CurrentClassName.IsEmpty);

		ReadOnlySpan<char> fieldname = "empty";
		FieldTypeDescFlags flags = 0;

		if (CurrentField != null) {
			flags = CurrentField.Flags;
			fieldname = CurrentField.FieldName != null ? CurrentField.FieldName : "NULL";
		}

		if (ErrorCount == 1)
			Msg("\n");

		Msg($"{ErrorCount} {CurrentClassName}::{fieldname} - {txt}");
		ShouldReport = false;
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
	delegate DiffType COMPARE_FUNC_TOL<T>(bool usetolerance, double tolerance, T? o, T? i);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	DiffType BASIC_COMPARE_TOLERANCE<T>(in PredictionIO output, in PredictionIO input, int count, COMPARE_FUNC_TOL<T> fn) where T : IEquatable<T> {
		if (!ErrorCheck) return DiffType.Differs;

		DiffType retval = DiffType.Identical;
		if (CanCheck()) {
			double tolerance = CurrentField!.FieldTolerance;
			Assert(tolerance >= 0.0);
			bool usetolerance = tolerance > 0.0;

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
	DiffType CompareChar(in PredictionIO output, in PredictionIO input, int count) => BASIC_COMPARE<char>(in output, in input, count);
	DiffType CompareBool(in PredictionIO output, in PredictionIO input, int count) => BASIC_COMPARE<bool>(in output, in input, count);
	DiffType CompareFloat(in PredictionIO output, in PredictionIO input, int count) => BASIC_COMPARE_TOLERANCE<float>(in output, in input, count, static (usetolerance, tolerance, op, ip) => {
		if (usetolerance && MathF.Abs(op - ip) <= tolerance)
			return DiffType.WithinTolerance;
		return DiffType.Differs;
	});
	DiffType CompareDouble(in PredictionIO output, in PredictionIO input, int count) => BASIC_COMPARE_TOLERANCE<double>(in output, in input, count, static (usetolerance, tolerance, op, ip) => {
		if (usetolerance && Math.Abs(op - ip) <= tolerance)
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



	void CopyChar(DiffType dt, in PredictionIO output, in PredictionIO input, int count) {
		if (!PerformCopy) return;
		if (dt == DiffType.Identical) return;
		for (int i = 0; i < count; i++) output.Set<char>(input.Get<char>(i), i);
	}
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
	void CopyDouble(DiffType dt, in PredictionIO output, in PredictionIO input, int count) {
		if (!PerformCopy) return;
		if (dt == DiffType.Identical) return;
		for (int i = 0; i < count; i++) output.Set<double>(input.Get<double>(i), i);
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

	public string Operation;

	public void WatchMsg(ReadOnlySpan<char> txt) {
#if CLIENT_DLL || GAME_DLL
		Msg($"{gpGlobals.TickCount} {Operation} {CurrentField?.FieldName} : {txt}\n");
#endif
	}

	public void DescribeData(DiffType dt, int size, PredictionIO outdata, PredictionIO indata) {
		if (!ErrorCheck) return;
		if (dt == DiffType.Differs) ReportFieldsDiffer($"binary data differes ({size} bytes)\n");
		DescribeFields(dt, $"binary ({size} bytes)\n");
	}

	public void WatchData(DiffType dt, int size, PredictionIO outdata, PredictionIO indata) {
		if (WatchField != CurrentField)
			return;
		WatchMsg($"binary ({size} bytes)");
	}

	public void DescribeShort(DiffType dt, PredictionIO outdata, PredictionIO indata, int size) {
		if (!ErrorCheck) return;
		int invalue = indata.Get<int>(0), outvalue = outdata.Get<int>(0);
		if (dt == DiffType.Differs) ReportFieldsDiffer($"short differs (net {invalue} pred {outvalue}) diff({outvalue - invalue})\n");
		DescribeFields(dt, $"short ({outvalue})\n");
	}

	public void WatchShort(DiffType dt, PredictionIO outdata, PredictionIO indata, int size) {
		if (WatchField != CurrentField)
			return;
		WatchMsg($"short ({outdata.Get<int>(0)})");
	}

	// TODO: Model indices for the int functions.
	public void DescribeInt(DiffType dt, PredictionIO outdata, PredictionIO indata, int size) {
		if (!ErrorCheck) return;
		int invalue = indata.Get<int>(0), outvalue = outdata.Get<int>(0);
		if (dt == DiffType.Differs) ReportFieldsDiffer($"int differs (net {invalue} pred {outvalue}) diff({outvalue - invalue})\n");
		DescribeFields(dt, $"int ({outvalue})\n");
	}

	public void WatchInt(DiffType dt, PredictionIO outdata, PredictionIO indata, int size) {
		if (WatchField != CurrentField)
			return;
		WatchMsg($"int ({outdata.Get<int>(0)})");
	}

	public void DescribeBool(DiffType dt, PredictionIO outdata, PredictionIO indata, int size) {
		if (!ErrorCheck) return;
		string invalue = indata.Get<bool>(0) ? "true" : "false", outvalue = outdata.Get<bool>(0) ? "true" : "false";
		if (dt == DiffType.Differs) ReportFieldsDiffer($"bool differs (net {invalue} pred {outvalue})\n");
		DescribeFields(dt, $"bool ({outvalue})\n");
	}

	public void WatchBool(DiffType dt, PredictionIO outdata, PredictionIO indata, int size) {
		if (WatchField != CurrentField)
			return;
		WatchMsg($"bool ({(outdata.Get<bool>(0) ? "true" : "false")})");
	}

	public void DescribeFloat(DiffType dt, PredictionIO outdata, PredictionIO indata, int size) {
		if (!ErrorCheck) return;
		float invalue = indata.Get<float>(0), outvalue = outdata.Get<float>(0);
		if (dt == DiffType.Differs) ReportFieldsDiffer($"float differs (net {invalue} pred {outvalue}) diff({outvalue - invalue})\n");
		DescribeFields(dt, $"float ({outvalue})\n");
	}

	public void WatchFloat(DiffType dt, PredictionIO outdata, PredictionIO indata, int size) {
		if (WatchField != CurrentField)
			return;
		WatchMsg($"float ({outdata.Get<float>(0)})");
	}

	public void DescribeDouble(DiffType dt, PredictionIO outdata, PredictionIO indata, int size) {
		if (!ErrorCheck) return;
		double invalue = indata.Get<double>(0), outvalue = outdata.Get<double>(0);
		if (dt == DiffType.Differs) ReportFieldsDiffer($"double differs (net {invalue} pred {outvalue}) diff({outvalue - invalue})\n");
		DescribeFields(dt, $"double ({outvalue})\n");
	}

	public void WatchDouble(DiffType dt, PredictionIO outdata, PredictionIO indata, int size) {
		if (WatchField != CurrentField)
			return;
		WatchMsg($"double ({outdata.Get<double>(0)})");
	}
	public static int GetPredictionIOStringLength(PredictionIO input) {
		int i = 0;
		while (true) {
			char ic = input.Get<char>(i);
			if (ic == '\0') break;
			i++;
		}
		return i;
	}
	public static Span<char> GetPredictionIOString(PredictionIO input, Span<char> buffer) {
		int i = 0;
		while (true) {
			char ic = input.Get<char>(i);
			if (ic == '\0') break;
			buffer[i] = ic;
			i++;
		}
		return buffer[..i];
	}
	public void DescribeString(DiffType dt, PredictionIO outstringd, PredictionIO instringd) {
		if (!ErrorCheck) return;

		ReadOnlySpan<char> outstring = GetPredictionIOString(outstringd, stackalloc char[GetPredictionIOStringLength(outstringd)]);
		ReadOnlySpan<char> instring = GetPredictionIOString(instringd, stackalloc char[GetPredictionIOStringLength(instringd)]);

		if (dt == DiffType.Differs) ReportFieldsDiffer($"string differs (net {instring} pred {outstring})\n");

		DescribeFields(dt, $"string ({outstring})\n");
	}

	public void WatchString(DiffType dt, PredictionIO outstringd, PredictionIO instringd) {
		if (WatchField != CurrentField)
			return;
		ReadOnlySpan<char> outstring = GetPredictionIOString(outstringd, stackalloc char[GetPredictionIOStringLength(outstringd)]);
		WatchMsg($"string ({outstring.SliceNullTerminatedString()})");
	}

	public void DescribeVector(DiffType dt, PredictionIO outdata, PredictionIO indata, int size) {
		if (!ErrorCheck) return;
		Vector3 outValue = outdata.Get<Vector3>(0);
		if (dt == DiffType.Differs) {
			int i = 0;
			Vector3 inValue = indata.Get<Vector3>(0);
			Vector3 delta = outValue - inValue;

			ReportFieldsDiffer($"vec[] differs (1st diff) (net {inValue.X} {inValue.Y} {inValue.Z} - pred {outValue.X} {outValue.Y} {outValue.Z}) delta({delta.X} {delta.Y} {delta.Z})\n");
		}

		DescribeFields(dt, $"vector ({outValue.X} {outValue.Y} {outValue.Z})\n");
	}

	public void WatchVector(DiffType dt, PredictionIO outdata, PredictionIO indata, int size) {
		if (WatchField != CurrentField)
			return;
		Vector3 outValue = outdata.Get<Vector3>(0);
		WatchMsg($"vector ({outValue.X} {outValue.Y} {outValue.Z})");
	}

	public void DescribeQuaternion(DiffType dt, PredictionIO outdata, PredictionIO indata, int size) {
		if (!ErrorCheck) return;
		Quaternion outValue = outdata.Get<Quaternion>(0);
		if (dt == DiffType.Differs) {
			int i = 0;
			Quaternion inValue = indata.Get<Quaternion>(0);
			Quaternion delta = outValue - inValue;

			ReportFieldsDiffer($"quaternion[] differs (1st diff) (net {inValue.X} {inValue.Y} {inValue.Z} {inValue.W} - pred {outValue.X} {outValue.Y} {outValue.Z} {outValue.Z}) delta({delta.X} {delta.Y} {delta.Z} {outValue.W})\n");
		}

		DescribeFields(dt, $"quaternion ({outValue.X} {outValue.Y} {outValue.Z} {outValue.W})\n");
	}

	public void WatchQuaternion(DiffType dt, PredictionIO outdata, PredictionIO indata, int size) {
		if (WatchField != CurrentField)
			return;
		Quaternion outValue = outdata.Get<Quaternion>(0);
		WatchMsg($"quaternion ({outValue.X} {outValue.Y} {outValue.Z} {outValue.W})");
	}

	public void DescribeEHandle(DiffType dt, PredictionIO outdata, PredictionIO indata, int size) {
		if (!ErrorCheck) return;
		EHANDLE invalue = indata.Get<EHANDLE>(0), outvalue = outdata.Get<EHANDLE>(0);

		if (dt == DiffType.Differs) {
			int i = 0;
			ReportFieldsDiffer($"EHandles differ (net) 0x{invalue.Index:X} (pred) 0x{outvalue.Index:X}\n");
		}

#if CLIENT_DLL
		C_BaseEntity? ent = outvalue.Get();
		if (ent != null) {
			ReadOnlySpan<char> classname = ent.GetClassname();
			if (classname.IsStringEmpty)
				classname = ent.GetType().Name;

			DescribeFields(dt, $"EHandle (0x{outvalue.Index:X}->{classname})");
		}
		else
			DescribeFields(dt, "EHandle (NULL)");

#else
		DescribeFields(dt, $"EHandle (0x{outvalue.Index:X})");
#endif
	}

	public void WatchEHandle(DiffType dt, PredictionIO outdata, PredictionIO indata, int size) {
		if (WatchField != CurrentField)
			return;
#if CLIENT_DLL
		C_BaseEntity? ent = outdata.Get<EHANDLE>(0).Get();
		if (ent != null) {
			ReadOnlySpan<char> classname = ent.GetClassname();
			if (classname.IsStringEmpty)
				classname = ent.GetType().Name;

			WatchMsg($"EHandle (0x{outdata.Get<EHANDLE>(0).Index:X}->{classname})");
		}
		else
			WatchMsg("EHandle (NULL)");
#else
		WatchMsg($"EHandle (0x{outdata.Get<EHANDLE>(0).Index:X})");
#endif
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
						var saveSrcDF = Src_DataFrame;
						var saveDestDF = Dest_DataFrame;
						var saveSrcObj = Src_Object;
						var saveDestObj = Dest_Object;
						var saveName = CurrentClassName;
						var saveField = CurrentField;

						CurrentClassName = CurrentField.TD!.DataClassName;

						switch (Relationship) {
							case PredictionCopyRelationship.DataFrameToObject:
								Src_DataFrame = Src_DataFrame.Get<DataFrame>(CurrentField);
								Dest_Object = CurrentField.FieldAccessor.GetValue<object>(Dest_Object!);
								break;
							case PredictionCopyRelationship.ObjectToDataFrame:
								Src_Object = CurrentField.FieldAccessor.GetValue<object>(Src_Object!);
								Dest_DataFrame = Dest_DataFrame.Get<DataFrame>(CurrentField);
								break;
							case PredictionCopyRelationship.DataFrameToDataFrame:
								Src_DataFrame = Src_DataFrame.Get<DataFrame>(CurrentField);
								Dest_DataFrame = Dest_DataFrame.Get<DataFrame>(CurrentField);
								break;
							case PredictionCopyRelationship.ObjectToObject:
								Src_Object = CurrentField.FieldAccessor.GetValue<object>(Src_Object!);
								Dest_Object = CurrentField.FieldAccessor.GetValue<object>(Dest_Object!);
								break;
						}

						CopyFields(chaincount, pRootMap, CurrentField.TD!.DataDesc);

						// Restore state
						Src_DataFrame = saveSrcDF;
						Dest_DataFrame = saveDestDF;
						Src_Object = saveSrcObj;
						Dest_Object = saveDestObj;
						CurrentClassName = saveName;
						CurrentField = saveField;
					}
					break;
				case FieldType.Float: {
						difftype = CompareFloat(pOutputData, pInputData, fieldSize);
						CopyFloat(difftype, pOutputData, pInputData, fieldSize);
						if (ErrorCheck && ShouldDescribe) DescribeFloat(difftype, pOutputData, pInputData, fieldSize);
						if (bShouldWatch) WatchFloat(difftype, pOutputData, pInputData, fieldSize);
					}
					break;
				case FieldType.Double: {
						difftype = CompareDouble(pOutputData, pInputData, fieldSize);
						CopyDouble(difftype, pOutputData, pInputData, fieldSize);
						if (ErrorCheck && ShouldDescribe) DescribeDouble(difftype, pOutputData, pInputData, fieldSize);
						if (bShouldWatch) WatchDouble(difftype, pOutputData, pInputData, fieldSize);
					}
					break;

				case FieldType.Time:
				case FieldType.Tick:
					Assert(false);
					break;

				case FieldType.String: {
						difftype = CompareString(pOutputData, pInputData);
						CopyString(difftype, pOutputData, pInputData);
						if (ErrorCheck && ShouldDescribe) DescribeString(difftype, pOutputData, pInputData);
						if (bShouldWatch) WatchString(difftype, pOutputData, pInputData);
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
						if (ErrorCheck && ShouldDescribe) DescribeVector(difftype, pOutputData, pInputData, fieldSize);
						if (bShouldWatch) WatchVector(difftype, pOutputData, pInputData, fieldSize);
					}
					break;

				case FieldType.Quaternion: {
						difftype = CompareQuaternion(pOutputData, pInputData, fieldSize);
						CopyQuaternion(difftype, pOutputData, pInputData, fieldSize);
						if (ErrorCheck && ShouldDescribe) DescribeQuaternion(difftype, pOutputData, pInputData, fieldSize);
						if (bShouldWatch) WatchQuaternion(difftype, pOutputData, pInputData, fieldSize);
					}
					break;

				case FieldType.Color32: {
						difftype = CompareColor(pOutputData, pInputData, fieldSize);
						CopyColor(difftype, pOutputData, pInputData, fieldSize);
						if (ErrorCheck && ShouldDescribe) DescribeData(difftype, 4 * fieldSize, pOutputData, pInputData);
						if (bShouldWatch) WatchData(difftype, 4 * fieldSize, pOutputData, pInputData);
					}
					break;

				case FieldType.Boolean: {
						difftype = CompareBool(pOutputData, pInputData, fieldSize);
						CopyBool(difftype, pOutputData, pInputData, fieldSize);
						if (ErrorCheck && ShouldDescribe) DescribeBool(difftype, pOutputData, pInputData, fieldSize);
						if (bShouldWatch) WatchBool(difftype, pOutputData, pInputData, fieldSize);
					}
					break;

				case FieldType.Integer: {
						difftype = CompareInt(pOutputData, pInputData, fieldSize);
						CopyInt(difftype, pOutputData, pInputData, fieldSize);
						if (ErrorCheck && ShouldDescribe) DescribeInt(difftype, pOutputData, pInputData, fieldSize);
						if (bShouldWatch) WatchInt(difftype, pOutputData, pInputData, fieldSize);
					}
					break;

				case FieldType.Short: {
						difftype = CompareShort(pOutputData, pInputData, fieldSize);
						CopyShort(difftype, pOutputData, pInputData, fieldSize);
						if (ErrorCheck && ShouldDescribe) DescribeShort(difftype, pOutputData, pInputData, fieldSize);
						if (bShouldWatch) WatchShort(difftype, pOutputData, pInputData, fieldSize);
					}
					break;

				case FieldType.Character: {
						difftype = CompareByte(pOutputData, pInputData, fieldSize);
						CopyByte(difftype, pOutputData, pInputData, fieldSize);

						if (ErrorCheck && ShouldDescribe) DescribeInt(difftype, pOutputData, pInputData, fieldSize);
						if (bShouldWatch) WatchData(difftype, fieldSize, (pOutputData), pInputData);
					}
					break;
				case FieldType.StringCharacter: {
						difftype = CompareChar(pOutputData, pInputData, fieldSize);
						CopyChar(difftype, pOutputData, pInputData, fieldSize);
						if (ErrorCheck && ShouldDescribe) DescribeInt(difftype, pOutputData, pInputData, fieldSize);
						if (bShouldWatch) WatchData(difftype, fieldSize, (pOutputData), pInputData);
					}
					break;
				case FieldType.EHandle: {
						difftype = CompareEHandle(pOutputData, pInputData, fieldSize);
						CopyEHandle(difftype, pOutputData, pInputData, fieldSize);
						if (ErrorCheck && ShouldDescribe) DescribeEHandle(difftype, pOutputData, pInputData, fieldSize);
						if (bShouldWatch) WatchEHandle(difftype, pOutputData, pInputData, fieldSize);
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
