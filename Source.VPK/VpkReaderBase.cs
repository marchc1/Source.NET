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

		public struct DirectoryReader(VpkReaderBase reader, VpkArchive parentArchive)
		{
			readonly VpkReaderBase Reader = reader;
			readonly VpkArchive ParentArchive = parentArchive;
			string? ext = null;
			public VpkDirectory Current = null!;

			public bool MoveNext() {
				while (true) {
					if (ext == null) {
						ext = Reader.ReadNullTerminatedString();
						if (string.IsNullOrEmpty(ext))
							return false;            // end of tree
						ext = ext.ToLowerInvariant(); // once per extension block
					}

					var path = Reader.ReadNullTerminatedString();
					if (string.IsNullOrEmpty(path)) {
						ext = null;                   // end of this ext's paths, get the next one
						continue;
					}

					Current = new VpkDirectory(ParentArchive, path,
						Reader.ReadEntries(ParentArchive, ext, path).ToList());
					return true;
				}
			}
		}

		#region default
		public DirectoryReader ReadDirectories(VpkArchive parentArchive) => new(this, parentArchive);

		public struct EntryReader(VpkReaderBase reader, VpkArchive parentArchive, string ext, string path)
		{
			readonly VpkReaderBase Reader = reader;
			readonly VpkArchive ParentArchive = parentArchive;
			readonly string Ext = ext;
			readonly string Path = path;
			public bool MoveNext() {
				var fileName = Reader.ReadNullTerminatedString();
				if (string.IsNullOrEmpty(fileName))
					return false;

				var crc = Reader.Read<uint>();
				var preloadBytes = Reader.Read<ushort>();
				var archiveIdx = Reader.Read<ushort>();
				var entryOffset = Reader.Read<uint>();
				var entryLen = Reader.Read<uint>();
				// skip terminator
				Reader.Read<ushort>();
				nuint preloadDataOffset = Reader.ptr;
				if (preloadBytes > 0)
					Reader.ptr += preloadBytes;

				Current = new VpkEntry(ParentArchive, crc, preloadBytes, preloadDataOffset, archiveIdx, entryOffset, entryLen, Ext, Path, fileName.ToLowerInvariant());
				return true;
			}

			public List<VpkEntry> ToList() {
				List<VpkEntry> entries = [];
				while (MoveNext())
					entries.Add(Current);
				return entries;
			}

			public VpkEntry Current = null!;
		}
		public EntryReader ReadEntries(VpkArchive parentArchive, string ext, string path) => new(this, parentArchive, ext.ToLowerInvariant(), path.ToLowerInvariant());

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

		#endregion

		public abstract uint CalculateEntryOffset(uint offset);
	}
}

