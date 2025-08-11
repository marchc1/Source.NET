using Source.Common.Mathematics;
using System.Runtime.InteropServices;

namespace Source.Common.Networking.DataTable;

public static class NetConstants
{
	public const int MAX_DATATABLES = 1024;
	public const int MAX_DATATABLE_PROPS = 4096;
	public const int MAX_ARRAY_ELEMENTS = 2048;

	public const float HIGH_DEFAULT = -121121.121121f;

	public const int BITS_FULLRES = -1;
	public const int BITS_WORLDCOORD = -2;

	public const int DT_MAX_STRING_BITS = 9;
	public const int DT_MAX_STRING_BUFFERSIZE = 1 << DT_MAX_STRING_BITS;

	public const int SIZEOF_IGNORE = -1;

	// Flags (SPROP)
	public const int SPROP_UNSIGNED = 1 << 0;
	public const int SPROP_COORD = 1 << 1;
	public const int SPROP_NOSCALE = 1 << 2;
	public const int SPROP_ROUNDDOWN = 1 << 3;
	public const int SPROP_ROUNDUP = 1 << 4;
	public const int SPROP_NORMAL = 1 << 5;
	public const int SPROP_EXCLUDE = 1 << 6;
	public const int SPROP_XYZE = 1 << 7;
	public const int SPROP_INSIDEARRAY = 1 << 8;
	public const int SPROP_PROXY_ALWAYS_YES = 1 << 9;
	public const int SPROP_CHANGES_OFTEN = 1 << 10;
	public const int SPROP_IS_A_VECTOR_ELEM = 1 << 11;
	public const int SPROP_COLLAPSIBLE = 1 << 12;
	public const int SPROP_COORD_MP = 1 << 13;
	public const int SPROP_COORD_MP_LOWPRECISION = 1 << 14;
	public const int SPROP_COORD_MP_INTEGRAL = 1 << 15;

	public const int SPROP_ENCODED_AGAINST_TICKCOUNT = 1 << 16;

	public const int SPROP_VARINT = SPROP_NORMAL;

	public const int SPROP_NUMFLAGBITS_NETWORKED = 16;
	public const int SPROP_NUMFLAGBITS = 17;


	public const int COORD_INTEGER_BITS = 14;
	public const int COORD_FRACTIONAL_BITS = 5;
	public const int COORD_DENOMINATOR = (1<<(COORD_FRACTIONAL_BITS));
	public const float COORD_RESOLUTION = (1.0f/(COORD_DENOMINATOR));
	public const int COORD_INTEGER_BITS_MP = 11;
	public const int COORD_FRACTIONAL_BITS_MP_LOWPRECISION = 3;
	public const int COORD_DENOMINATOR_LOWPRECISION = (1<<(COORD_FRACTIONAL_BITS_MP_LOWPRECISION));
	public const float COORD_RESOLUTION_LOWPRECISION = (1.0f/(COORD_DENOMINATOR_LOWPRECISION));
	public const int NORMAL_FRACTIONAL_BITS = 11;
	public const int NORMAL_DENOMINATOR = ( (1<<(NORMAL_FRACTIONAL_BITS)) - 1 );
	public const float NORMAL_RESOLUTION = (1.0f/(NORMAL_DENOMINATOR));
}

public enum SendPropType
{
	Int = 0,
	Float,
	Vector,
	VectorXY,
	String,
	Array,
	DataTable,
	GModDataTable,
	NUMSendPropTypes
}

public struct DVariant
{
	public SendPropType Type;

	public int IntValue;
	public float FloatValue;
	public Vector VectorValue;
	public string StringValue;
	public byte[] DataValue;

	private DVariant(
		SendPropType type,
		int i = default,
		float f = default,
		Vector v = default,
		string s = null,
		byte[] data = null)
	{
		Type = type;
		IntValue = i;
		FloatValue = f;
		VectorValue = v;
		StringValue = s;
		DataValue = data;
	}

	public static DVariant FromInt(int value) =>
		new DVariant(SendPropType.Int, i: value);

	public static DVariant FromFloat(float value) =>
		new DVariant(SendPropType.Float, f: value);

	public static DVariant FromVector(Vector value) =>
		new DVariant(SendPropType.Vector, v: value);

	public static DVariant FromVectorXY(float x, float y) =>
		new DVariant(SendPropType.VectorXY, v: new Vector(x, y, 0f));

	public static DVariant FromString(string value) =>
		new DVariant(SendPropType.String, s: value);

	public static DVariant FromData(byte[] value) =>
		new DVariant(SendPropType.DataTable, data: value);

	public string ToReadableString()
	{
		return Type switch
		{
			SendPropType.Int          => IntValue.ToString(),
			SendPropType.Float        => FloatValue.ToString("F3"),
			SendPropType.Vector       => $"({VectorValue.X:F3},{VectorValue.Y:F3},{VectorValue.Z:F3})",
			SendPropType.VectorXY     => $"({VectorValue.X:F3},{VectorValue.Y:F3})",
			SendPropType.String       => StringValue ?? "NULL",
			SendPropType.Array        => "Array",
			SendPropType.DataTable    => "DataTable",
			SendPropType.GModDataTable=> "GMODTable",
			_                         => $"Unknown type ({(int)Type})"
		};
	}
}

public static class NetUtils
{
	public static int NumBitsForCount(int nMaxElements)
	{
		int nBits = 0;
		while (nMaxElements > 0)
		{
			nBits++;
			nMaxElements >>= 1;
		}
		return nBits;
	}
}