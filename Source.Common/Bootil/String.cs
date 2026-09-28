namespace Bootil;

public static class String
{
	public static void Lower(ref string str) => str = str.ToLowerInvariant();

	public static class Format
	{
		public static string Memory(ulong bytes) {
			float mb = bytes / 1024.0f / 1024.0f;

			if (mb / 1024.0f >= 1.0)
				return FormattableString.Invariant($"{mb / 1024.0f:0.0} GB");

			if (mb >= 1.0)
				return FormattableString.Invariant($"{mb:0.0} MB");

			float kb = bytes / 1024.0f;
			if (kb >= 1.0)
				return FormattableString.Invariant($"{kb:0.0} KB");

			return $"{bytes} B";
		}
	}

	public static class To
	{
		public static ulong UInt64(ReadOnlySpan<char> str) {
			int i = 0;
			while (i < str.Length && char.IsWhiteSpace(str[i]))
				i++;

			if (i < str.Length && str[i] == '+')
				i++;

			ulong value = 0;
			while (i < str.Length && str[i] >= '0' && str[i] <= '9') {
				value = value * 10 + (ulong)(str[i] - '0');
				i++;
			}

			return value;
		}
	}

	public static class Test
	{
		public static bool StartsWith(string str, string strFind) => str.StartsWith(strFind, StringComparison.Ordinal);

		public static bool EndsWith(string str, string strFind) {
			int i = str.LastIndexOf(strFind, StringComparison.Ordinal);
			return i != -1 && i == str.Length - strFind.Length;
		}

		public static bool Wildcard(string strWildcard, string strHaystack) {
			int w = 0, s = 0;
			int mp = -1, cp = -1;

			while (s < strHaystack.Length && (w >= strWildcard.Length || strWildcard[w] != '*')) {
				if (w >= strWildcard.Length || (strWildcard[w] != strHaystack[s] && strWildcard[w] != '?'))
					return false;
				w++;
				s++;
			}

			while (s < strHaystack.Length) {
				if (w < strWildcard.Length && strWildcard[w] == '*') {
					if (++w >= strWildcard.Length)
						return true;
					mp = w;
					cp = s + 1;
				}
				else if (w < strWildcard.Length && (strWildcard[w] == strHaystack[s] || strWildcard[w] == '?')) {
					w++;
					s++;
				}
				else {
					if (mp == -1)
						return false;
					w = mp;
					s = cp++;
				}
			}

			while (w < strWildcard.Length && strWildcard[w] == '*')
				w++;

			return w >= strWildcard.Length;
		}
	}

	public static class File
	{
		public static void CleanPath(ref string strIn) {
			FixSlashes(ref strIn);
			Util.Trim(ref strIn, "/");
			Util.FindAndReplace(ref strIn, "//", "/");
		}

		public static void FixSlashes(ref string strIn, string strFrom = "\\", string strTo = "/") => Util.FindAndReplace(ref strIn, strFrom, strTo);

		public static void ExtractFilename(ref string str) {
			int i = str.LastIndexOf('/');
			if (i == -1)
				i = str.LastIndexOf('\\');
			if (i == -1)
				return;

			str = str[(i + 1)..];
		}

		public static void StripFilename(ref string str) {
			int i = str.LastIndexOf('/');
			if (i == -1)
				i = str.LastIndexOf('\\');
			if (i == -1) {
				str = "";
				return;
			}

			str = str[..(i + 1)];
		}

		public static string GetFileExtension(string str) {
			int i = str.LastIndexOf('.');
			if (i == -1)
				return "";

			return str[(i + 1)..];
		}

		public static void StripExtension(ref string str) {
			int i = str.LastIndexOf('.');
			if (i == -1)
				return;

			str = str[..i];
		}
	}

	public static class Util
	{
		public static void FindAndReplace(ref string strIn, string strFind, string strReplace) {
			if (strFind.Length == 0)
				return;

			int pos = 0;
			while ((pos = strIn.IndexOf(strFind, pos, StringComparison.Ordinal)) != -1) {
				strIn = strIn[..pos] + strReplace + strIn[(pos + strFind.Length)..];
				pos += strReplace.Length;
			}
		}

		public static void TrimRight(ref string str, string strChars) => str = str.TrimEnd(strChars.ToCharArray());
		public static void TrimLeft(ref string str, string strChars) => str = str.TrimStart(strChars.ToCharArray());

		public static void Trim(ref string str, string strChars = " \n\t\r") {
			TrimLeft(ref str, strChars);
			TrimRight(ref str, strChars);
		}

		public static int Count(string str, char chrFind) {
			int count = 0;
			foreach (char c in str)
				if (c == chrFind)
					count++;
			return count;
		}
	}
}
