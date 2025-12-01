#pragma once

extern "C"
{
	// Not looking good but fully valid.
	#include "../Lua.JIT/src/lua.hpp"
	#include "../Lua.JIT/src/lj_obj.h"
}

#include "Types.h"
#include "ILuaInterface.h"
#include <string>
#include <list>
#include <atomic>
#include <deque>
#include <mutex>

#define MAX_PATH 260
#define LUA_MAX_TEMP_OBJECTS 32
#define LUA_MAX_RETURN_OBJECTS 4

extern ILuaShared* g_pCLuaShared;
extern ILuaGameCallback* g_pLuaGameCallback;

class CLuaInterface : public ILuaInterface
{
public:
	~CLuaInterface();

	virtual int Top(void);
	virtual void Push(int iStackPos);
	virtual void Pop(int iAmt = 1);
	virtual void GetTable(int iStackPos);
	virtual void GetField(int iStackPos, const char* strName);
	virtual void SetField(int iStackPos, const char* strName);
	virtual void CreateTable();
	virtual void SetTable(int iStackPos);
	virtual bool SetMetaTable(int iStackPos);
	virtual bool GetMetaTable(int i);
	virtual void Call(int iArgs, int iResults);
	virtual int PCall(int iArgs, int iResults, int iErrorFunc);
	virtual int Equal(int iA, int iB);
	virtual int RawEqual(int iA, int iB);
	virtual void Insert(int iStackPos);
	virtual void Remove(int iStackPos);
	virtual int Next(int iStackPos);
	virtual ILuaBase::UserData* NewUserdata(unsigned int iSize);
	[[noreturn]]
	virtual void ThrowError(const char* strError);
	virtual void CheckType(int iStackPos, int iType);
	[[noreturn]]
	virtual void ArgError(int iArgNum, const char* strMessage);
	virtual void RawGet(int iStackPos);
	virtual void RawSet(int iStackPos);
	virtual const char* GetString(int iStackPos = -1, unsigned int* iOutLen = nullptr);
	virtual double GetNumber(int iStackPos = -1);
	virtual bool GetBool(int iStackPos = -1);
	virtual CFunc GetCFunction(int iStackPos = -1);
	virtual ILuaBase::UserData* GetUserdata(int iStackPos = -1);
	virtual void PushNil();
	virtual void PushString(const char* val, unsigned int iLen = 0);
	virtual void PushNumber(double val);
	virtual void PushBool(bool val);
	virtual void PushCFunction(CFunc val);
	virtual void PushCClosure(CFunc val, int iVars);
	virtual void PushUserdata(ILuaBase::UserData* pData);
	virtual int ReferenceCreate();
	virtual void ReferenceFree(int i);
	virtual void ReferencePush(int i);
	virtual void PushSpecial(int iType);
	virtual bool IsType(int iStackPos, int iType);
	virtual int GetType(int iStackPos);
	virtual const char* GetTypeName(int iType);
	virtual void CreateMetaTableType(const char* strName, int iType);
	virtual const char* CheckString(int iStackPos = -1);
	virtual double CheckNumber(int iStackPos = -1);
	virtual int ObjLen(int iStackPos = -1);
	virtual const QAngle& GetAngle(int iStackPos = -1);
	virtual const Vector& GetVector(int iStackPos = -1);
	virtual void PushAngle(const QAngle& val);
	virtual void PushVector(const Vector& val);
	virtual void SetState(lua_State* L);
	virtual int CreateMetaTable(const char* strName);
	virtual bool PushMetaTable(int iType);
	virtual void PushUserType(void* data, int iType);
	virtual void SetUserType(int iStackPos, void* data);

public:
	virtual bool Init(ILuaGameCallback *, bool);
	virtual void Shutdown();
	virtual void Cycle();
	virtual ILuaObject *Global();
	virtual ILuaObject *GetObject(int index);
	virtual void PushLuaObject(ILuaObject *obj);
	virtual void PushLuaFunction(CFunc func);
	virtual void LuaError(const char *err, int index);
	virtual void TypeError(const char *name, int index);
	virtual void CallInternal(int args, int rets);
	virtual void CallInternalNoReturns(int args);
	virtual bool CallInternalGetBool( int args );
	virtual const char *CallInternalGetString( int args );
	virtual bool CallInternalGet( int args, ILuaObject *obj );
	virtual void NewGlobalTable( const char *name );
	virtual ILuaObject *NewTemporaryObject( );
	virtual bool isUserData( int index );
	virtual ILuaObject *GetMetaTableObject( const char *name, int type );
	virtual ILuaObject *GetMetaTableObject( int index );
	virtual ILuaObject *GetReturn( int index );
	virtual bool IsServer( );
	virtual bool IsClient( );
	virtual bool IsMenu( );
	virtual void DestroyObject( ILuaObject *obj );
	virtual ILuaObject *CreateObject( );
	virtual void SetMember( ILuaObject *table, ILuaObject *key, ILuaObject *value );
	virtual ILuaObject* GetNewTable( );
	virtual void SetMember( ILuaObject *table, float key );
	virtual void SetMember( ILuaObject *table, float key, ILuaObject *value );
	virtual void SetMember( ILuaObject *table, const char *key );
	virtual void SetMember( ILuaObject *table, const char *key, ILuaObject *value );
	virtual void SetType( unsigned char );
	virtual void PushLong( long num );
	virtual int GetFlags( int index );
	virtual bool FindOnObjectsMetaTable( int objIndex, int keyIndex );
	virtual bool FindObjectOnTable( int tableIndex, int keyIndex );
	virtual void SetMemberFast( ILuaObject *table, int keyIndex, int valueIndex );
	virtual bool RunString( const char *filename, const char *path, const char *stringToRun, bool run, bool showErrors );
	virtual bool IsEqual( ILuaObject *objA, ILuaObject *objB );
	virtual void Error( const char *err );
	virtual const char *GetStringOrError( int index );
	virtual bool RunLuaModule( const char *name );
	virtual bool FindAndRunScript( const char *filename, bool run, bool showErrors, const char *stringToRun, bool noReturns );
	virtual void SetPathID( const char *pathID );
	virtual const char *GetPathID( );
	virtual void ErrorNoHalt( const char *fmt, ... );
	virtual void Msg( const char *fmt, ... );
	virtual void PushPath( const char *path );
	virtual void PopPath( );
	virtual const char *GetPath( );
	virtual int GetColor( int index );
	virtual ILuaObject* PushColor( Color color );
	virtual int GetStack( int level, lua_Debug *dbg );
	virtual int GetInfo( const char *what, lua_Debug *dbg );
	virtual const char *GetLocal( lua_Debug *dbg, int n );
	virtual const char *GetUpvalue( int funcIndex, int n );
	virtual bool RunStringEx( const char *filename, const char *path, const char *stringToRun, bool run, bool printErrors, bool dontPushErrors, bool noReturns );
	virtual size_t GetDataString( int index, const char **str );
	virtual void ErrorFromLua( const char *fmt, ... );
	virtual const char *GetCurrentLocation( );
	virtual void MsgColour( const Color &col, const char *fmt, ... );
	virtual void GetCurrentFile( std::string &outStr );
	virtual bool CompileString( Bootil::Buffer &dumper, const std::string &stringToCompile );
	virtual bool CallFunctionProtected( int iArgs, int iRets, bool showError );
	virtual void Require( const char *name );
	virtual const char *GetActualTypeName( int type );
	virtual void PreCreateTable( int arrelems, int nonarrelems );
	virtual void PushPooledString( int index );
	virtual const char *GetPooledString( int index );
	virtual int AddThreadedCall( ILuaThreadedCall * );
	virtual void AppendStackTrace( char *, unsigned int );
	virtual void *CreateConVar( const char *, const char *, const char *, int );
	virtual void *CreateConCommand( const char *, const char *, int, FnCommandCallback_t callback, FnCommandCompletionCallback completionFunc );
	virtual const char* CheckStringOpt( int iStackPos, const char* def );
	virtual double CheckNumberOpt( int iStackPos, double def );
	virtual int RegisterMetaTable( const char* name, ILuaObject* obj );

public: // RaphaelIT7: Our new functions
	virtual void* NewUserdata( unsigned int iSize, unsigned char nType );

public:
	std::string RunMacros(std::string script);
	int FilterConVarFlags(int& flags);
	void ShutdownThreadedCalls();

public:
	inline ILuaGameCallback *GetLuaGameCallback() const
	{
		return m_pGameCallback;
	}

	inline void SetLuaGameCallback( ILuaGameCallback *callback )
	{
		m_pGameCallback = callback;
	}

public: // We keep gmod's structure in case any modules depend on it.
	struct Path
	{
		char path[MAX_PATH];
	};

	lua_State* m_pState = nullptr;
	int m_nLuaErrorReporter = -1; // Always 1 since it's always the first registry reference.
	std::deque<Path> m_CurrentPaths; // ToDo: Recheck this one since the source engine's version is smaller/all offsets are broken by this!
	std::list<ILuaThreadedCall*> m_pThreadedCalls;
	ILuaObject* m_ProtectedFunctionReturns[LUA_MAX_RETURN_OBJECTS] = {nullptr};
	ILuaObject* m_TempObjects[LUA_MAX_TEMP_OBJECTS] = {nullptr};
	unsigned char m_iRealm = (unsigned char)2; // CLIENT = 0, SERVER = 1, MENU = 2
	ILuaGameCallback* m_pGameCallback = nullptr;
	char m_sPathID[32] = "LuaMenu"; // lsv, lsc or LuaMenu
	int m_iCurrentTempObject = 0;
	ILuaObject* m_pGlobal = nullptr;
	ILuaObject* m_pStringPool = nullptr;
	// But wait, theres more. In the next fields the metatables objects are saved but idk if it just has a field for each metatable or if it uses a map.
	unsigned char m_iMetaTableIDCounter = Type::Type_Count;
	ILuaObject* m_pMetaTables[255] = {nullptr}; // Their index is based off their type. means m_MetaTables[Type::Entity] returns the Entity metatable.
private: // NOT GMOD stuff
	std::mutex m_pThreadedCallsMutex;
	std::atomic<bool> m_bShutDownThreadedCalls = false;

	void RunThreadedCalls();

public:
	inline void DoStackCheck() {
		//DebugPrint(2, "Top: %i\n", Top());
		if (Top() != 0)
		{
			// ::Error("holylib - lua: Stack leak! %i (%p)\n", Top(), this);
		}
	}
};