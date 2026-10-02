using Source.Common.Commands;

using System.Runtime.InteropServices;

using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Source.Common.GarrysMod.Lua;


public interface ILuaThreadedCall
{
	void Init(); // NOTE: Always called on the main thread, so if you need to prepare something there, you can do it in here.
	void Run(ILuaBase lua); // NOTE: After the call was executed, it won't be deleted! So call `delete this;` or reuse it.
}

public struct lua_Debug; // TODO

public interface ILuaInterface : ILuaBase
{
	bool Init(ILuaGameCallback callbacks, bool unk);
	void Shutdown();
	void Cycle();
	ILuaObject Global();
	ILuaObject GetObject(int index);
	void PushLuaObject(ILuaObject obj);
	void PushLuaFunction(CFunc func);
	void LuaError(ReadOnlySpan<char> err, int index);
	void TypeError(ReadOnlySpan<char> name, int index);
	void CallInternal(int args, int rets);
	void CallInternalNoReturns(int args);
	bool CallInternalGetBool(int args);
	ReadOnlySpan<char> CallInternalGetString(int args);
	bool CallInternalGet(int args, ILuaObject obj);
	void NewGlobalTable(ReadOnlySpan<char> name);
	ILuaObject NewTemporaryObject();
	bool isUserData(int index);
	ILuaObject GetMetaTableObject(ReadOnlySpan<char> name, int type);
	ILuaObject GetMetaTableObject(int index);
	ILuaObject GetReturn(int index);
	bool IsServer();
	bool IsClient();
	bool IsMenu();
	void DestroyObject(ILuaObject obj);
	ILuaObject CreateObject();
	void SetMember(ILuaObject table, ILuaObject key, ILuaObject value);
	ILuaObject GetNewTable();
	void SetMember(ILuaObject table, float key);
	void SetMember(ILuaObject table, float key, ILuaObject value);
	void SetMember(ILuaObject table, ReadOnlySpan<char> key);
	void SetMember(ILuaObject table, ReadOnlySpan<char> key, ILuaObject value);
	void SetType(byte unk );
	void PushLong(long num);
	int GetFlags(int index);
	bool FindOnObjectsMetaTable(int objIndex, int keyIndex);
	bool FindObjectOnTable(int tableIndex, int keyIndex);
	void SetMemberFast(ILuaObject table, int keyIndex, int valueIndex);
	bool RunString(ReadOnlySpan<char> filename, ReadOnlySpan<char> path, ReadOnlySpan<char> stringToRun, bool run, bool showErrors);
	bool IsEqual(ILuaObject objA, ILuaObject objB);
	void Error(ReadOnlySpan<char> err);
	ReadOnlySpan<char> GetStringOrError(int index);
	bool RunLuaModule(ReadOnlySpan<char> name);
	bool FindAndRunScript(ReadOnlySpan<char> filename, bool run, bool showErrors, ReadOnlySpan<char> stringToRun, bool noReturns);
	void SetPathID(ReadOnlySpan<char> pathID);
	ReadOnlySpan<char> GetPathID();
	void ErrorNoHalt(ReadOnlySpan<char> msg);
	void Msg(ReadOnlySpan<char> msg);
	void PushPath(ReadOnlySpan<char> path);
	void PopPath();
	ReadOnlySpan<char> GetPath();
	int GetColor(int index);
	nint PushColor(Color color); // ToDo: This seems to return something, but it hasn't been figured out what yet.
	int GetStack(int level, ref lua_Debug dbg);
	int GetInfo(ReadOnlySpan<char> what, ref lua_Debug dbg);
	ReadOnlySpan<char> GetLocal(ref lua_Debug dbg, int n);
	ReadOnlySpan<char> GetUpvalue(int funcIndex, int n);
	bool RunStringEx(ReadOnlySpan<char> filename, ReadOnlySpan<char> path, ReadOnlySpan<char> stringToRun, bool run, bool printErrors, bool dontPushErrors, bool noReturns);
	void GetDataString(int index, out ReadOnlySpan<char> str);
	void ErrorFromLua(ReadOnlySpan<char> msg);
	// Returns "<nowhere>" if nothing was found.
	ReadOnlySpan<char> GetCurrentLocation();
	void MsgColour(in Color col, ReadOnlySpan<char> msg);
	// outStr is set to "!UNKNOWN" if it couldn't be found.
	void GetCurrentFile(out string outStr);
	// bool CompileString(Bootil::Buffer &dumper, const std::string &stringToCompile );
	bool CallFunctionProtected(int unk1, int unk2, bool unk3);
	void Require(ReadOnlySpan<char> name);
	ReadOnlySpan<char> GetActualTypeName(int type);
	void PreCreateTable(int arrelems, int nonarrelems);
	void PushPooledString(int index);
	ReadOnlySpan<char> GetPooledString(int index);
	int AddThreadedCall(ILuaThreadedCall call); // NOTE: Returns the number of queried threaded calls.
	void AppendStackTrace(Span<char> chars);
	ConVar CreateConVar(ReadOnlySpan<char> name, ReadOnlySpan<char> defaultValue, ReadOnlySpan<char> helpString, int flags);
	ConCommand CreateConCommand(ReadOnlySpan<char> name, ReadOnlySpan<char> helpString, int flags, FnCommandCallback? callback, FnCommandCompletionCallback? completionCallback);
	ReadOnlySpan<char> CheckStringOpt(int stackPos, ReadOnlySpan<char> def);
	double CheckNumberOpt(int stackPos, double def);
	int RegisterMetaTable(ReadOnlySpan<char> name, ILuaObject tbl);
}
