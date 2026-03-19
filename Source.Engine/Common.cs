using CommunityToolkit.HighPerformance;

using Microsoft.Extensions.DependencyInjection;

using Snappier;

using Source.Common;
using Source.Common.Engine;
using Source.Common.Filesystem;

using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;

using static Source.Common.FilesystemHelpers;

namespace Source.Engine;

/// <summary>
/// Common functionality
/// </summary>
/// <param name="providers"></param>
public class Common(IServiceProvider providers, ILocalize? Localize, Sys Sys)
{
	public static string Gamedir { get; private set; }
	const uint SNAPPY_ID = ('P' << 24) | ('A' << 16) | ('N' << 8) | ('S');

	// TODO: make safe. I'm lazy right now
	static unsafe byte* CompressBuffer_Snappy(byte* source, uint sourceLen, uint* compressedLen, uint maxCompressedLen) {
		Assert(source != null);
		Assert(compressedLen != null);

		// Allocate a buffer big enough to hold the worst case.
		uint nMaxCompressedSize = GetIdealDestinationCompressionBufferSize_Snappy(sourceLen);
		byte* pCompressed = (byte*)NativeMemory.Alloc(nMaxCompressedSize);
		if (pCompressed == null)
			return null;

		// Do the compression
		*(uint*)pCompressed = SNAPPY_ID;
		int compressed_length = Snappy.Compress(new Span<byte>(source, (int)sourceLen), new(pCompressed + sizeof(uint), (int)nMaxCompressedSize - sizeof(uint)));
		compressed_length += 4;
		Assert(compressed_length <= nMaxCompressedSize);

		// Check if this result is OK
		if (maxCompressedLen != 0 && compressed_length > maxCompressedLen) {
			NativeMemory.Free(pCompressed);
			return null;
		}

		*compressedLen = (uint)compressed_length;
		return pCompressed;
	}
	static uint GetIdealDestinationCompressionBufferSize_Snappy(uint uncompressed) => 4 + (uint)Snappy.GetMaxCompressedLength((int)uncompressed);
	static unsafe bool BufferToBufferCompress_Snappy(byte* dest, uint* destLen, byte* source, uint sourceLen) {
		Assert(dest != null);
		Assert(destLen != null);
		Assert(source != null);

		// Check if we need to use a temporary buffer
		uint nMaxCompressedSize = GetIdealDestinationCompressionBufferSize_Snappy(sourceLen);
		uint compressedLen = *destLen;
		if (compressedLen < nMaxCompressedSize) {
			// Yep.  Use the other function to allocate the buffer of the right size and comrpess into it
			byte* temp = CompressBuffer_Snappy(source, sourceLen, &compressedLen, compressedLen);
			if (temp == null)
				return false;

			// Copy over the data
			memcpy(dest, temp, compressedLen);
			*destLen = compressedLen;
			NativeMemory.Free(temp);
			return true;
		}

		// We have room and should be able to compress directly
		*(uint*)dest = SNAPPY_ID;
		int compressed_length = Snappy.Compress(new Span<byte>(source, (int)sourceLen), new(dest + sizeof(uint), (int)(destLen - sizeof(uint))));
		compressed_length += 4;
		Assert(compressed_length <= nMaxCompressedSize);
		*destLen = (uint)compressed_length;
		return true;
	}

	public static unsafe bool BufferToBufferCompress_Snappy(ref Span<byte> destinationBuffer, ReadOnlySpan<byte> sourceBuffer) {
		fixed (byte* dest = destinationBuffer)
		fixed (byte* src = sourceBuffer) {
			uint destLen = (uint)destinationBuffer.Length;
			bool result = BufferToBufferCompress_Snappy(dest, &destLen, src, (uint)sourceBuffer.Length);
			destinationBuffer = new(dest, (int)destLen);
			return result;
		}
	}

	public void InitFilesystem(ReadOnlySpan<char> fullModPath) {
		CFSSearchPathsInit initInfo = new();
		IEngineAPI engineAPI = providers.GetRequiredService<IEngineAPI>();
		Host Host = providers.GetRequiredService<Host>();
		FileSystem FileSystem = providers.GetRequiredService<FileSystem>();

		initInfo.FileSystem = engineAPI.GetRequiredService<IFileSystem>();
		initInfo.DirectoryName = new(fullModPath);
		if (initInfo.DirectoryName == null)
			initInfo.DirectoryName = Host.GetCurrentGame();

		Host.CheckGore();

		initInfo.LowViolence = Host.LowViolence;
		initInfo.MountHDContent = false; // Study this further

		FileSystem.LoadSearchPaths(in initInfo);

		Gamedir = initInfo.ModPath ?? "";
	}

	public bool Initialized { get; private set; }
	public void Init() {
		Initialized = true;
	}

	const int COM_TOKEN_MAX_LENGTH = 1024;
	static readonly byte[] com_token = new byte[COM_TOKEN_MAX_LENGTH];

	public static ReadOnlySpan<byte> ParseFile(ReadOnlySpan<byte> data, Span<char> token) {
		ReadOnlySpan<byte> returnData = Parse(data);
		ReadOnlySpan<byte> nullTermToken = com_token.AsSpan()[..System.MemoryExtensions.IndexOf(com_token, (byte)0)];
		token.Clear(); // todo: only set one char
		Encoding.ASCII.GetChars(nullTermToken, token);

		return returnData;
	}

	static ReadOnlySpan<byte> Parse(ReadOnlySpan<byte> data) {
		byte c;
		int len;
		CharacterSet breaks;

		breaks = BreakSetIncludingColons;
		if (com_ignorecolons)
			breaks = BreakSet;

		len = 0;
		com_token[0] = 0;

		if (data.IsEmpty)
			return null;

	skipwhite:
		while ((c = data[0]) <= ' ') {
			if (c == 0)
				return null;
			data = data[1..];
			if (data.IsEmpty)
				return null;
		}

		if (c == '/' && data[1] == '/') {
			while (!data.IsEmpty && data[0] != '\0' && data[0] != '\n')
				data = data[1..];
			goto skipwhite;
		}

		if (c == '\"') {
			data = data[1..];
			while (true) {
				c = data[0];
				data = data[1..];
				if (c == '\"' || c == '\0') {
					com_token[len] = 0;
					return data;
				}
				com_token[len] = c;
				len++;
			}
		}

		if (breaks.Contains((char)c)) {
			com_token[len] = c;
			len++;
			com_token[len] = 0;
			return data[1..];
		}

		do {
			com_token[len] = c;
			data = data[1..];
			len++;
			c = data[0];
			if (breaks.Contains((char)c))
				break;
		} while (c > 32);

		com_token[len] = 0;
		return data;
	}

	public static bool IsValidPath(ReadOnlySpan<char> filename) {
		if (filename.IsEmpty)
			return false;

		if (filename.Length == 0
			|| filename.Contains("\\\\", StringComparison.OrdinalIgnoreCase) // To protect network paths
			|| filename.Contains(":", StringComparison.OrdinalIgnoreCase) // To protect absolute paths
			|| filename.Contains("..", StringComparison.OrdinalIgnoreCase) // To protect relative paths
			|| filename.Contains("\n", StringComparison.OrdinalIgnoreCase)
			|| filename.Contains("\r", StringComparison.OrdinalIgnoreCase)
		)
			return false;

		return true;
	}

	public void ExplainDisconnection(bool print, ReadOnlySpan<char> disconnectReason) {
		if (print && !disconnectReason.IsEmpty) {
			if (disconnectReason.Length > 0 && disconnectReason[0] == '#')
				disconnectReason = Localize == null ? disconnectReason : Localize.Find(disconnectReason);

			ConMsg($"{disconnectReason}\n");
		}
		Sys.DisconnectReason = new(disconnectReason);
		Sys.ExtendedError = true;
	}

	internal static void TimestampedLog(ReadOnlySpan<char> msg) {
		string time = DateTime.Now.ToString("G");
		Span<char> finalMsg = stackalloc char[msg.Length + 5 + time.Length];
		finalMsg[0] = '[';
		time.CopyTo(finalMsg[1..]);
		"]: ".CopyTo(finalMsg[(1 + time.Length)..]);
		msg.CopyTo(finalMsg[(1 + time.Length + 3)..]);
		finalMsg[^1] = '\n';
		Msg(finalMsg);
	}

	public void Shutdown() {

	}

	public static ReadOnlySpan<char> FormatSeconds(double seconds) {
		int hours = 0;
		int minutes = (int)(seconds / 60);

		if (minutes > 0) {
			seconds -= minutes * 60;
			hours = minutes / 60;

			if (hours > 0)
				minutes -= hours * 60;
		}

		if (hours > 0)
			return $"{hours}:{minutes:00}:{(int)seconds:00}";
		else
			return $"{minutes}:{(int)seconds:00}";
	}
}
