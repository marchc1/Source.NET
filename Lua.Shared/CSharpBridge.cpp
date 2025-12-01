#include "csharpbridge.h"
#include "CLuaInterface.h"

CSharpInterface* g_pCSharpInterface;
extern "C" void __cdecl SetupCSharpCallback(CSharpInterface* pInterface)
{
	// Called by C# before init
	g_pCSharpInterface = pInterface;
}

// Exposed for C# to call for ILuaShared

extern "C" void __cdecl LuaShared_Init()
{
	g_pCLuaShared->Init();
}

extern "C" void* __cdecl LuaShared_CreateLuaInterface(unsigned char nRealm)
{
	return g_pCLuaShared->CreateLuaInterface(nRealm, false);
}

// Exposed for C# to call for ILuaInterface

extern "C" bool __cdecl LuaInterface_Init(void* pInterface, bool isServer)
{
	return ((ILuaInterface*)pInterface)->Init(g_pLuaGameCallback, isServer);
}