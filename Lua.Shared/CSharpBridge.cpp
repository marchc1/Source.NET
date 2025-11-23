#include "csharpbridge.h"

CSharpInterface* g_pCSharpInterface;
extern "C" void __cdecl SetupCSharpCallback(CSharpInterface* pInterface)
{
	// Called by C# before init
	g_pCSharpInterface = pInterface;
}