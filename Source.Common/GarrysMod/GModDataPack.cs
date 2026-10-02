using Source.Common.GarrysMod.Lua;

namespace Source.Common.GarrysMod;

public struct LuaFindResult
{
	public string FileName;
	public bool IsFolder;
}

public interface IGModDataPack : ILuaClientDatatableHook
{
}
