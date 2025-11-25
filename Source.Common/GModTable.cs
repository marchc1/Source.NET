#if GMOD_DLL
using Source.Common.Bitbuffers;
using Source.Common.Mathematics;

using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Source.Common;

public struct GModVariant {
	public int Int;
	public float Float;
	public Vector3 Vector;
	public QAngle Angle;
	public string? String;
	public BaseHandle? Handle;
	public void Clear() {
		Int = default;
		Float = default;
		Vector = default;
		String = default;
	}

	public static implicit operator int(GModVariant v) => v.Int;
	public static implicit operator float(GModVariant v) => v.Float;
	public static implicit operator Vector3(GModVariant v) => v.Vector;
	public static implicit operator QAngle(GModVariant v) => v.Angle;
	public static implicit operator string?(GModVariant v) => v.String;
}

public delegate void GModReadFn(bf_read buf, ref GModVariant dvariant);
public delegate bool GModCompareFn(bf_read buf1, bf_read buf2);
public delegate void GModSkipFn(bf_read buf);

public enum GModTableType {
	Invalid,
	Float,
	Int,
	Bool,
	Vector,
	Angle,
	Entity,
	String
}

public struct GmodTableTypeFns {
	public static readonly GmodTableTypeFns Empty = new(
		(bf_read _, ref GModVariant _) => Warning("Attempted to call a Read function with an invalid GModTableType!\n"), 
		(_, _) => { Warning("Attempted to call a Compare function with an invalid GModTableType!\n"); return false; }, 
		(_) => Warning("Attempted to call a Skip function with an invalid GModTableType!\n")
	);

	public GModReadFn Read;
	public GModCompareFn Compare;
	public GModSkipFn Skip;
	public GmodTableTypeFns(GModReadFn read, GModCompareFn compare, GModSkipFn skip) {
		Read = read;
		Compare = compare;
		Skip = skip;
	}

	public static readonly GmodTableTypeFns[] Fns = [
		new(Float_Read, Float_Compare, Float_Skip),
		new(Int_Read, Int_Compare, Int_Skip),
		new(Bool_Read, Bool_Compare, Bool_Skip),
		new(Vector_Read, Vector_Compare, Vector_Skip),
		new(Angle_Read, Angle_Compare, Angle_Skip),
		new(Entity_Read, Entity_Compare, Entity_Skip),
		new(String_Read, String_Compare, String_Skip),
	];

	public static ref readonly GmodTableTypeFns Get(GModTableType type)
		=> ref Get((int)type);
	public static ref readonly GmodTableTypeFns Get(int type) {
		int ptr = type - 1;
		if (ptr < 0 || ptr >= Fns.Length)
			return ref Empty;

		return ref Fns[ptr];
	}

	static void Float_Read(bf_read buf, ref GModVariant dvariant) => dvariant.Float = buf.ReadBitFloat();
	static bool Float_Compare(bf_read buf1, bf_read buf2) => buf1.ReadBitFloat() != buf2.ReadBitFloat();
	static void Float_Skip(bf_read buf) => buf.SeekRelative(32);

	static void Int_Read(bf_read buf, ref GModVariant dvariant) => dvariant.Int = (int)buf.ReadUBitLong(32);
	static bool Int_Compare(bf_read buf1, bf_read buf2) => buf1.ReadUBitLong(32) != buf2.ReadUBitLong(32);
	static void Int_Skip(bf_read buf) => buf.SeekRelative(32);

	static void Bool_Read(bf_read buf, ref GModVariant dvariant) => dvariant.Int = buf.ReadOneBit();
	static bool Bool_Compare(bf_read buf1, bf_read buf2) => buf1.ReadBool() != buf2.ReadBool();
	static void Bool_Skip(bf_read buf) => buf.SeekRelative(1);

	static void Vector_Read(bf_read buf, ref GModVariant dvariant) => dvariant.Vector = new(buf.ReadBitFloat(), buf.ReadBitFloat(), buf.ReadBitFloat());
	static bool Vector_Compare(bf_read buf1, bf_read buf2) {
		float x1 = buf1.ReadBitFloat(), y1 = buf1.ReadBitFloat(), z1 = buf1.ReadBitFloat();
		float x2 = buf2.ReadBitFloat(), y2 = buf2.ReadBitFloat(), z2 = buf2.ReadBitFloat();
		return x1 != x2 || y1 != y2 || z1 != z2;
	}
	static void Vector_Skip(bf_read buf) => buf.SeekRelative(32 * 3);

	static void Angle_Read(bf_read buf, ref GModVariant dvariant) => dvariant.Angle = new(buf.ReadBitFloat(), buf.ReadBitFloat(), buf.ReadBitFloat());
	static bool Angle_Compare(bf_read buf1, bf_read buf2) => Vector_Compare(buf1, buf2);
	static void Angle_Skip(bf_read buf) => Vector_Skip(buf);

	static void Entity_Read(bf_read buf, ref GModVariant dvariant) {
		uint val = buf.ReadUBitLong(Constants.NUM_NETWORKED_EHANDLE_BITS);

		if(val != Constants.INVALID_NETWORKED_EHANDLE_VALUE) {
			uint entity = val & ((1 << Constants.MAX_EDICT_BITS) - 1);
			uint serialNum = val >> Constants.MAX_EDICT_BITS;
			(dvariant.Handle ??= new()).Init((int)entity, (int)serialNum);
		}
	}
	static bool Entity_Compare(bf_read buf1, bf_read buf2) => buf1.ReadUBitLong(Constants.NUM_NETWORKED_EHANDLE_BITS) != buf2.ReadUBitLong(Constants.NUM_NETWORKED_EHANDLE_BITS);
	static void Entity_Skip(bf_read buf) => buf.SeekRelative(Constants.NUM_NETWORKED_EHANDLE_BITS);

	static void String_Read(bf_read buf, ref GModVariant dvariant) {
		int len = (int)buf.ReadUBitLong(Constants.DT_MAX_STRING_BITS);
		Span<char> data = stackalloc char[len];
		buf.ReadString(data);
		dvariant.String = new(data);
	}
	static bool String_Compare(bf_read buf1, bf_read buf2) {
		int len1 = (int)buf1.ReadUBitLong(Constants.DT_MAX_STRING_BITS);
		int len2 = (int)buf2.ReadUBitLong(Constants.DT_MAX_STRING_BITS);

		if(len1 == len2) {
			if (len1 == 0)
				return false;

			Span<char> data1 = stackalloc char[len1];
			Span<char> data2 = stackalloc char[len2];

			buf1.ReadString(data1);
			buf2.ReadString(data2);

			return !((ReadOnlySpan<char>)data1).Equals(data2, StringComparison.Ordinal);
		}
		else {
			buf1.SeekRelative(len1 * 8);
			buf1.SeekRelative(len2 * 8);
			return true;
		}
	}
	static void String_Skip(bf_read buf) => buf.SeekRelative((int)buf.ReadUBitLong(Constants.DT_MAX_STRING_BITS));
}

public class GModTable {
	/// <summary>
	/// Never used but can freely be edited.
	/// </summary>
	public static GModVariant Empty;

	// This sucks. But I need this function, and its internal to a Dictionary<TKey, TValue>...
	static MethodInfo FindValueMethod = typeof(Dictionary<int, GModVariant>).GetMethod("FindValue", (BindingFlags)~0)!;
	delegate ref GModVariant GetRefVariant(int key);
	readonly Dictionary<int, GModVariant> Values;
	readonly GetRefVariant FindValue;

	public GModTable() {
		Values = [];
		FindValue = FindValueMethod.CreateDelegate<GetRefVariant>(Values);
	}

	public const int ENTRIES_BITS = 12;
	public const int MAX_ENTRIES= 1 << ENTRIES_BITS;

	public const int ENTRY_KEY_BITS = 12;
	public const int MAX_ENTRY_KEYS = 1 << ENTRY_KEY_BITS;

	public const int ENTRY_VALUE_TYPE_BITS = 3;
	public const int MAX_VALUE_TYPE = 1 << ENTRY_VALUE_TYPE_BITS;

	public static readonly string WARNING_UNDERFLOW = "Attempted to index below 0 on a GModTable!\n";
	public static readonly string WARNING_OVERFLOW = "Attempted to index above " + (MAX_ENTRY_KEYS - 1) + " on a GModTable!\n";

	public ref GModVariant this[int index] {
		get {
			if(index < 0) {
				DevWarning(WARNING_UNDERFLOW);
				return ref Empty;
			}

			if(index >= MAX_ENTRY_KEYS) {
				DevWarning(WARNING_OVERFLOW);
				return ref Empty;
			}

			ref GModVariant valRef = ref FindValue(index);
			if (!Unsafe.IsNullRef(ref valRef)) 
				return ref valRef;
			else {
				Values[index] = new();
				return ref FindValue(index);
			}
		}
	}
	public void Clear() {
		Values.Clear();
	}
}
#endif
