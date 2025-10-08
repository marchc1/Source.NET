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
		return HashToSymbol.TryGetValue(value.Hash(), out ulong index) ? index : 0;
	}

	public ReadOnlySpan<char> GetValueByIndex(ulong hash) {
		return Lookup.TryGetValue(hash, out string? value) ? value : null;
	}

	public ReadOnlySpan<char> Find(ReadOnlySpan<char> text) {
		ulong index = FindIndex(text);
		if (index == 0)
			return null;
		return GetValueByIndex(index);
	}
}
