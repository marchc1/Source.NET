#if CLIENT_DLL || GAME_DLL
using Source;
using Source.Common.GarrysMod;
using Source.Common.GarrysMod.Lua;
#if CLIENT_DLL
using Source.Common.Launcher;
#endif

using System.Text;

#if CLIENT_DLL
namespace Game.Client.GarrysMod;
#else
namespace Game.Server.GarrysMod;
#endif

public static class LuaGlobalFunctions
{
	static readonly LuaLibraryFunction worker__GLobal__include = LuaGlobalLibrary.Add("include", include);
	static readonly LuaLibraryFunction worker__GLobal__require = LuaGlobalLibrary.Add("require", require);
	static readonly LuaLibraryFunction worker__GLobal__Msg = LuaGlobalLibrary.Add("Msg", Msg);
	static readonly LuaLibraryFunction worker__GLobal__MsgC = LuaGlobalLibrary.Add("MsgC", MsgC);
	static readonly LuaLibraryFunction worker__GLobal__MsgN = LuaGlobalLibrary.Add("MsgN", MsgN);
	static readonly LuaLibraryFunction worker__GLobal__ErrorNoHalt = LuaGlobalLibrary.Add("ErrorNoHalt", ErrorNoHalt);
	static readonly LuaLibraryFunction worker__GLobal__RegisterMetaTable = LuaGlobalLibrary.Add("RegisterMetaTable", RegisterMetaTable);
	static readonly LuaLibraryFunction worker__GLobal__FindMetaTable = LuaGlobalLibrary.Add("FindMetaTable", FindMetaTable);
	static readonly LuaLibraryFunction worker__GLobal__TypeID = LuaGlobalLibrary.Add("TypeID", TypeID);
	static readonly LuaLibraryFunction worker__GLobal__isbool = LuaGlobalLibrary.Add("isbool", isbool);
	static readonly LuaLibraryFunction worker__GLobal__isnumber = LuaGlobalLibrary.Add("isnumber", isnumber);
	static readonly LuaLibraryFunction worker__GLobal__isstring = LuaGlobalLibrary.Add("isstring", isstring);
	static readonly LuaLibraryFunction worker__GLobal__istable = LuaGlobalLibrary.Add("istable", istable);
	static readonly LuaLibraryFunction worker__GLobal__isfunction = LuaGlobalLibrary.Add("isfunction", isfunction);
	static readonly LuaLibraryFunction worker__GLobal__isentity = LuaGlobalLibrary.Add("isentity", isentity);
	static readonly LuaLibraryFunction worker__GLobal__isvector = LuaGlobalLibrary.Add("isvector", isvector);
	static readonly LuaLibraryFunction worker__GLobal__isangle = LuaGlobalLibrary.Add("isangle", isangle);
	static readonly LuaLibraryFunction worker__GLobal__ispanel = LuaGlobalLibrary.Add("ispanel", ispanel);
	static readonly LuaLibraryFunction worker__GLobal__ismatrix = LuaGlobalLibrary.Add("ismatrix", ismatrix);
	static readonly LuaLibraryFunction worker__GLobal__CurTime = LuaGlobalLibrary.Add("CurTime", CurTime);
	static readonly LuaLibraryFunction worker__GLobal__UnPredictedCurTime = LuaGlobalLibrary.Add("UnPredictedCurTime", UnPredictedCurTime);
	static readonly LuaLibraryFunction worker__GLobal__RealTime = LuaGlobalLibrary.Add("RealTime", RealTime);
	static readonly LuaLibraryFunction worker__GLobal__FrameTime = LuaGlobalLibrary.Add("FrameTime", FrameTime);
	static readonly LuaLibraryFunction worker__GLobal__FrameNumber = LuaGlobalLibrary.Add("FrameNumber", FrameNumber);
	static readonly LuaLibraryFunction worker__GLobal__SysTime = LuaGlobalLibrary.Add("SysTime", SysTime);
	static readonly LuaLibraryFunction worker__GLobal__VGUIFrameTime = LuaGlobalLibrary.Add("VGUIFrameTime", VGUIFrameTime);

	static int include(ILuaInterface lua) {
		string file = g_Lua!.CheckString(1).ToString();
		Bootil.String.Lower(ref file);
		g_Lua.GetCurrentFile(out string current);
		int top = g_Lua.Top();
		g_Lua.FindAndRunScript(file, true, true, current, false);
		return g_Lua.Top() - top;
	}

	static int require(ILuaInterface lua) {
		string name = g_Lua!.CheckString(1).ToString();
		Bootil.String.Lower(ref name);
		if (name != "timer") // Wow wtf
			g_Lua.Require(name);
		return 0;
	}

	static string ToStringArgs(LuaObject tostring, int top, string error) {
		string str = "";
		for (int i = 1; i <= top; i++) {
			LuaObject obj = new(i, LuaType.None);
			tostring.Push();
			obj.Push();
			str += g_Lua!.CallInternalGetString(1) ?? error;
			obj.UnReference();
		}
		return str;
	}

	static int Msg(ILuaInterface lua) {
		int top = g_Lua!.Top();
		LuaObject tostring = new();
		g_Lua.Global().GetMember("tostring", tostring);
		string str = ToStringArgs(tostring, top, "Msg tostring ERROR");
		g_Lua.Msg(str);
		tostring.UnReference();
		return 0;
	}

	static int MsgN(ILuaInterface lua) {
		int top = g_Lua!.Top();
		LuaObject tostring = new();
		g_Lua.Global().GetMember("tostring", tostring);
		string str = ToStringArgs(tostring, top, "MsgN tostring ERROR");
		str += "\n";
		g_Lua.Msg(str);
		tostring.UnReference();
		return 0;
	}

	static Color GetColor(ILuaObject obj) => new(
		(byte)(int)obj.GetMemberFloat("r", 255.0f),
		(byte)(int)obj.GetMemberFloat("g", 255.0f),
		(byte)(int)obj.GetMemberFloat("b", 255.0f),
		(byte)(int)obj.GetMemberFloat("a", 255.0f)
	);

	static int MsgC(ILuaInterface lua) {
		int top = g_Lua!.Top();
		Color color = new(0, 200, 255, 255);
		ILuaObject? first = g_Lua.GetObject(1);
		if (first != null && first.isTable())
			color = GetColor(first);

		LuaObject tostring = new();
		g_Lua.Global().GetMember("tostring", tostring);

		string str = "";
		for (int i = 1; i <= top; i++) {
			LuaObject obj = new(i, LuaType.None);
			if (obj.isTable() && !obj.MemberIsNil("r") && !obj.MemberIsNil("g") && !obj.MemberIsNil("b")) {
				if (str.Length != 0)
					g_Lua.MsgColour(in color, str);
				str = "";
				color = new(0, 200, 255, 255);
				if (obj.isTable())
					color = GetColor(obj);
			}
			else {
				tostring.Push();
				obj.Push();
				str += g_Lua.CallInternalGetString(1) ?? "MsgC tostring ERROR";
			}
			obj.UnReference();
		}

		g_Lua.MsgColour(in color, str);
		tostring.UnReference();
		return 0;
	}

	static int ErrorNoHalt(ILuaInterface lua) {
		int top = g_Lua!.Top();
		LuaObject tostring = new();
		g_Lua.Global().GetMember("tostring", tostring);

		StringBuilder buffer = new();
		for (int i = 1; i <= top; i++) {
			LuaObject obj = new(i, LuaType.None);
			tostring.Push();
			obj.Push();
			string str = g_Lua.CallInternalGetString(1) ?? "ErrorNoHalt tostring ERROR";
			buffer.Append(str.AsSpan(0, Math.Min(str.Length, Math.Max(0, 0x1000 - 1 - buffer.Length))));
			obj.UnReference();
		}
		string message = buffer.ToString();

		LuaError error = new() {
			Message = message,
			Side = g_Lua.IsServer() ? "server" : g_Lua.IsMenu() ? "menu" : "client"
		};
		ReadStackFrom(ref error, g_Lua);

		bool isAddon = LuaGameCallback.GetAddonFromError(in error, out IAddonSystem.Information addon, out _);
		get.MenuSystem()?.OnLuaError(in error, isAddon ? addon : null);
		LuaHelper.CallOnLuaErrorHook(in error, isAddon ? addon.Title : null, isAddon ? addon.WorkshopID : 0);

		g_Lua.ErrorNoHalt(message);
		tostring.UnReference();
		return 0;
	}

	static int RegisterMetaTable(ILuaInterface lua) {
		LuaObject table = new(2, LuaType.None);
		if (!table.isTable())
			lua.TypeError("table", 2);
		else
			g_Lua!.RegisterMetaTable(g_Lua.CheckString(1), table);
		table.UnReference();
		return 0;
	}

	static int FindMetaTable(ILuaInterface lua) {
		ILuaObject? meta = g_Lua!.GetMetaTableObject(g_Lua.CheckString(1), -1);
		if (meta == null)
			return 0;
		meta.Push();
		return 1;
	}

	static int TypeID(ILuaInterface lua) {
		g_Lua!.PushNumber((int)g_Lua.GetType(1));
		return 1;
	}

	static int IsType(LuaType type) {
		g_Lua!.PushBool(g_Lua.IsType(1, type));
		return 1;
	}

	static int isbool(ILuaInterface lua) => IsType(LuaType.Bool);
	static int isnumber(ILuaInterface lua) => IsType(LuaType.Number);
	static int isstring(ILuaInterface lua) => IsType(LuaType.String);
	static int istable(ILuaInterface lua) => IsType(LuaType.Table);
	static int isfunction(ILuaInterface lua) => IsType(LuaType.Function);
	static int isentity(ILuaInterface lua) => IsType(LuaType.Entity);
	static int isvector(ILuaInterface lua) => IsType(LuaType.Vector);
	static int isangle(ILuaInterface lua) => IsType(LuaType.Angle);
	static int ispanel(ILuaInterface lua) => IsType(LuaType.Panel);
	static int ismatrix(ILuaInterface lua) => IsType(LuaType.Matrix);

	static int CurTime(ILuaInterface lua) {
		g_Lua!.PushNumber(gpGlobals.CurTime);
		return 1;
	}

	static int UnPredictedCurTime(ILuaInterface lua) {
#if CLIENT_DLL
		if (Prediction.UnpredictedCurTime != 0.0) {
			g_Lua!.PushNumber(Prediction.UnpredictedCurTime);
			return 1;
		}
#endif
		g_Lua!.PushNumber(gpGlobals.CurTime);
		return 1;
	}

	static int RealTime(ILuaInterface lua) {
		g_Lua!.PushNumber((float)gpGlobals.RealTime);
		return 1;
	}

	static int FrameTime(ILuaInterface lua) {
		g_Lua!.PushNumber((float)gpGlobals.FrameTime);
		return 1;
	}

	static int FrameNumber(ILuaInterface lua) {
		g_Lua!.PushNumber(gpGlobals.FrameCount);
		return 1;
	}

	static int SysTime(ILuaInterface lua) {
#if CLIENT_DLL
		g_Lua!.PushNumber(Singleton<ISystem>().GetCurrentTime());
#else
		g_Lua!.PushNumber(Platform.Time);
#endif
		return 1;
	}

	static int VGUIFrameTime(ILuaInterface lua) {
#if CLIENT_DLL
		g_Lua!.PushNumber(Singleton<ISystem>().GetFrameTime());
#else
		g_Lua!.PushNumber(Platform.Time);
#endif
		return 1;
	}

	public static void ReadStackFrom(ref LuaError error, ILuaInterface lua) {
		error.Stack.Clear();
		lua_Debug ar = default;
		for (int level = 1; level != 17; level++) {
			if (lua.GetStack(level, ref ar) == 0)
				break;
			lua.GetInfo("Slnu", ref ar);
			error.Stack.Add(new LuaError.StackEntry() {
				Source = ar.ShortSource,
				Function = ar.Name ?? "",
				Line = ar.CurrentLine
			});
		}
	}
}
#endif
