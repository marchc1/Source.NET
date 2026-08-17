// TODO: Logging calls when things go wrong, ie. try/catches
#if !WIN32
#define FORCE_CASE_INSENSITIVE_ON_DISK
#endif

using SharpCompress.Common;

using Source.Common.Commands;
using Source.Common.Filesystem;
using Source.Common.Utilities;

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Source.FileSystem;

public sealed class DiskEntityCache
{
	public readonly DirectoryCache Root;
	/// <summary>
	/// The platform-specific absolute path.
	/// </summary>
	public readonly string AbsolutePath = "";
	/// <summary>
	/// The platform-agnostic relative path. (slashing is always /)
	/// </summary>
	public readonly string RelativePath = "";
	// public readonly UtlSymId_t AbsoluteHash;
	// public readonly UtlSymId_t RelativeHash;
	public bool IsDirectory;

	public DiskEntityCache(DirectoryCache root, string absolutePath, string relativePath, bool isDirectory) {
		Root = root;
		AbsolutePath = absolutePath;
		RelativePath = relativePath;
		IsDirectory = isDirectory;
		// AbsoluteHash = absolutePath.Hash();
		// RelativeHash = relativePath.Hash();
	}
}

public sealed class DirectoryCache
{
	public static string CreateRelativePath(string basePath, string absolutePath) {
		return System.IO.Path.GetRelativePath(basePath, absolutePath).Replace('\\', '/').ToLowerInvariant();
	}

	public readonly string Path;
	readonly ConcurrentDictionary<UtlSymId_t, DiskEntityCache> items = [];
	FileSystemWatcher watcher;

	public DirectoryCache(ReadOnlySpan<char> path) {
		path = path.SliceNullTerminatedString();
		Path = new(path);

		ReinitializeWatcher();
	}

	[MemberNotNull(nameof(watcher))]
	private void ReinitializeWatcher() {
		items.Clear();

		Directory.CreateDirectory(Path);
		
		if (watcher != null) {
			watcher.EnableRaisingEvents = false;
			watcher.Dispose();
		}

		watcher = new(Path) {
			InternalBufferSize = 1024 * 64,
			IncludeSubdirectories = true
		};

		watcher.Changed += (_, args) => OnChanged(args);
		watcher.Created += (_, args) => OnCreated(args);
		watcher.Deleted += (_, args) => OnDeleted(args);
		watcher.Error += (_, _) => ReinitializeWatcher();
		watcher.Renamed += (_, args) => OnRenamed(args);

		watcher.NotifyFilter |= 
								NotifyFilters.FileName | 
								NotifyFilters.CreationTime | 
								NotifyFilters.Attributes | 
								NotifyFilters.LastWrite | 
								NotifyFilters.DirectoryName | 
								NotifyFilters.Size;

		watcher.EnableRaisingEvents = true;

		foreach (var entry in new DirectoryInfo(Path).EnumerateFileSystemInfos("*", new EnumerationOptions() {
			MatchCasing = MatchCasing.CaseInsensitive,
			MatchType = MatchType.Simple,
			RecurseSubdirectories = true,
			ReturnSpecialDirectories = true,
			IgnoreInaccessible = true
		})) {
			// Create a relative path
			string relativePath = CreateRelativePath(Path, entry.FullName);
			bool isDirectory = (entry.Attributes & FileAttributes.Directory) != 0;
			items[relativePath.Hash(invariant: true)] = new(this, entry.FullName, relativePath, isDirectory);
		}
	}
	private void OnChanged(FileSystemEventArgs args) {

	}
	private void OnCreated(FileSystemEventArgs args) {
		string relativePath = CreateRelativePath(Path, args.FullPath);
		items.GetOrAdd(relativePath.Hash(invariant: true), (_) => new(this, args.FullPath, relativePath, Directory.Exists(args.FullPath)));
	}
	private void OnDeleted(FileSystemEventArgs args) {
		string relativePath = CreateRelativePath(Path, args.FullPath);
		items.TryRemove(relativePath.Hash(invariant: true), out _);
	}
	private void OnRenamed(RenamedEventArgs args) {
		string oldRelativePath = CreateRelativePath(Path, args.OldFullPath);
		items.TryRemove(oldRelativePath.Hash(invariant: true), out _);

		string relativePath = CreateRelativePath(Path, args.FullPath);
		items.GetOrAdd(relativePath.Hash(invariant: true), (_) => new(this, args.FullPath, relativePath, Directory.Exists(args.FullPath)));
	}

	private string GetAbsolutePath(ReadOnlySpan<char> path) {
		string p = path.ToString();
		return System.IO.Path.IsPathFullyQualified(p) ? p : System.IO.Path.Combine(Path, p);
	}

	private UtlSymId_t GetRelativeKey(ReadOnlySpan<char> path)		=> CreateRelativePath(Path, GetAbsolutePath(path)).Hash(invariant: true);
	internal bool Directory_Exists(ReadOnlySpan<char> path)		=> items.TryGetValue(GetRelativeKey(path), out var entity) && entity.IsDirectory;
	internal bool Path_Exists(ReadOnlySpan<char> path)		=> items.ContainsKey(GetRelativeKey(path));

	internal FileInfo Info(ReadOnlySpan<char> path) {
		if (items.TryGetValue(GetRelativeKey(path), out var entity))
			return new FileInfo(entity.AbsolutePath);
		return new FileInfo(GetAbsolutePath(path));
	}

	internal void Rename(ReadOnlySpan<char> oldPath, ReadOnlySpan<char> newPath) {
		string oldAbsolute = items.TryGetValue(GetRelativeKey(oldPath), out var entity)
			? entity.AbsolutePath
			: GetAbsolutePath(oldPath);
		string newAbsolute = GetAbsolutePath(newPath);

		if (Directory.Exists(oldAbsolute))
			Directory.Move(oldAbsolute, newAbsolute);
		else
			File.Move(oldAbsolute, newAbsolute, overwrite: true);
	}

	internal void Find(List<string> files, List<string> dirs, string? wildcard) {
		string relativeDir;
		string pattern;
		if (string.IsNullOrEmpty(wildcard)) {
			relativeDir = "";
			pattern = "*";
		}
		else {
			string normalized = wildcard.Replace('\\', '/');
			int lastSlash = normalized.LastIndexOf('/');
			if (lastSlash >= 0) {
				relativeDir = normalized[..lastSlash].ToLowerInvariant();
				pattern = normalized[(lastSlash + 1)..];
			}
			else {
				relativeDir = "";
				pattern = normalized;
			}
		}

		foreach (var entity in items.Values) {
			string rel = entity.RelativePath;
			int slash = rel.LastIndexOf('/');
			string parent = slash >= 0 ? rel[..slash] : "";

			if (parent != relativeDir)
				continue;

			string name = System.IO.Path.GetFileName(entity.AbsolutePath);
			if (name is "." or "..")
				continue;

			if (!System.IO.Enumeration.FileSystemName.MatchesSimpleExpression(pattern, name, ignoreCase: true))
				continue;

			if (entity.IsDirectory)
				dirs.Add(name);
			else
				files.Add(name);
		}
	}
}

public class DiskSearchPath : BaseSearchPath
{
	private DirectoryCache dir;
	private IFileSystem parent;

	public DiskSearchPath(IFileSystem filesystem, string absPath) {
		parent = filesystem;

		if (!Path.IsPathFullyQualified(absPath))
			absPath = Path.GetFullPath(absPath);

		SetDiskPath(absPath);
		dir = new(absPath);
	}

	public override bool Exists(ReadOnlySpan<char> path) => dir.Path_Exists(path);
	public override bool IsDirectory(ReadOnlySpan<char> path) => dir.Directory_Exists(path);

	public override bool IsFileWritable(ReadOnlySpan<char> path) {
		var info = dir.Info(path);
		return info.Exists && !info.IsReadOnly;
	}

	public override IFileHandle? Open(ReadOnlySpan<char> path, FileOpenOptions options) {
		var info = dir.Info(path);

		// Scram early if the file doesn't even exist
		if (!info.Exists && (options == FileOpenOptions.Read || options == FileOpenOptions.ReadEx)) return null;

		// Check file options for invalid access
		FileOpenOptions operation = options.GetOperation();
		if (operation == FileOpenOptions.Write && info.IsReadOnly && info.Exists)
			return null;

		// Open the file stream
		FileMode mode = operation switch {
			FileOpenOptions.Read => FileMode.Open,
			FileOpenOptions.Write => FileMode.Create,
			FileOpenOptions.Append => FileMode.Append,
			_ => throw new NotSupportedException()
		};

		FileAccess access = options.Extended() ? FileAccess.ReadWrite : operation switch {
			FileOpenOptions.Read => FileAccess.Read,
			FileOpenOptions.Write => FileAccess.Write,
			FileOpenOptions.Append => FileAccess.Write,
			_ => throw new NotSupportedException()
		};

		try {
			return new DiskFileHandle(parent, info.Open(mode, access), parent.FindOrAddFileName(path));
		}
		catch {
			return null;
		}
	}

	public override bool RemoveFile(ReadOnlySpan<char> path) {
		var info = dir.Info(path);

		if (!info.Exists) return false;
		if (info.IsReadOnly) return false;

		try {
			info.Delete();
		}
		catch {
			return false;
		}

		return true;
	}

	public override bool RenameFile(ReadOnlySpan<char> oldPath, ReadOnlySpan<char> newPath) {
		var info = dir.Info(oldPath);

		if (!info.Exists) return false;
		if (info.IsReadOnly) return false;

		try {
			dir.Rename(oldPath, newPath);
		}
		catch {
			return false;
		}

		return true;
	}

	// Do nothing
	public override bool SetFileWritable(ReadOnlySpan<char> path, bool writable) => false;

	public override long Size(ReadOnlySpan<char> path) {
		var info = dir.Info(path);
		if (!info.Exists) return -1;

		return info.Length;
	}

	public override DateTime Time(ReadOnlySpan<char> path) {
		var info = dir.Info(path);
		if (!info.Exists) return DateTime.UnixEpoch;

		return info.LastWriteTimeUtc;
	}

	public override ReadOnlySpan<char> GetPathString() => DiskPath;

	public override object? GetPackFile() {
		return null;
	}

	public override object? GetPackedStore() {
		return null;
	}

	protected override void PrepareFinds(List<string> files, List<string> dirs, string? wildcard) {
		dir.Find(files, dirs, wildcard);
	}
}
