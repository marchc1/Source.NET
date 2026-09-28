using Source.Common.Filesystem;

using System.Text;

namespace Source.Filesystem.GarrysMod;

public class AddonReader(ulong timestamp)
{
	const int BufferSize = 10485760;
	const int MinAddonSize = 37;
	const ulong MaxFileSize = 2000000000;

	static readonly byte[] Buffer = new byte[BufferSize];
	static int BufferWritten;
	static int BufferPos;

	string FileName = "";
	readonly List<AddonFormat.FileEntry> Files = [];
	long ContentsOffset;
	long AddonSize;
	ulong SteamID;
	ulong Timestamp;
	string Title = "";
	string Author = "";
	string Description = "";
	int AddonVersion;
	int GMAVersion;
	bool BadAddon;
	readonly ulong WhitelistTimestamp = timestamp;

	public string GetTitle() => Title;

	public bool OpenFile(string fileName) {
		FileName = fileName;

		IFileHandle? file = g_FullFileSystem.Open(FileName, FileOpenOptions.Read | FileOpenOptions.Binary, "MOD");
		if (file == null) {
			file = g_FullFileSystem.Open(FileName, FileOpenOptions.Read | FileOpenOptions.Binary, "GAME");
			if (file == null) {
				Warning($"Not loading addon '{FileName}' - file doesn't exist\n");
				return false;
			}
		}

		using (file) {
			AddonSize = (uint)file.Stream.Length;
			if (AddonSize == 0) {
				Warning($"Not loading addon '{FileName}' - file is empty?\n");
				return false;
			}

			if (AddonSize < MinAddonSize) {
				Warning($"Not loading addon '{FileName}' - file is too small?\n");
				return false;
			}

			BufferWritten = (int)Math.Min(AddonSize, Buffer.Length);
			BufferPos = 0;
			file.Stream.ReadAtLeast(Buffer.AsSpan(0, BufferWritten), BufferWritten, false);

			ReadOnlySpan<byte> header = Read(5);
			if (header[0] == 'G' && header[1] == 'M' && header[2] == 'A' && header[3] == 'D') {
				GMAVersion = (sbyte)header[4];
				return ReadAddonFile(file);
			}

			Warning($"Not loading addon '{FileName}' - addon header invalid\n");
			return false;
		}
	}

	bool ReadAddonFile(IFileHandle file) {
		if (BufferPos + 16 >= BufferWritten)
			return false;

		SteamID = BitConverter.ToUInt64(Read(8));
		Timestamp = BitConverter.ToUInt64(Read(8));

		if (GMAVersion >= 2 && !ReadRequiredContent(file)) {
			Warning($"Not loading addon '{FileName}' - couldn't read required addons!\n");
			return false;
		}

		if (BufferWritten <= BufferPos + 1)
			return false;

		Title = ReadString();
		if (BufferPos + 1 >= BufferWritten)
			return false;

		Description = ReadString();
		if (BufferPos + 1 >= BufferWritten)
			return false;

		Author = ReadString();
		if (BufferPos + 4 >= BufferWritten)
			return false;

		AddonVersion = BitConverter.ToInt32(Read(4));

		while (ReadFileEntry()) { }

		if (BadAddon)
			return false;

		ContentsOffset = BufferPos;
		long offset = ContentsOffset;
		for (int i = 0; i < Files.Count; i++) {
			AddonFormat.FileEntry entry = Files[i];
			entry.Offset = offset;
			Files[i] = entry;
			offset += entry.Size;

			if (offset > AddonSize) {
				Warning($"Not loading addon '{Title}' - Malformed .gma file\n");
				BadAddon = true;
				return false;
			}

			if (offset > uint.MaxValue) {
				Warning($"Not loading addon '{Title}' - file size too large (above 4GB)\n");
				BadAddon = true;
				return false;
			}
		}

		return true;
	}

	bool ReadRequiredContent(IFileHandle file) {
		string content = ReadString();
		while (true) {
			if (content.Length == 0)
				return true;

			if (BufferWritten <= BufferPos)
				return false;

			StringBuilder builder = new();
			int c;
			while ((c = file.Stream.ReadByte()) > 0)
				builder.Append((char)c);
			content = builder.ToString();
		}
	}

	bool ReadFileEntry() {
		if (BufferWritten <= BufferPos + 4) {
			Warning($"Not loading addon '{Title}', failed to parse addon file list!\n");
			BadAddon = true;
			return false;
		}

		uint fileNumber = BitConverter.ToUInt32(Read(4));
		if (fileNumber == 0)
			return false;

		if (BufferWritten <= BufferPos + 1) {
			Warning($"Not loading addon '{Title}', failed to parse addon file list!\n");
			BadAddon = true;
			return false;
		}

		string fileName = ReadString();
		if (BufferPos + 12 >= BufferWritten) {
			Warning($"Not loading addon '{Title}', failed to parse addon file list!\n");
			BadAddon = true;
			return false;
		}

		ulong size = BitConverter.ToUInt64(Read(8));
		uint crc = BitConverter.ToUInt32(Read(4));

		Bootil.String.Lower(ref fileName);
		string error = AddonWhiteList.FilenameErrors(fileName, WhitelistTimestamp);
		if (error.Length != 0) {
			Warning($"Not loading addon '{Title}' - {error}\n");
			BadAddon = true;
			return false;
		}

		Bootil.String.File.FixSlashes(ref fileName);

		if (size > MaxFileSize) {
			Warning($"Not loading addon '{Title}' - {fileName} is too large! ({Bootil.String.Format.Memory(size)})\n");
			BadAddon = true;
			return false;
		}

		Files.Add(new() {
			Name = fileName,
			Size = (long)size,
			CRC = crc,
			FileNumber = fileNumber,
			Offset = 0
		});
		return true;
	}

	static bool ShouldBeExtracted(string fileName) {
		if (fileName.Contains(".."))
			return false;

		if (Bootil.String.Test.Wildcard("resource/fonts/*.ttf", fileName))
			return true;

		return Bootil.String.Test.Wildcard("gamemodes/*/content/resource/fonts/*.ttf", fileName);
	}

	public void ExtractFiles() {
		foreach (AddonFormat.FileEntry entry in Files) {
			string outputPath = "cache/workshop/" + entry.Name;
			if (entry.Name.IndexOf("gamemodes/", StringComparison.Ordinal) == 0) {
				int slash = entry.Name.Length > 10 ? entry.Name.IndexOf('/', 10) : -1;
				if (slash != -1 && entry.Name.IndexOf("/content/", slash, StringComparison.Ordinal) == slash)
					outputPath = "cache/workshop/" + entry.Name[(slash + 9)..];
			}

			if (!ShouldBeExtracted(entry.Name))
				continue;

			if (g_FullFileSystem.Size(entry.Name, "workshop") == g_FullFileSystem.Size(outputPath, "MOD"))
				continue;

			byte[] content;
			using (IFileHandle? file = g_FullFileSystem.Open(entry.Name, FileOpenOptions.Read | FileOpenOptions.Binary, "workshop")) {
				if (file == null) {
					DevWarning($"Couldn't read '{entry.Name}' - what's up with that?\n");
					continue;
				}

				content = new byte[file.Stream.Length];
				file.Stream.ReadExactly(content);
			}

			string folder = outputPath;
			Bootil.String.File.StripFilename(ref folder);
			g_FullFileSystem.CreateDirHierarchy(folder, "MOD");

			if (g_FullFileSystem.WriteFile(outputPath, "MOD", content))
				Msg($"Extracted '{entry.Name}' from workshop addon\n");
			else
				DevWarning($"Couldn't write '{outputPath}' - already in use?\n");
		}
	}

	public IFileHandle? GetPackFile() => g_FullFileSystem.Open(FileName, FileOpenOptions.Read | FileOpenOptions.Binary, "MOD");

	public int GetNumFiles() => Files.Count;

	public ref readonly AddonFormat.FileEntry GetFile(int fileID) => ref System.Runtime.InteropServices.CollectionsMarshal.AsSpan(Files)[fileID];

	ReadOnlySpan<byte> Read(int count) {
		ReadOnlySpan<byte> data = Buffer.AsSpan(BufferPos, Math.Min(count, BufferWritten - BufferPos));
		BufferPos += data.Length;
		return data;
	}

	string ReadString() {
		int start = BufferPos;
		while (BufferPos < BufferWritten && Buffer[BufferPos] != 0)
			BufferPos++;

		string str = Encoding.UTF8.GetString(Buffer, start, BufferPos - start);
		if (BufferPos < BufferWritten)
			BufferPos++;
		return str;
	}
}
