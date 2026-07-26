using Source.Common.Commands;

namespace Source.Common.GarrysMod;

public static partial class Lua
{
	public interface ILuaConVars
	{
		void Init();
		ConVar CreateConVar(ReadOnlySpan<char> unk1, ReadOnlySpan<char> unk12, ReadOnlySpan<char> unk3, int unk4);
		ConCommand CreateConCommand(ReadOnlySpan<char> unk1, ReadOnlySpan<char> unk2, int unk3, FnCommandCallback unk4, FnCommandCompletionCallback unk5);
		void DestroyManaged();
		void Cache(ReadOnlySpan<char> unk1, ReadOnlySpan<char> unk2);
		void ClearCache();
	}
}
