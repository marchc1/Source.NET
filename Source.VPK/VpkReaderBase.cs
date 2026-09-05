using Microsoft.Win32.SafeHandles;

using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Source.VPK
{
	internal abstract class VpkReaderBase
	{
		readonly BinaryReader reader;

		protected VpkReaderBase(string filename) => reader = new(File.OpenRead(filename));
		protected VpkReaderBase(byte[] file) =>reader = new(new MemoryStream(file));

		public abstract IVpkArchiveHeader ReadArchiveHeader();

		readonly List<char> StringBuffer = [];
		public string ReadNullTerminatedString() {
			StringBuffer.Clear();
			do {
				int ic = reader.ReadByte();
				if (ic == -1 || ic == 0)
					break;
				StringBuffer.Add((char)ic);
			} while (reader.BaseStream.Position < reader.BaseStream.Length);
			return new(CollectionsMarshal.AsSpan(StringBuffer)[..StringBuffer.Count]);
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
				nuint preloadDataOffset = (nuint)Reader.reader.BaseStream.Position;
				if (preloadBytes > 0)
					Reader.reader.Read(stackalloc byte[preloadBytes]);

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
		protected T Read<T>() where T : unmanaged {
			int sizeofT = Unsafe.SizeOf<T>();
			Span<byte> data = stackalloc byte[sizeofT];
			reader.Read(data);
			return MemoryMarshal.Cast<byte, T>(data)[0];
		}

		protected ReadOnlySpan<byte> ReadBytes(Span<byte> data) {
			reader.Read(data);
			return data;
		}

		#endregion

		public abstract uint CalculateEntryOffset(uint offset);
	}
}

