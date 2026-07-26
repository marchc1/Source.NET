namespace Source.Common.GarrysMod;

public static class GMOD
{
	public static bool IsValidPath(ReadOnlySpan<char> inputPath) {
		if (inputPath.IsEmpty)
			return false;

		char prev = '\0';
		for (int i = 0; i < inputPath.Length; i++) {
			char c = inputPath[i];
			switch (c) {
				case '\0':
				case '\n':
				case '\r':
				case '\t':
				case ':':
				case '?':
				case '|':
				case '>':
				case '<':
				case '"':
					return false;
				case '\\':
					if (prev == '\\') return false;
					break;
				case '.':
					if (prev == '.') return false;
					break;
			}
			prev = c;
		}
		return true;
	}
}
