using Source;
using Source.Common;
using Source.Common.Commands;
using Source.Common.GarrysMod.Lua;
using Source.Common.Mathematics;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using static Game.Lua.LuaApi;

namespace Game.Lua;

public unsafe class LuaInterfaceImpl : ILuaInterface
{
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	delegate int NativeCFunction(nint L);

	[StructLayout(LayoutKind.Sequential)]
	struct UserData
	{
		public nint Data;
		public byte Type;
	}

	static class DefaultValue<T> where T : unmanaged
	{
		public static T Value;
	}

	const int MaxCachedMetaTables = 0xFE;
	const int MessageBufferSize = 0x1000;
	const int TemporaryObjectCount = 32;
	const int MaxReturns = 4;
	const double FPUPrecisionCheck = 1437217655.0;

	static LuaError g_LastError = new();

	nint state;
	nint mainState;
	GCHandle selfHandle;
	ILuaGameCallback? gameCallback;
	int errorReporterRef;
	byte luaType;
	ILuaObject? global;
	ILuaObject? stringPool;
	readonly ILuaObject?[] temporaryObjects = new ILuaObject?[TemporaryObjectCount];
	int temporaryObjectIndex;
	readonly ILuaObject?[] returnObjects = new ILuaObject?[MaxReturns];
	readonly LinkedList<ILuaThreadedCall> threadedCalls = new();

	readonly int[] metaTableRefs = new int[MaxCachedMetaTables + 1];
	int nextMetaTableType = (int)LuaType.Type_Count;

	readonly Dictionary<CFunc, nint> functionPointers = [];
	readonly Dictionary<nint, CFunc> functionsByPointer = [];
	readonly List<NativeCFunction> functionThunks = [];

	object?[] userTypeObjects = new object?[64];
	int[] userTypeGenerations = new int[64];
	readonly Stack<int> freeUserTypeSlots = [];
	int userTypeSlotCount;
	readonly Dictionary<object, int> userTypeSlotsByObject = new(ReferenceEqualityComparer.Instance);

	public LuaInterfaceImpl() {
		Array.Fill(metaTableRefs, -1);
	}

	public lua_State State => new(state);

	public bool Init(ILuaGameCallback callbacks, bool isServer) {
		gameCallback = callbacks;
		global = CreateObject();
		Array.Clear(temporaryObjects);
		nextMetaTableType = (int)LuaType.Type_Count;
		Array.Fill(metaTableRefs, -1);
		temporaryObjectIndex = 0;

		SetState(new(luaL_newstate()));
		if (state == 0)
			return false;
		mainState = state;
		selfHandle = GCHandle.Alloc(this);
		lua_sn_init(state);
		lua_setoutputf(state, &OnOutput, GCHandle.ToIntPtr(selfHandle));
		lua_setstatef(state, &OnSetState, GCHandle.ToIntPtr(selfHandle));

		lua_atpanic(state, &LuaPanic);
		PushCFunction(AdvancedLuaErrorReporter);
		errorReporterRef = ReferenceCreate();

		PushNumber(FPUPrecisionCheck);
		if (GetNumber(-1) != FPUPrecisionCheck) {
			luaJIT_setmode(state, 0, 0);
			Warning("Lua detected bad FPU precision! Prepare for weirdness!\n");
		}
		Pop(1);
		DoStackCheck();

		lua_pushvalue(state, LuaIndex.Global);
		global!.SetFromStack(-1);
		Pop(1);

		lua_createtable(state, 0, 0);
		for (int i = 0; i < PooledStrings.g_PooledStrings.Length; i++) {
			PushString(PooledStrings.g_PooledStrings[i]);
			lua_rawseti(state, -2, i + 1);
		}
		stringPool = CreateObject();
		stringPool.SetFromStack(-1);
		Pop(1);

		gameCallback?.InterfaceCreated(this);

		Global().SetMember("VERSION", 1234.0f);
		Global().SetMember("BRANCH", "unknown");

		luaL_openlibs(state);
		DoStackCheck();

		GetField(LuaIndex.Global, "debug");
		PushNil();
		SetField(-2, "setlocal");
		PushNil();
		SetField(-2, "setupvalue");
		PushNil();
		SetField(-2, "upvalueid");
		PushNil();
		SetField(-2, "upvaluejoin");
		lua_settop(state, -2);
		return true;
	}

	public void Shutdown() {
		ShutdownThreadedCalls();
		for (int i = 0; i < temporaryObjects.Length; i++) {
			if (temporaryObjects[i] != null) {
				DestroyObject(temporaryObjects[i]);
				temporaryObjects[i] = null;
			}
		}
		DestroyObject(global);
		global = null;
		if (mainState != 0)
			lua_close(mainState);
		mainState = 0;
		state = 0;
		if (selfHandle.IsAllocated)
			selfHandle.Free();
	}

	[UnmanagedCallersOnly]
	static int LuaPanic(nint L) {
		nuint len;
		byte* str = lua_tolstring(L, -1, &len);
		Dbg.Error($"Lua Panic! Something went horribly wrong!\n\n\"{(str == null ? "(null)" : Encoding.UTF8.GetString(str, (int)len))}\"\n");
		return 0;
	}

	static void ReadStackIntoError(nint L) {
		g_LastError.Stack.Clear();
		lua_Debug ar = default;
		fixed (byte* what = "Slnu\0"u8) {
			for (int level = 1; level < 17; level++) {
				if (lua_getstack(L, level, &ar) == 0)
					break;
				lua_getinfo(L, what, &ar);
				g_LastError.Stack.Add(new LuaError.StackEntry() {
					Source = ar.ShortSource,
					Function = ar.Name ?? "",
					Line = ar.CurrentLine
				});
			}
		}
	}

	static int AdvancedLuaErrorReporter(ILuaInterface lua) {
		nint L = ((LuaInterfaceImpl)lua).state;
		if (lua_isstring(L, 1) != 0) {
			g_LastError.Message = lua.GetString(1)!;
			ReadStackIntoError(L);
			lua.PushString("");
			return 1;
		}
		ReadStackIntoError(L);
		int type = lua_type(L, 1);
		if (type == TNIL)
			g_LastError.Message = "Nil was passed as the error message!";
		else
			g_LastError.Message = $"Unknown type {type} was given as Lua error!";
		return 0;
	}

	void DoStackCheck() {
		int top = Top();
		if (top == 0)
			return;
		StringBuilder dump = new();
		for (int i = 0; i < top && i <= 49; i++)
			dump.Append($"{i}> {GetString(i) ?? "UNKNOWN"} ({GetTypeName(GetType(i))})\n");
		Dbg.Error($"{(IsClient() ? "Client" : "Server")} Lua Stack Leak [{top}]!\n{dump}");
	}

	void RunThreadedCalls() {
		List<ILuaThreadedCall> finished = [];
		foreach (ILuaThreadedCall call in threadedCalls)
			if (call.IsFinished())
				finished.Add(call);
		foreach (ILuaThreadedCall call in finished) {
			call.DoFinish(this);
			LinkedListNode<ILuaThreadedCall>? node = threadedCalls.First;
			while (node != null) {
				LinkedListNode<ILuaThreadedCall>? next = node.Next;
				if (node.Value == call)
					threadedCalls.Remove(node);
				node = next;
			}
		}
	}

	void ShutdownThreadedCalls() {
		foreach (ILuaThreadedCall call in threadedCalls)
			call.DestroyForced();
		threadedCalls.Clear();
	}

	[UnmanagedCallersOnly]
	static void OnOutput(nint L, byte* data, nuint len, nint ud) {
		var lua = (LuaInterfaceImpl)GCHandle.FromIntPtr(ud).Target!;
		lua.gameCallback!.Msg(Encoding.UTF8.GetString(data, (int)Math.Min(len, MessageBufferSize - 1)), true);
	}

	[UnmanagedCallersOnly]
	static void OnSetState(nint L, nint ud) {
		var lua = (LuaInterfaceImpl)GCHandle.FromIntPtr(ud).Target!;
		lua.state = L;
	}

	ref struct Utf8
	{
		byte* allocated;
		public readonly byte* Pointer;
		public readonly int Length;

		public Utf8(ReadOnlySpan<char> text, Span<byte> buffer) {
			int max = Encoding.UTF8.GetMaxByteCount(text.Length) + 1;
			if (max > buffer.Length) {
				allocated = (byte*)NativeMemory.Alloc((nuint)max);
				buffer = new Span<byte>(allocated, max);
			}
			Length = Encoding.UTF8.GetBytes(text, buffer);
			buffer[Length] = 0;
			Pointer = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(buffer));
		}

		public void Dispose() {
			if (allocated != null) {
				NativeMemory.Free(allocated);
				allocated = null;
			}
		}
	}

	static ReadOnlySpan<char> UntilNul(ReadOnlySpan<char> text) {
		int nul = text.IndexOf('\0');
		return nul < 0 ? text : text[..nul];
	}

	int AbsIndex(int stackPos) => stackPos < 0 && stackPos > LuaIndex.Registry ? lua_gettop(state) + stackPos + 1 : stackPos;

	void CheckStatus(int status) {
		if (status == OK)
			return;
		string text = GetString(-1) ?? "";
		throw new LuaException(LuaRaise.Rethrow, status, luaL_ref(state, LuaIndex.Registry), 0, text);
	}

	int Op(int op, int idx, int n = 0, byte* k = null) {
		int result;
		CheckStatus(lua_sn_op(state, op, idx, n, k, &result));
		return result;
	}

	nint GetFunctionPointer(CFunc func) {
		if (functionPointers.TryGetValue(func, out nint ptr))
			return ptr;
		NativeCFunction thunk = L => Invoke(func, L);
		functionThunks.Add(thunk);
		ptr = Marshal.GetFunctionPointerForDelegate(thunk);
		functionPointers[func] = ptr;
		functionsByPointer[ptr] = func;
		return ptr;
	}

	int Invoke(CFunc func, nint L) {
		state = L;
		try {
			return func(this);
		}
		catch (LuaException e) {
			return Raise(e);
		}
		catch (Exception e) {
			Warning($"{e}\n");
			PushString($"{e.GetType().Name}: {e.Message}");
			return SN_ERRORMSG;
		}
	}

	int Raise(LuaException e) {
		switch (e.Raise) {
			case LuaRaise.Rethrow:
				lua_rawgeti(state, LuaIndex.Registry, e.Reference);
				luaL_unref(state, LuaIndex.Registry, e.Reference);
				return SN_RETHROW(e.Status);
			case LuaRaise.Value:
				lua_rawgeti(state, LuaIndex.Registry, e.Reference);
				luaL_unref(state, LuaIndex.Registry, e.Reference);
				return SN_ERROR;
			case LuaRaise.Message:
				PushString(e.Text);
				return SN_ERRORMSG;
			case LuaRaise.Argument:
				lua_pushnumber(state, e.Argument);
				PushString(e.Text);
				return SN_ARGERROR;
			default:
				lua_pushnumber(state, e.Argument);
				PushString(e.Text);
				return SN_TYPEERROR;
		}
	}

	public int Top() => lua_gettop(state);

	public void Push(int stackPos) => lua_pushvalue(state, stackPos);

	public void Pop(int amt = 1) {
		if (lua_gettop(state) < amt)
			Dbg.Error("Lua stack over-popped!\n");
		lua_settop(state, -amt - 1);
	}

	public void GetTable(int stackPos) => Op(SN_GETTABLE, stackPos);

	public void GetField(int stackPos, ReadOnlySpan<char> name) {
		using Utf8 key = new(name, stackalloc byte[128]);
		Op(SN_GETFIELD, stackPos, 0, key.Pointer);
	}

	public void SetField(int stackPos, ReadOnlySpan<char> name) {
		using Utf8 key = new(name, stackalloc byte[128]);
		Op(SN_SETFIELD, stackPos, 0, key.Pointer);
	}

	public void CreateTable() => lua_createtable(state, 0, 0);

	public void SetTable(int stackPos) => Op(SN_SETTABLE, stackPos);

	public void SetMetaTable(int stackPos) => lua_setmetatable(state, stackPos);

	public bool GetMetaTable(int stackPos) => lua_getmetatable(state, stackPos) != 0;

	public void Call(int args, int results) {
		nint L = state;
		CheckStatus(lua_sn_call(L, args, results));
		SetState(new(L));
	}

	public int PCall(int args, int results, int errorFunc) {
		nint L = state;
		int status = lua_pcall(L, args, results, errorFunc);
		SetState(new(L));
		return status;
	}

	public bool Equal(int a, int b) => Op(SN_EQUAL, a, AbsIndex(b)) != 0;

	public bool RawEqual(int a, int b) => lua_rawequal(state, a, b) != 0;

	public void Insert(int stackPos) => lua_insert(state, stackPos);

	public void Remove(int stackPos) => lua_remove(state, stackPos);

	public bool Next(int stackPos) => Op(SN_NEXT, stackPos) != 0;

	public nint NewUserdata(uint size) => lua_newuserdata(state, size);

	[DoesNotReturn]
	public void ThrowError(ReadOnlySpan<char> error) => throw new LuaException(LuaRaise.Message, ERRRUN, 0, 0, error.ToString());

	public void CheckType(int stackPos, LuaType type) {
		if (GetType(stackPos) != type)
			TypeError(GetTypeName(type), stackPos);
	}

	[DoesNotReturn]
	public void ArgError(int argNum, ReadOnlySpan<char> message) => throw new LuaException(LuaRaise.Argument, ERRRUN, 0, AbsIndex(argNum), message.ToString());

	public void RawGet(int stackPos) => lua_rawget(state, stackPos);

	public void RawSet(int stackPos) => Op(SN_RAWSET, stackPos);

	public ReadOnlySpan<byte> GetStringBytes(int stackPos = -1) {
		nuint len;
		byte* str = lua_tolstring(state, stackPos, &len);
		return str == null ? default : new ReadOnlySpan<byte>(str, (int)len);
	}

	public string? GetString(int stackPos = -1) {
		nuint len;
		byte* str = lua_tolstring(state, stackPos, &len);
		return str == null ? null : Encoding.UTF8.GetString(str, (int)len);
	}

	public double GetNumber(int stackPos = -1) => lua_tonumber(state, stackPos);

	public bool GetBool(int stackPos = -1) => lua_toboolean(state, stackPos) != 0;

	public CFunc? GetCFunction(int stackPos = -1) => functionsByPointer.GetValueOrDefault(lua_tocfunction(state, stackPos));

	public nint GetUserdata(int stackPos = -1) => lua_touserdata(state, stackPos);

	public void PushNil() => lua_pushnil(state);

	public void PushString(ReadOnlySpan<char> val) {
		using Utf8 str = new(UntilNul(val), stackalloc byte[256]);
		lua_pushlstring(state, str.Pointer, (nuint)str.Length);
	}

	public void PushString(ReadOnlySpan<byte> val) {
		fixed (byte* str = val)
			lua_pushlstring(state, str, (nuint)val.Length);
	}

	public void PushNumber(double val) => lua_pushnumber(state, val);

	public void PushBool(bool val) => lua_pushboolean(state, val ? 1 : 0);

	public void PushCFunction(CFunc val) => lua_pushcclosure(state, GetFunctionPointer(val), 0);

	public void PushCClosure(CFunc val, int vars) => lua_pushcclosure(state, GetFunctionPointer(val), vars);

	public void PushUserdata(nint userdata) => lua_pushlightuserdata(state, userdata);

	public int ReferenceCreate() => luaL_ref(state, LuaIndex.Registry);

	public void ReferenceFree(int i) => luaL_unref(state, LuaIndex.Registry, i);

	public void ReferencePush(int i) => lua_rawgeti(state, LuaIndex.Registry, i);

	public void PushSpecial(Special type) {
		switch (type) {
			case Special.Glob: lua_pushvalue(state, LuaIndex.Global); break;
			case Special.Env: lua_pushvalue(state, LuaIndex.Environment); break;
			case Special.Reg: lua_pushvalue(state, LuaIndex.Registry); break;
			default: PushNil(); break;
		}
	}

	public bool IsType(int stackPos, LuaType type) {
		LuaType actual = (LuaType)lua_type(state, stackPos);
		if (actual == type)
			return true;
		if (actual != LuaType.UserData || type <= LuaType.UserData)
			return false;
		UserData* ud = (UserData*)lua_touserdata(state, stackPos);
		return ud != null && (LuaType)ud->Type == type;
	}

	public LuaType GetType(int stackPos) {
		int type = lua_type(state, stackPos);
		if (type == TUSERDATA) {
			UserData* ud = (UserData*)lua_touserdata(state, stackPos);
			if (ud != null && ud->Type >= 9)
				return (LuaType)ud->Type;
			return (LuaType)type;
		}
		return (LuaType)(type is >= 1 and <= 8 ? type : 0);
	}

	public string GetTypeName(LuaType type) {
		if (type < 0)
			return "none";
		if (type >= LuaType.Type_Count)
			return "unknown";
		return LuaShared.GetTypeName(type);
	}

	public void CreateMetaTableType(ReadOnlySpan<char> name, int type) {
		using Utf8 tname = new(name, stackalloc byte[128]);
		if (luaL_newmetatable_type(state, tname.Pointer, type) == 0 || type > MaxCachedMetaTables)
			return;
		if (metaTableRefs[type] != -1)
			luaL_unref(state, LuaIndex.Registry, metaTableRefs[type]);
		lua_pushvalue(state, -1);
		metaTableRefs[type] = luaL_ref(state, LuaIndex.Registry);
	}

	public string CheckString(int stackPos = -1) {
		int type = lua_type(state, stackPos);
		if (type != TSTRING && type != TNUMBER)
			TypeError("string", stackPos);
		return GetString(stackPos)!;
	}

	public double CheckNumber(int stackPos = -1) {
		if (lua_isnumber(state, stackPos) == 0)
			TypeError("number", stackPos);
		return lua_tonumber(state, stackPos);
	}

	public int ObjLen(int stackPos = -1) => (int)lua_objlen(state, stackPos);

	public QAngle GetAngle(int stackPos = -1) => GetValueUserType<QAngle>(stackPos, LuaType.Angle);

	public Vector3 GetVector(int stackPos = -1) => GetValueUserType<Vector3>(stackPos, LuaType.Vector);

	public void PushAngle(in QAngle val) => PushValueUserType(in val, LuaType.Angle);

	public void PushVector(in Vector3 val) => PushValueUserType(in val, LuaType.Vector);

	public void SetState(lua_State state) => this.state = state.Handle;

	public int CreateMetaTable(ReadOnlySpan<char> name) {
		int type = nextMetaTableType;
		if (type > MaxCachedMetaTables)
			Dbg.Error("CLuaInterface::CreateMetaTable - out of meta table types!\n");
		using Utf8 tname = new(name, stackalloc byte[128]);
		if (luaL_newmetatable_type(state, tname.Pointer, type) != 0) {
			if (metaTableRefs[type] != -1)
				luaL_unref(state, LuaIndex.Registry, metaTableRefs[type]);
			lua_pushvalue(state, -1);
			metaTableRefs[type] = luaL_ref(state, LuaIndex.Registry);
			nextMetaTableType++;
			return type;
		}
		fixed (byte* key = "MetaID\0"u8)
			Op(SN_GETFIELD, -1, 0, key);
		int existing = (int)lua_tonumber(state, -1);
		lua_settop(state, -2);
		return existing;
	}

	public bool PushMetaTable(LuaType type) {
		if ((uint)type > MaxCachedMetaTables || metaTableRefs[(int)type] == -1)
			return false;
		lua_rawgeti(state, LuaIndex.Registry, metaTableRefs[(int)type]);
		return true;
	}

	public void PushUserType(nint data, LuaType type) {
		UserData* ud = (UserData*)lua_newuserdata(state, (nuint)sizeof(UserData));
		ud->Data = data;
		ud->Type = (byte)type;
		if (PushMetaTable(type))
			lua_setmetatable(state, -2);
	}

	public void SetUserType(int stackPos, nint data) {
		UserData* ud = (UserData*)lua_touserdata(state, stackPos);
		if (ud != null)
			ud->Data = data;
	}

	public void PushValueUserType<T>(in T val, LuaType type) where T : unmanaged {
		UserData* ud = (UserData*)lua_newuserdata(state, (nuint)(sizeof(UserData) + sizeof(T)));
		T* payload = (T*)(ud + 1);
		*payload = val;
		ud->Data = (nint)payload;
		ud->Type = (byte)type;
		if (PushMetaTable(type))
			lua_setmetatable(state, -2);
	}

	public ref T GetValueUserType<T>(int stackPos, LuaType type) where T : unmanaged {
		UserData* ud = (UserData*)lua_touserdata(state, stackPos);
		if (ud != null && ud->Data != 0 && (LuaType)ud->Type == type)
			return ref *(T*)ud->Data;
		DefaultValue<T>.Value = default;
		return ref DefaultValue<T>.Value;
	}

	public void PushObjectUserType<T>(T? obj, LuaType type) where T : class {
		PushUserType(obj == null ? 0 : AcquireUserTypeSlot(obj), type);
	}

	public T? GetObjectUserType<T>(int stackPos, LuaType type) where T : class {
		if (!IsType(stackPos, type))
			return null;
		UserData* ud = (UserData*)lua_touserdata(state, stackPos);
		if (ud == null || ud->Data == 0)
			return null;
		long handle = ud->Data;
		int slot = (int)(handle & 0xFFFFFFFF) - 1;
		int generation = (int)(handle >> 32);
		if ((uint)slot >= (uint)userTypeSlotCount || userTypeGenerations[slot] != generation)
			return null;
		return userTypeObjects[slot] as T;
	}

	public void ReleaseUserTypeObject(object obj) {
		if (!userTypeSlotsByObject.Remove(obj, out int slot))
			return;
		userTypeObjects[slot] = null;
		userTypeGenerations[slot]++;
		freeUserTypeSlots.Push(slot);
	}

	nint AcquireUserTypeSlot(object obj) {
		if (!userTypeSlotsByObject.TryGetValue(obj, out int slot)) {
			if (!freeUserTypeSlots.TryPop(out slot)) {
				slot = userTypeSlotCount++;
				if (slot == userTypeObjects.Length) {
					Array.Resize(ref userTypeObjects, slot * 2);
					Array.Resize(ref userTypeGenerations, slot * 2);
				}
			}
			userTypeObjects[slot] = obj;
			userTypeSlotsByObject[obj] = slot;
		}
		return (nint)(((long)userTypeGenerations[slot] << 32) | (uint)(slot + 1));
	}

	public void Cycle() {
		Array.Clear(returnObjects);
		DoStackCheck();
		RunThreadedCalls();
	}

	public ILuaObject Global() => global!;

	public ILuaObject GetObject(int index) {
		ILuaObject obj = NewTemporaryObject();
		obj.SetFromStack(index);
		return obj;
	}

	public void PushLuaObject(ILuaObject? obj) {
		if (obj != null)
			obj.Push();
		else
			lua_pushnil(state);
	}

	public void PushLuaFunction(CFunc func) => lua_pushcclosure(state, GetFunctionPointer(func), 0);

	public void LuaError(ReadOnlySpan<char> err, int index) {
		if (index == -1) {
			ErrorNoHalt(err);
			return;
		}
		ArgError(index, err);
	}

	[DoesNotReturn]
	public void TypeError(ReadOnlySpan<char> name, int index) => throw new LuaException(LuaRaise.Type, ERRRUN, 0, AbsIndex(index), name.ToString());

	public void CallInternal(int args, int rets) {
		if (GetType(-(args + 1)) != LuaType.Function)
			Dbg.Error("Lua tried to call non functions");
		if (!ThreadInMainThread())
			Dbg.Error("Calling Lua function in a thread other than main!\n");
		if (rets > MaxReturns)
			Dbg.Error("[CLuaInterface::Call] Expecting more returns than possible");
		Array.Clear(returnObjects);
		for (int i = 0; i < rets; i++)
			returnObjects[i] = NewTemporaryObject();
		if (!CallFunctionProtected(args, rets, false)) {
			gameCallback!.LuaError(in g_LastError);
			return;
		}
		for (int i = 0; i < rets; i++)
			(returnObjects[i] ??= NewTemporaryObject()).SetFromStack(-1 - i);
		Pop(rets);
	}

	public void CallInternalNoReturns(int args) {
		if (!CallFunctionProtected(args, 0, false))
			gameCallback!.LuaError(in g_LastError);
	}

	public bool CallInternalGetBool(int args) {
		if (!CallFunctionProtected(args, 1, false)) {
			gameCallback!.LuaError(in g_LastError);
			return false;
		}
		bool ret = GetType(-1) == LuaType.Bool && GetBool(-1);
		Pop(1);
		return ret;
	}

	public string? CallInternalGetString(int args) {
		if (!CallFunctionProtected(args, 1, false)) {
			gameCallback!.LuaError(in g_LastError);
			return null;
		}
		string? ret = GetType(-1) == LuaType.String ? GetString(-1) : null;
		Pop(1);
		return ret;
	}

	public bool CallInternalGet(int args, ILuaObject obj) {
		if (!CallFunctionProtected(args, 1, false)) {
			gameCallback!.LuaError(in g_LastError);
			return false;
		}
		obj.SetFromStack(-1);
		Pop(1);
		return true;
	}

	public void NewGlobalTable(ReadOnlySpan<char> name) {
		lua_createtable(state, 0, 0);
		SetField(LuaIndex.Global, name);
	}

	public ILuaObject NewTemporaryObject() {
		int index = temporaryObjectIndex + 1;
		if (index >= TemporaryObjectCount)
			index = 0;
		temporaryObjectIndex = index;
		ILuaObject? obj = temporaryObjects[index];
		if (obj != null) {
			obj.UnReference();
			return obj;
		}
		return temporaryObjects[index] = CreateObject();
	}

	public bool isUserData(int index) => lua_type(state, index) == TUSERDATA;

	public ILuaObject? GetMetaTableObject(ReadOnlySpan<char> name, int type) {
		GetField(LuaIndex.Registry, name);
		if (GetType(-1) != LuaType.Table) {
			Pop(1);
			if (type == -1)
				return null;
			CreateMetaTableType(name, type);
		}
		ILuaObject obj = NewTemporaryObject();
		obj.SetFromStack(-1);
		Pop(1);
		return obj;
	}

	public ILuaObject? GetMetaTableObject(int index) {
		if (lua_getmetatable(state, index) == 0)
			return null;
		ILuaObject obj = NewTemporaryObject();
		obj.SetFromStack(-1);
		Pop(1);
		return obj;
	}

	public ILuaObject GetReturn(int index) {
		if (index > MaxReturns - 1)
			Dbg.Error("Tried to get return higher than max");
		ILuaObject? obj = returnObjects[index];
		if (obj == null)
			Dbg.Error("Error: Calling GetReturn on an invalid return! Check code!!\n");
		return obj!;
	}

	public bool IsServer() => luaType == 1;
	public bool IsClient() => luaType == 0;
	public bool IsMenu() => luaType == 2;

	public void DestroyObject(ILuaObject? obj) => gameCallback!.DestroyLuaObject(obj!);

	public ILuaObject CreateObject() => gameCallback!.CreateLuaObject();

	public void SetMember(ILuaObject table, ILuaObject key, ILuaObject? value) {
		if (!table.isTable())
			return;
		table.Push();
		key.Push();
		PushLuaObject(value);
		SetTable(-3);
		Pop(1);
	}

	public ILuaObject GetNewTable() {
		CreateTable();
		ILuaObject obj = GetObject(-1);
		Pop(1);
		return obj;
	}

	public void SetMember(ILuaObject table, float key) {
		table.Push();
		PushNumber(key);
		lua_pushvalue(state, -3);
		SetTable(-3);
		Pop(2);
	}

	public void SetMember(ILuaObject table, float key, ILuaObject? value) {
		table.Push();
		PushNumber(key);
		PushLuaObject(value);
		SetTable(-3);
		Pop(1);
	}

	public void SetMember(ILuaObject table, ReadOnlySpan<char> key) {
		if (!table.isTable())
			return;
		table.Push();
		PushString(key);
		lua_pushvalue(state, -3);
		SetTable(-3);
		Pop(2);
	}

	public void SetMember(ILuaObject table, ReadOnlySpan<char> key, ILuaObject? value) {
		if (!table.isTable())
			return;
		table.Push();
		PushString(key);
		PushLuaObject(value);
		SetTable(-3);
		Pop(1);
	}

	public void SetType(byte type) => luaType = type;

	public void PushLong(long num) => lua_pushnumber(state, (int)num);

	public int GetFlags(int index) {
		ILuaObject? table = GetObject(index);
		if (table == null)
			return 0;
		if (!table.isTable())
			return table.GetInt();
		ILuaObject member = CreateObject();
		int flags = 0;
		for (int i = 1; ; i++) {
			member.UnReference();
			table.GetMember(i, member);
			if (member.isNil())
				break;
			flags |= member.GetInt();
		}
		DestroyObject(member);
		return flags;
	}

	public bool FindOnObjectsMetaTable(int objIndex, int keyIndex) {
		if (lua_getmetatable(state, objIndex) == 0)
			return false;
		lua_pushvalue(state, keyIndex);
		GetTable(-2);
		if (lua_type(state, -1) != TNIL)
			return true;
		Pop(1);
		return false;
	}

	public bool FindObjectOnTable(int tableIndex, int keyIndex) {
		lua_pushvalue(state, tableIndex);
		lua_pushvalue(state, keyIndex);
		GetTable(-2);
		return lua_type(state, -1) != TNIL;
	}

	public void SetMemberFast(ILuaObject table, int keyIndex, int valueIndex) {
		table.Push();
		lua_pushvalue(state, keyIndex);
		lua_pushvalue(state, valueIndex);
		SetTable(-3);
		Pop(1);
	}

	public bool RunString(ReadOnlySpan<char> filename, ReadOnlySpan<char> path, ReadOnlySpan<char> stringToRun, bool run, bool showErrors) => throw new NotImplementedException();

	public bool IsEqual(ILuaObject? objA, ILuaObject? objB) {
		if (objA == null || objB == null)
			return false;
		if (objA.GetType() != objB.GetType())
			return false;
		objA.Push();
		objB.Push();
		bool equal = Op(SN_EQUAL, -1, AbsIndex(-2)) != 0;
		Pop(2);
		return equal;
	}

	[DoesNotReturn]
	public void Error(ReadOnlySpan<char> err) => throw new LuaException(LuaRaise.Message, ERRRUN, 0, 0, err.ToString());

	public string GetStringOrError(int index) {
		string? str = GetString(index);
		if (str == null)
			TypeError("string", index);
		return str;
	}

	public bool RunLuaModule(ReadOnlySpan<char> name) => throw new NotImplementedException();
	public bool FindAndRunScript(ReadOnlySpan<char> filename, bool run, bool showErrors, ReadOnlySpan<char> stringToRun, bool noReturns) => throw new NotImplementedException();
	public void SetPathID(ReadOnlySpan<char> pathID) => throw new NotImplementedException();
	public string GetPathID() => throw new NotImplementedException();

	public void ErrorNoHalt(ReadOnlySpan<char> msg) => gameCallback!.ErrorPrint(FormatMessage(msg), true);

	public void Msg(ReadOnlySpan<char> msg) => gameCallback!.Msg(FormatMessage(msg), true);

	static string FormatMessage(ReadOnlySpan<char> msg) {
		msg = UntilNul(msg);
		int max = MessageBufferSize - 1;
		if (Encoding.UTF8.GetByteCount(msg) <= max)
			return msg.ToString();
		byte[] bytes = Encoding.UTF8.GetBytes(msg.ToString());
		return Encoding.UTF8.GetString(bytes, 0, max);
	}

	public void PushPath(ReadOnlySpan<char> path) => throw new NotImplementedException();
	public void PopPath() => throw new NotImplementedException();
	public string GetPath() => throw new NotImplementedException();
	public Color GetColor(int index) => throw new NotImplementedException();
	public void PushColor(Color color) => throw new NotImplementedException();

	public int GetStack(int level, ref lua_Debug dbg) {
		fixed (lua_Debug* ar = &dbg)
			return lua_getstack(state, level, ar);
	}

	public int GetInfo(ReadOnlySpan<char> what, ref lua_Debug dbg) {
		using Utf8 w = new(what, stackalloc byte[16]);
		fixed (lua_Debug* ar = &dbg)
			return lua_getinfo(state, w.Pointer, ar);
	}

	public string? GetLocal(ref lua_Debug dbg, int n) => throw new NotImplementedException();
	public string? GetUpvalue(int funcIndex, int n) => throw new NotImplementedException();
	public bool RunStringEx(ReadOnlySpan<char> filename, ReadOnlySpan<char> path, ReadOnlySpan<char> stringToRun, bool run, bool printErrors, bool dontPushErrors, bool noReturns) => throw new NotImplementedException();
	public ReadOnlySpan<byte> GetDataString(int index) => throw new NotImplementedException();
	public void ErrorFromLua(ReadOnlySpan<char> msg) => throw new NotImplementedException();
	public string GetCurrentLocation() => throw new NotImplementedException();

	public void MsgColour(in Color col, ReadOnlySpan<char> msg) => gameCallback!.MsgColour(FormatMessage(msg), in col);

	public void GetCurrentFile(out string outStr) => throw new NotImplementedException();
	public bool CompileString(out byte[] dump, ReadOnlySpan<char> stringToCompile) => throw new NotImplementedException();

	public bool CallFunctionProtected(int args, int rets, bool showError) {
		nint L = state;
		int errorFunc = lua_gettop(L) - args;
		lua_rawgeti(L, LuaIndex.Registry, errorReporterRef);
		lua_insert(L, errorFunc);
		int status = PCall(args, rets, errorFunc);
		lua_remove(state, errorFunc);
		if (status == OK)
			return true;

		LuaType type = GetType(-1);
		if (type == LuaType.Nil)
			g_LastError.Message = "CallFunctionProtected was passed a nil as the error message!";
		else if (type == LuaType.String) {
			string message = GetString(-1)!;
			if (message.Length != 0) {
				g_LastError.Message = message;
				ReadStackIntoError(state);
			}
		}
		else
			g_LastError.Message = $"Unhandled error type {type} in CallFunctionProtected!";
		Pop(1);

		g_LastError.Side = IsServer() ? "server" : IsMenu() ? "menu" : "client";
		if (showError)
			gameCallback!.LuaError(in g_LastError);
		return false;
	}

	public void Require(ReadOnlySpan<char> name) => throw new NotImplementedException();

	public string GetActualTypeName(int stackPos) {
		byte* name = lua_typename(state, lua_type(state, stackPos), stackPos);
		return Marshal.PtrToStringUTF8((nint)name) ?? "";
	}

	public void PreCreateTable(int arrelems, int nonarrelems) => lua_createtable(state, arrelems, nonarrelems);

	public void PushPooledString(int index) {
		stringPool!.Push();
		lua_rawgeti(state, -1, index + 1);
		lua_remove(state, -2);
	}

	public string GetPooledString(int index) => PooledStrings.g_PooledStrings[index];

	public int AddThreadedCall(ILuaThreadedCall call) {
		threadedCalls.AddLast(call);
		return threadedCalls.Count;
	}

	public void AppendStackTrace(StringBuilder output) => throw new NotImplementedException();
	public ConVar CreateConVar(ReadOnlySpan<char> name, ReadOnlySpan<char> defaultValue, ReadOnlySpan<char> helpString, int flags) => throw new NotImplementedException();
	public ConCommand CreateConCommand(ReadOnlySpan<char> name, ReadOnlySpan<char> helpString, int flags, FnCommandCallback? callback, FnCommandCompletionCallback? completionCallback) => throw new NotImplementedException();

	public string CheckStringOpt(int stackPos, ReadOnlySpan<char> def) {
		if (lua_type(state, stackPos) <= TNIL)
			return def.ToString();
		return CheckString(stackPos);
	}

	public double CheckNumberOpt(int stackPos, double def) {
		if (lua_type(state, stackPos) <= TNIL)
			return def;
		return CheckNumber(stackPos);
	}

	public int RegisterMetaTable(ReadOnlySpan<char> name, ILuaObject tbl) {
		GetField(LuaIndex.Registry, name);
		LuaType type = GetType(-1);
		if (type == LuaType.Nil) {
			Pop(1);
			tbl.SetMemberDouble("MetaID", nextMetaTableType++);
			tbl.SetMember("MetaName", name);
			tbl.Push();
			SetField(LuaIndex.Registry, name);
			GetField(LuaIndex.Registry, name);
		}
		else if (type != LuaType.Table) {
			Pop(1);
			return -1;
		}
		GetField(-1, "MetaID");
		int id = (int)lua_tonumber(state, -1);
		lua_settop(state, -2);
		return id;
	}
}
