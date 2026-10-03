#if CLIENT_DLL || GAME_DLL
using Source;
using Source.Common.Engine;
using Source.Common.GarrysMod;
using Source.Common.GarrysMod.Lua;

using System.Security.Cryptography;
using System.Text;

#if CLIENT_DLL
namespace Game.Client.GarrysMod;
#else
namespace Game.Server.GarrysMod;
#endif

public class GModDataPack : IGModDataPack
{
	public static readonly GModDataPack pDataPack = new();
	public static GModDataPack DataPack() => pDataPack;

	INetworkStringTable? Table;
	bool OverflowWarned;
	readonly SortedSet<string> SingleplayerFiles = new(StringComparer.Ordinal);

	public void Initialize() {
		Table = networkstringtable.CreateStringTable("client_lua_files", IsSingleplayer() ? 0x8000 : 0x2000, 0, 0);
		Table.AddString(true, "paths");
	}

	public void BuildSearchPaths() {
		string gameDir = new(get.GameDir());
		Bootil.String.File.FixSlashes(ref gameDir, "\\", "/");
		Bootil.String.Lower(ref gameDir);

		char[] buffer = new char[filesystem.GetSearchPath("lsv", false, default)];
		filesystem.GetSearchPath("lsv", false, buffer);
		string paths = new(((ReadOnlySpan<char>)buffer).SliceNullTerminatedString());
		Bootil.String.File.FixSlashes(ref paths, "\\", "/");
		Bootil.String.Lower(ref paths);
		Bootil.String.Util.FindAndReplace(ref paths, gameDir, "");

		byte[] userData = Encoding.UTF8.GetBytes(paths + '\0');
		Table!.SetStringUserData(0, userData.Length, userData);
	}

	public void Reset() {
		Table = null;
		SingleplayerFiles.Clear();
	}

	public bool Contains(ReadOnlySpan<char> name) {
		Span<char> fixedName = stackalloc char[0x104];
		strcpy(fixedName, name);
		for (int i = 0; i < fixedName.Length && fixedName[i] != '\0'; i++) {
			if (fixedName[i] == '\\')
				fixedName[i] = '/';
		}

		return Table!.FindStringIndex(fixedName.SliceNullTerminatedString()) != INetworkStringTable.INVALID_STRING_INDEX;
	}

	public void AddOrUpdateFile(LuaFile file, bool refresh) {
		if (IsSingleplayer()) {
			string fileName = new(((ReadOnlySpan<char>)file.Name).UnqualifiedFileName());
			if (!SingleplayerFiles.Add(fileName))
				return;

			string files = ":";
			foreach (string name in SingleplayerFiles)
				files += name + ":";

			for (int i = 0, offset = 0; i < 0x400; i++, offset += 0x19000) {
				string key = $"singleplayer_files{i}";
				int index = Table!.FindStringIndex(key);
				if (index == INetworkStringTable.INVALID_STRING_INDEX)
					index = Table.AddString(true, key);
				if (index == INetworkStringTable.INVALID_STRING_INDEX) {
					Warning($"Couldn't add network file ({file.Name}) - overflow?\n");
					return;
				}

				string chunk = files.Substring(offset, Math.Min(files.Length - offset, 0x19000));
				Bootil.String.Lower(ref chunk);
				byte[] userData = Encoding.UTF8.GetBytes(chunk + '\0');
				Table.SetStringUserData(index, userData.Length, userData);

				if (offset + 0x19000 > files.Length)
					return;
			}
			return;
		}

		int stringIndex = Table!.FindStringIndex(file.Name);
		if (stringIndex == INetworkStringTable.INVALID_STRING_INDEX) {
			stringIndex = Table.AddString(true, file.Name);
			if (stringIndex == INetworkStringTable.INVALID_STRING_INDEX) {
				if (!OverflowWarned) {
					OverflowWarned = true;
					g_Lua!.ErrorFromLua("Too many clientside Lua files (AddCSLuaFile), you have hit the limit!\n");
				}
				Warning($"Couldn't add network string [{file.Name}] - overflow?\n");
				return;
			}
		}

		byte[] contents = new byte[file.Contents.Length + 1];
		file.Contents.CopyTo(contents, 0);

		List<byte> buffer = new(0x20);
		buffer.AddRange(GetHashFromString(contents));
		if (refresh) {
			// todo: file.Compressed.Clear(); Bootil::Compression::LZMA::Compress(contents, len + 1, buffer, 5, 0x10000), "GModDataPack::AddOrUpdateFile: Couldn't compress file\n", "AUTOREFRESH: Not adding %s to datatable, its too large! (%i vs 65536)\n"
		}

		Table.SetStringUserData(stringIndex, buffer.Count, buffer.ToArray());
	}

	public byte[] GetHashFromString(ReadOnlySpan<byte> data) => SHA256.HashData(data);

	public bool IsSingleplayer() => gpGlobals.MaxClients == 1;

	public string? GetFromDatatable(ReadOnlySpan<char> name) => throw new NotImplementedException();
	public byte[] GetHashFromDatatable(ReadOnlySpan<char> name) => throw new NotImplementedException();
	public void FindInDatatable(ReadOnlySpan<char> wildcard, List<LuaFindResult> output, bool unk) => throw new NotImplementedException();
	public string? FindFileInDatatable(ReadOnlySpan<char> path, bool unk, bool isGamePath) => throw new NotImplementedException();
	public bool IsLocalLuaBlocked() => throw new NotImplementedException();
	public bool IsValidDirectory(ReadOnlySpan<char> name) => throw new NotImplementedException();
}
#endif
