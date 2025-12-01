#pragma once

/*
	NOTES:
	Gmod has the ILuaBase - a weaker and very basic class of the ILuaInterface. There is no point in having that here!
	Gmod uses a struct UserData but its not meant to be used by outsiders / outside the lua-shared dll
	and since we use a custom LuaJIT build we store userdata way more efficiently!

	If there is any dll that assumes the void* pointer and the type are directly besides each other
	-> expect this struct

	struct UserData
	{
		void* data;
		unsigned char type;
	};

	then gg
	Let the interface handle it.
	Though if this is really needed tell me (RaphaelIT7), probably only takes like 30min to add support for both our new and old ways of handling userdata.
*/

#include <string>
#include <vector>


#define COMMAND_COMPLETION_MAXITEMS 128
#define COMMAND_COMPLETION_ITEM_LENGTH 128

class CCommand {};
typedef void (*FnCommandCallback_t)(const CCommand &command);
typedef int (*FnCommandCompletionCallback)(const char* partial, char commands[COMMAND_COMPLETION_MAXITEMS][COMMAND_COMPLETION_ITEM_LENGTH]);


struct lua_State;
typedef int (*CFunc)(lua_State* L);

enum
{
	SPECIAL_GLOB, // Global table
	SPECIAL_ENV, // Environment table
	SPECIAL_REG, // Registry table
};

struct Vector
{
	Vector(float x, float y, float z)
	{
		this->x = x;
		this->y = y;
		this->z = z;
	}

	float x, y, z;
};
typedef Vector QAngle;

class CLuaInterface;
class ILuaInterface;
class ILuaBase
{
private:
	struct UserData // Unused though just for reference
	{
		void* data;
		unsigned char type;
	};

	friend class ILuaInterface;
	friend class CLuaInterface;
};

class ILuaThreadedCall
{
public:
	// Called every frame, to check if the call is done. Return true to mark them as done.
	// NOTE: Don't call AddThreadedCall from inside here as it could break the iteration that the ILuaInterface is internally doing while calling this.
	virtual bool IsDone() = 0;

	// Called once after the it was marked as done, inside of here you should do cleanup
	virtual void Done(ILuaInterface* LUA) = 0;

	// Called from CLuaInterface::ShutdownThreadedCalls when the Lua Interface is closing.
	// Cancel whatever you were doing and shut down.
	virtual void OnShutdown() = 0;
};

struct VMatrix
{
	float m[4][4];
};

class IPhysicsObject;
class BaseEntity;
class ILuaObject
{
public:
	virtual void Set(ILuaObject *obj) = 0;
	virtual void SetFromStack(int i) = 0;
	virtual void UnReference() = 0;

	virtual int GetType() = 0;
	virtual const char*GetString() = 0;
	virtual float GetFloat() = 0;
	virtual int GetInt() = 0;
	virtual void*GetUserData() = 0;

	virtual void SetMember(const char *name) = 0;
	virtual void SetMember(const char *name, ILuaObject* obj) = 0;
	virtual void SetMember(const char *name, float val) = 0;
	virtual void SetMember(const char *name, bool val) = 0;
	virtual void SetMember(const char *name, const char* val) = 0;
	virtual void SetMember(const char *name, CFunc f) = 0;

	virtual bool GetMemberBool(const char *name, bool b = true) = 0;
	virtual int GetMemberInt(const char *name, int i = 0) = 0;
	virtual float GetMemberFloat(const char *name, float f = 0.0f) = 0;
	virtual const char*GetMemberStr(const char *name, const char *s = "") = 0;
	virtual void*GetMemberUserData(const char *name, void *u = 0) = 0;
	virtual void*GetMemberUserData(float name, void *u = 0) = 0;
	virtual ILuaObject*GetMember(const char *name, ILuaObject *obj) = 0;
	virtual ILuaObject*GetMember(ILuaObject *key, ILuaObject *obj) = 0;

	virtual void SetMetaTable(ILuaObject *obj) = 0;
	virtual void SetUserData(void *obj) = 0;

	virtual void Push() = 0;

	virtual bool isNil() = 0;
	virtual bool isTable() = 0;
	virtual bool isString() = 0;
	virtual bool isNumber() = 0;
	virtual bool isFunction() = 0;
	virtual bool isUserData() = 0;

	virtual ILuaObject*GetMember(float fKey, ILuaObject *obj) = 0;

	virtual void*Remove_Me_1(const char *name, void* = 0) = 0;

	virtual void SetMember(float fKey) = 0;
	virtual void SetMember(float fKey, ILuaObject *obj) = 0;
	virtual void SetMember(float fKey, float val) = 0;
	virtual void SetMember(float fKey, bool val) = 0;
	virtual void SetMember(float fKey, const char* val) = 0;
	virtual void SetMember(float fKey, CFunc f) = 0;

	virtual const char*GetMemberStr(float name, const char *s = "") = 0;

	virtual void SetMember(ILuaObject *k, ILuaObject *v) = 0;
	virtual bool GetBool() = 0;

	virtual bool PushMemberFast(int iStackPos) = 0;
	virtual void SetMemberFast(int iKey, int iValue) = 0;

	virtual void SetFloat(float val) = 0;
	virtual void SetString(const char *val) = 0;

	virtual double GetDouble() = 0;

	virtual void SetMember_FixKey(const char *, float) = 0;
	virtual void SetMember_FixKey(const char *, const char *) = 0;
	virtual void SetMember_FixKey(const char *, ILuaObject *) = 0;
	virtual void SetMember_FixKey(const char *, double) = 0;
	virtual void SetMember_FixKey(const char *, int) = 0;

	virtual bool isBool() = 0;

	virtual void SetMemberDouble(const char *, double) = 0;

	virtual void SetMemberNil(const char *) = 0;
	virtual void SetMemberNil(float) = 0;

	virtual bool RemoveMe() = 0;

	virtual void Init() = 0;

	virtual void SetFromGlobal(const char *) = 0;

	virtual int GetStringLen(unsigned int *) = 0;

	virtual unsigned int GetMemberUInt(const char *, unsigned int) = 0;

	virtual void SetMember(const char *, unsigned long long) = 0;
	virtual void SetMember(const char *, int) = 0;
	virtual void SetReference(int) = 0;

	virtual void RemoveMember(const char *) = 0;
	virtual void RemoveMember(float) = 0;

	virtual bool MemberIsNil(const char *) = 0;

	virtual void SetMemberDouble(float, double) = 0;
	virtual double GetMemberDouble(const char *, double) = 0;
	// NOTE: All members below do NOT exist in ILuaObjects returned from the menusystem!

	virtual BaseEntity* GetMemberEntity(const char *, BaseEntity *entity) = 0;
	virtual void SetMemberEntity(float, BaseEntity *entity) = 0;
	virtual void SetMemberEntity(const char *, BaseEntity *entity) = 0;
	virtual bool isEntity() = 0;
	virtual BaseEntity* GetEntity() = 0;
	virtual void SetEntity(BaseEntity* entity) = 0;

	virtual void SetMemberVector(const char*, Vector *) = 0;
	virtual void SetMemberVector(const char*, Vector &) = 0;
	virtual void SetMemberVector(float, Vector *) = 0;
	virtual Vector*GetMemberVector(const char *, const Vector *) = 0;
	virtual Vector*GetMemberVector(int) = 0;
	virtual Vector*GetVector() = 0;
	virtual bool isVector() = 0;

	virtual void SetMemberAngle(const char *, QAngle *) = 0;
	virtual void SetMemberAngle(const char *, QAngle &) = 0;
	virtual QAngle*GetMemberAngle(const char *, QAngle *) = 0;
	virtual QAngle*GetAngle() = 0;
	virtual bool isAngle() = 0;

	virtual void SetMemberMatrix(const char *, VMatrix const *) = 0;
	virtual void SetMemberMatrix(const char *, VMatrix const &) = 0;
	virtual void SetMemberMatrix(float, VMatrix const *) = 0;
	virtual void SetMemberMatrix(int, VMatrix const *) = 0;

	virtual void SetMemberPhysObject(const char *, IPhysicsObject *) = 0;
	virtual double GetMemberDouble(float, double) = 0;

private:
	bool m_bUserData;
	int m_iType;
	int m_iReference;
	ILuaBase* m_pLua;
};

class Color // Source engine port - color.h
{
public:
	Color()
	{
		*((int *)this) = 0;
	}
	Color(int _r,int _g,int _b)
	{
		SetColor(_r, _g, _b, 0);
	}
	Color(int _r,int _g,int _b,int _a)
	{
		SetColor(_r, _g, _b, _a);
	}
	
	void SetColor(int _r, int _g, int _b, int _a = 0)
	{
		color[0] = (unsigned char)_r;
		color[1] = (unsigned char)_g;
		color[2] = (unsigned char)_b;
		color[3] = (unsigned char)_a;
	}

	void GetColor(int &_r, int &_g, int &_b, int &_a) const
	{
		_r = color[0];
		_g = color[1];
		_b = color[2];
		_a = color[3];
	}

	void SetRawColor( int col32 )
	{
		*((int *)this) = col32;
	}

	int GetRawColor() const
	{
		return *((int *)this);
	}

	inline int r() const { return color[0]; }
	inline int g() const { return color[1]; }
	inline int b() const { return color[2]; }
	inline int a() const { return color[3]; }

private:
	unsigned char color[4];
};

class ILuaGameCallback
{
public:
	struct CLuaError
	{
		~CLuaError()
		{
			stack.clear();
		}

		struct StackEntry
		{
			StackEntry() {};

			std::string source;
			std::string function;
			int line = -1;
		};

		std::string message;
		std::string side;
		std::vector<StackEntry> stack;
	};

	virtual ILuaObject *CreateLuaObject( ) = 0;
	virtual void DestroyLuaObject( ILuaObject *pObject ) = 0;

	virtual void ErrorPrint( const char *error, bool print ) = 0;

	virtual void Msg( const char *msg, bool useless ) = 0;
	virtual void MsgColour( const char *msg, const Color &color ) = 0;

	virtual void LuaError( const CLuaError *error ) = 0;

	virtual void InterfaceCreated( ILuaInterface *iface ) = 0;
};

namespace Bootil
{
	class Buffer
	{
	private:
		void*				m_pData;
		unsigned int		m_iSize;
		unsigned int		m_iPos;
		unsigned int		m_iWritten;
	};

	typedef Buffer AutoBuffer;
}

struct lua_Debug;
class ILuaInterface
{
public: // ILuaBase
	virtual int Top(void) = 0;
	virtual void Push(int iStackPos) = 0;
	virtual void Pop(int iAmt = 1) = 0;
	virtual void GetTable(int iStackPos) = 0;
	virtual void GetField(int iStackPos, const char* strName) = 0;
	virtual void SetField(int iStackPos, const char* strName) = 0;
	virtual void CreateTable() = 0;
	virtual void SetTable(int iStackPos) = 0;
	virtual bool SetMetaTable(int iStackPos) = 0;
	virtual bool GetMetaTable(int i) = 0;
	virtual void Call(int iArgs, int iResults) = 0;
	virtual int PCall(int iArgs, int iResults, int iErrorFunc) = 0;
	virtual int Equal(int iA, int iB) = 0;
	virtual int RawEqual(int iA, int iB) = 0;
	virtual void Insert(int iStackPos) = 0;
	virtual void Remove(int iStackPos) = 0;
	virtual int Next(int iStackPos) = 0;
	virtual ILuaBase::UserData* NewUserdata(unsigned int iSize) = 0;
	[[noreturn]] virtual void ThrowError(const char* strError) = 0;
	virtual void CheckType(int iStackPos, int iType) = 0;
	[[noreturn]] virtual void ArgError(int iArgNum, const char* strMessage) = 0;
	virtual void RawGet(int iStackPos) = 0;
	virtual void RawSet(int iStackPos) = 0;
	virtual const char* GetString(int iStackPos = -1, unsigned int* iOutLen = nullptr) = 0;
	virtual double GetNumber(int iStackPos = -1) = 0;
	virtual bool GetBool(int iStackPos = -1) = 0;
	virtual CFunc GetCFunction(int iStackPos = -1) = 0;
	virtual ILuaBase::UserData* GetUserdata(int iStackPos = -1) = 0;
	virtual void PushNil() = 0;
	virtual void PushString(const char* val, unsigned int iLen = 0) = 0;
	virtual void PushNumber(double val) = 0;
	virtual void PushBool(bool val) = 0;
	virtual void PushCFunction(CFunc val) = 0;
	virtual void PushCClosure(CFunc val, int iVars) = 0;
	virtual void PushUserdata(ILuaBase::UserData* pData) = 0;
	virtual int ReferenceCreate() = 0;
	virtual void ReferenceFree(int i) = 0;
	virtual void ReferencePush(int i) = 0;
	virtual void PushSpecial(int iType) = 0;
	virtual bool IsType(int iStackPos, int iType) = 0;
	virtual int GetType(int iStackPos) = 0;
	virtual const char* GetTypeName(int iType) = 0;
	virtual void CreateMetaTableType(const char* strName, int iType) = 0;
	virtual const char* CheckString(int iStackPos = -1) = 0;
	virtual double CheckNumber(int iStackPos = -1) = 0;
	virtual int ObjLen(int iStackPos = -1) = 0;
	virtual const QAngle& GetAngle(int iStackPos = -1) = 0;
	virtual const Vector& GetVector(int iStackPos = -1) = 0;
	virtual void PushAngle(const QAngle& val) = 0;
	virtual void PushVector(const Vector& val) = 0;
	virtual void SetState(lua_State* L) = 0;
	virtual int CreateMetaTable(const char* strName) = 0;
	virtual bool PushMetaTable(int iType) = 0;
	virtual void PushUserType(void* data, int iType) = 0;
	virtual void SetUserType(int iStackPos, void* data) = 0;

public: // ILuaInterface
	virtual bool Init(ILuaGameCallback *, bool) = 0;
	virtual void Shutdown() = 0;
	virtual void Cycle() = 0;
	virtual ILuaObject *Global() = 0;
	virtual ILuaObject *GetObject(int index) = 0;
	virtual void PushLuaObject(ILuaObject *obj) = 0;
	virtual void PushLuaFunction(CFunc func) = 0;
	virtual void LuaError(const char *err, int index) = 0;
	virtual void TypeError(const char *name, int index) = 0;
	virtual void CallInternal(int args, int rets) = 0;
	virtual void CallInternalNoReturns(int args) = 0;
	virtual bool CallInternalGetBool( int args ) = 0;
	virtual const char *CallInternalGetString( int args ) = 0;
	virtual bool CallInternalGet( int args, ILuaObject *obj ) = 0;
	virtual void NewGlobalTable( const char *name ) = 0;
	virtual ILuaObject *NewTemporaryObject( ) = 0;
	virtual bool isUserData( int index ) = 0;
	virtual ILuaObject *GetMetaTableObject( const char *name, int type ) = 0;
	virtual ILuaObject *GetMetaTableObject( int index ) = 0;
	virtual ILuaObject *GetReturn( int index ) = 0;
	virtual bool IsServer( ) = 0;
	virtual bool IsClient( ) = 0;
	virtual bool IsMenu( ) = 0;
	virtual void DestroyObject( ILuaObject *obj ) = 0;
	virtual ILuaObject *CreateObject( ) = 0;
	virtual void SetMember( ILuaObject *table, ILuaObject *key, ILuaObject *value ) = 0;
	virtual ILuaObject* GetNewTable( ) = 0;
	virtual void SetMember( ILuaObject *table, float key ) = 0;
	virtual void SetMember( ILuaObject *table, float key, ILuaObject *value ) = 0;
	virtual void SetMember( ILuaObject *table, const char *key ) = 0;
	virtual void SetMember( ILuaObject *table, const char *key, ILuaObject *value ) = 0;
	virtual void SetType( unsigned char ) = 0;
	virtual void PushLong( long num ) = 0;
	virtual int GetFlags( int index ) = 0;
	virtual bool FindOnObjectsMetaTable( int objIndex, int keyIndex ) = 0;
	virtual bool FindObjectOnTable( int tableIndex, int keyIndex ) = 0;
	virtual void SetMemberFast( ILuaObject *table, int keyIndex, int valueIndex ) = 0;
	virtual bool RunString( const char *filename, const char *path, const char *stringToRun, bool run, bool showErrors ) = 0;
	virtual bool IsEqual( ILuaObject *objA, ILuaObject *objB ) = 0;
	virtual void Error( const char *err ) = 0;
	virtual const char *GetStringOrError( int index ) = 0;
	virtual bool RunLuaModule( const char *name ) = 0;
	virtual bool FindAndRunScript( const char *filename, bool run, bool showErrors, const char *stringToRun, bool noReturns ) = 0;
	virtual void SetPathID( const char *pathID ) = 0;
	virtual const char *GetPathID( ) = 0;
	virtual void ErrorNoHalt( const char *fmt, ... ) = 0;
	virtual void Msg( const char *fmt, ... ) = 0;
	virtual void PushPath( const char *path ) = 0;
	virtual void PopPath( ) = 0;
	virtual const char *GetPath( ) = 0;
	virtual int GetColor( int index ) = 0;
	virtual ILuaObject* PushColor( Color color ) = 0;
	virtual int GetStack( int level, lua_Debug *dbg ) = 0;
	virtual int GetInfo( const char *what, lua_Debug *dbg ) = 0;
	virtual const char *GetLocal( lua_Debug *dbg, int n ) = 0;
	virtual const char *GetUpvalue( int funcIndex, int n ) = 0;
	virtual bool RunStringEx( const char *filename, const char *path, const char *stringToRun, bool run, bool printErrors, bool dontPushErrors, bool noReturns ) = 0;
	virtual size_t GetDataString( int index, const char **str ) = 0;
	virtual void ErrorFromLua( const char *fmt, ... ) = 0;
	virtual const char *GetCurrentLocation( ) = 0;
	virtual void MsgColour( const Color &col, const char *fmt, ... ) = 0;
	virtual void GetCurrentFile( std::string &outStr ) = 0;
	virtual bool CompileString( Bootil::Buffer &dumper, const std::string &stringToCompile ) = 0; // NOT Implemented - and never will be!
	virtual bool CallFunctionProtected( int iArgs, int iRets, bool showError ) = 0;
	virtual void Require( const char *name ) = 0;
	virtual const char *GetActualTypeName( int type ) = 0;
	virtual void PreCreateTable( int arrelems, int nonarrelems ) = 0;
	virtual void PushPooledString( int index ) = 0;
	virtual const char *GetPooledString( int index ) = 0;
	virtual int AddThreadedCall( ILuaThreadedCall * ) = 0;
	virtual void AppendStackTrace( char *, unsigned int ) = 0;
	virtual void *CreateConVar( const char *, const char *, const char *, int ) = 0;
	virtual void *CreateConCommand( const char *, const char *, int, FnCommandCallback_t callback, FnCommandCompletionCallback completionFunc ) = 0;
	virtual const char* CheckStringOpt( int iStackPos, const char* def ) = 0;
	virtual double CheckNumberOpt( int iStackPos, double def ) = 0;
	virtual int RegisterMetaTable( const char* name, ILuaObject* obj ) = 0;

public: // RaphaelIT7: Our new functions
	virtual void* NewUserdata( unsigned int iSize, unsigned char nType ) = 0;
};

namespace State
{
	enum
	{
		CLIENT = 0,
		SERVER,
		MENU
	};

	static const char *Name[] = {
		"client",
		"server",
		"menu",
		nullptr
	};
}

struct LuaFile
{
	~LuaFile();
	int time;
#ifdef WIN32
	std::string name;
	std::string source;
	std::string contents;
	inline const char* GetName() { return name.c_str(); }
	inline const char* GetSource() { return source.c_str(); }
	inline const char* GetContents() { return contents.c_str(); }
#else
	const char* name;
	const char* source;
	const char* contents;
	inline const char* GetName() { return name; }
	inline const char* GetSource() { return source; }
	inline const char* GetContents() { return contents; }
#endif
	Bootil::AutoBuffer compressed;
#ifndef WIN32
	int random = 1; // Unknown thing
#endif
	unsigned int timesloadedserver;
	unsigned int timesloadedclient;
};

struct LuaFindResult
{
#ifdef WIN32
	std::string fileName;
	inline const char* GetFileName() { return fileName.c_str(); }
#else
	const char* fileName;
	inline const char* GetFileName() { return fileName; }
#endif
	bool isFolder;
};

class LuaClientDatatableHook;
class ILuaShared
{
public:
	virtual ~ILuaShared() {};
	virtual void Init() = 0;
	virtual void Shutdown() = 0;
	virtual void DumpStats() = 0;
	virtual ILuaInterface* CreateLuaInterface(unsigned char, bool) = 0;
	virtual void CloseLuaInterface(ILuaInterface*) = 0;
	virtual ILuaInterface* GetLuaInterface(unsigned char) = 0;
	virtual LuaFile* LoadFile(const std::string& path, const std::string& pathId, bool fromDatatable, bool fromFile) = 0;
	virtual LuaFile* GetCache(const std::string&) = 0;
	virtual void MountLua(const char*) = 0;
	virtual void MountLuaAdd(const char*, const char*) = 0;
	virtual void UnMountLua(const char*) = 0;
	virtual void SetFileContents(const char*, const char*) = 0;
	virtual void SetLuaFindHook(LuaClientDatatableHook*) = 0;
	virtual void FindScripts(const std::string&, const std::string&, std::vector<LuaFindResult>&) = 0;
	virtual const char* GetStackTraces() = 0;
	virtual void InvalidateCache(const std::string&) = 0;
	virtual void EmptyCache() = 0;
	virtual bool ScriptExists(const std::string&, const std::string&, bool) = 0;
};