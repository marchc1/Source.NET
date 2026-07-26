namespace Source.Common.GarrysMod;

public interface ILuaUser
{
	bool IsUsingLua();
	void InitLibraries(Lua.ILuaInterface unk1);
}
