using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Source.VPK
{
	internal abstract class VpkReaderBase
	{
		private readonly byte[] memoryFile;
		private nuint ptr;

		protected VpkReaderBase(string filename) {
			memoryFile = File.ReadAllBytes(filename);
		}

		protected VpkReaderBase(byte[] file) {
			memoryFile = file;
		}

		protected ReadOnlySpan<byte> GetMemoryFileNoOffset() => memoryFile;
		protected ReadOnlySpan<byte> GetMemoryFileWithOffset() => memoryFile.AsSpan()[(int)ptr..];
		protected ReadOnlySpan<byte> GetMemoryFileWithOffsetAndSize(nuint size) => memoryFile.AsSpan()[(int)ptr..][..(int)size];

		public abstract IVpkArchiveHeader ReadArchiveHeader();

		public string ReadNullTerminatedString() {
			ReadOnlySpan<byte> rest = GetMemoryFileWithOffset();
			int len = rest.IndexOf((byte)0);
			string s = Encoding.ASCII.GetString(rest[..len]);
			ptr += (nuint)len + 1;
			return s;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static ref readonly T BytesToStructure<T>(ReadOnlySpan<byte> bytearray) where T : unmanaged {
			return ref MemoryMarshal.Cast<byte, T>(bytearray[..Unsafe.SizeOf<T>()])[0];
		}

		#region default
		public IEnumerable<VpkDirectory> ReadDirectories(VpkArchive parentArchive) {
			while (true) {
				var ext = ReadNullTerminatedString();
				if (string.IsNullOrEmpty(ext))
					break;
				while (true) {
					var path = ReadNullTerminatedString();
					if (string.IsNullOrEmpty(path))
						break;

					var entries = ReadEntries(parentArchive, ext, path).ToList();
					yield return new VpkDirectory(parentArchive, path, entries);
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected ref readonly T Read<T>() where T : unmanaged {
			int sizeofT = Unsafe.SizeOf<T>();
			ref readonly T val = ref MemoryMarshal.Cast<byte, T>(GetMemoryFileWithOffsetAndSize((nuint)sizeofT))[0];
			ptr += (nuint)sizeofT;
			return ref val;
		}

		protected ReadOnlySpan<byte> ReadBytes(int bytes) {
			ReadOnlySpan<byte> subspan = GetMemoryFileWithOffsetAndSize((nuint)bytes);
			ptr += (nuint)bytes;
			return subspan;
		}


		public IEnumerable<VpkEntry> ReadEntries(VpkArchive parentArchive, string ext, string path) {
			while (true) {
				var fileName = ReadNullTerminatedString();
				if (string.IsNullOrEmpty(fileName))
					break;

				var crc = Read<uint>();
				var preloadBytes = Read<ushort>();
				var archiveIdx = Read<ushort>();
				var entryOffset = Read<uint>();
				var entryLen = Read<uint>();
				// skip terminator
				Read<ushort>();
				nuint preloadDataOffset = ptr;
				if (preloadBytes > 0)
					ptr += preloadBytes;

				yield return new VpkEntry(parentArchive, crc, preloadBytes, preloadDataOffset, archiveIdx, entryOffset, entryLen, ext.ToLower(), path.ToLower(), fileName.ToLower());
			}
		}
		#endregion

		public abstract uint CalculateEntryOffset(uint offset);
	}
}

