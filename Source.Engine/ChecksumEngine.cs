using Source.Common.Filesystem;
using Source.Common.Formats.BSP;
using Source.Common.Hashing;

using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Source.Engine;

public static class ChecksumEngine
{
	public static bool MD5_MapFile(out MD5Value md5Value, ReadOnlySpan<char> fileName) {
		md5Value = default;

		using IFileHandle? fp = g_pFileSystem.Open(fileName, FileOpenOptions.Read | FileOpenOptions.Binary);
		if (fp == null)
			return false;

		Stream stream = fp.Stream;
		long startOfs = stream.Position;

		BSPHeader header = default;
		if (stream.Read(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref header, 1))) == 0) {
			ConMsg($"Could not read BSP header for map [{fileName}].\n");
			return false;
		}

		if (header.Version < BSPFileCommon.MINBSPVERSION || header.Version > BSPFileCommon.BSPVERSION) {
			ConMsg($"Map [{fileName}] has incorrect BSP version ({header.Version} should be {BSPFileCommon.BSPVERSION}).\n");
			return false;
		}

		using IncrementalHash md5 = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
		Span<byte> chunk = stackalloc byte[1024];

		for (int l = 0; l < BSPFileCommon.HEADER_LUMPS; l++) {
			if ((LumpIndex)l == LumpIndex.Entities)
				continue;

			BSPLump curLump = header.Lumps[l];
			int nSize = curLump.FileLength;

			stream.Seek(startOfs + curLump.FileOffset, SeekOrigin.Begin);

			while (nSize > 0) {
				int toRead = Math.Min(1024, nSize);
				int nBytesRead = stream.Read(chunk[..toRead]);

				if (nBytesRead > 0) {
					nSize -= nBytesRead;
					md5.AppendData(chunk[..nBytesRead]);
				}

				if (!fp.IsOK())
					return false;
			}
		}

		md5Value.Bits = md5.GetHashAndReset();

#if DEBUG
		ConWarning($"MD5 for map [{fileName}]: {md5Value}\n");
#endif

		return true;
	}
}