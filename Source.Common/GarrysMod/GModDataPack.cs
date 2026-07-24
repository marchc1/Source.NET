namespace Source.Common.GarrysMod;

public interface GModDataPack
{
	 object? GetFromDatatable(ReadOnlySpan<char> unk1);
	 object? GetHashFromDatatable(ReadOnlySpan<char> unk1);
	 object? GetHashFromString(ReadOnlySpan<char> unk1, uint unk2);
	 void FindInDatatable(ReadOnlySpan<char> unk1, List<LuaFindResult> unk2, bool unk3);
	 object? FindFileInDatatable(ReadOnlySpan<char> unk1, bool unk2, bool unk3);
	 bool IsSingleplayer();
	 void UnknownMethod(); 
}
