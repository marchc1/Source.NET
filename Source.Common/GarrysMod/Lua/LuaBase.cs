using Source.Common.Engine;
using Source.Common.Mathematics;

using System.Numerics;
using System.Reflection.Metadata;

namespace Source.Common.GarrysMod.Lua;

public unsafe delegate int CFunc(lua_State* L);

public enum Special
{
	Glob,
	Env,
	Reg
}

public enum Index
{
	Global = -10002,
	Environment,
	Registry
}

public interface ILuaBase
{
	int Top();
	void Push(int stackPos);
	void Pop(int iAmt = 1);
	void GetTable(int stackPos);
	void GetField(int stackPos, ReadOnlySpan<char> strName);
	void SetField(int stackPos, ReadOnlySpan<char> strName);
	void CreateTable();
	void SetTable(int i);
	void SetMetaTable(int i);
	bool GetMetaTable(int i);
	void Call(int args, int results);
	int PCall(int args, int results, int errorFunc);
	int Equal(int a, int b);
	int RawEqual(int a, int b);
	void Insert(int stackPos);
	void Remove(int stackPos);
	int Next(int stackPos);
	unsafe void* NewUserdata(uint size);
	void ThrowError(ReadOnlySpan<char> strError);
	void CheckType(int stackPos, int type);
	void ArgError(int argNum, ReadOnlySpan<char> strMessage);
	void RawGet(int stackPos);
	void RawSet(int stackPos);

	ReadOnlySpan<char> GetString(int stackPos = -1);
	double GetNumber(int stackPos = -1);
	bool GetBool(int stackPos = -1);
	CFunc GetCFunction(int stackPos = -1);
	unsafe void* GetUserdata(int stackPos = -1);

	void PushNil();
	void PushString(ReadOnlySpan<char> val);
	void PushNumber(double val);
	void PushBool(bool val);
	void PushCFunction(CFunc val);
	void PushCClosure(CFunc val, int vars);
	unsafe void PushUserdata(void* userdata);

	// If you create a reference - don't forget to free it!
	int ReferenceCreate();
	void ReferenceFree(int i);
	void ReferencePush(int i);

	// Push a special value onto the top of the stack ( see below )
	void PushSpecial(int type);

	// For type enums see Types.h 
	bool IsType(int stackPos, int type);
	int GetType(int stackPos);
	ReadOnlySpan<char> GetTypeName(int type);

	// Creates a new meta table of string and type and leaves it on the stack.
	// Will return the old meta table of this name if it already exists.
	void CreateMetaTableType(ReadOnlySpan<char> strName, int type);

	// Like Get* but throws errors and returns if they're not of the expected type
	ReadOnlySpan<char> CheckString(int stackPos = -1);
	double CheckNumber(int stackPos = -1);
}
