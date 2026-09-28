using Source.Common.Filesystem;

namespace Source.Filesystem.GarrysMod;

public struct AddonFileInfo
{
	public string FileName;
	public string FolderName;
	public long Size;
	public long Offset;
	public IFileHandle? FileHandle;
	public bool Folder;
	public bool ServerDownloaded;
	public ulong WorkshopID;
}

public class AddonFileHandle : Stream, IFileHandle
{
	static readonly List<AddonFileHandle> OpenHandles = [];

	IFileHandle? PackFile;
	readonly long FileSize;
	readonly long FileOffset;
	long Position_;
	readonly FileNameHandle_t FileName;

	public AddonFileHandle(in AddonFileInfo info, FileNameHandle_t fileName) {
		PackFile = info.FileHandle;
		FileOffset = info.Offset;
		FileSize = info.Size;
		Position_ = 0;
		FileName = fileName;

		lock (OpenHandles)
			OpenHandles.Add(this);
	}

	public static void OnPackFileUnmounted(IFileHandle packFile) {
		lock (OpenHandles) {
			foreach (AddonFileHandle handle in OpenHandles) {
				if (handle.PackFile == packFile)
					handle.PackFile = null;
			}
		}
	}

	public Stream Stream => this;
	public FileNameHandle_t FileNameHandle => FileName;
	public ReadOnlySpan<char> GetPath() => g_FullFileSystem.String(FileName);
	public bool IsOK() => PackFile != null;

	public override bool CanRead => true;
	public override bool CanSeek => true;
	public override bool CanWrite => false;
	public override long Length => FileSize;
	public override long Position { get => Position_; set => Seek(value, SeekOrigin.Begin); }

	public override int Read(Span<byte> buffer) {
		int remaining = (int)(FileSize - Position_);
		if (buffer.Length < remaining)
			remaining = buffer.Length;

		if (remaining <= 0)
			return 0;

		IFileHandle? packFile = PackFile;
		if (packFile == null)
			return 0;

		lock (packFile) {
			packFile.Stream.Seek(FileOffset + Position_, SeekOrigin.Begin);
			packFile.Stream.ReadExactly(buffer[..remaining]);
		}

		Position_ += remaining;
		return remaining;
	}

	public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

	public override long Seek(long offset, SeekOrigin origin) {
		long newPosition = origin switch {
			SeekOrigin.Begin => offset,
			SeekOrigin.Current => Position_ + offset,
			SeekOrigin.End => FileSize + offset,
			_ => FileOffset
		};

		if (newPosition < 0)
			newPosition = 0;

		if (newPosition > FileSize)
			newPosition = FileSize;

		Position_ = newPosition;
		return Position_;
	}

	public override void Flush() { }
	public override void SetLength(long value) => throw new NotSupportedException();
	public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

	protected override void Dispose(bool disposing) {
		lock (OpenHandles)
			OpenHandles.Remove(this);
		base.Dispose(disposing);
	}
}
