using Source;

using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Shared;


public delegate void PrecacheFn(object? userdata);

public class PrecacheRegisterAttribute(string classname) : Attribute{
	public string ClassName => classname;
}
public class PrecacheWeaponRegisterAttribute(string classname) : PrecacheRegisterAttribute(classname);

public class PrecacheRegister
{
	public static PrecacheRegister? PrecacheRegisters;

	static PrecacheRegister(){
		foreach(var type in ReflectionUtils.GetLoadedTypesWithAttribute<PrecacheRegisterAttribute>())
			new PrecacheRegister(PrecacheFn_Other, type.Value.ClassName);
	}

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
