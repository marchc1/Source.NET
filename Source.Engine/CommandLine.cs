using Source.Common.Commands;

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Source.Common;

public class CommandLine : ICommandLine
{
	unsafe void ParseCommandLine() {
		CleanUpParms();
		if (cmdLine == null) return;

		fixed (char* pCharFx = cmdLine) {
			char* pChar = pCharFx;

			while (*pChar > 0 && char.IsWhiteSpace(*pChar))
				++pChar;

			bool inQuotes = false;
			char* firstLetter = null;
			for (; *pChar > 0; ++pChar) {
				if (inQuotes) {
					if (*pChar != '\"')
						continue;

					AddArgument(firstLetter, pChar);
					firstLetter = null;
					inQuotes = false;
					continue;
				}

				if (firstLetter == null) {
					if (*pChar == '\"') {
						inQuotes = true;
						firstLetter = pChar + 1;
						continue;
					}

					if (char.IsWhiteSpace(*pChar))
						continue;

					firstLetter = pChar;
					continue;
				}

				if (char.IsWhiteSpace(*pChar)) {
					AddArgument(firstLetter, pChar);
					firstLetter = null;
				}
			}

			if (firstLetter != null)
				AddArgument(firstLetter, pChar);
		}
	}
	void CleanUpParms() {
		parms.Clear();
	}
	unsafe void AddArgument(char* first, char* last) {
		if (last <= first)
			return;

		nint len = (nint)(last - first);
		parms.Add(new string(new Span<char>(first, (int)len)));
	}

	bool IsInvalidIndex(int index) => index == 0 || index == parms.Count - 1;
	bool IsLikelyCmdLineParameter(int index) {
		char c = parms[index][0];
		return c == '-' || c == '+';
	}

	string? cmdLine;
	List<string> parms = [];


	public CommandLine() { }
	public CommandLine(string cmdline) => CreateCmdLine(cmdline);

	public void CreateCmdLine(IEnumerable<string> commandLine) {
		Span<char> cmdline = stackalloc char[2048];
		Span<char> dest = cmdline;
		nint size = cmdline.Length;
		string space = "";
		foreach (var arg in commandLine) {
			Dbg.Assert(space.Length + arg.Length + 2 + 1 <= size);

			string inserted = string.Empty;

			if (size > 0) {
				inserted = $"{space}\"{arg}\"";
				inserted.AsSpan().CopyTo(dest);
			}
			int len = inserted.Length;
			size -= len;
			dest = dest[len..];
			space = " ";
		}

		CreateCmdLine(cmdLine);
	}

	public unsafe void CreateCmdLine(ReadOnlySpan<char> commandLine) {
		const int MAX_BUFFER_LEN = 4096;
		char* full = stackalloc char[MAX_BUFFER_LEN];
		full[0] = '\0';

		char* dst = full;
		fixed (char* pCommandLine = commandLine) {
			char* src = pCommandLine;
			bool inQuotes = false;
			char* inQuotesStart = null;
			while (*src > 0) {
				if (*src == '"') {
					if (src == pCommandLine || (src[-1] != '/' && src[-1] != '\\')) {
						inQuotes = !inQuotes;
						inQuotesStart = src + 1;
					}
				}

				if (*src == '*') {
					if (src == pCommandLine || (inQuotes && char.IsWhiteSpace(src[-1])) || (inQuotes && src == inQuotesStart)) {
						LoadParametersFromFile(src, dst, MAX_BUFFER_LEN - ((nint)dst - (nint)full), inQuotes);
						continue;
					}
				}

				if ((dst - full) >= (MAX_BUFFER_LEN - ((nint)dst - (nint)full) - 1))
					break;

				*dst++ = *src++;
			}

			*dst = '\0';
			string managed = new string(full);
			cmdLine = managed;
			ParseCommandLine();
		}
	}

	private unsafe void LoadParametersFromFile(char* src, char* dst, nint v, bool inQuotes) {
		throw new NotImplementedException();
	}

	public void AppendParm(string name, string? values = null) {
		throw new NotImplementedException();
	}

	public class CommandLineParmValueEnumerable(CommandLine cmd, int index) : IEnumerable<string>
	{
		public IEnumerator<string> GetEnumerator() {
			for (int i = index + 1; i < cmd.ParmCount(); i++) {
				string? value = cmd.ParmValueByIndex(i);
				if (value == null)
					yield break;
				yield return value;
			}
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}
	}

	public bool CheckParm(string name, out IEnumerable<string> values) {
		values = Enumerable.Empty<string>();

		int i = FindParm(name);
		if (i == 0)
			return false;

		values = new CommandLineParmValueEnumerable(this, i);
		return true;
	}



	public int FindParm(ReadOnlySpan<char> name) {
		for (int i = 1; i < parms.Count; i++) {
			if (name.Equals(parms[i], StringComparison.InvariantCultureIgnoreCase))
				return i;
		}

		return 0;
	}

	public string? GetCmdLine() => cmdLine;

	public string GetParm(int index) {
		if (IsInvalidIndex(index))
			return "";
		return parms[index];
	}

	public int ParmCount() => parms.Count;


	[return: NotNullIfNotNull("defaultValue")]
	public string? ParmValue(string name, string? defaultValue = null) {
		int index = FindParm(name);
		if (IsInvalidIndex(index))
			return defaultValue;

		if (IsLikelyCmdLineParameter(index + 1))
			return defaultValue;

		return parms[index + 1];
	}

	public int ParmValue(string name, int defaultValue) => int.TryParse(ParmValue(name), out int result) ? result : defaultValue;
	public float ParmValue(string name, float defaultValue) => float.TryParse(ParmValue(name), out float result) ? result : defaultValue;
	public double ParmValue(string name, double defaultValue) => double.TryParse(ParmValue(name), out double result) ? result : defaultValue;


	[return: NotNullIfNotNull("defaultValue")]
	public string? ParmValueByIndex(int index, string? defaultValue = null) {
		if (IsInvalidIndex(index))
			return defaultValue;

		if (IsLikelyCmdLineParameter(index + 1))
			return defaultValue;

		return parms[index + 1];
	}


	static int StrLen(char[] buffer, int start) {
		int len = 0;
		while (buffer[start + len] != '\0')
			len++;
		return len;
	}

	static int StrIStr(char[] buffer, int start, string needle) {
		int haystackLen = StrLen(buffer, start);
		if (needle.Length > haystackLen)
			return -1;

		for (int i = 0; i <= haystackLen - needle.Length; i++) {
			bool match = true;
			for (int j = 0; j < needle.Length; j++) {
				if (char.ToLowerInvariant(buffer[start + i + j]) != char.ToLowerInvariant(needle[j])) {
					match = false;
					break;
				}
			}
			if (match)
				return start + i;
		}

		return -1;
	}

	public void RemoveParm(string name) {
		if (cmdLine == null)
			return;

		int nParmLen = name.Length;

		char[] buffer = new char[cmdLine.Length + 1];
		cmdLine.AsSpan().CopyTo(buffer);
		buffer[cmdLine.Length] = '\0';

		int p = 0;
		while (buffer[p] != '\0') {
			int curlen = StrLen(buffer, p);

			int found = StrIStr(buffer, p, name);
			if (found == -1)
				break;

			int nextparam = found + 1;
			bool hadQuote = false;
			if (found > 0 && buffer[found - 1] == '"')
				hadQuote = true;

			while (buffer[nextparam] != '\0' && buffer[nextparam] != ' ' && buffer[nextparam] != '"')
				nextparam++;

			if ((nextparam - found) > nParmLen) {
				p = nextparam;
				continue;
			}

			while (buffer[nextparam] != '\0' && buffer[nextparam] != '-' && buffer[nextparam] != '+')
				nextparam++;

			if (hadQuote)
				found--;

			if (buffer[nextparam] != '\0') {
				int n = curlen - (nextparam - p);
				new Span<char>(buffer, nextparam, n).CopyTo(new Span<char>(buffer, found, n));
				buffer[found + n] = '\0';
			}
			else {
				int n = nextparam - found;
				new Span<char>(buffer, found, n).Clear();
			}
		}

		int len = StrLen(buffer, 0);
		while (len > 0 && buffer[len - 1] == ' ')
			len--;

		cmdLine = new string(buffer, 0, len);
		ParseCommandLine();
	}

	public void SetParm(int index, string newParm) {
		Dbg.Assert(index >= 0 && index < parms.Count);
		if (index >= 0 && index < parms.Count)
			parms[index] = newParm;
	}
}
