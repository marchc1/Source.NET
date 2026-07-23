// TODO: Logging calls when things go wrong, ie. try/catches


using Source.Common.Filesystem;
using Source.Common.Utilities;

namespace Source.FileSystem;

public abstract class BaseSearchPath : ISearchPath
{
	public string? DiskPath { get; private set; }
	public ReadOnlySpan<char> GetDiskPath() => DiskPath;
	public void SetDiskPath(ReadOnlySpan<char> diskPath) {
		DiskPath = new(diskPath.SliceNullTerminatedString());
	}
	PathGroupName name;
	public PathGroupName GetGroupName() => name;
	public void SetGroupName(PathGroupName value) => name = value;


	public abstract bool Exists(ReadOnlySpan<char> path); // Returns if the file or directory exists
	public abstract bool IsDirectory(ReadOnlySpan<char> path); // Returns true if the path is a directory
	public abstract bool IsFileWritable(ReadOnlySpan<char> path); // Returns true if the path can be written to
	public abstract IFileHandle? Open(ReadOnlySpan<char> path, FileOpenOptions options); // Can return null if something went wrong
	public abstract bool RemoveFile(ReadOnlySpan<char> path); // Return true if the file was deleted
	public abstract bool RenameFile(ReadOnlySpan<char> oldPath, ReadOnlySpan<char> newPath); // Renames a single file, returns true if it worked
	public abstract bool SetFileWritable(ReadOnlySpan<char> path, bool writable); // Determines if the file is writable
	public abstract long Size(ReadOnlySpan<char> path); // Gets the size of a file
	/// <summary>
	/// Gets the last modified time of a file (UTC)
	/// </summary>
	/// <param name="path"></param>
	/// <returns></returns>
	public abstract DateTime Time(ReadOnlySpan<char> path);
	public abstract ReadOnlySpan<char> GetPathString();
	public abstract object? GetPackFile();
	public abstract object? GetPackedStore();

	protected abstract void PrepareFinds(List<string> files, List<string> dirs, string? wildcard);

	uint FindsIdx;
	readonly List<string> files = [];
	readonly List<string> dirs = [];
	public void LockFinds(UtlSymbol wildcard, HashSet<UtlSymId_t> foundAlready) {
		if (Interlocked.Increment(ref FindsIdx) == 1) {
			// Prepare the find buffers...
			// unfortunately requires a lock here.
			lock (files)
				lock (dirs) {
					files.Clear();
					dirs.Clear();
					PrepareFinds(files, dirs, wildcard.String());
					for (int i = dirs.Count - 1; i >= 0; i--)
						if (!foundAlready.Add(dirs[i].Hash()))
							dirs.RemoveAt(i);
					for (int i = files.Count - 1; i >= 0; i--)
						if (!foundAlready.Add(files[i].Hash()))
							files.RemoveAt(i);
				}

		}
	}
	public void UnlockFinds() {
		Interlocked.Decrement(ref FindsIdx);
	}
	public string? FindAt(int index) {
		if (Interlocked.CompareExchange(ref FindsIdx, 0, 0) == 0) {
			AssertMsg(false, "Unlocked find attempt");
			return null;
		}

		if (index >= files.Count) {
			if (index >= (files.Count + dirs.Count))
				return null;
			else
				return dirs[index - files.Count];
		}
		else
			return files[index];
	}
}
