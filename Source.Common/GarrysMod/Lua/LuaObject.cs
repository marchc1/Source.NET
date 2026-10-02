using Source.Common.Mathematics;
using Source.Common.Physics;

using System.Numerics;

namespace Source.Common.GarrysMod.Lua;

// TODO: Review how much of this should actually be void*'s (probably none of it)
// This is just getting the base stuff written out

public interface ILuaObject
{
	void Set(ILuaObject obj);
	void SetFromStack(int i);
	void UnReference();

	int GetType();
	ReadOnlySpan<char> GetString();
	float GetFloat();
	int GetInt();
	unsafe void* GetUserData();

	void SetMember(ReadOnlySpan<char> name);
	void SetMember(ReadOnlySpan<char> name, ILuaObject obj);
	void SetMember(ReadOnlySpan<char> name, float val);
	void SetMember(ReadOnlySpan<char> name, bool val);
	void SetMember(ReadOnlySpan<char> name, ReadOnlySpan<char> val);
	void SetMember(ReadOnlySpan<char> name, CFunc f);

	bool GetMemberBool(ReadOnlySpan<char> name, bool b = true);
	int GetMemberInt(ReadOnlySpan<char> name, int i = 0);
	float GetMemberFloat(ReadOnlySpan<char> name, float f = 0.0f);
	ReadOnlySpan<char> GetMemberStr(ReadOnlySpan<char> name, ReadOnlySpan<char> s = "");
	unsafe void* GetMemberUserData(ReadOnlySpan<char> name, void* u = null);
	unsafe void* GetMemberUserData(float name, void* u = null);
	ILuaObject GetMember(ReadOnlySpan<char> name, ILuaObject obj);
	ILuaObject GetMember(ILuaObject key, ILuaObject obj);

	void SetMetaTable(ILuaObject obj);
	unsafe void SetUserData(void* obj);

	void Push();

	bool isNil();
	bool isTable();
	bool isString();
	bool isNumber();
	bool isFunction();
	bool isUserData();

	// ... why are these keys floats?
	// If they're meant to be lua indices... why aren't they doubles?
	// Raphael maybe you know??
	ILuaObject GetMember(float key, ILuaObject obj);

	// ok ill remove you then unsafe void* Remove_Me_1(ReadOnlySpan<char> name, void* unk2);

	void SetMember(float key);
	void SetMember(float key, ILuaObject obj);
	void SetMember(float key, float val);
	void SetMember(float key, bool val);
	void SetMember(float key, ReadOnlySpan<char> val);
	void SetMember(float key, CFunc f);

	ReadOnlySpan<char> GetMemberStr(float name, ReadOnlySpan<char> s = "");

	void SetMember(ILuaObject k, ILuaObject v);
	bool GetBool();

	bool PushMemberFast(int iStackPos);
	void SetMemberFast(int iKey, int iValue);

	void SetFloat(float val);
	void SetString(ReadOnlySpan<char> val);

	double GetDouble();

	void SetMember_FixKey(ReadOnlySpan<char> unk1, float unk2);
	void SetMember_FixKey(ReadOnlySpan<char> unk1, ReadOnlySpan<char> unk2);
	void SetMember_FixKey(ReadOnlySpan<char> unk1, ILuaObject unk2);
	void SetMember_FixKey(ReadOnlySpan<char> unk1, double unk2);
	void SetMember_FixKey(ReadOnlySpan<char> unk1, int unk2);

	bool isBool();

	void SetMemberDouble(ReadOnlySpan<char> unk1, double unk2);

	void SetMemberNil(ReadOnlySpan<char> unk);
	void SetMemberNil(float unk);

	// bool RemoveMe();

	void Init();

	void SetFromGlobal(ReadOnlySpan<char> unk);

	int GetStringLen(out uint len);

	uint GetMemberUInt(ReadOnlySpan<char> unk1, uint unk2);

	void SetMember(ReadOnlySpan<char> unk1, ulong unk2);
	void SetMember(ReadOnlySpan<char> unk1, int unk2);
	void SetReference(int unk);

	void RemoveMember(ReadOnlySpan<char> unk);
	void RemoveMember(float unk);

	bool MemberIsNil(ReadOnlySpan<char> unk);

	void SetMemberDouble(float unk1, double unk2);
	double GetMemberDouble(ReadOnlySpan<char> unk1, double unk2);

	// NOTE: All members below do NOT exist in ILuaObjects returned from the menusystem!

	IHandleEntity GetMemberEntity(ReadOnlySpan<char> unk1, IHandleEntity unk2);
	void SetMemberEntity(float unk1, IHandleEntity unk2);
	void SetMemberEntity(ReadOnlySpan<char> unk1, IHandleEntity unk2);
	bool isEntity();
	IHandleEntity GetEntity();
	void SetEntity(IHandleEntity unk);

	void SetMemberVector3(ReadOnlySpan<char> unk1, in Vector3 unk2);
	void SetMemberVector3(float unk1, in Vector3 unk2);
	ref Vector3 GetMemberVector3(ReadOnlySpan<char> unk1, in Vector3 unk2);
	ref Vector3 GetMemberVector3(int unk);
	ref Vector3 GetVector3();
	bool isVector3();

	void SetMemberAngle(ReadOnlySpan<char> unk1, QAngle unk2);
	ref QAngle GetMemberAngle(ReadOnlySpan<char> unk1, QAngle unk2);
	ref QAngle GetAngle();
	bool isAngle();

	void SetMemberMatrix(ReadOnlySpan<char> unk1, in Matrix4x4 unk2);
	void SetMemberMatrix(float unk1, in Matrix4x4 unk2);
	void SetMemberMatrix(int unk1, in Matrix4x4 unk2);

	void SetMemberPhysObject(ReadOnlySpan<char> unk1, IPhysicsObject unk2);
	double GetMemberDouble(float unk1, double unk2);
}
