// TODO: Logging calls when things go wrong, ie. try/catches


using CommunityToolkit.HighPerformance;

using Source.Common.Filesystem;
using Source.Common.Formats.BSP;
using Source.Common.Utilities;
using Source.Filesystem;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Source.FileSystem;

// Maybe we redo this one day...
public class BaseFileSystem : IFileSystem
{
	private SearchPathIDCollection SearchPaths = [];

	public BaseFileSystem() {
		RemoveSearchPaths("EXECUTABLE_PATH");
		AddSearchPath(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "EXECUTABLE_PATH");
		AddSearchPath(AppContext.BaseDirectory, "BASE_PATH");
	}

	internal static ReadOnlySpan<char> Normalize(ReadOnlySpan<char> unnormalizedString, Span<char> normalizedOutput) {
		int len = Math.Min(normalizedOutput.Length, unnormalizedString.Length);

		for (int i = 0; i < len; i++) {
			char c = unnormalizedString[i];
			normalizedOutput[i] = c == '\\' ? '/' : c;
		}

		return normalizedOutput[..len];
	}

	private void AddMapPackFile(ReadOnlySpan<char> path, ReadOnlySpan<char> pathID, SearchPathAdd addType) {
		using IFileHandle? file = Open(path, FileOpenOptions.Read | FileOpenOptions.Binary, "GAME");
		if (file == null) {
			Warning("Couldn't open BSP for embedded pack file\n");
			return;
		}

		BSPHeader header = default;
		file.Stream.ReadToStruct(ref header);

		BSPLump pakfile = header.GetLump(LumpIndex.PakFile);
		if (pakfile.FileLength <= Unsafe.SizeOf<BSPLump>()) {
			// Must be invalid
			return;
		}

		Span<char> fullPath = stackalloc char[MAX_PATH];
		ReadOnlySpan<char> newPath = RelativePathToFullPath(path, "GAME", fullPath);

		if (!SearchPaths.OpenOrCreateCollection(pathID, out SearchPathCollection collection)) {
			for (int i = 0, c = collection.Count; i < c; i++) {
				var searchPath = collection[i];
				if (searchPath.DiskPath == newPath) {
					if ((addType == SearchPathAdd.ToHead && i == 0) || addType == SearchPathAdd.ToTail)
						return;
					else {
						collection.RemoveAt(i);
						i--;
						c--;
						break;
					}
				}
			}
		}

		ZipPackFileSearchPath zip = new(this, new(newPath), file.Stream, in pakfile);
		if (!zip.IsValid()) {
			Warning("ZipPackFileSearchPath not valid\n");
			return;
		}

		if (addType == SearchPathAdd.ToHead)
			collection.Insert(0, zip);
		else
			collection.Add(zip);
	}

	private void AddVPKFile(ReadOnlySpan<char> path, ReadOnlySpan<char> pathID, SearchPathAdd addType) {
		string newPath = Path.IsPathFullyQualified(path) ? new(path) : Path.GetFullPath(new(path));

		if (!SearchPaths.OpenOrCreateCollection(pathID, out SearchPathCollection collection)) {
			for (int i = 0, c = collection.Count; i < c; i++) {
				var searchPath = collection[i];
				if (searchPath.DiskPath == newPath) {
					if ((addType == SearchPathAdd.ToHead && i == 0) || addType == SearchPathAdd.ToTail)
						return;
					else {
						collection.RemoveAt(i);
						i--;
						c--;
						break;
					}
				}
			}
		}

		if (addType == SearchPathAdd.ToHead)
			collection.Insert(0, new PackStoreSearchPath(this, newPath));
		else
			collection.Add(new PackStoreSearchPath(this, newPath));
	}
	private void AddPackFiles(ReadOnlySpan<char> path, ReadOnlySpan<char> pathID, SearchPathAdd addType) { } // TODO 
	private void AddSeparatorAndFixPath(ref string path) { // this sucks fix it later
		path = (path.TrimEnd('\\').TrimEnd('/') + "/").Replace("\\", "/");
	}
	private void AddSearchPathInternal(ReadOnlySpan<char> path, ReadOnlySpan<char> pathID, SearchPathAdd addType, bool addPackFiles) {
		var ext = Path.GetExtension(path);

		switch (ext) {
			case ".bsp": AddMapPackFile(path, pathID, addType); return;
			case ".vpk": AddVPKFile(path, pathID, addType); return;
		}

		string newPath = Path.IsPathFullyQualified(path) ? new(path) : Path.GetFullPath(new(path));
		AddSeparatorAndFixPath(ref newPath);

		if (!SearchPaths.OpenOrCreateCollection(pathID, out SearchPathCollection collection)) {
			for (int i = 0, c = collection.Count; i < c; i++) {
				var searchPath = collection[i];
				if (searchPath.DiskPath == newPath) {
					if ((addType == SearchPathAdd.ToHead && i == 0) || addType == SearchPathAdd.ToTail)
						return;
					else {
						collection.RemoveAt(i);
						i--;
						c--;
						break;
					}
				}
			}
		}

		if (addPackFiles) {
			AddPackFiles(newPath, pathID, addType);
		}

		if (addType == SearchPathAdd.ToHead)
			collection.Insert(0, new DiskSearchPath(this, newPath));
		else
			collection.Add(new DiskSearchPath(this, newPath));

	}

	public void AddSearchPath(ReadOnlySpan<char> path, ReadOnlySpan<char> pathID, SearchPathAdd addType = SearchPathAdd.ToTail) {
		AddSearchPathInternal(path, pathID, addType, true);
	}

	public IEnumerable<SearchPath> GetCollections(ulong hashID) {
		if (hashID == 0) {
			foreach (var path in SearchPaths.Values)
				if (!path.RequestOnly)
					foreach (var searchPath in path)
						yield return searchPath;
		}
		else {
			if (!SearchPaths.TryGetValue(hashID, out var collection))
				yield break;

			foreach (var searchPath in collection)
				yield return searchPath;
		}
	}
	delegate T FileSystemFuncPost<T>(SearchPath searchPath, ReadOnlySpan<char> providedFileName);
	/// <summary>
	/// Iterates through all <see cref="SearchPathCollection"/>'s (or a single lookup if pathID != null), and returns the first time <paramref name="winCondition"/> returns true.
	/// <br/> 
	/// If nothing returns true, the method returns <see cref="loseDefault"/>.
	/// </summary>
	/// <param name="filename">A local-to-searchpath filename</param>
	/// <param name="pathID">A pathID. If null, will search through every <see cref="SearchPathCollection"/>; otherwise searches for the single collection in the <see cref="SearchPaths"/> lookup table.</param>
	/// <param name="func">A delegate to run on every <see cref="SearchPath"/></param>
	/// <param name="winCondition">Compares the return value from the search path. Return true if the search path won.</param>
	/// <param name="loseDefault">If no search paths won, then this value is returned.</param>
	/// <param name="winner">The <see cref="SearchPath"/> that won (if the method returns true)</param>
	/// <returns>True if a <see cref="SearchPath"/> won.</returns>
	private T? FirstToThePost<T>(
		ReadOnlySpan<char> filename,
		ReadOnlySpan<char> pathID,
		FileSystemFuncPost<T> func,
		Func<T, bool> winCondition,
		T? loseDefault,
		[NotNullWhen(true)] out SearchPath? winner
	) {
		filename = filename.SliceNullTerminatedString();
		Span<char> filenameNormalizedBuffer = stackalloc char[MAX_PATH];
		ReadOnlySpan<char> filenameNormalized = Normalize(filename, filenameNormalizedBuffer);
		ulong hashID = pathID.Hash();
		foreach (var path in GetCollections(hashID)) {
			T? ret = func(path, filenameNormalized);
			if (winCondition(ret)) {
				winner = path;
				return ret;
			}
		}
		winner = null;
		return loseDefault;
	}
	public ReadOnlySpan<char> RelativePathToFullPath(ReadOnlySpan<char> fileName, ReadOnlySpan<char> pathID, Span<char> dest, PathTypeFilter filter = PathTypeFilter.None) {
		fileName = fileName.SliceNullTerminatedString();
		if (!FirstToThePost(fileName, pathID, (path, filename) => path.Exists(filename), boolWin, false, out SearchPath? winner))
			return null;

		Span<char> concatBuffer = stackalloc char[MAX_PATH];
		var concatted = winner.Concat(fileName, concatBuffer);
		return new string(concatted);
	}

	private static bool boolWin(bool inp) => inp;
	private static bool notNullWin<T>(T? v) => v != null;

	public bool IsDirectory(ReadOnlySpan<char> fileName, ReadOnlySpan<char> pathID) {
		return FirstToThePost(fileName, pathID, (path, filename) => path.IsDirectory(filename), boolWin, false, out _);
	}
	public bool IsFileWritable(ReadOnlySpan<char> fileName, ReadOnlySpan<char> pathID) {
		return FirstToThePost(fileName, pathID, (path, filename) => path.IsFileWritable(filename), boolWin, false, out _);
	}

	public void MarkPathIDByRequestOnly(ReadOnlySpan<char> pathID, bool requestOnly) {
		ulong hashID = pathID.Hash();

		if (!SearchPaths.TryGetValue(hashID, out var collection))
			return;

		collection.RequestOnly = requestOnly;
	}

	public FileSystemMountRetval MountSteamContent(long extraAppID = -1) {
		throw new NotImplementedException(); // todo
	}

	public IFileHandle? Open(ReadOnlySpan<char> fileName, FileOpenOptions options, ReadOnlySpan<char> pathID) {
		return FirstToThePost(fileName, pathID, (path, filename) => path.Open(filename, options), notNullWin, null, out _);
	}


	public void RemoveAllSearchPaths() {
		SearchPaths.Clear();
	}

	public bool RemoveFile(ReadOnlySpan<char> relativePath, ReadOnlySpan<char> pathID) {
		string fn = new(relativePath);
		return FirstToThePost(relativePath, pathID, (path, filename) => path.RemoveFile(filename), boolWin, false, out _);
	}
	public bool RemoveSearchPath(ReadOnlySpan<char> path, ReadOnlySpan<char> pathID) {
		ulong hash = pathID.Hash();
		if (hash == 0) return false;

		if (!SearchPaths.TryGetValue(hash, out var collection))
			return false;

		bool ret = false;

		for (int i = collection.Count - 1; i >= 0; i--) {
			if (collection[i].DiskPath != path)
				continue;
			collection.RemoveAt(i);
			ret = true;
		}

		return ret;
	}

	public void RemoveSearchPaths(ReadOnlySpan<char> pathID) {
		ulong hash = pathID.Hash();
		if (hash == 0) return;
		SearchPaths.Remove(hash);
	}

	public unsafe bool RenameFile(ReadOnlySpan<char> oldPath, ReadOnlySpan<char> newPath, ReadOnlySpan<char> pathID) {
		int newPathLength = newPath.Length;
		fixed (char* nPath = newPath) {
			char** reallyBadHack = &nPath;
			return FirstToThePost(oldPath, pathID, (path, filename) => path.RenameFile(filename, new(*reallyBadHack, newPathLength)), boolWin, false, out _);
		}
	}

	public bool SetFileWritable(ReadOnlySpan<char> fileName, bool writable, ReadOnlySpan<char> pathID) {
		return FirstToThePost(fileName, pathID, (path, filename) => path.SetFileWritable(filename, writable), boolWin, false, out _);
	}

	public long Size(ReadOnlySpan<char> fileName, ReadOnlySpan<char> pathID) {
		return FirstToThePost(fileName, pathID, (path, filename) => path.Size(filename), (v) => v != -1, -1, out _);
	}

	public DateTime Time(ReadOnlySpan<char> fileName, ReadOnlySpan<char> pathID) {
		return FirstToThePost(fileName, pathID, (path, filename) => path.Time(filename), (v) => v != DateTime.UnixEpoch, DateTime.UnixEpoch, out _);
	}

	public bool FileExists(ReadOnlySpan<char> fileName, ReadOnlySpan<char> pathID) {
		return FirstToThePost(fileName, pathID, (path, filename) => path.Exists(filename), boolWin, false, out _);
	}

	public void CreateDirHierarchy(ReadOnlySpan<char> relativePath, ReadOnlySpan<char> pathID) {
		Span<char> scratchFileName = stackalloc char[MAX_PATH];
		if (!Path.IsPathFullyQualified(relativePath)) {
			Assert(!pathID.IsEmpty);
			ComputeFullWritePath(scratchFileName, relativePath, pathID);
		}
		else {
			relativePath.CopyTo(scratchFileName);
		}
		Directory.CreateDirectory(new(scratchFileName.SliceNullTerminatedString()));
	}
	private SearchPath? FindWritePath(ReadOnlySpan<char> filename, ReadOnlySpan<char> pathID) {
		ulong hash = pathID.Hash();
		if (hash == 0) return null;

		foreach (var searchPaths in SearchPaths) {
			foreach (var searchPath in searchPaths.Value) {
				if (searchPath is not DiskSearchPath)
					continue;

				if (pathID.IsEmpty || searchPaths.Key == hash)
					return searchPath;
			}
		}
		return null;
	}
	private ReadOnlySpan<char> GetWritePath(ReadOnlySpan<char> filename, ReadOnlySpan<char> pathID) {
		SearchPath? searchPath = null;
		if (!pathID.IsEmpty && pathID.Length > 0) {
			if (pathID.Equals("game", StringComparison.OrdinalIgnoreCase))
				searchPath = FindWritePath(filename, "game_write");
			else if (pathID.Equals("game", StringComparison.OrdinalIgnoreCase))
				searchPath = FindWritePath(filename, "mod_write");

			searchPath ??= FindWritePath(filename, pathID);
			if (searchPath != null)
				return searchPath.GetPathString();

			Warning("Requested non-existent write path %s!\n", new string(pathID));
		}

		searchPath = FindWritePath(filename, "DEFAULT_WRITE_PATH");
		if (searchPath != null) return searchPath.GetPathString();

		searchPath = FindWritePath(filename, null);
		if (searchPath != null) return searchPath.GetPathString();

		// Hope this is reasonable!!
		return "./";
	}

	private void ComputeFullWritePath(Span<char> dest, ReadOnlySpan<char> relativePath, ReadOnlySpan<char> pathID) {
		string combined = Path.Combine(new(GetWritePath(relativePath, pathID)), new(relativePath));
		combined.AsSpan().CopyTo(dest);
	}

	public bool ReadFile(ReadOnlySpan<char> fileName, ReadOnlySpan<char> path, Span<byte> buf, int startingByte) {
		using var handle = Open(fileName, FileOpenOptions.Read, path);
		if (handle == null) return false;

		int bytes = handle.Stream.Read(buf[startingByte..]);
		return bytes > 0;
	}

	public bool ReadFile(ReadOnlySpan<char> fileName, ReadOnlySpan<char> path, Span<char> buf, int startingByte) {
		throw new Exception();
	}

	public void GetLocalCopy(ReadOnlySpan<char> path) {

	}

	public void MarkAllCRCsUnverified() {
		// Todo
	}

	public ReadOnlySpan<char> WhereIsFile(ReadOnlySpan<char> fileName, ReadOnlySpan<char> pathID = default) {
		if (FirstToThePost(fileName, pathID, (path, filename) => path.Exists(filename), boolWin, false, out SearchPath? path)) {
			Span<char> concatBuffer = stackalloc char[MAX_PATH];
			return new string(path.Concat(fileName, concatBuffer));
		}
		return null;
	}

	public void PrintSearchPaths() {
		Msg("---------------\n");
		Msg("Paths:\n");

		foreach (var searchpath in SearchPaths) {
			ReadOnlySpan<char> pathID = SearchPaths.GetName(searchpath.Key);
			foreach (var spi in searchpath.Value) {
				ReadOnlySpan<char> pack = "";
				ReadOnlySpan<char> type = "";
				if (false /* TODO: Map-based pack files */) {
					// type = "(map)";
				}
				else if (spi is PackStoreSearchPath pssp) {
					type = "(VPK)";
					pack = pssp.DiskPath;
				}
				else if (spi is DiskSearchPath dsp) {
					pack = dsp.DiskPath;
				}

				Msg($"\"{pack}\" \"{pathID}\" {type}\n");
			}
		}

	}

	readonly Dictionary<ulong, FileNameHandle_t> fileNameHandles = [];
	readonly Dictionary<FileNameHandle_t, string> fileNameStrings = [];
	FileNameHandle_t currentHandle;

	Span<char> FormatFileName(ReadOnlySpan<char> name, Span<char> newNameBuffer) {
		int newNamePtr = 0;
		for (int i = 0; i < name.Length; i++) {
			char c = char.ToLowerInvariant(name[i]);
			if (c != '/' && c != '\\')
				newNameBuffer[newNamePtr++] = c;
		}

		return newNameBuffer[..newNamePtr];
	}

	public FileNameHandle_t FindFileName(ReadOnlySpan<char> name) {
		ulong hash = FormatFileName(name, stackalloc char[name.Length]).Hash();
		return fileNameHandles.TryGetValue(hash, out FileNameHandle_t handle) ? handle : FILENAMEHANDLE_INVALID;
	}
	public FileNameHandle_t FindOrAddFileName(ReadOnlySpan<char> name) {
		ulong hash = FormatFileName(name, stackalloc char[name.Length]).Hash();
		if (!fileNameHandles.TryGetValue(hash, out var handle)) {
			handle = fileNameHandles[hash] = ++currentHandle;
			fileNameStrings[handle] = new(name); // Make a copy of the string to live forever
		}

		return handle;
	}

	public void BeginMapAccess() {

	}

	public void EndMapAccess() {

	}

	public struct FileFindContext {
		public int Locked;

		public UtlSymbol Wildcard;
		public UtlSymbol PathID;
		public FileFindHandle_t FindHandle;
		public volatile int FileIdx;
		public volatile int PathIdx;
		public volatile int CollectionIdx;

		int ranAtLeastOnce;
		BaseFileSystem system;
		SearchPathCollection? currentCollection;
		SearchPath? currentPath;
		HashSet<FileNameHandle_t>? foundAlready;

		public void FullyLock(BaseFileSystem system, FileFindHandle_t lockedIdx, ReadOnlySpan<char> wildcard, ReadOnlySpan<char> pathID) {
			this.system = system;
			Reset();

			FindHandle = lockedIdx;
			Wildcard = new UtlSymbol(wildcard);
			PathID = new UtlSymbol(pathID);
		}

		public void Reset() {
			Wildcard = default;
			PathID = default;

			FileIdx = -1;
			PathIdx = -1;
			CollectionIdx = -1;
			ranAtLeastOnce = 0;

			currentCollection = null;
			currentPath = null;

			foundAlready ??= [];
			foundAlready.Clear();
		}

		

		public ReadOnlySpan<char> Next() {
			findCollection:
			if(currentCollection == null) {
				currentCollection = PathID == 0
					? system.SearchPaths.At(Interlocked.Increment(ref CollectionIdx))
					: Interlocked.CompareExchange(ref ranAtLeastOnce, 1, 0) == 0
						? system.SearchPaths[PathID]
						: null;

				if(currentCollection != null) {
					// Reset these parts...
					Interlocked.Exchange(ref FileIdx, -1);
					Interlocked.Exchange(ref PathIdx, -1);
					goto findPath; // We don't need to perform the next check
				}
			}
			if (currentCollection == null)
				return null; // Cannot continue.

			findPath:
			if(currentPath == null) {
				// Find the next collection.
				currentPath = currentCollection.At(Interlocked.Increment(ref PathIdx));

				if (currentPath != null) {
					currentPath.LockFinds(Wildcard, foundAlready!);
					Interlocked.Exchange(ref FileIdx, -1);
					// We don't need to perform the next check
					goto findFileDir;
				}
			}

			if(currentPath == null) {
				// Search for a new collection?
				currentCollection = null;
				goto findCollection;
			}

		findFileDir:
			string? currentFile = currentPath.FindAt(Interlocked.Increment(ref FileIdx));
			if(currentFile == null) {
				// Search for a new path?
				currentPath.UnlockFinds();
				currentPath = null;
				goto findPath;
			}

			return currentFile;
		}

		public void Close() {
			if(Locked == 0) {
				Warning("Tried to unlock a file handle that was already unlocked!!!\n");
				Assert(false);
				return;
			}

			Locked = 0;
			Reset();
		}
	}

	const int MAX_FILE_HANDLES = 512;
	readonly FileFindContext[] contexts = new FileFindContext[MAX_FILE_HANDLES];
	FileFindHandle_t currentFindHandle;

	public ReadOnlySpan<char> FindFirstEx(ReadOnlySpan<char> wildcard, ReadOnlySpan<char> pathID, out FileFindHandle_t findHandle) {
		for (int i = 0; i < MAX_FILE_HANDLES; i++) {
			findHandle = Interlocked.Increment(ref currentFindHandle);
			ref FileFindContext ctx = ref contexts[(int)(findHandle % MAX_FILE_HANDLES)];
			if (Interlocked.CompareExchange(ref ctx.Locked, 1, 0) == 0) {
				ctx.FullyLock(this, findHandle, wildcard, pathID);
				return ctx.Next();
			}
		}

		throw new Exception("File find error - we likely aren't as thread safe as we had hoped, or 512+ file handles are currently allocated");
	}

	public ReadOnlySpan<char> FindNext(FileFindHandle_t findHandle) {
		ref FileFindContext ctx = ref contexts[(int)(findHandle % MAX_FILE_HANDLES)];
		return ctx.Next();
	}

	public void FindClose(FileFindHandle_t findHandle) {
		ref FileFindContext ctx = ref contexts[(int)(findHandle % MAX_FILE_HANDLES)];
		ctx.Close();
	}

	public ReadOnlySpan<char> String(FileNameHandle_t handle) {
		return fileNameStrings.TryGetValue(handle, out string? v) ? v : null;
	}
}
