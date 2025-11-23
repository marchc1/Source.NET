#pragma once

typedef void (*Msg)(const char* msg);
typedef void (*MsgColour)(const char* msg, const int color[3]);

struct CSharpInterface
{
	Msg MsgFunc;
	MsgColour MsgColourFunc;
};

extern CSharpInterface* g_pCSharpInterface;