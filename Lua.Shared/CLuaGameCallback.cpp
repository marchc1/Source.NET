#include "ILuaInterface.h"
#include "csharpbridge.h"
#include <sstream>

class CLuaGameCallback : public ILuaGameCallback
{
public:
	virtual ILuaObject *CreateLuaObject();
	virtual void DestroyLuaObject(ILuaObject *pObject);

	virtual void ErrorPrint(const char *error, bool print);

	virtual void Msg(const char *msg, bool useless);
	virtual void MsgColour(const char *msg, const Color &color);

	virtual void LuaError(const CLuaError *error);

	virtual void InterfaceCreated(ILuaInterface *iface);
};

ILuaObject* CLuaGameCallback::CreateLuaObject()
{
	return NULL; // static_cast<ILuaObject*>(static_cast<void*>(new UnholyLuaObject()));
}

void CLuaGameCallback::DestroyLuaObject(ILuaObject* pObject)
{
	delete pObject;
}

void CLuaGameCallback::ErrorPrint(const char* error, bool print)
{
	// g_pCSharpInterface->ErrorPrint();
}

void CLuaGameCallback::Msg(const char* msg, bool unknown)
{
	g_pCSharpInterface->MsgFunc(msg);
}

void CLuaGameCallback::MsgColour(const char* msg, const Color& color)
{
	int iColor[4];
	color.GetColor(iColor[0], iColor[1], iColor[2], iColor[3]);
	g_pCSharpInterface->MsgColourFunc(msg, iColor);	
}

// ToDo: Perferably we let C# handle this!
#ifdef CLIENT_DLL
Color col_msg(255, 241, 122, 200);
Color col_error(255, 221, 102, 255);
#elif defined(MENUSYSTEM)
Color col_msg(100, 220, 100, 200);
Color col_error(120, 220, 100, 255);
#else
Color col_msg(156, 241, 255, 200);
Color col_error(136, 221, 255, 255);
#endif

void CLuaGameCallback::LuaError(const CLuaError* error)
{
	std::stringstream str;

	str << "[ERROR] ";
	str << error->message;
	str << "\n";

	int i = 0;
	for (CLuaError::StackEntry entry : error->stack)
	{
		++i;
		for (int j=-1;j<i;++j)
		{
			str << " ";
		}

		str << i;
		str << ". ";
		str << entry.function;
		str << " - ";
		str << entry.source;
		str << ":";
		str << entry.line;
		str << "\n";
	}

	MsgColour(str.str().c_str(), col_error);
}

void CLuaGameCallback::InterfaceCreated(ILuaInterface* interface)
{

}

static CLuaGameCallback pLuaGameCallback;
ILuaGameCallback* g_pLuaGameCallback = &pLuaGameCallback;