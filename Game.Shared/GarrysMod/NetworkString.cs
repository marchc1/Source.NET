#if CLIENT_DLL || GAME_DLL
using Source.Common.Engine;

#if CLIENT_DLL
namespace Game.Client.GarrysMod;
#else
namespace Game.Server.GarrysMod;
#endif

public static class NetworkString
{
	public static INetworkStringTable? pStringTable;

#if GAME_DLL
	public static void Create() {
		pStringTable = networkstringtable.CreateStringTable("networkstring", 0x1000, 0, 0);
		Add("x");
	}
#endif

	public static void Install() => pStringTable = networkstringtable.FindTable("networkstring");

	public static void Reset() => pStringTable = null;

	public static string? Convert(int id) {
		if (id <= 0 || pStringTable == null)
			return null;
		ReadOnlySpan<char> str = pStringTable.GetString(id);
		return str == null ? null : new(str);
	}

	public static int Get(ReadOnlySpan<char> name) {
		if (pStringTable == null)
			return 0;
		int index = pStringTable.FindStringIndex(name);
		return index == INetworkStringTable.INVALID_STRING_INDEX ? 0 : index;
	}

#if GAME_DLL
	public static int Add(ReadOnlySpan<char> name) {
		int index = Get(name);
		if (index != 0)
			return index;
		return pStringTable!.AddString(true, name);
	}
#endif
}
#endif
