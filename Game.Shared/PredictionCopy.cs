global using static Game.Shared.PredictionCopy;

using Source;
using Source.Common;

using System;
using System.Collections.Generic;
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
	public readonly bool CountErrors;
	public readonly bool ReportErrors;
	public readonly bool PerformCopy;
	public readonly bool DescribeFields;
	public readonly FN_FIELD_COMPARE? Func;


	public PredictionCopy(PredictionCopyType type, DataFrame dest, object src,
		bool countErrors = false, bool reportErrors = false, bool performCopy = true, bool describeFields = false, FN_FIELD_COMPARE? func = null) {
		Type = type;
		Dest_DataFrame = dest;
		Src_Object = src;
		Relationship = PredictionCopyRelationship.ObjectToDataFrame;

		CountErrors = countErrors;
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

		CountErrors = countErrors;
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

		CountErrors = countErrors;
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

		CountErrors = countErrors;
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
}
