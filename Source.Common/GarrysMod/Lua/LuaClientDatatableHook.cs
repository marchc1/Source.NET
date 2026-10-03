using Source.Common.GarrysMod;

namespace Source.Common.GarrysMod.Lua;

public interface ILuaClientDatatableHook
{
	string? GetFromDatatable(ReadOnlySpan<char> name);
	byte[] GetHashFromDatatable(ReadOnlySpan<char> name);
	byte[] GetHashFromString(ReadOnlySpan<byte> data);
	void FindInDatatable(ReadOnlySpan<char> wildcard, List<LuaFindResult> output, bool unk);
	string? FindFileInDatatable(ReadOnlySpan<char> path, bool unk, bool isGamePath);
	bool IsSingleplayer();
	bool IsLocalLuaBlocked();
	bool IsValidDirectory(ReadOnlySpan<char> name);
}
