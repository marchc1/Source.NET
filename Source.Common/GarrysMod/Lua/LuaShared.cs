namespace Source.Common.GarrysMod;

public static partial class Lua
{
	public enum State
	{
		Client,
		Server,
		Menu
	}

	static readonly string[] RealmNames = ["client", "server", "menu"];
	public static ReadOnlySpan<char> GetStateName(State state) => RealmNames[(int)state];

	public struct LuaFile
	{
		public static LuaFile? NULL = null;

		public int Time;
		public string Name;
		public string Source;
		public string Contents;
		public Stream Compressed;
		public uint TimesLoadedServer;
		public uint TimesLoadedClient;
	}

	public interface ILuaShared
	{
		void Init(IServiceProvider services, bool unk1, IGet unk2);
		void Shutdown();
		void DumpStats();
		ILuaInterface CreateLuaInterface(byte unk1, bool unk2);
		void CloseLuaInterface(ILuaInterface unk1);
		ILuaInterface GetLuaInterface(byte unk1);
		ref LuaFile? LoadFile(ReadOnlySpan<char> path, ReadOnlySpan<char> pathId, bool fromDatatable, bool fromFile);
		ref LuaFile? GetCache(ReadOnlySpan<char> unk1);
		void MountLua(ReadOnlySpan<char> unk1);
		void MountLuaAdd(ReadOnlySpan<char> unk1, ReadOnlySpan<char> unk2);
		void UnMountLua(ReadOnlySpan<char> unk1);
		void SetFileContents(ReadOnlySpan<char> unk1, ReadOnlySpan<char> unk2);
		void SetLuaFindHook(LuaClientDatatableHook unk1);
		void FindScripts(ReadOnlySpan<char> unk1, ReadOnlySpan<char> unk2, List<string> unk3);
		ReadOnlySpan<char> GetStackTraces();
		void InvalidateCache(ReadOnlySpan<char> unk1);
		void EmptyCache();
	}
}
