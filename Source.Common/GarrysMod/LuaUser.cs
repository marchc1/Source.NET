using Source.Common.GarrysMod.Lua;

namespace Source.Common.GarrysMod;

public interface LuaUser
{
	bool IsUsingLua();
	void InitLibraries(ILuaInterface unk1);
}
