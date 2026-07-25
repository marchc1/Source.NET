namespace Source.VPK
{
	public class VpkEntry
	{
		public string Extension { get; set; }
		public string Path { get; set; }
		public string Filename { get; set; }
		public string FilenameAndExtension { get; set; }
		public byte[] PreloadData { get { return ReadPreloadData(); } }
		public byte[] Data { get { return ReadData(); } }
		public bool HasPreloadData { get; set; }

		public readonly uint CRC;
		public readonly ushort PreloadBytes;
		public readonly nuint PreloadDataOffset;
		public readonly ushort ArchiveIndex;
		public readonly uint EntryOffset;
		public readonly uint EntryLength;
		public readonly VpkArchive ParentArchive;

		public override string ToString() => $"VpkEntry '{Path}/{Filename}.{Extension}' [crc {CRC}, entry<{EntryOffset}-{EntryLength}>]";

		internal VpkEntry(VpkArchive parentArchive, uint crc, ushort preloadBytes, nuint preloadDataOffset, ushort archiveIndex, uint entryOffset,
			uint entryLength, string extension, string path, string filename) {
			ParentArchive = parentArchive;
			CRC = crc;
			PreloadBytes = preloadBytes;
			PreloadDataOffset = preloadDataOffset;
			ArchiveIndex = archiveIndex;
			EntryOffset = entryOffset;
			EntryLength = entryLength;
			Extension = extension;
			Path = path;
			Filename = filename;
			FilenameAndExtension = filename + "." + Extension;
			HasPreloadData = preloadBytes > 0;
		}

		private byte[] ReadPreloadData() {
			if (PreloadBytes > 0) {
				var buff = new byte[PreloadBytes];
				using (var fs = new FileStream(ParentArchive.ArchivePath, FileMode.Open, FileAccess.Read)) {
					buff = new byte[PreloadBytes];
					fs.Seek((long)PreloadDataOffset, SeekOrigin.Begin);
					fs.Read(buff, 0, buff.Length);
				}
				return buff;
			}
			return null;
		}

		private byte[]? dataCache;
		private byte[] ReadData() {
			if (dataCache != null)
				return dataCache;

			var partFile = ParentArchive.Parts.FirstOrDefault(part => part.Index == ArchiveIndex);
			if (partFile == null)
				throw new Exception("Part file was null!");

			if (HasPreloadData) {
				dataCache = new byte[PreloadBytes + EntryLength];
				using (var fs = new FileStream(ParentArchive.ArchivePath, FileMode.Open, FileAccess.Read)) {
					fs.Seek((long)PreloadDataOffset, SeekOrigin.Begin);
					fs.Read(dataCache, 0, PreloadBytes);
				}

				using (var fs = new FileStream(partFile.Filename, FileMode.Open, FileAccess.Read)) {
					fs.Seek(EntryOffset, SeekOrigin.Begin);
					fs.Read(dataCache, PreloadBytes, (int)EntryLength);
				}
			}
			else {
				dataCache = new byte[EntryLength];
				using (var fs = new FileStream(partFile.Filename, FileMode.Open, FileAccess.Read)) {
					fs.Seek(EntryOffset, SeekOrigin.Begin);
					fs.Read(dataCache, 0, dataCache.Length);
				}
			}

			return dataCache;
		}

		public byte[] AnyData { get { if (this.HasPreloadData) return this.ReadPreloadData(); else return this.ReadData(); } }
	}
}
