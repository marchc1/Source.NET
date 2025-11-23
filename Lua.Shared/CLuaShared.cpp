#include "ILuaInterface.h"

/*
	IMPORTANT
	We call back to C# for most things, as we wanna do stuff over there when its relistic/possible without huge problems!
*/

class LuaClientDatatableHook;
class CLuaShared : public ILuaShared
{
public:
	virtual ~CLuaShared() {};
	virtual void Init();
	virtual void Shutdown();
	virtual void DumpStats();
	virtual ILuaInterface* CreateLuaInterface(unsigned char realm, bool unknown);
	virtual void CloseLuaInterface(ILuaInterface *interface);
	virtual ILuaInterface* GetLuaInterface(unsigned char realm);
	virtual LuaFile* LoadFile(const std::string &path, const std::string &pathId, bool fromDatatable, bool fromFile);
	virtual LuaFile* GetCache(const std::string &fileName);
	virtual void MountLua(const char *pathID);
	virtual void MountLuaAdd(const char *file, const char *pathID);
	virtual void UnMountLua(const char *realm);
	virtual void SetFileContents(const char *idk1, const char *idk2);
	virtual void SetLuaFindHook(LuaClientDatatableHook *hook);
	virtual void FindScripts(const std::string &path, const std::string &pathID, std::vector<LuaFindResult> &outPut);
	virtual const char* GetStackTraces();
	virtual void InvalidateCache(const std::string &str);
	virtual void EmptyCache();
	virtual bool ScriptExists(const std::string &file, const std::string &path, bool idk);
};

static CLuaShared g_CLuaShared;

void CLuaShared::Init()
{
}

void CLuaShared::Shutdown()
{
}

void CLuaShared::DumpStats()
{
}

ILuaInterface * CLuaShared::CreateLuaInterface(unsigned char realm, bool unknown)
{
	return nullptr;
}

void CLuaShared::CloseLuaInterface(ILuaInterface *interface)
{
}

ILuaInterface * CLuaShared::GetLuaInterface(unsigned char realm)
{
	return nullptr;
}

LuaFile * CLuaShared::LoadFile(const std::string &path,const std::string &pathId, bool fromDatatable, bool fromFile)
{
	return nullptr;
}

LuaFile * CLuaShared::GetCache(const std::string &fileName)
{
	return nullptr;
}

void CLuaShared::MountLua(const char *pathID)
{
}

void CLuaShared::MountLuaAdd(const char *file, const char *pathID)
{
}

void CLuaShared::UnMountLua(const char *realm)
{
}

void CLuaShared::SetFileContents(const char *idk1,const char *idk2)
{
}

void CLuaShared::SetLuaFindHook(LuaClientDatatableHook *hook)
{
}

void CLuaShared::FindScripts(const std::string &path, const std::string &pathID, std::vector<LuaFindResult> &outPut)
{
}

const char * CLuaShared::GetStackTraces()
{
	return nullptr;
}

void CLuaShared::InvalidateCache(const std::string &str)
{
}

void CLuaShared::EmptyCache()
{
}

bool CLuaShared::ScriptExists(const std::string &file, const std::string &path, bool idk)
{
	return false;
}

// C# bridge

extern "C" void __cdecl LuaShared_Init()
{
	g_CLuaShared.Init();
}