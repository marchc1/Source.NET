namespace Source.Common.Filesystem;

public interface IBaseFileSystem
{
	/// <summary>
	/// Tries to open the file. May return null.
	/// </summary>
	/// <param name="fileName">The file name.</param>
	/// <param name="options">File options.<br/><code>
	/// |==============================================|
	/// | r  | Read                                    |
	/// | w  | Read                                    |
	/// | a  | Read                                    |
	/// | +  | Extended                                |
	/// | b  | Binary                                  |
	/// | n  | Text                                    |
	/// |==============================================|
	/// | r+ | Read   + Extended (or just ReadEx)      |
	/// | w+ | Write  + Extended (or just WriteEx)     |
	/// | a+ | Append + Extended (or just AppendEx)    |
	/// |==============================================|
	/// </code></param>
	/// <param name="pathID"></param>
	/// <returns></returns>
	public IFileHandle? Open(ReadOnlySpan<char> fileName, FileOpenOptions options, ReadOnlySpan<char> pathID);
	/// <summary>
	/// Tries to open the file. May return null.
	/// </summary>
	/// <param name="fileName">The file name.</param>
	/// <param name="options">File options.<br/><code>
	/// |==============================================|
	/// | r  | Read                                    |
	/// | w  | Read                                    |
	/// | a  | Read                                    |
	/// | +  | Extended                                |
	/// | b  | Binary                                  |
	/// | n  | Text                                    |
	/// |==============================================|
	/// | r+ | Read   + Extended (or just ReadEx)      |
	/// | w+ | Write  + Extended (or just WriteEx)     |
	/// | a+ | Append + Extended (or just AppendEx)    |
	/// |==============================================|
	/// </code></param>
	/// <returns></returns>
	public IFileHandle? Open(ReadOnlySpan<char> fileName, FileOpenOptions options)
		=> Open(fileName, options, null);
	/// <summary>
	/// Checks if the file is writable.
	/// </summary>
	/// <param name="fileName">The file name.</param>
	/// <param name="pathID">The search path ID.</param>
	/// <returns>True if the file is writable, and vice versa.</returns>
	public bool IsFileWritable(ReadOnlySpan<char> fileName, ReadOnlySpan<char> pathID);
	/// <summary>
	/// Tries to set the file as writable.
	/// </summary>
	/// <param name="fileName">The file name.</param>
	/// <param name="writable">Is it writable?</param>
	/// <param name="pathID">The search path ID.</param>
	/// <returns>True if the operation succeded, and false if it didn't.</returns>
	public bool SetFileWritable(ReadOnlySpan<char> fileName, bool writable, ReadOnlySpan<char> pathID);
	public long Size(ReadOnlySpan<char> fileName, ReadOnlySpan<char> pathID);
	public DateTime Time(ReadOnlySpan<char> fileName, ReadOnlySpan<char> pathID);
	public bool FileExists(ReadOnlySpan<char> fileName, ReadOnlySpan<char> pathID);


	// I can't default these :/
	public long Size(ReadOnlySpan<char> fileName) => Size(fileName, null);
	public DateTime Time(ReadOnlySpan<char> fileName) => Time(fileName, null);
	public bool FileExists(ReadOnlySpan<char> fileName) => FileExists(fileName, null);

	public bool ReadFile(ReadOnlySpan<char> fileName, ReadOnlySpan<char> path, Span<byte> buf, int startingByte);
	public bool ReadFile(ReadOnlySpan<char> fileName, ReadOnlySpan<char> path, Span<char> buf, int startingByte);

}

public interface IFileSystem : IBaseFileSystem
{
	public FileSystemMountRetval MountSteamContent(long extraAppID = -1);
	/// <summary>
	/// Add a search path.
	/// </summary>
	/// <param name="path"></param>
	/// <param name="pathID"></param>
	/// <param name="addType"></param>
	public void AddSearchPath(ReadOnlySpan<char> path, ReadOnlySpan<char> pathID, SearchPathAdd addType = SearchPathAdd.ToTail);
	/// <summary>
	/// Remove a search path.
	/// </summary>
	/// <param name="path"></param>
	/// <param name="pathID"></param>
	public bool RemoveSearchPath(ReadOnlySpan<char> path, ReadOnlySpan<char> pathID);
	/// <summary>
	/// Remove all search paths.
	/// </summary>
	public void RemoveAllSearchPaths();
	/// <summary>
	/// Remove all search paths associated with a given path ID.
	/// </summary>
	/// <param name="pathID"></param>
	public void RemoveSearchPaths(ReadOnlySpan<char> pathID);

	/// <summary>
	/// Marks a path ID by request only, which means files inside of it will only be accessed if the path ID is specifically requested.<br/>
	/// Otherwise, it will be ignored (in the case of global lookups without a path ID). <br/><br/>
	/// <b>NOTE</b>: <i>If there are currently no search paths with this path ID, then it will still remember it for later if you add other search paths with that path ID.</i>
	/// </summary>
	/// <param name="pathID"></param>
	/// <param name="requestOnly"></param>
	public void MarkPathIDByRequestOnly(ReadOnlySpan<char> pathID, bool requestOnly);

	bool RemoveFile(ReadOnlySpan<char> relativePath, ReadOnlySpan<char> pathID);
	bool RemoveFile(ReadOnlySpan<char> relativePath) => RemoveFile(relativePath, null);
	bool RenameFile(ReadOnlySpan<char> oldPath, ReadOnlySpan<char> newPath, ReadOnlySpan<char> pathID);
	bool RenameFile(ReadOnlySpan<char> oldPath, ReadOnlySpan<char> newPath) => RenameFile(oldPath, newPath, null);
	void CreateDirHierarchy(ReadOnlySpan<char> path, ReadOnlySpan<char> pathID);
	void CreateDirHierarchy(ReadOnlySpan<char> path) => CreateDirHierarchy(path, null);
	bool IsDirectory(ReadOnlySpan<char> fileName, ReadOnlySpan<char> pathID);
	bool IsDirectory(ReadOnlySpan<char> fileName) => IsDirectory(fileName, null);
	void GetLocalCopy(ReadOnlySpan<char> path);
	ReadOnlySpan<char> RelativePathToFullPath(ReadOnlySpan<char> fileName, ReadOnlySpan<char> pathID, Span<char> dest, PathTypeFilter filter = PathTypeFilter.None);
	void MarkAllCRCsUnverified();
	ReadOnlySpan<char> WhereIsFile(ReadOnlySpan<char> relativePath, ReadOnlySpan<char> pathID = default);
	void PrintSearchPaths();

	/// <summary>
	/// FileNameHandle_t's are case-insensitive and slash-insensitive.
	/// </summary>
	/// <param name="name"></param>
	/// <returns></returns>
	FileNameHandle_t FindOrAddFileName(ReadOnlySpan<char> name);
	FileNameHandle_t FindFileName(ReadOnlySpan<char> name);
	void BeginMapAccess();
	void EndMapAccess();

	ReadOnlySpan<char> FindFirstEx(ReadOnlySpan<char> wildcard, ReadOnlySpan<char> pathID, out ulong findHandle);
	ReadOnlySpan<char> FindNext(ulong findHandle);
	void FindClose(ulong findHandle);

	ReadOnlySpan<char> String(FileNameHandle_t nameHandle);
}

public enum PathTypeFilter
{
	None,
	CullPack,
	CullNonPack
}
