using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Shared;


public delegate void PrecacheFn(object? userdata);

public class PrecacheRegister
{
	public static PrecacheRegister? PrecacheRegisters;

	public PrecacheRegister(PrecacheFn fn, object? userdata) {
		Fn = fn;
		User = userdata;

		Next = PrecacheRegisters;
		PrecacheRegisters = this;
	}

	public PrecacheFn Fn;
	public object? User;
	public PrecacheRegister? Next;

	public static void Precache() {
		for (PrecacheRegister? cur = PrecacheRegisters; cur != null; cur = cur.Next)
			cur.Fn(cur.User);
	}

	public static void PrecacheFn_Other(object? userdata) {
#if GAME_DLL || CLIENT_DLL
		Util.PrecacheOther((string?)userdata);
#endif
	}
}
