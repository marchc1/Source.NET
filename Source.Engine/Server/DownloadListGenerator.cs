global using static Source.Engine.Server.DownloadListGeneratorGlobals;

using Source.Common;
using Source.Common.Engine;
using Source.Common.Filesystem;
using Source.Common.Formats.Keyvalues;
using Source.Common.Utilities;

using System.Numerics;

namespace Source.Engine.Server;

public enum ConsistencyType : byte
{
	None,
	Exact,
	SimpleMaterial,
	Bounds
}

public struct ExactFileUserData
{
	public ConsistencyType ConsistencyType;
	public CRC32_t CRC;
}

public struct ModelBoundsUserData
{
	public ConsistencyType ConsistencyType;
	public Vector3 Mins;
	public Vector3 Maxs;
}

public static class DownloadListGeneratorGlobals
{
	public static readonly DownloadListGenerator g_DownloadListGenerator = new DownloadListGenerator();
}

public class DownloadListGenerator
{
	INetworkStringTable? StringTable;
	string GameDir = "";
	string MapName = "";
	IFileHandle? ReslistFile;
	readonly UtlSymbolTable AlreadyWrittenFileNames = new();

	static readonly string[] ModelSiblingExtensions = [".vvd", ".ani", ".dx80.vtx", ".dx90.vtx", ".sw.vtx", ".phy", ".jpg"];

	public void SetStringTable(INetworkStringTable? pStringTable) {
		StringTable = pStringTable;

		// reset the duplication list
		AlreadyWrittenFileNames.Clear();

		// add in the bsp file to the list, and its node graph and nav mesh
		Span<char> path = stackalloc char[MAX_PATH];
		OnResourcePrecached(new PrintF(path, "maps/%s.bsp").S(MapName).ToSpan());

		bool useNodeGraph = true;
		KeyValues modinfo = new("ModInfo");
		if (modinfo.LoadFromFile(g_pFileSystem, "gameinfo.txt", null))
			useNodeGraph = modinfo.GetInt("nodegraph", 1) != 0;

		if (useNodeGraph)
			OnResourcePrecached(new PrintF(path, "maps/graphs/%s.ain").S(MapName).ToSpan());

		OnResourcePrecached(new PrintF(path, "maps/%s.nav").S(MapName).ToSpan());

		Span<char> resfilename = stackalloc char[MAX_PATH];
		ReadOnlySpan<char> resfile = new PrintF(resfilename, "maps/%s.res").S(MapName).ToSpan();
		KeyValues resfilekeys = new("resourcefiles");
		if (resfilekeys.LoadFromFile(g_pFileSystem, resfile, "GAME")) {
			for (KeyValues? entry = resfilekeys.GetFirstSubKey(); entry != null; entry = entry.GetNextKey())
				OnResourcePrecached(entry.Name);
		}
	}

	// call to mark level load start
	public void OnLevelLoadStart(ReadOnlySpan<char> levelName) {
		// reset the duplication list
		AlreadyWrittenFileNames.Clear();

		// add a slash to the end of the game dir, so we can only deal with files for this mod
		Span<char> gameDir = stackalloc char[MAX_PATH];
		ReadOnlySpan<char> fixedGameDir = new PrintF(gameDir, "%s/").S(Common.Gamedir).ToSpan();
		gameDir = gameDir[..fixedGameDir.Length];
		StrTools.FixSlashes(gameDir);

		GameDir = new string(gameDir.SliceNullTerminatedString());
		// save off the map name
		MapName = new string(levelName.SliceNullTerminatedString());
	}

	// call to mark level load end
	public void OnLevelLoadEnd() {
		StringTable = null;
	}

	// logs the precache as a file access
	public void OnResourcePrecached(ReadOnlySpan<char> relativePathFileName) {
		relativePathFileName = relativePathFileName.SliceNullTerminatedString();

		// ignore empty string
		if (relativePathFileName.IsEmpty)
			return;

		// ignore files that start with '*' since they signify special models
		if (relativePathFileName[0] == '*')
			return;

		Span<char> fullPath = stackalloc char[MAX_PATH];
		ReadOnlySpan<char> resolved = g_pFileSystem.RelativePathToFullPath(relativePathFileName, null, fullPath);
		if (!resolved.IsEmpty)
			OnResourcePrecachedFullPath(resolved, relativePathFileName);
	}

	// logs and handles mdl files being precached
	public void OnModelPrecached(ReadOnlySpan<char> relativePathFileName) {
		relativePathFileName = relativePathFileName.SliceNullTerminatedString();

		if (stristr(relativePathFileName, ".vmt").IsEmpty) {
			OnResourcePrecached(relativePathFileName);
			return;
		}

		// it's a materials file; make sure it starts in the materials directory, and grab the .vtf too
		Span<char> file = stackalloc char[MAX_PATH];
		int len;
		if (strnicmp(relativePathFileName, "materials", "materials".Length) == 0)
			len = strcpy(file, relativePathFileName);
		else
			len = new PrintF(file, "materials/%s").S(relativePathFileName).Length;

		OnResourcePrecached(file[..len]);

		// get the matching vtf file
		int vmt = IndexOfExtension(file[..len], ".vmt");
		if (vmt >= 0) {
			Span<char> vtf = stackalloc char[MAX_PATH];
			int vtfLen = new PrintF(vtf, "%s.vtf").S(file[..vmt]).Length;
			OnResourcePrecached(vtf[..vtfLen]);
		}
	}

	// logs sound file access
	public void OnSoundPrecached(ReadOnlySpan<char> relativePathFileName) {
		relativePathFileName = relativePathFileName.SliceNullTerminatedString();

		// skip any special characters
		if (!relativePathFileName.IsEmpty && !char.IsLetterOrDigit(relativePathFileName[0]))
			relativePathFileName = relativePathFileName[1..];

		// prepend the sound/ directory if necessary
		Span<char> file = stackalloc char[MAX_PATH];
		int len;
		if (strnicmp(relativePathFileName, "sound", "sound".Length) == 0)
			len = strcpy(file, relativePathFileName);
		else
			len = new PrintF(file, "sound/%s").S(relativePathFileName).Length;

		OnResourcePrecached(file[..len]);
	}

	// logs out file access to a file
	void OnResourcePrecachedFullPath(ReadOnlySpan<char> fullPathFileName, ReadOnlySpan<char> relativeFileName) {
		Span<char> full = stackalloc char[MAX_PATH];
		full = full[..strcpy(full, fullPathFileName)];
		StrTools.FixSlashes(full);

		if (strnicmp(GameDir, full, GameDir.Length) != 0)
			return; // the game dir must be part of the full name

		// make sure the filename hasn't already been written
		if (AlreadyWrittenFileNames.Find(full) == UTL_INVAL_SYMBOL)
			return;

		// add extras for mdl's
		int mdl = IndexOfExtension(relativeFileName, ".mdl");
		if (mdl >= 0) {
			ReadOnlySpan<char> baseName = relativeFileName.SliceNullTerminatedString()[..mdl];
			Span<char> sibling = stackalloc char[MAX_PATH];
			foreach (string extension in ModelSiblingExtensions)
				OnResourcePrecached(new PrintF(sibling, "%s%s").S(baseName).S(extension).ToSpan());
		}

		StringTable?.AddString(true, relativeFileName.SliceNullTerminatedString());
	}

	static int IndexOfExtension(ReadOnlySpan<char> path, ReadOnlySpan<char> extension) {
		return path.SliceNullTerminatedString().IndexOf(extension, System.StringComparison.OrdinalIgnoreCase);
	}
}
