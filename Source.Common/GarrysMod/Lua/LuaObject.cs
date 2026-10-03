using Source.Common.Mathematics;
using Source.Common.Physics;

using System.Numerics;
using System.Runtime.CompilerServices;

namespace Source.Common.GarrysMod.Lua;

public interface ILuaObject
{
	void Set(ILuaObject? obj);
	void SetFromStack(int i);
	void UnReference();

	LuaType GetType();
	string? GetString();
	float GetFloat();
	int GetInt();
	nint GetUserData();

	void SetMember(ReadOnlySpan<char> name);
	void SetMember(ReadOnlySpan<char> name, ILuaObject? obj);
	void SetMember(ReadOnlySpan<char> name, float val);
	void SetMember(ReadOnlySpan<char> name, bool val);
	void SetMember(ReadOnlySpan<char> name, ReadOnlySpan<char> val);
	void SetMember(ReadOnlySpan<char> name, CFunc f);

	bool GetMemberBool(ReadOnlySpan<char> name, bool b = true);
	int GetMemberInt(ReadOnlySpan<char> name, int i = 0);
	float GetMemberFloat(ReadOnlySpan<char> name, float f = 0.0f);
	string? GetMemberStr(ReadOnlySpan<char> name, string? s = "");
	nint GetMemberUserData_DontUseMe(ReadOnlySpan<char> name, nint u = 0);
	nint GetMemberUserData_DontUseMe(float name, nint u = 0);
	void GetMember(ReadOnlySpan<char> name, ILuaObject obj);
	void GetMember(ILuaObject key, ILuaObject obj);

	void SetMetaTable(ILuaObject obj);
	void SetUserData(nint obj);

	void Push();

	bool isNil();
	bool isTable();
	bool isString();
	bool isNumber();
	bool isFunction();
	bool isUserData();

	void GetMember(float key, ILuaObject obj);

	// ok ill remove you then unsafe void* Remove_Me_1(ReadOnlySpan<char> name, void* unk2);

	void SetMember(float key);
	void SetMember(float key, ILuaObject? obj);
	void SetMember(float key, float val);
	void SetMember(float key, bool val);
	void SetMember(float key, ReadOnlySpan<char> val);
	void SetMember(float key, CFunc f);

	string? GetMemberStr(float name, string? s = "");

	void SetMember(ILuaObject key, ILuaObject? value);
	bool GetBool();

	bool PushMemberFast(int stackPos);
	void SetMemberFast(int key, int value);

	void SetFloat(float val);
	void SetString(ReadOnlySpan<char> val);

	double GetDouble();

	void SetMember_FixKey(ReadOnlySpan<char> key, float val);
	void SetMember_FixKey(ReadOnlySpan<char> key, ReadOnlySpan<char> val);
	void SetMember_FixKey(ReadOnlySpan<char> key, ILuaObject? val);
	void SetMember_FixKey(ReadOnlySpan<char> key, double val);
	void SetMember_FixKey(ReadOnlySpan<char> key, int val);

	bool isBool();

	void SetMemberDouble(scoped ReadOnlySpan<char> name, double val);

	void SetMemberNil(ReadOnlySpan<char> name);
	void SetMemberNil(float key);

	// bool RemoveMe();

	void Init();

	void SetFromGlobal(ReadOnlySpan<char> name);

	string? GetStringLen(out uint len);

	uint GetMemberUInt(ReadOnlySpan<char> name, uint def);

	void SetMember(ReadOnlySpan<char> name, ulong val);
	void SetMember(ReadOnlySpan<char> name, int val);
	void SetReference(int i);

	void RemoveMember(ReadOnlySpan<char> name);
	void RemoveMember(float key);

	bool MemberIsNil(ReadOnlySpan<char> name);

	void SetMemberDouble(float key, double val);
	double GetMemberDouble(ReadOnlySpan<char> name, double def);

	IHandleEntity? GetMemberEntity(ReadOnlySpan<char> name, IHandleEntity? def);
	void SetMemberEntity(float key, IHandleEntity? ent);
	void SetMemberEntity(ReadOnlySpan<char> name, IHandleEntity? ent);
	bool isEntity();
	IHandleEntity? GetEntity();
	void SetEntity(IHandleEntity? ent);

	void SetMemberVector(ReadOnlySpan<char> name, in Vector3 vec);
	void SetMemberVector(float key, in Vector3 vec);
	Vector3 GetMemberVector(ReadOnlySpan<char> name, in Vector3 def);
	Vector3 GetMemberVector(int key);
	Vector3 GetVector();
	bool isVector();

	void SetMemberAngle(ReadOnlySpan<char> name, in QAngle ang);
	QAngle GetMemberAngle(ReadOnlySpan<char> name, in QAngle def);
	QAngle GetAngle();
	bool isAngle();

	void SetMemberMatrix(ReadOnlySpan<char> name, in Matrix4x4 mat);
	void SetMemberMatrix(float key, in Matrix4x4 mat);
	void SetMemberMatrix(int key, in Matrix4x4 mat);

	void SetMemberPhysObject(ReadOnlySpan<char> name, IPhysicsObject? obj);
	double GetMemberDouble(float key, double def);
	IHandleEntity? GetMemberEntity(int key, IHandleEntity? def);
	Matrix4x4 GetMemberMatrix(int key, in Matrix4x4 def);

	public void SetMemberEnumValue<E>(scoped ReadOnlySpan<char> name, in E value) where E : struct, Enum {
		ref E r = ref Unsafe.AsRef(in value);
		double d = Unsafe.SizeOf<E>() switch {
			1 => Unsafe.As<E, byte>(ref r),
			2 => Unsafe.As<E, short>(ref r),
			4 => Unsafe.As<E, int>(ref r),
			8 => Unsafe.As<E, long>(ref r),
			_ => throw new NotSupportedException()
		};

		SetMemberDouble(name, d);
	}

	public delegate bool ShouldSetEnum<E>(E value);
	public delegate ReadOnlySpan<char> EnumNameProducer<E>(E value);
}
