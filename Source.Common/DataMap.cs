using SharpCompress.Common;

using Source.Common;

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Source.Common
{

	public enum FieldType
	{
		Void = 0,         // No type or value
		Float,            // Any floating point value
		String,           // A string ID (return from ALLOC_STRING)
		Vector,           // Any vector, QAngle, or AngularImpulse
		Quaternion,       // A quaternion
		Integer,          // Any integer or enum
		Boolean,          // boolean, implemented as an int, I may use this as a hint for compression
		Short,            // 2 byte integer
		Character,        // a byte
		Color32,          // 8-bit per channel r,g,b,a (32bit color)
		Embedded,         // an embedded object with a datadesc, recursively traverse and embedded class/structure based on an additional typedescription
		Custom,           // special type that contains function pointers to it's read/write/parse functions

		ClassPtr,         // CBaseEntity *
		EHandle,          // Entity handle
		EDict,            // edict_t *

		PositionVector,  // A world coordinate (these are fixed up across level transitions automagically)
		Time,             // a floating point time (these are fixed up automatically too!)
		Tick,             // an integer tick count( fixed up similarly to time)
		ModelName,        // Engine string that is a model name (needs precache)
		SoundName,        // Engine string that is a sound name (needs precache)

		Input,            // a list of inputed data fields (all derived from CMultiInputVar)
		Function,         // A class function pointer (Think, Use, etc)

		Matrix,          // a vmatrix (output coords are NOT worldspace)

		// NOTE: Use float arrays for local transformations that don't need to be fixed up.
		WorldspaceMatrix,// A VMatrix that maps some local space to world space (translation is fixed up on level transitions)
		WorldspaceMatrix3x4, // matrix3x4_t that maps some local space to world space (translation is fixed up on level transitions)

		Interval,         // a start and range floating point interval ( e.g., 3.2->3.6 == 3.2 and 0.4 )
		ModelIndex,       // a model index
		MaterialIndex,    // a material index (using the material precache string table)

		Vector2D,         // 2 floats

		Count,        // MUST BE LAST
	}

	public enum TypeDescriptionOffset
	{
		Normal,
		Packed,
		Count
	}

	public class TypeDescription
	{
		public readonly FieldType FieldType;
		public readonly string FieldName = "";
		public readonly IDynamicAccessor FieldAccessor;
		public nuint PackedOffset = nuint.MaxValue;
		public readonly ushort FieldSize;
		public readonly FieldTypeDescFlags Flags;
		public readonly string ExternalName = "";
		public readonly ISaveRestoreOps? SaveRestoreOps;
		// InputFunc?
		public readonly DataMap? TD;
		public TypeDescription? OverrideField;
		public int OverrideCount;
		public readonly float FieldTolerance;

		public TypeDescription(FieldType type, ReadOnlySpan<char> fieldName, IDynamicAccessor accessor, ushort fieldSize, FieldTypeDescFlags flags, ReadOnlySpan<char> externalName, ISaveRestoreOps? saveRestoreOps, float fieldTolerance) {
			FieldType = type;
			FieldName = new(fieldName);
			FieldAccessor = accessor;
			FieldSize = fieldSize;
			Flags |= flags;
			ExternalName = new(externalName);
			SaveRestoreOps = saveRestoreOps;
			FieldTolerance = fieldTolerance;
		}
	}

	public enum FieldTypeDescFlags : ushort
	{
		Global = 0x0001, // This field is masked for global entity save/restore
		Save = 0x0002, // This field is saved to disk
		Key = 0x0004, // This field can be requested and written to by string name at load time
		Input = 0x0008, // This field can be written to by string name at run time, and a function called
		Output = 0x0010, // This field propogates it's value to all targets whenever it changes
		FunctionTable = 0x0020, // This is a table entry for a member function pointer
		Ptr = 0x0040, // This field is a pointer, not an embedded object
		Override = 0x0080, // The field is an override for one in a base class (only used by prediction system for now)
		InSendTable = 0x0100, // This field is present in a network SendTable
		Private = 0x0200, // The field is local to the client or server only (not referenced by prediction code and not replicated by networking)
		NoErrorCheck = 0x0400, // The field is part of the prediction typedescription, but doesn't get compared when checking for errors
		ModelIndex = 0x0800, // The field is a model index (used for debugging output)
		Index = 0x1000, // The field is an index into file data, used for byteswapping. 
		ViewOtherPlayer = 0x2000, // By default you can only view fields on the local player (yourself), 
		ViewOwnTeam = 0x4000, // Only show this data if the player is on the same team as the local player
		ViewNever = 0x8000, // Never show this field to anyone, even the local player (unusual)
	}


	public class DataMap
	{
		public DataMap() { }
		public DataMap(TypeDescription[]? dataDesc, ReadOnlySpan<char> dataClassName, DataMap? baseMap) {
			DataDesc = dataDesc ?? [];
			DataClassName = new(dataClassName);
			BaseMap = baseMap;
		}

		public TypeDescription[] DataDesc = [];
		public int DataNumFields => DataDesc?.Length ?? 0;
		public string DataClassName = "";
		public DataMap? BaseMap;
		public bool ChainsValidated;
		public bool PackedOffsetsComputed;
		public nuint PackedSize;
#if DEBUG
		public bool ValidityChecked = false;
#endif
	}
}

namespace Source
{
	public static class DEFINE<T>
	{
		static TypeDescription _FIELD(ReadOnlySpan<char> name, FieldType fieldType, int count, FieldTypeDescFlags flags, ReadOnlySpan<char> mapname, float tolerance) {
			return new(fieldType, name, FIELD<T>.OF(name), (ushort)count, flags, mapname, null, tolerance);
		}
		static TypeDescription _FIELD_ARRAY(ReadOnlySpan<char> name, FieldType fieldType, int count, FieldTypeDescFlags flags, ReadOnlySpan<char> mapname, float tolerance) {
			return new(fieldType, name, FIELD<T>.OF_ARRAY(name), (ushort)count, flags, mapname, null, tolerance);
		}

		public static TypeDescription FIELD(ReadOnlySpan<char> name, FieldType fieldType) {
			return _FIELD(name, fieldType, 1, FieldTypeDescFlags.Save, null, 0);
		}

		public static TypeDescription PRED_FIELD(ReadOnlySpan<char> name, FieldType fieldType, FieldTypeDescFlags flags) {
			return _FIELD(name, fieldType, 1, flags, null, 0);
		}
		public static TypeDescription PRED_ARRAY(ReadOnlySpan<char> name, FieldType fieldType, int count, FieldTypeDescFlags flags) {
			return _FIELD_ARRAY(name, fieldType, count, flags, null, 0);
		}

		public static TypeDescription PRED_FIELD_TOL(ReadOnlySpan<char> name, FieldType fieldType, FieldTypeDescFlags flags, float tolerance) {
			return _FIELD(name, fieldType, 1, flags, null, tolerance);
		}
		public static TypeDescription PRED_ARRAY_TOL(ReadOnlySpan<char> name, FieldType fieldType, int count, FieldTypeDescFlags flags, float tolerance) {
			return _FIELD_ARRAY(name, fieldType, count, flags, null, tolerance);
		}
	}

	public interface IDataFrameContainer
	{
		public T? Get<T>(int offset = 0);
		public void Set<T>(in T? v, int offset = 0);
	}

	public class DataFrameContainer<T> : IDataFrameContainer
	{
		public readonly T?[] v;
		readonly object?[] tempHandleArgsHack = [0u];
		public DataFrameContainer(int count = 1) => v = new T?[count];
		public DataFrameContainer(T? val, int count = 1) : this(count) => v[0] = val;

		public virtual TC? Get<TC>(int offset = 0) {
			if (typeof(TC) == typeof(T))
				return (TC?)(object?)v[offset];
			else {
				ILAssembler.DynamicCast(in v[offset]!, out TC ret);
				return ret;
			}
		}

		public virtual void Set<TC>(in TC? val, int offset = 0) {
			if (typeof(TC) == typeof(T))
				v[offset] = (T?)(object?)val;
			else
				ILAssembler.DynamicCast(in val, out v[offset]!);
		}
	}

	/// <summary>
	/// Source Engine deviation. Used mostly in prediction. 
	/// </summary>
	public class DataFrame
	{
		readonly IDataFrameContainer[] Data;
		readonly DataMap DataMap;

		public T? Get<T>(TypeDescription td, int offset = 0) => (Data[td.PackedOffset]).Get<T>(offset);
		public void Set<T>(TypeDescription td, T? value, int offset = 0) => (Data[td.PackedOffset]).Set<T>(value, offset);

		public DataFrame(DataMap? map) {
			ArgumentNullException.ThrowIfNull(map);

			DataMap = map;
			Assert(map.PackedOffsetsComputed);

			Data = new IDataFrameContainer[map.PackedSize];
			SetupDataFrame_R(map);
		}

		private void SetupDataFrame_R(DataMap map) {
			// Recurse through basemaps first
			if (map.BaseMap != null)
				SetupDataFrame_R(map.BaseMap);

			for (nuint i = 0; i < (nuint)map.DataNumFields; i++) {
				TypeDescription td = map.DataDesc[i];
				if (td.PackedOffset == nuint.MaxValue)
					continue;

				IDataFrameContainer framecontainer;
				switch (td.FieldType) {
					case FieldType.Embedded: framecontainer = new DataFrameContainer<DataFrame>(new DataFrame(td.TD)); break;
					case FieldType.Float: framecontainer = new DataFrameContainer<float>(); break;
					case FieldType.Vector: framecontainer = new DataFrameContainer<Vector3>(); break;
					case FieldType.Quaternion: framecontainer = new DataFrameContainer<Quaternion>(); break;
					case FieldType.Integer: framecontainer = new DataFrameContainer<int>(); break;
					case FieldType.EHandle: framecontainer = new DataFrameContainer<BaseHandle>(); break;
					case FieldType.Short: framecontainer = new DataFrameContainer<short>(); break;
					case FieldType.String: framecontainer = new DataFrameContainer<char[]>(); break;
					case FieldType.Color32: framecontainer = new DataFrameContainer<Color>(); break;
					case FieldType.Boolean: framecontainer = new DataFrameContainer<bool>(); break;
					case FieldType.Character: framecontainer = new DataFrameContainer<byte>(); break;

					default:
						continue;
				}

				Data[td.PackedOffset] = framecontainer;
			}
		}
	}
}
