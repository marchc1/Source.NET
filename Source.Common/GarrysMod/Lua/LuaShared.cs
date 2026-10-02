namespace Source.Common.GarrysMod.Lua;

public enum State // Dupe of Realm?
{
	Client,
	Server,
	Menu
}

public static partial class LuaShared
{
	static readonly string[] RealmNames = ["client", "server", "menu"];
	public static ReadOnlySpan<char> GetStateName(State state) => RealmNames[(int)state];
}

public class LuaFile
{
	public int Time;
	public string Name = "";
	public string Source = "";
	public byte[] Contents = [];
	public byte[] Compressed = [];
	public uint TimesLoadedServer;
	public uint TimesLoadedClient;
}

public interface ILuaShared
{
	void Init(IServiceProvider services, bool unk1, IGet unk2);
	void Shutdown();
	void DumpStats();
	ILuaInterface CreateLuaInterface(Realm realm, bool renew);
	void CloseLuaInterface(ILuaInterface iface);
	ILuaInterface? GetLuaInterface(byte realm);
	LuaFile? LoadFile(ReadOnlySpan<char> path, ReadOnlySpan<char> pathId, bool fromDatatable, bool fromFile);
	LuaFile? GetCache(ReadOnlySpan<char> name);
	void MountLua(ReadOnlySpan<char> pathId);
	void MountLuaAdd(ReadOnlySpan<char> path, ReadOnlySpan<char> pathId);
	void UnMountLua(ReadOnlySpan<char> pathId);
	void SetFileContents(ReadOnlySpan<char> unk1, ReadOnlySpan<char> unk2);
	void SetLuaFindHook(ILuaClientDatatableHook? hook);
	void FindScripts(ReadOnlySpan<char> wildcard, ReadOnlySpan<char> pathId, List<LuaFindResult> output);
	ReadOnlySpan<char> GetStackTraces();
	void InvalidateCache(ReadOnlySpan<char> name);
	void EmptyCache();
	bool ScriptExists(ReadOnlySpan<char> file, ReadOnlySpan<char> pathId, bool directoryOnly);
}
