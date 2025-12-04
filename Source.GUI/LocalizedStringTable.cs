using CommunityToolkit.HighPerformance;

using Source.Common;
using Source.Common.Filesystem;
using Source.Common.Formats.Keyvalues;
using Source.Common.Launcher;

namespace Source.GUI;

public struct LocalizationFileInfo
{
	internal string SymName;
	internal string SymPathID;
	internal bool IncludeFallbacks;
}

public class LocalizedStringTable(ISystem system, IFileSystem fileSystem) : ILocalize
{
	bool UseOnlyLongestLanguageString;
	string? Language;

	public readonly List<LocalizationFileInfo> LocalizationFiles = [];
	ulong curSymbol;
	public readonly Dictionary<ulong, ulong> HashToSymbol = [];
	public readonly Dictionary<ulong, string> Lookup = [];

	public bool AddFile(ReadOnlySpan<char> file, ReadOnlySpan<char> pathID = default, bool includeFallbackSearchPaths = false) {
		const string LANGUAGE_STRING = "%language%";
		const string ENGLISH_STRING = "english";
		const int MAX_LANGUAGE_NAME_LENGTH = 64;

		Span<char> language = stackalloc char[MAX_LANGUAGE_NAME_LENGTH];
		Span<char> fileName = stackalloc char[MAX_PATH];

		int langptr = file.IndexOf(LANGUAGE_STRING);
		if (langptr != -1) {
			if (system.CommandLineParamExists("-all_languages")) {
				ReadOnlySpan<char> fileBase = file[..langptr];
				UseOnlyLongestLanguageString = true;
				return AddAllLanguageFiles(fileBase);
			}
			bool success;

			string fileName2 = new string(file).Replace(LANGUAGE_STRING, ENGLISH_STRING);
			success = AddFile(fileName2, pathID, includeFallbackSearchPaths);

			system.GetUILanguage(language);

			if (language.IndexOfAnyExcept('\0') != -1 && ((ReadOnlySpan<char>)language).Equals(ENGLISH_STRING, StringComparison.OrdinalIgnoreCase)) {
				string fileName3 = new string(file).Replace(LANGUAGE_STRING, new(language));
				success &= AddFile(fileName3, pathID, includeFallbackSearchPaths);
			}

			return success;
		}

		LocalizationFileInfo search;
		search.SymName = new string(fileName);
		search.SymPathID = !pathID.IsEmpty ? new string(pathID) : "";
		search.IncludeFallbacks = includeFallbackSearchPaths;

		Span<LocalizationFileInfo> localizationFiles = LocalizationFiles.AsSpan();
		int lfc = localizationFiles.Length;
		for (int lf = 0; lf < lfc; ++lf) {
			ref LocalizationFileInfo entry = ref localizationFiles[lf];
			if (entry.SymName.Hash() == fileName.Hash()) {
				LocalizationFiles.RemoveAt(lf);
				break;
			}
		}

		LocalizationFiles.Add(search);

		KeyValues kvs = new();
		kvs.UsesConditionals(true);
		kvs.UsesEscapeSequences(true);
		if (kvs.LoadFromFile(fileSystem, file, pathID)) {
			var tokens = kvs.FindKey("Tokens", true)!;
			foreach (var token in tokens) {
				if (token == null)
					continue;

				// Hash the incoming string.
				ReadOnlySpan<char> incomingName = token.Name;
				ulong nameHash = incomingName.Hash();
				// Check if we've produced a hash for this before.
				if (!HashToSymbol.TryGetValue(nameHash, out ulong symbol)) {
					// and if not, produce a new symbol
					symbol = HashToSymbol[nameHash] = ++curSymbol;
				}

				// Write this symbol
				Lookup[symbol] = new string(token.GetString());
			}
			return true;
		}
		else {
			AssertMsg(false, "Bad localization file?");
			return false;
		}

	}

	private bool AddAllLanguageFiles(ReadOnlySpan<char> fileBase) {
		throw new NotImplementedException();
	}

	public ulong FindIndex(ReadOnlySpan<char> value) {
		value = value.SliceNullTerminatedString();

		return HashToSymbol.TryGetValue(value.Hash(), out ulong index) ? index : 0;
	}

	public ReadOnlySpan<char> GetValueByIndex(ulong hash) {
		return Lookup.TryGetValue(hash, out string? value) ? value : null;
	}

	public ReadOnlySpan<char> Find(ReadOnlySpan<char> text) {
		text = text.SliceNullTerminatedString();

		if (text.Length > 0 && text[0] == '#')
			text = text[1..];
		ulong index = FindIndex(text);
		if (index == 0)
			return null;
		return GetValueByIndex(index);
	}
	public ReadOnlySpan<char> TryFind(ReadOnlySpan<char> text) {
		text = text.SliceNullTerminatedString();

		if (text.Length > 0 && text[0] == '#')
			text = text[1..];
		ulong index = FindIndex(text);
		if (index == 0)
			return text;
		return GetValueByIndex(index);
	}

	// Untested...
	public void ConstructString(Span<char> localized, ReadOnlySpan<char> format, ReadOnlySpan<char> s1, ReadOnlySpan<char> s2, ReadOnlySpan<char> s3, ReadOnlySpan<char> s4, ReadOnlySpan<char> s5, ReadOnlySpan<char> s6, ReadOnlySpan<char> s7, ReadOnlySpan<char> s8) {
		const int FORMAT_NOT_MET = 0;
		const int FORMAT_AWAITING_NEXT = 1;
		const int FORMAT_AWAITING_STRNUM = 2;

		int writePtr = 0;
		int readPtr = 0;

		int readStrIdx = 0;

		int enteringFormat = FORMAT_NOT_MET;
		while (writePtr < localized.Length) {
			char c;
			if (readPtr >= format.Length) {
				if (enteringFormat == FORMAT_AWAITING_STRNUM)
					c = '\0';
				else
					break;
			}
			else {
				c = format[readPtr++];
			}

			switch (enteringFormat) {
				case FORMAT_NOT_MET:
					if (c == '%')
						enteringFormat = FORMAT_AWAITING_NEXT;
					else
						localized[writePtr++] = c;
					break;
				case FORMAT_AWAITING_NEXT:
					if (c == '%') {
						localized[writePtr++] = c;
						if (writePtr < localized.Length)
							localized[writePtr++] = c;
						enteringFormat = FORMAT_NOT_MET;
					}
					else if (c == 's') {
						enteringFormat = FORMAT_AWAITING_STRNUM;
						readStrIdx = 0;
					}
					break;
				case FORMAT_AWAITING_STRNUM:
					if (char.IsDigit(c)) {
						if (readStrIdx != 0)
							readStrIdx = readStrIdx * 10;

						readStrIdx += c - '0';
					}
					else {
						if (readStrIdx != 0) {
							ReadOnlySpan<char> t;
							switch (readStrIdx) {
								case 1: t = s1; break;
								case 2: t = s2; break;
								case 3: t = s3; break;
								case 4: t = s4; break;
								case 5: t = s5; break;
								case 6: t = s6; break;
								case 7: t = s7; break;
								case 8: t = s8; break;
								default: t = ""; break;
							}
							int len = t.ClampedCopyTo(localized[writePtr..]);
							// Go back! We need that character!
							writePtr += len;
							if (len != 0)
								readPtr = readPtr - 1;
						}
						enteringFormat = FORMAT_NOT_MET;
					}
					break;
			}

			if (c == '\0')
				break;
		}
	}
}
