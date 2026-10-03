#if GAME_DLL
using Source.Common.Filesystem;
using Source.Common.GarrysMod.Lua;

namespace Game.Server.GarrysMod;

public static class FileServ
{
	public static bool IsValidFileName(string name) {
		char first = name.Length > 0 ? name[0] : '\0';
		if (first is '\\' or '.' or '/' or '~' or '@')
			return false;

		if (name.Length == 0)
			return true;

		if (first is ':' or ' ')
			return false;

		for (int i = 1; i < name.Length; i++) {
			char c = name[i];
			if (c is ':' or ' ')
				return false;
			if (c == '.' && i + 1 < name.Length && name[i + 1] == '.')
				return false;
		}

		return true;
	}

	public static LuaFile? AddCSLuaFile(string file, string? source) {
		if (file.Length <= 3)
			return null;

		if (!IsValidFileName(file)) {
			g_Lua!.ErrorFromLua($"Refusing to AddCSLuaFile '{file}' because its name contains dodgy symbols\n");
			return null;
		}

		if (Bootil.String.File.GetFileExtension(file) != "lua") {
			g_Lua!.ErrorFromLua($"AddCSLuaFile: Invalid filename '{file}'\n");
			return null;
		}

		string name = file;
		Bootil.String.File.FixSlashes(ref name, "\\", "/");
		Bootil.String.Lower(ref name);

		ILuaShared luaShared = get.LuaShared()!;
		LuaFile? luaFile = null;
		string? path = g_Lua!.GetPath();
		if (path != null)
			luaFile = luaShared.LoadFile($"{path}/{name}", "lsv", false, true);

		if (luaFile == null) {
			luaFile = luaShared.LoadFile(name, "lsv", false, true);
			if (luaFile == null) {
				luaFile = luaShared.LoadFile(name, "GAME", false, true);
				if (luaFile == null) {
					g_Lua.ErrorFromLua($"AddCSLuaFile: Couldn't find '{name}'\n");
					return null;
				}
			}
		}

		if (luaFile.Contents.Length == 0) {
			g_Lua.ErrorFromLua($"AddCSLuaFile: Empty file '{luaFile.Name}'\n");
			return null;
		}

		if (source != null)
			luaFile.Source = source;

		if (GModDataPack.DataPack().Contains(luaFile.Name))
			return null;

		GModDataPack.DataPack().AddOrUpdateFile(luaFile, false);
		return luaFile;
	}

	public static void Add(string path) {
		using IFileHandle? file = filesystem.Open(path, FileOpenOptions.Read | FileOpenOptions.Text, "GAME");
		if (file == null)
			return;

		Span<char> line = stackalloc char[0x104];
		while (true) {
			ReadOnlySpan<char> read = filesystem.ReadLine(line, file);
			if (read.IsEmpty)
				break;

			Span<char> entry = line[..read.Length];
			for (int i = 0; i < entry.Length; i++) {
				if (entry[i] == '\\')
					entry[i] = '/';
			}

			if (entry[0] == '#')
				continue;

			int length = entry.Length;
			while (length > 0 && (entry[length - 1] == '\n' || entry[length - 1] == '\r'))
				length--;

			AddCSLuaFile(new string(entry[..length]), null);
		}
	}
}
#endif
