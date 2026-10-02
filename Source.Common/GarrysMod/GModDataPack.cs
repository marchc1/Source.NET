namespace Source.Common.GarrysMod;

public struct LuaFindResult
{
	public string FileName;
	public bool IsFolder;
}

public interface IGModDataPack
{
	ref T GetFromDatatable<T>(ReadOnlySpan<char> unk);
	ref T GetHashFromDatatable<T>(ReadOnlySpan<char> unk);
	ref T GetHashFromString<T>(ReadOnlySpan<char> unk1, ulong unk2);
	void FindInDatatable(ReadOnlySpan<char> unk1, List<LuaFindResult> unk2, bool unk3);
	ref T FindFileInDatatable<T>(ReadOnlySpan<char> unk1, bool unk2, bool unk3);
	bool IsSingleplayer();
	bool IsValidDirectory(ReadOnlySpan<char> unk);
}
