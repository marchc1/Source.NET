#include "CLuaInterface.h"
#include "PooledStrings.h"
#include <filesystem>
#include <algorithm>

// CLuaInterface things

ILuaGameCallback::CLuaError* ReadStackIntoError(lua_State* L)
{
	// VPROF ReadStackIntoError GLua

	int level = 0;
	lua_Debug ar;
	ILuaGameCallback::CLuaError* lua_error = new ILuaGameCallback::CLuaError;
	while (lua_getstack(L, level, &ar)) {
		lua_getinfo(L, "nSl", &ar);

		lua_error->stack.emplace_back();
		ILuaGameCallback::CLuaError::StackEntry& entry = lua_error->stack.back();
		entry.source = ar.source ? ar.source : "unknown";
		entry.function = ar.name ? ar.name : "unknown";

		entry.line = ar.currentline;

		++level;
	}

	const char* str = lua_tolstring(L, -1, NULL);
	if (str != NULL) // Setting a std::string to NULL causes a crash. Don't care.
		lua_error->message = str;

	CLuaInterface* LUA = (CLuaInterface*)L->luabase;
	lua_error->side = LUA->IsClient() ? "client" : (LUA->IsMenu() ? "menu" : "server");

	return lua_error;
}

int AdvancedLuaErrorReporter(lua_State *L) 
{
	// VPROF AdvancedLuaErrorReporter GLua

	if (lua_isstring(L, 1)) {
		// const char* str = lua_tostring(L, 1);

		// g_LastError.assign(str);

		CLuaInterface* LUA = (CLuaInterface*)L->luabase;
		lua_pushvalue(L, 1);
		LUA->GetLuaGameCallback()->LuaError(ReadStackIntoError(L));
		lua_pop(L, 1);

		// lua_pushstring(L, g_LastError.c_str());
	}

	return 0;
}

CLuaInterface::~CLuaInterface()
{
	if (m_pState != nullptr)
	{
		Shutdown();
		return;
	}

	// This is just for safety to ensure no memory leaks.
	for (int i=0; i<UCHAR_MAX; ++i) {
		if (m_pMetaTables[i])
		{
			DestroyObject(m_pMetaTables[i]);
			m_pMetaTables[i] = nullptr;
		}
	}

	for (int i=0; i<LUA_MAX_TEMP_OBJECTS; ++i) {
		if (m_TempObjects[i])
		{
			DestroyObject(m_TempObjects[i]);
			m_TempObjects[i] = nullptr;
		}
	}
}

int CLuaInterface::Top()
{
	return lua_gettop(m_pState);
}

void CLuaInterface::Push(int iStackPos)
{
	lua_pushvalue(m_pState, iStackPos);
}

void CLuaInterface::Pop(int iAmt)
{
	lua_pop(m_pState, iAmt);
#if _DEBUG
	if (lua_gettop(m_pState) < 0)
	{
		__debugbreak();
		m_pGameCallback->ErrorPrint("CLuaInterface::Pop -> That was too much :<", true);
	}
#endif
}

void CLuaInterface::GetTable(int iStackPos)
{
	lua_gettable(m_pState, iStackPos);
}

void CLuaInterface::GetField(int iStackPos, const char* strName)
{
	lua_getfield(m_pState, iStackPos, strName);
}

void CLuaInterface::SetField(int iStackPos, const char* strName)
{
	lua_setfield(m_pState, iStackPos, strName);
}

void CLuaInterface::CreateTable()
{
	lua_createtable(m_pState, 0, 0);
}

void CLuaInterface::SetTable(int iStackPos)
{
	lua_settable(m_pState, iStackPos);
}

bool CLuaInterface::SetMetaTable(int iStackPos)
{
	return lua_setmetatable(m_pState, iStackPos);
}

bool CLuaInterface::GetMetaTable(int iStackPos)
{
	return lua_getmetatable(m_pState, iStackPos);
}

void CLuaInterface::Call(int iArgs, int iResults)
{
	lua_State* currentState = m_pState;
	lua_call(currentState, iArgs, iResults);
	SetState(currentState); // done for some reason, idk. Probably done in case the m_pState somehow changes like if you call a coroutine?
}

int CLuaInterface::PCall(int iArgs, int iResults, int iErrorFunc)
{
	lua_State* pCurrentState = m_pState;
	int ret = lua_pcall(pCurrentState, iArgs, iResults, iErrorFunc);
	SetState(pCurrentState);
	return ret;
}

int CLuaInterface::Equal(int iA, int iB)
{
	return lua_equal(m_pState, iA, iB);
}

int CLuaInterface::RawEqual(int iA, int iB)
{
	return lua_rawequal(m_pState, iA, iB);
}

void CLuaInterface::Insert(int iStackPos)
{
	lua_insert(m_pState, iStackPos);
}

void CLuaInterface::Remove(int iStackPos)
{
	lua_remove(m_pState, iStackPos);
}

int CLuaInterface::Next(int iStackPos)
{
	return lua_next(m_pState, iStackPos);
}

ILuaBase::UserData* CLuaInterface::NewUserdata(unsigned int iSize)
{
	return (ILuaBase::UserData*)lua_newuserdata(m_pState, iSize, UDTYPE_USERDATA);
}

[[noreturn]] void CLuaInterface::ThrowError(const char* strError)
{
	luaL_error(m_pState, "%s", strError);
}

void CLuaInterface::CheckType(int iStackPos, int iType)
{
	int actualType = GetType(iStackPos);
	if (actualType != iType)
		TypeError(GetTypeName(iType), iStackPos);
}

[[noreturn]] void CLuaInterface::ArgError(int iArgNum, const char* strMessage)
{
	luaL_argerror(m_pState, iArgNum, strMessage);
}

void CLuaInterface::RawGet(int iStackPos)
{
	lua_rawget(m_pState, iStackPos);
}

void CLuaInterface::RawSet(int iStackPos)
{
	lua_rawset(m_pState, iStackPos);
}

const char* CLuaInterface::GetString(int iStackPos, unsigned int* iOutLen)
{
	size_t length;
	const char* pString = lua_tolstring(m_pState, iStackPos, &length);
	if (iOutLen)
		*iOutLen = length;

	return pString;
}

double CLuaInterface::GetNumber(int iStackPos)
{
	return lua_tonumber(m_pState, iStackPos);
}

bool CLuaInterface::GetBool(int iStackPos)
{
	return lua_toboolean(m_pState, iStackPos);
}

CFunc CLuaInterface::GetCFunction(int iStackPos)
{
	return lua_tocfunction(m_pState, iStackPos);
}

ILuaBase::UserData* CLuaInterface::GetUserdata(int iStackPos)
{
	return (ILuaBase::UserData*)lua_touserdata(m_pState, iStackPos);
}

void CLuaInterface::PushNil()
{
	lua_pushnil(m_pState);
}

void CLuaInterface::PushString(const char* val, unsigned int iLen)
{
	if (iLen > 0)
		lua_pushlstring(m_pState, val, iLen);
	else
		lua_pushstring(m_pState, val);
}

void CLuaInterface::PushNumber(double val)
{
	lua_pushnumber(m_pState, val);
}

void CLuaInterface::PushBool(bool val)
{
	lua_pushboolean(m_pState, val);
}

void CLuaInterface::PushCFunction(CFunc val)
{
	lua_pushcclosure(m_pState, val, 0);
}

void CLuaInterface::PushCClosure(CFunc val, int iVars)
{
	lua_pushcclosure(m_pState, val, iVars);
}

void CLuaInterface::PushUserdata(ILuaBase::UserData* val)
{
	lua_pushlightuserdata(m_pState, val);
}

int CLuaInterface::ReferenceCreate()
{
	return luaL_ref(m_pState, LUA_REGISTRYINDEX);
}

void CLuaInterface::ReferenceFree(int i)
{
	luaL_unref(m_pState, LUA_REGISTRYINDEX, i);
}

void CLuaInterface::ReferencePush(int i)
{
	lua_rawgeti(m_pState, LUA_REGISTRYINDEX, i);
}

void CLuaInterface::PushSpecial(int iType)
{
	switch (iType) {
		case SPECIAL_GLOB:
			lua_pushvalue(m_pState, LUA_GLOBALSINDEX);
			break;
		case SPECIAL_ENV:
			lua_pushvalue(m_pState, LUA_ENVIRONINDEX);
			break;
		case SPECIAL_REG:
			lua_pushvalue(m_pState, LUA_REGISTRYINDEX);
			break;
		default:
			lua_pushnil(m_pState);
			break;
	}
}

bool CLuaInterface::IsType(int iStackPos, int iType)
{
	int actualType = lua_type(m_pState, iStackPos);

	if (actualType == iType)
		return true;

	if (actualType == Type::UserData && iType > Type::UserData)
	{
		GCudata* pData = lua_getuserdata(m_pState, iStackPos);
		if (pData)
		{
			if (pData->udtype != UDTYPE_USERDATA) // If the type is not UDTYPE_USERDATA, then it's our funny one
				return iType == pData->udtype;

			return iType == ((ILuaBase::UserData*)uddata(pData))->type;
		}
	}

	return false;
}

int CLuaInterface::GetType(int iStackPos) // WHY DOES THIS USE A SWITCH BLOCK IN GMOD >:(
{
	int type = lua_type(m_pState, iStackPos);

	if (type == Type::UserData)
	{
		GCudata* pData = lua_getuserdata(m_pState, iStackPos);
		if (pData)
		{
			if (pData->udtype != UDTYPE_USERDATA) // If the type is not UDTYPE_USERDATA, then it's our funny one
				return pData->udtype;

			return ((ILuaBase::UserData*)uddata(pData))->type;
		}
	}

	return type == -1 ? Type::Nil : type;
}

const char* CLuaInterface::GetTypeName(int iType)
{
	if (iType < 0)
		return "none";

	constexpr int typeCount = sizeof(Type::Name) / sizeof(const char*);
	if (iType <= typeCount)
		return Type::Name[iType];

	return "unknown";
}

void CLuaInterface::CreateMetaTableType(const char* strName, int iType)
{
	int ret = luaL_newmetatable_type(m_pState, strName, iType);
	if (ret && iType < UCHAR_MAX)
	{
		ILuaObject* pObject = m_pMetaTables[iType];
		if (!pObject)
		{
			pObject = CreateObject();
			pObject->SetFromStack(-1);
			m_pMetaTables[iType] = pObject;
		}
		pObject->SetFromStack(-1);
	}
}

const char* CLuaInterface::CheckString(int iStackPos)
{
	return luaL_checklstring(m_pState, iStackPos, nullptr);
}

double CLuaInterface::CheckNumber(int iStackPos)
{
	return luaL_checknumber(m_pState, iStackPos);
}

int CLuaInterface::ObjLen(int iStackPos)
{
	return lua_objlen(m_pState, iStackPos);
}

static thread_local QAngle angle_fallback = QAngle(0, 0, 0);
const QAngle& CLuaInterface::GetAngle(int iStackPos)
{
	ILuaBase::UserData* udata = GetUserdata(iStackPos);
	if (!udata || !udata->data || udata->type != Type::Angle)
		return angle_fallback;

	return *(QAngle*)udata->data;
}

static thread_local Vector vector_fallback = Vector(0, 0, 0);
const Vector& CLuaInterface::GetVector(int iStackPos)
{
	ILuaBase::UserData* udata = GetUserdata(iStackPos);
	if (!udata || !udata->data || udata->type != Type::Vector)
		return vector_fallback;

	return *(Vector*)udata->data;
}

void CLuaInterface::PushAngle(const QAngle& val)
{
	ILuaBase::UserData* udata = NewUserdata(20); // Should we use PushUserType?
	*(QAngle*)udata->data = val;
	udata->type = Type::Angle;

	if (PushMetaTable(Type::Angle))
		SetMetaTable(-2);
}

void CLuaInterface::PushVector(const Vector& val)
{
	ILuaBase::UserData* udata = NewUserdata(20);
	*(Vector*)udata->data = val;
	udata->type = Type::Vector;

	if (PushMetaTable(Type::Vector))
		SetMetaTable(-2);
}

void CLuaInterface::SetState(lua_State* L)
{
	m_pState = L;
}

int CLuaInterface::CreateMetaTable(const char* strName)
{
	int ref = -1;
	GetField(LUA_REGISTRYINDEX, strName);
	if (IsType(-1, Type::Table))
		ref = ReferenceCreate();
	else
		Pop(1);

	if (ref != -1)
	{
		ReferencePush(ref);
		ReferenceFree(ref);
		lua_getfield(m_pState, -1, "MetaID");
		int metaID = (int)lua_tonumber(m_pState, -1);
		lua_pop(m_pState, 1);
		return metaID;
	} else {
		// Missing this logic in lua-shared, CreateMetaTable creates it if it's missing, just as its name would imply.
		luaL_newmetatable_type(m_pState, strName, ++m_iMetaTableIDCounter);
		ILuaObject* pObject = CreateObject();
		pObject->SetFromStack(-1);
		m_pMetaTables[m_iMetaTableIDCounter] = pObject;
		return m_iMetaTableIDCounter;
	}
}

bool CLuaInterface::PushMetaTable(int iType)
{
	if (iType < UCHAR_MAX)
	{
		ILuaObject* pMetaObject = m_pMetaTables[iType];
		if (pMetaObject)
		{
			pMetaObject->Push();
			return true;
		}
	}

	return false;
}

void CLuaInterface::PushUserType(void* data, int iType)
{
	ILuaBase::UserData* udata = NewUserdata(sizeof(ILuaBase::UserData));
	udata->data = data;
	udata->type = (unsigned char)iType;

	if (PushMetaTable(iType))
		SetMetaTable(-2);
}

void CLuaInterface::SetUserType(int iStackPos, void* data)
{
	GCudata* pData = lua_getuserdata(m_pState, iStackPos);
	if (pData)
	{
		if (pData->udtype == UDTYPE_USERDATA)
		{
			ILuaBase::UserData* pLuaData = (ILuaBase::UserData*)uddata(pData);
			pLuaData->data = data;
		} else if (pData->udtype > UDTYPE__MAX) {
			// We'll assume it has a void* as the first 4-8 bytes.
			// If this assumption fails - gg
			void** pDataPointer = (void**)uddata(pData);
			*pDataPointer = data;
		} else {
			__debugbreak();
		}
	}
}

// =================================
// ILuaInterface implementations
// =================================

int LuaPanic(lua_State* lua)
{
	CLuaInterface* pInterface = (CLuaInterface*)lua->luabase;

	std::string errMsg = "Lua Panic! Something went horribly wrong!\n";
	errMsg.append(lua_tolstring(lua, -1, 0));
	pInterface->m_pGameCallback->ErrorPrint(errMsg.c_str(), true);
	return 0;
}

bool CLuaInterface::Init(ILuaGameCallback* callback, bool bIsServer)
{
	m_pGameCallback = callback;
	m_pGlobal = CreateObject();

	for (int i=0; i<LUA_MAX_TEMP_OBJECTS;++i)
	{
		m_TempObjects[i] = CreateObject();
	}

	m_iMetaTableIDCounter = Type::Type_Count;
	for (int i=0; i<UCHAR_MAX; ++i)
		m_pMetaTables[i] = nullptr;

	m_iCurrentTempObject = 0;

	m_bShutDownThreadedCalls = false;

	m_pState = luaL_newstate();
	luaL_openlibs(m_pState);

	m_pState->luabase = this;
	SetState(m_pState);

	lua_atpanic(m_pState, LuaPanic);

	lua_pushcclosure(m_pState, AdvancedLuaErrorReporter, 0);
	m_nLuaErrorReporter = luaL_ref(m_pState, LUA_REGISTRYINDEX); // Since this is the first ever ref call luaErrorReporter will always be 1

	{
		luaJIT_setmode(m_pState, -1, LUAJIT_MODE_WRAPCFUNC|LUAJIT_MODE_ON);

		// ToDo: Find out how to check the FPU precision
		// Warning("Lua detected bad FPU precision! Prepare for weirdness!");
	}

	int reference = -1;
	lua_getfield(m_pState, LUA_GLOBALSINDEX, "require"); // Keep the original require function
	if (IsType(-1, Type::Function))
	{
		reference = ReferenceCreate();
	} else {
		Pop(1);
	}

	DoStackCheck();

	lua_pushvalue(m_pState, LUA_GLOBALSINDEX);
	m_pGlobal->SetFromStack(-1);
	Pop(1);

	if (reference != -1)
	{
		ReferencePush(reference);
		SetMember(Global(), "requiree");
		ReferenceFree(reference);
	}

	DoStackCheck();

	constexpr int pooledStrings = sizeof(g_PooledStrings) / sizeof(const char*);
	lua_createtable(m_pState, pooledStrings, 0);
	
	int idx = 0;
	for(const char* str : g_PooledStrings)
	{
		++idx;
		PushNumber(idx);
		PushString(str);
		SetTable(-3);
	}

	m_pStringPool = CreateObject();
	m_pStringPool->SetFromStack(-1);
	Pop(1);

	DoStackCheck();


	// lua_pushnil(m_pState);
	// lua_setfield(m_pState, -2, "setlocal");

	// lua_pushnil(m_pState);
	// lua_setfield(m_pState, -2, "setupvalue");

	// lua_pushnil(m_pState);
	// lua_setfield(m_pState, -2, "upvalueid");

	// lua_pushnil(m_pState);
	// lua_setfield(m_pState, -2, "upvaluejoin");

	// lua_pop(m_pState, 1);

	return true;
}

extern void GMOD_UnloadBinaryModules(lua_State* L);
void CLuaInterface::Shutdown()
{
	if (!m_pState)
	{
		__debugbreak();
		m_pGameCallback->ErrorPrint("CLuaInterface::Shutdown called while having no lua m_pState! Was it already called once?\n", true);
		return;
	}

	GMOD_UnloadBinaryModules(m_pState);
	ShutdownThreadedCalls();

	lua_close(m_pState);
	m_pState = nullptr;

	for (int i=0; i<UCHAR_MAX; ++i) {
		if (m_pMetaTables[i])
		{
			DestroyObject(m_pMetaTables[i]);
			m_pMetaTables[i] = nullptr;
		}
	}

	for (int i=0; i<LUA_MAX_TEMP_OBJECTS; ++i) {
		if (m_TempObjects[i])
		{
			DestroyObject(m_TempObjects[i]);
			m_TempObjects[i] = nullptr;
		}
	}
}

static thread_local int iLastTimeCheck = 0;
void CLuaInterface::Cycle()
{
	iLastTimeCheck = 0;
	// someotherValue = 0;
	// m_ProtectedFunctionReturns = nullptr; // Why would we want this? Sounds like a possible memory leak.
	DoStackCheck();

	RunThreadedCalls();
}

#include <mutex>
#include <unordered_set>
void CLuaInterface::RunThreadedCalls()
{
	// unordered_set instead of a vector to improve performance of the second pass
	std::unordered_set<ILuaThreadedCall*> pFinishedCalls;

	// We create a copy for the rare case that a task might call AddThreadedCall inside IsDone, if we didn't do this it could break iteration!
	m_pThreadedCallsMutex.lock(); // We don't need to keep it locked for longer thanks to our copy.
	std::list<ILuaThreadedCall*> pThreadedCalls = m_pThreadedCalls;
	m_pThreadedCallsMutex.unlock();

	for (auto it = pThreadedCalls.begin(); it != pThreadedCalls.end();)
	{
		if ((*it)->IsDone())
		{
			pFinishedCalls.insert(*it);
			continue;
		}

		it++;
	}

	// Second pass though without calling any callback ensuring that AddThreadedCall is not possibly invoked
	if (!pFinishedCalls.empty())
	{
		std::lock_guard<std::mutex> lock(m_pThreadedCallsMutex);
		m_pThreadedCalls.remove_if([&pFinishedCalls](ILuaThreadedCall* call) {
			return pFinishedCalls.find(call) != pFinishedCalls.end();
		});
	}

	for (ILuaThreadedCall* call : pFinishedCalls)
		call->Done(this);

	pFinishedCalls.clear();
}

int CLuaInterface::AddThreadedCall(ILuaThreadedCall* call)
{
	if (m_bShutDownThreadedCalls.load())
	{
		call->OnShutdown();
		return 0;
	}

	std::lock_guard<std::mutex> lock(m_pThreadedCallsMutex);
	m_pThreadedCalls.push_back(call);
	int nSize = m_pThreadedCalls.size(); // Just to be sure, idk if the Mutex would cover a call in the return

	return nSize;
}

void CLuaInterface::ShutdownThreadedCalls()
{
	m_bShutDownThreadedCalls.store(true);

	// We don't check for the case of a deadlock as it shouldn't happen that from inside IsDone it could enter ShutdownThreadedCalls?
	// If something inside the OnShutdown callback does try to add a new task m_bShutDownThreadedCalls will stop them.
	std::lock_guard<std::mutex> lock(m_pThreadedCallsMutex);

	for (ILuaThreadedCall* pCall : m_pThreadedCalls)
		pCall->OnShutdown();

	m_pThreadedCalls.clear();
}

ILuaObject* CLuaInterface::Global()
{
	return m_pGlobal;
}

ILuaObject* CLuaInterface::GetObject(int index)
{
	ILuaObject* obj = CreateObject();
	obj->SetFromStack(index);

	return obj;
}

void CLuaInterface::PushLuaObject(ILuaObject* obj)
{
	if (obj)
		obj->Push();
	else
		PushNil();
}

void CLuaInterface::PushLuaFunction(CFunc func)
{
	lua_pushcclosure(m_pState, func, 0);
}

void CLuaInterface::LuaError(const char* str, int iStackPos)
{
	if (iStackPos != -1)
		luaL_argerror(m_pState, iStackPos, str);
	else
		ErrorNoHalt("%s", str);
}

void CLuaInterface::TypeError(const char* str, int iStackPos)
{
	luaL_typerror(m_pState, iStackPos, str);
}

void CLuaInterface::CallInternal(int args, int rets)
{
	//if (!ThreadInMainThread())
	//	Error("Calling Lua function in a thread other than main!");

	if (rets >= LUA_MAX_RETURN_OBJECTS)
		Error("[CLuaInterface::Call] Expecting more returns than possible\n");

	for (int i=0; i<LUA_MAX_RETURN_OBJECTS; ++i)
		m_ProtectedFunctionReturns[i] = nullptr;

	if (IsType(-(args + 1), Type::Function))
	{
		if (CallFunctionProtected(args, rets, true))
		{
			for (int i=0; i<rets; ++i)
			{
				ILuaObject* obj = NewTemporaryObject();
				obj->SetFromStack(-1);
				m_ProtectedFunctionReturns[i] = obj;
				Pop(1);
			}
		}
	} else {
		Error("Lua tried to call non functions");
	}
}

void CLuaInterface::CallInternalNoReturns(int args)
{
	CallFunctionProtected(args, 0, true);
}

bool CLuaInterface::CallInternalGetBool(int args)
{
	bool ret = false;
	if (CallFunctionProtected(args, 1, 1))
	{
		ret = GetBool(-1);
		Pop(1);
	}

	return ret;
}

const char* CLuaInterface::CallInternalGetString(int args)
{
	const char* ret = nullptr;
	if (CallFunctionProtected(args, 1, 1))
	{
		ret = GetString(-1);
		Pop(1);
	}

	return ret;
}

bool CLuaInterface::CallInternalGet(int args, ILuaObject* obj)
{
	if (CallFunctionProtected(args, 1, 1))
	{
		obj->SetFromStack(-1);
		Pop(1);
		return true;
	}

	return false;
}

void CLuaInterface::NewGlobalTable(const char* name)
{
	lua_createtable(m_pState, 0, 0);
	lua_setfield(m_pState, LUA_GLOBALSINDEX, name);
}

ILuaObject* CLuaInterface::NewTemporaryObject()
{
	++m_iCurrentTempObject;
	if (m_iCurrentTempObject >= LUA_MAX_TEMP_OBJECTS)
		m_iCurrentTempObject = 0;

	ILuaObject* obj = m_TempObjects[m_iCurrentTempObject];
	if (obj)
	{
		obj->UnReference();
	} else {
		obj = CreateObject();
		m_TempObjects[m_iCurrentTempObject] = obj;
	}

	return obj;
}

bool CLuaInterface::isUserData(int iStackPos)
{
	return lua_type(m_pState, iStackPos) == Type::UserData;
}

ILuaObject* CLuaInterface::GetMetaTableObject(const char* name, int type)
{
	lua_getfield(m_pState, LUA_REGISTRYINDEX, name);
	if (GetType(-1) != 5)
	{
		Pop(1);
		if (type != -1)
		{
			CreateMetaTableType(name, type);
			lua_getfield(m_pState, LUA_REGISTRYINDEX, name);
		} else {
			return nullptr;
		}
	}

	ILuaObject* obj = NewTemporaryObject();
	obj->SetFromStack(-1);
	Pop(1);
	return obj;
}

ILuaObject* CLuaInterface::GetMetaTableObject(int iStackPos)
{
	if (lua_getmetatable(m_pState, iStackPos))
	{
		ILuaObject* obj = NewTemporaryObject();
		obj->SetFromStack(-1);
		Pop(1);
		return obj;
	}

	return nullptr;
}

ILuaObject* CLuaInterface::GetReturn(int iStackPos)
{
	int idx = abs(iStackPos);
	if (idx >= 0 && idx < 4)
	{
		if (m_ProtectedFunctionReturns[idx] == nullptr)
		{
#ifdef WIN32
			__debugbreak();
#endif
		}

		return m_ProtectedFunctionReturns[idx];
	}

#ifdef WIN32
	__debugbreak();
#endif
	return nullptr;
}

bool CLuaInterface::IsServer()
{
	return m_iRealm == State::SERVER;
}

bool CLuaInterface::IsClient()
{
	return m_iRealm == State::CLIENT;
}

bool CLuaInterface::IsMenu()
{
	return m_iRealm == State::MENU;
}

void CLuaInterface::DestroyObject(ILuaObject* obj)
{
	m_pGameCallback->DestroyLuaObject(obj);
}

ILuaObject* CLuaInterface::CreateObject()
{
	return m_pGameCallback->CreateLuaObject();
}

void CLuaInterface::SetMember(ILuaObject* obj, ILuaObject* key, ILuaObject* value)
{
	obj->Push();
	key->Push();
	if (value)
		value->Push();
	else
		lua_pushnil(m_pState);

	SetTable(-3);
	Pop(1);
}

ILuaObject* CLuaInterface::GetNewTable()
{
	CreateTable();
	ILuaObject* obj = CreateObject();
	obj->SetFromStack(-1);
	return obj;
}

void CLuaInterface::SetMember(ILuaObject* obj, float key)
{
	obj->Push();
	PushNumber(key);
	lua_pushvalue(m_pState, -3);
	SetTable(-3);
	Pop(2);
}

void CLuaInterface::SetMember(ILuaObject* obj, float key, ILuaObject* value)
{
	obj->Push();
	PushNumber(key);
	if (value)
		value->Push();
	else
		lua_pushnil(m_pState);

	SetTable(-3);
	Pop(1);
}

void CLuaInterface::SetMember(ILuaObject* obj, const char* key)
{
	obj->Push();
	PushString(key);
	lua_pushvalue(m_pState, -3);
	SetTable(-3);
	Pop(2);
}

void CLuaInterface::SetMember(ILuaObject* obj, const char* key, ILuaObject* value)
{
	obj->Push();
	PushString(key);
	if (value)
		value->Push();
	else
		lua_pushnil(m_pState);

	SetTable(-3);
	Pop(1);
}

void CLuaInterface::SetType(unsigned char realm)
{
	m_iRealm = realm;
}

void CLuaInterface::PushLong(long number)
{
	lua_pushnumber(m_pState, number);
}

int CLuaInterface::GetFlags(int iStackPos)
{
	return (int)GetNumber(iStackPos); // ToDo: Verify this
}

bool CLuaInterface::FindOnObjectsMetaTable(int iStackPos, int keyIndex)
{
	if (!lua_getmetatable(m_pState, iStackPos))
		return false;

	lua_pushvalue(m_pState, keyIndex);
	GetTable(-2);
	if (lua_type(m_pState, -1))
		return true;

	return false;
}

bool CLuaInterface::FindObjectOnTable(int iStackPos, int keyIndex)
{
	lua_pushvalue(m_pState, iStackPos);
	lua_pushvalue(m_pState, keyIndex);

	return lua_type(m_pState, -1) != 0;
}

void CLuaInterface::SetMemberFast(ILuaObject* pObj, int keyIndex, int valueIndex)
{
	if (pObj->isTable() || pObj->GetType() == Type::Table)
	{
		pObj->Push();
		Push(keyIndex);
		Push(valueIndex);
		SetTable(-3);
		Pop(1);
	}
}

bool CLuaInterface::RunString(const char* filename, const char* path, const char* stringToRun, bool run, bool showErrors)
{
	return RunStringEx(filename, path, stringToRun, run, showErrors, true, true);
}

bool CLuaInterface::IsEqual(ILuaObject* objA, ILuaObject* objB)
{
	objA->Push();
	objB->Push();
	bool ret = Equal(-1, -2);
	Pop(2);

	return ret;
}

void CLuaInterface::Error(const char* err)
{
	luaL_error(m_pState, "%s", err);
}

const char* CLuaInterface::GetStringOrError(int index)
{
	const char* string = lua_tolstring(m_pState, index, nullptr);
	if (!string)
		Error("You betraid me"); // ToDo: This should probably be an Arg error

	return string;
}

bool CLuaInterface::RunLuaModule(const char* name)
{
	// ToDo
	char* dest = new char[511];
	snprintf(dest, 511, "includes/modules/%s.lua", name);

	// NOTE: Why does it use !MODULE ?
	bool found = FindAndRunScript(dest, true, true, "", true);

	delete[] dest;

	return found;
}

std::string ToPath(std::string path)
{
	size_t lastSeparatorPos = path.find_last_of("/\\");

	std::string resultPath = path;
	if (lastSeparatorPos != std::string::npos)
		resultPath = path.substr(0, lastSeparatorPos + 1);

	if (resultPath.find("lua/") == 0)
		resultPath.erase(0, 4);

	if (resultPath.find("gamemodes/") == 0)
		resultPath.erase(0, 10);

	if (resultPath.rfind("addons/", 0) == 0) // ToDo: I think we can remove this again.
	{
		size_t first = path.find('/', 7);
		if (first != std::string::npos)
		{
			size_t second = path.find('/', first + 1);
			if (second != std::string::npos)
				resultPath = resultPath.substr(second + 1);
		}
	}

	return resultPath;
}

bool CLuaInterface::FindAndRunScript(const char *filename, bool run, bool showErrors, const char *stringToRun, bool noReturns)
{
	bool bDataTable = ((std::string)filename).rfind("!lua", 0) == 0;
	std::string filePath = filename;
	if (GetPath())
	{
		std::string currentPath = GetPath();
		if (!currentPath.empty() && currentPath.back() != '/')
			currentPath.append("/");

		if (filePath.rfind(currentPath, 0) != 0)
		{
			filePath = GetPath();
			filePath.append("/");
			filePath.append(filename);
		}

		if (filePath.find("lua/") == 0)
			filePath.erase(0, 4);

		if (filePath.find("gamemodes/") == 0)
			filePath.erase(0, 10);
	}

	LuaFile* file = g_pCLuaShared->LoadFile(filePath.c_str(), m_sPathID, bDataTable, true);
	if (!file)
	{
		filePath = filename;
		file = g_pCLuaShared->LoadFile(filename, m_sPathID, bDataTable, true);
	}

	bool ret = false;
	if (file)
	{
		PushPath(ToPath(filePath).c_str());
		ret = RunStringEx(filePath.c_str(), filePath.c_str(), file->GetContents(), true, showErrors, true, noReturns);
		PopPath();
	}

	if (!file)
		__debugbreak(); // Failed to find script!

	return ret;
}

void CLuaInterface::SetPathID(const char* pathID)
{
	strncpy(m_sPathID, pathID, sizeof(m_sPathID));
}

const char* CLuaInterface::GetPathID()
{
	return m_sPathID;
}

void CLuaInterface::ErrorNoHalt(const char* fmt, ...)
{
	ILuaGameCallback::CLuaError* error = ReadStackIntoError(m_pState);

	va_list args;
	va_start(args, fmt);

	int size = vsnprintf(nullptr, 0, fmt, args);
	if (size < 0) {
		va_end(args);
		return;
	}

	char* buffer = new char[size + 1];
	vsnprintf(buffer, size + 1, fmt, args);
	buffer[size] = '\0';

	error->message = buffer;
	va_end(args);

	m_pGameCallback->LuaError(error);

	delete error; // Deconstuctor will delete our buffer.
	delete buffer; // Update: I'm a idiot, it won't since a std::string copies it
}

void CLuaInterface::Msg(const char* fmt, ...)
{
	va_list args;
	va_start(args, fmt);

	char* buffer = new char[4096];
	vsnprintf(buffer, 4096, fmt, args);

	va_end(args);

	m_pGameCallback->Msg(buffer, false);

	delete[] buffer;
}

void CLuaInterface::PushPath(const char* path)
{
	m_CurrentPaths.emplace_back();
	strncpy(m_CurrentPaths.back().path, path, MAX_PATH);
}

void CLuaInterface::PopPath()
{
	m_CurrentPaths.pop_back();
}

const char* CLuaInterface::GetPath()
{
	return m_CurrentPaths.back().path;
}

int CLuaInterface::GetColor(int iStackPos) // Probably returns the StackPos
{
	int r, g, b, a = 0;
	ILuaObject* pObject = GetObject(iStackPos);
	if (!pObject)
		return 0xFFFFFFFF;

	r = pObject->GetMemberInt("r", 255);
	g = pObject->GetMemberInt("g", 255);
	b = pObject->GetMemberInt("b", 255);
	a = pObject->GetMemberInt("a", 255);

	return (a << 24) | (r << 16) | (g << 8) | b;
}

ILuaObject* CLuaInterface::PushColor(Color color)
{
	ILuaObject* pObject = CreateObject();
	pObject->SetMember("r", color.r());
	pObject->SetMember("g", color.g());
	pObject->SetMember("b", color.b());
	pObject->SetMember("a", color.a());
	ILuaObject* pMetaTable = GetMetaTableObject("Color", -1);
	if (pMetaTable)
		pObject->SetMetaTable(pMetaTable);

	pObject->Push();
	return pObject;
}

int CLuaInterface::GetStack(int level, lua_Debug* dbg)
{
	return lua_getstack(m_pState, level, dbg);
}

int CLuaInterface::GetInfo(const char* what, lua_Debug* dbg)
{
	return lua_getinfo(m_pState, what, dbg);
}

const char* CLuaInterface::GetLocal(lua_Debug* dbg, int n)
{
	return lua_getlocal(m_pState, dbg, n);
}

const char* CLuaInterface::GetUpvalue(int funcIndex, int n)
{
	return lua_getupvalue(m_pState, funcIndex, n);
}

bool CLuaInterface::RunStringEx(const char *filename, const char *path, const char *stringToRun, bool run, bool printErrors, bool dontPushErrors, bool noReturns)
{
	std::string code = RunMacros(stringToRun);
	int res = luaL_loadbuffer(m_pState, code.c_str(), code.length(), filename);
	if (res != 0)
	{
		ILuaGameCallback::CLuaError* err = ReadStackIntoError(m_pState);
		if (dontPushErrors)
			Pop(1);

		if (printErrors)
			m_pGameCallback->LuaError(err);

		delete err;

		return false;
	} else {
		return CallFunctionProtected(0, 0, printErrors);
	}
}

size_t CLuaInterface::GetDataString(int iStackPos, const char **pOutput)
{
	size_t length = 0;
	*pOutput = nullptr;
	const char* pString = lua_tolstring(m_pState, iStackPos, &length);
	if (!pString)
		return 0;

	*pOutput = pString;
	return length;
}

static thread_local char cMessageBuffer[4096];
void CLuaInterface::ErrorFromLua(const char *fmt, ...)
{
	ILuaGameCallback::CLuaError* error = ReadStackIntoError(m_pState);

	va_list args;
	va_start(args, fmt);

	int size = vsnprintf(nullptr, 0, fmt, args);
	if (size < 0) {
		va_end(args);
		return;
	}

	char* buffer = new char[size + 1];
	vsnprintf(buffer, size + 1, fmt, args);
	buffer[size] = '\0';

	error->message = buffer;
	va_end(args);

	m_pGameCallback->LuaError(error);

	delete error;
	delete buffer;
}

const char* CLuaInterface::GetCurrentLocation()
{
	lua_Debug ar;
	lua_getstack(m_pState, 1, &ar);
	lua_getinfo(m_pState, "Sl", &ar);
	if (ar.source && strcmp(ar.what, "C") != 0)
	{
		static thread_local char strOutput[512];
		snprintf(strOutput, sizeof(strOutput), "%s (line %i)", ar.source, ar.currentline);

		return strOutput;
	}

	return "<nowhere>";
}

void CLuaInterface::MsgColour(const Color& col, const char* fmt, ...)
{
	va_list args;
	va_start(args, fmt);
	vsnprintf(cMessageBuffer, sizeof(cMessageBuffer), fmt, args);
	va_end(args);

	m_pGameCallback->MsgColour(cMessageBuffer, col);
}

void CLuaInterface::GetCurrentFile(std::string &outStr)
{
	lua_Debug ar;
	int level = 0;
	while (lua_getstack(m_pState, level, &ar) != 0)
	{
		lua_getinfo(m_pState, "S", &ar);
		if (ar.source && strcmp(ar.what, "C") != 0)
		{
			outStr.assign(ar.source);
			return;
		}
		++level;
	}

	outStr = "!UNKNOWN";
}

int WriteToBuffer(lua_State* pState, const void *pData, size_t iSize, void* pBuffer)
{
	// ((Bootil::AutoBuffer*)pBuffer)->Write(pData, iSize);
	return 0;
}

bool CLuaInterface::CompileString(Bootil::Buffer& dumper, const std::string& stringToCompile)
{
	/*int loadResult = luaL_loadbufferx(m_pState, stringToCompile.c_str(), stringToCompile.size(), "", "t");
	if (loadResult != 0)
	{
		Pop(1);
		return 0;
	}

	bool success = lua_dump(m_pState, WriteToBuffer, &dumper) == 0;
	Pop(1);

	return success;*/
	__debugbreak(); // NOT IMPLEMENTED
	return false;
}

bool CLuaInterface::CallFunctionProtected(int iArgs, int iRets, bool showError)
{
	if (GetType(-(iArgs + 1)) != Type::Function)
	{
		__debugbreak();
		m_pGameCallback->ErrorPrint("[CLuaInterface::CallFunctionProtected] You betraid me. This is not a function :<\n", true);
		return false;
	}

	int nPos = lua_gettop(m_pState) - iArgs;
	lua_rawgeti(m_pState, LUA_REGISTRYINDEX, m_nLuaErrorReporter);
	lua_insert(m_pState, nPos);
	int ret = PCall(iArgs, iRets, -1);
	lua_remove(m_pState, nPos);
	if (ret != 0)
	{
		ILuaGameCallback::CLuaError* err = ReadStackIntoError(m_pState);
		if (showError)
			m_pGameCallback->LuaError(err);

		delete err;
		Pop(1);
	}

	return ret == 0;
}

extern void GMOD_LoadBinaryModule(lua_State* L, const char* name);
void CLuaInterface::Require(const char* cname)
{
	std::string name = cname;
	name = (IsClient() ? "gmcl_" : "gmsv_") + name + "_";

#ifdef SYSTEM_MACOSX
	name = name + "osx";
#else
	#ifdef SYSTEM_WINDOWS
		name = name + "win";
	#else
		name = name + "linux";
	#endif

	#ifdef ARCHITECTURE_X86_64
		name = name + "64";
	#else
		#ifdef SYSTEM_WINDOWS
			name = name + "32";
		#endif
	#endif
#endif
	name = name + ".dll";

	std::string path = (std::string)"garrysmod/lua/bin/" + name;
	if (std::filesystem::exists(path))
		GMOD_LoadBinaryModule(m_pState, path.c_str());
	else
		RunLuaModule(cname);
}

const char* CLuaInterface::GetActualTypeName(int typeID)
{
	return luaL_typename(m_pState, typeID);
}

void CLuaInterface::PreCreateTable(int arrelems, int nonarrelems)
{
	lua_createtable(m_pState, arrelems, nonarrelems);
}

void CLuaInterface::PushPooledString(int index)
{
	m_pStringPool->Push();
	lua_rawgeti(m_pState, -1, index + 1);
	lua_remove(m_pState, -2);
}

const char* CLuaInterface::GetPooledString(int index)
{
	return g_PooledStrings[index];
}

void CLuaInterface::AppendStackTrace(char* pOutput, unsigned int iOutputLength)
{
	if (!m_pState)
	{
		strncat(pOutput, "   Lua State = NULL\n\n", iOutputLength);
		return;
	}

	lua_Debug ar;
	int iStackLevel = 0;
	while (lua_getstack(m_pState, iStackLevel, &ar))
	{
		lua_getinfo(m_pState, "Slnu", &ar);

		char lineBuffer[256] = {0};
		snprintf(lineBuffer, sizeof(lineBuffer), "%d. %s - %s:%d\n", iStackLevel, ar.name, ar.short_src, ar.currentline);
		for (int i = 0; i <= iStackLevel; ++i)
			strncat(pOutput, "  ", iOutputLength);

		strncat(pOutput, lineBuffer, iOutputLength);

		if (++iStackLevel == 17)
			break;
	}

	if (iStackLevel == 0)
		strncat(pOutput, "\t*Not in Lua call OR Lua has panicked*\n", iOutputLength);

	strncat(pOutput, "\n", iOutputLength);
}

#ifndef FCVAR_LUA_CLIENT // 64x fun
static constexpr int FCVAR_GAMEDLL = (1 << 2);
static constexpr int FCVAR_CLIENTDLL = (1 << 3);
static constexpr int FCVAR_ARCHIVE = (1 << 7);
static constexpr int FCVAR_LUA_CLIENT = (1 << 18);
static constexpr int FCVAR_LUA_SERVER = (1 << 19);
static constexpr int FCVAR_SERVER_CAN_EXECUTE = (1 << 28);
static constexpr int FCVAR_CLIENTCMD_CAN_EXECUTE = (1 << 30);
#endif
int CLuaInterface::FilterConVarFlags(int& flags)
{
	flags &= ~(FCVAR_GAMEDLL | FCVAR_CLIENTDLL | FCVAR_LUA_CLIENT); // Check if FCVAR_RELEASE is added on 64x

	if (IsServer())
		flags |= FCVAR_GAMEDLL | FCVAR_LUA_SERVER;

	if (IsClient())
		flags |= FCVAR_CLIENTDLL | FCVAR_SERVER_CAN_EXECUTE | FCVAR_LUA_CLIENT;

	if (IsMenu())
		flags &= ~FCVAR_ARCHIVE;

	return IsMenu();
}

void* CLuaInterface::CreateConVar(const char* name, const char* defaultValue, const char* helpString, int flags)
{
	FilterConVarFlags(flags);

	__debugbreak(); // NOT IMPLEMENTED
	return nullptr;
}

void* CLuaInterface::CreateConCommand(const char* name, const char* helpString, int flags, FnCommandCallback_t callback, FnCommandCompletionCallback completionFunc)
{
	FilterConVarFlags(flags);
	if (IsServer())
		flags |= FCVAR_CLIENTCMD_CAN_EXECUTE;

	__debugbreak(); // NOT IMPLEMENTED
	return nullptr;
}

const char* CLuaInterface::CheckStringOpt(int iStackPos, const char* def)
{
	return luaL_optlstring(m_pState, iStackPos, def, nullptr);
}

double CLuaInterface::CheckNumberOpt(int iStackPos, double def)
{
	return luaL_optnumber(m_pState, iStackPos, def);
}

std::string replaceAll(std::string str, const std::string& from, const std::string& to)
{
    auto&& pos = str.find(from, size_t{});
    while (pos != std::string::npos)
    {
        str.replace(pos, from.length(), to);
        pos = str.find(from, pos + to.length());
    }

    return str;
}

std::string CLuaInterface::RunMacros(std::string code)
{
	// Bootil::String::Util::FindAndReplace(code, "DEFINE_BASECLASS", "local BaseClass = baseclass.Get");
	code = replaceAll(code, "DEFINE_BASECLASS", "local BaseClass = baseclass.Get");

	return code;
}

int CLuaInterface::RegisterMetaTable(const char* name, ILuaObject* pMetaObject)
{
	lua_getfield(m_pState, LUA_REGISTRYINDEX, name);
	if (GetType(-1) == 0)
	{
		Pop(1);
		int metaID = m_iMetaTableIDCounter++;
		pMetaObject->SetMember("MetaID", metaID); // Gmod uses SetMember_FixKey
		pMetaObject->SetMember("MetaName", name);

		pMetaObject->Push();
		lua_setfield(m_pState, LUA_REGISTRYINDEX, name);
		lua_getfield(m_pState, LUA_REGISTRYINDEX, name);
	}

	if (GetType(-1) == Type::Table)
	{
		lua_getfield(m_pState, -1, "MetaID");
		int id = (int)lua_tonumber(m_pState, -1);
		lua_settop(m_pState, -2);

		return id;
	}

	return -1;
}

void* CLuaInterface::NewUserdata(unsigned int iSize, unsigned char nType)
{
	return lua_newuserdata(m_pState, iSize, nType > UDTYPE__MAX ? nType : UDTYPE_SPECIALUSERDATA);
}

// Yes, we include it down here
// just so that the Windows.h include with it's unholy amount of macros won't screw us over
#ifdef _WIN32
#include <Windows.h>
#undef GetObject
#undef GetClassName
#define DLL_Handle HMODULE
#define DLL_LoadModule(name, _) LoadLibraryA(name)
#define DLL_UnloadModule(handle) FreeLibrary((DLL_Handle)handle)
#define DLL_GetAddress(handle, name) GetProcAddress((DLL_Handle)handle, name)
#define DLL_LASTERROR "LINUXONLY"
#else
#include <dlfcn.h>
#define DLL_Handle void*
#define DLL_LoadModule(name, type) dlopen(name, type)
#define DLL_UnloadModule(handle) dlclose(handle)
#define DLL_GetAddress(handle, name) dlsym(handle, name)
#define DLL_LASTERROR dlerror()
#endif

void GMOD_LoadBinaryModule(lua_State* L, const char* name)
{
	lua_pushfstring(L, "LOADLIB: %s", name);
	void** udata = (void**)lua_newuserdata(L, sizeof(void*), UDTYPE_BINARYMODULE);

	lua_pushvalue(L, LUA_REGISTRYINDEX);
	lua_getfield(L, -1, "_LOADLIB");
	lua_setmetatable(L, -3);

	lua_pushvalue(L, -3);
	lua_pushvalue(L, -3);
	lua_settable(L, -3);
	//lua_pop(L, 1);

	void* hDll = DLL_LoadModule(name, RTLD_LAZY);
	if (hDll == nullptr)
	{
		lua_pushliteral(L, "Failed to load dll!");
		lua_error(L);
		return;
	}

	CFunc gmod13_open = (CFunc)DLL_GetAddress(hDll, "gmod13_open");
	if (gmod13_open == nullptr)
	{
		lua_pushliteral(L, "Failed to get gmod13_open!");
		lua_error(L);
		DLL_UnloadModule(hDll);
		return;
	}

	*udata = hDll;

	lua_pushcclosure(L, gmod13_open, 0);
	lua_call(L, 0, 0);
}

void GMOD_UnloadBinaryModule(lua_State* L, const char* module, void* udata)
{
	if (udata != nullptr)
	{
		CFunc gmod13_close = (CFunc)DLL_GetAddress(udata, "gmod13_close");
		if (gmod13_close != nullptr)
		{
			lua_pushcclosure(L, gmod13_close, 0);
			lua_call(L, 0, 0);
		}

		DLL_UnloadModule(udata);
	}

	lua_pushvalue(L, LUA_REGISTRYINDEX);
	lua_pushnil(L);
	lua_setfield(L, -2, module);
	lua_pop(L, 1);
}

void GMOD_UnloadBinaryModules(lua_State* L)
{
	lua_pushvalue(L, LUA_REGISTRYINDEX);
	lua_pushnil(L);

	while (lua_next(L, -2) != 0)
	{
		if(lua_type(L, -2) == Type::String && lua_type(L, -1) == Type::UserData)
		{
			const char* moduleName = lua_tolstring(L, -2, nullptr);
			//if (strncmp(moduleName, "LOADLIB: ", 8) == 0) // Why are we doing it like this (I forgot)
			if (lua_getuserdatatype(L, -2) == UDTYPE_BINARYMODULE) // RaphaelIT7: Let's be safer than gmod
			{
				// printf("Unloading %s\n", moduleName);
				GMOD_UnloadBinaryModule(L, moduleName, lua_touserdata(L, -1));
			}
		}

		lua_pop(L, 1);
	}

	lua_pop(L, 1);
}