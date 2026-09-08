using Source;
using Source.Common;

using System.Numerics;

namespace Game.Shared;

public struct Variant_t
{
	public bool BoolVal;
	public string? StrVal;
	public int IntVal;
	public float FloatVal;
	public Vector3 VecVal;
	public Color RgbaVal;
#if CLIENT_DLL || GAME_DLL
	public Handle<BaseEntity> EntVal;
#endif
	public FieldType fieldType;

	public Variant_t() {
		fieldType = Source.Common.FieldType.Void;
	}
	public readonly bool Bool() { return (fieldType == Source.Common.FieldType.Boolean) ? BoolVal : false; }
	public readonly ReadOnlySpan<char> String() { return (fieldType == Source.Common.FieldType.String) ? StrVal : ToString(); }
	public readonly int Int() { return (fieldType == Source.Common.FieldType.Integer) ? IntVal : 0; }
	public readonly float Float() { return (fieldType == Source.Common.FieldType.Float) ? FloatVal : 0; }
#if CLIENT_DLL || GAME_DLL
	public readonly Handle<BaseEntity> Entity() => EntVal;
#endif

	public readonly Color Color32() { return RgbaVal; }
	public readonly void Vector3D(out Vector3 vec) => vec = VecVal;

	FieldType FieldType() => fieldType;

	void SetBool(bool b) { BoolVal = b; fieldType = fieldType = Source.Common.FieldType.Boolean; }
	void SetString(ReadOnlySpan<char> str) { StrVal = new(str.SliceNullTerminatedString()); fieldType = fieldType = Source.Common.FieldType.String; }
	void SetInt(int val) { IntVal = val; fieldType = fieldType = Source.Common.FieldType.Integer; }
	void SetFloat(float val) { FloatVal = val; fieldType = fieldType = Source.Common.FieldType.Float; }
#if CLIENT_DLL || GAME_DLL
	void SetEntity(BaseEntity val) {
		fieldType = Source.Common.FieldType.EHandle;
		EntVal = new();
		EntVal.Set(val);
	}
#endif
	void SetVector3D(in Vector3 val) { VecVal[0] = val[0]; VecVal[1] = val[1]; VecVal[2] = val[2]; fieldType = Source.Common.FieldType.Vector; }
	void SetPositionVector3D(in Vector3 val) { VecVal[0] = val[0]; VecVal[1] = val[1]; VecVal[2] = val[2]; fieldType = Source.Common.FieldType.PositionVector; }
	void SetColor32(Color val) { RgbaVal = val; fieldType = Source.Common.FieldType.Color32; }
	void SetColor32(int r, int g, int b, int a) { RgbaVal.R = (byte)r; RgbaVal.G = (byte)g; RgbaVal.B = (byte)b; RgbaVal.A = (byte)a; fieldType = Source.Common.FieldType.Color32; }
	// todo: void Set(FieldType ftype, void* data);
	// todo: void SetOther(void* data);
	public bool Convert(FieldType newType) {
		throw new NotImplementedException();
	}

	public static readonly TypeDescription SaveBool = DEFINE<Variant_t>.FIELD(nameof(BoolVal), Source.Common.FieldType.Boolean);
	public static readonly TypeDescription SaveInt = DEFINE<Variant_t>.FIELD(nameof(IntVal), Source.Common.FieldType.Integer);
	public static readonly TypeDescription SaveFloat = DEFINE<Variant_t>.FIELD(nameof(FloatVal), Source.Common.FieldType.Float);
#if CLIENT_DLL || GAME_DLL
	public static readonly TypeDescription SaveEHandle = DEFINE<Variant_t>.FIELD(nameof(EntVal), Source.Common.FieldType.EHandle);
#endif
	public static readonly TypeDescription SaveString = DEFINE<Variant_t>.FIELD(nameof(StrVal), Source.Common.FieldType.String);
	public static readonly TypeDescription SaveColor = DEFINE<Variant_t>.FIELD(nameof(RgbaVal), Source.Common.FieldType.Color32);
	public static readonly TypeDescription SaveVector = DEFINE<Variant_t>.FIELD(nameof(VecVal), Source.Common.FieldType.Vector);
	public static readonly TypeDescription SavePositionVector = DEFINE<Variant_t>.FIELD(nameof(VecVal), Source.Common.FieldType.PositionVector);
}
