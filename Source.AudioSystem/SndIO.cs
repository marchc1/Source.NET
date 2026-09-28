global using static Source.AudioSystem.SndIO;

using Source.Common.Filesystem;
using Source.Common.Formats;

namespace Source.AudioSystem;

//-----------------------------------------------------------------------------
// Purpose: Implements Audio IO on the engine's COMMON filesystem
//-----------------------------------------------------------------------------
public class COM_IOReadBinary : IFileReadBinary
{
	// prepend sound/ to the filename -- all sounds are loaded from the sound/ directory
	public object? Open(ReadOnlySpan<char> fileName) {
		Span<char> namebuffer = stackalloc char[512];
		strcpy(namebuffer, "sound");

		// the server is sending back sound names with slashes in front...
		if (fileName.Length > 0 && fileName[0] != '/' && fileName[0] != '\\')
			strcat(namebuffer, "/");

		strcat(namebuffer, fileName);

		IFileHandle? file = filesystem.Open(namebuffer.SliceNullTerminatedString(), FileOpenOptions.Read | FileOpenOptions.Binary, "GAME");

		return file;
	}

	public int Read(Span<byte> output, object? file) {
		if (file is not IFileHandle handle)
			return 0;

		return handle.Stream.Read(output);
	}

	public void Seek(object? file, int pos) {
		if (file is not IFileHandle handle)
			return;

		handle.Stream.Seek(pos, SeekOrigin.Begin);
	}

	public uint Tell(object? file) {
		if (file is not IFileHandle handle)
			return 0;

		return (uint)handle.Stream.Position;
	}

	public uint Size(object? file) {
		if (file is not IFileHandle handle)
			return 0;

		return (uint)handle.Stream.Length;
	}

	public void Close(object? file) {
		if (file is not IFileHandle handle)
			return;

		handle.Dispose();
	}
}

public static class SndIO
{
	public static readonly IFileReadBinary g_pSndIO = new COM_IOReadBinary();
}
