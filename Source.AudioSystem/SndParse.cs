namespace Source.AudioSystem;

public static class SndParse
{
	static readonly string BreakSet = "{}()'";
	static readonly string BreakSetIncludingColons = "{}()':";

	public static bool com_ignorecolons = false;

	/*
	==============
	COM_Parse

	Parse a token out of a string
	==============
	*/
	public static ReadOnlySpan<char> COM_Parse(ReadOnlySpan<char> data, scoped Span<char> com_token) {
		char c;
		int len;
		string breaks;

		breaks = BreakSetIncludingColons;
		if (com_ignorecolons)
			breaks = BreakSet;

		len = 0;
		com_token[0] = '\0';

		if (data.IsEmpty)
			return null;

	// skip whitespace
	skipwhite:
		while ((c = data.Length > 0 ? data[0] : '\0') <= ' ') {
			if (c == 0)
				return null;                    // end of file;
			data = data[1..];
		}

		// skip // comments
		if (c == '/' && data.Length > 1 && data[1] == '/') {
			while (!data.IsEmpty && data[0] != '\0' && data[0] != '\n')
				data = data[1..];
			goto skipwhite;
		}


		// handle quoted strings specially
		if (c == '\"') {
			data = data[1..];
			while (true) {
				c = data.Length > 0 ? data[0] : '\0';
				if (!data.IsEmpty)
					data = data[1..];
				if (c == '\"' || c == 0) {
					com_token[len] = '\0';
					return data;
				}
				com_token[len] = c;
				len++;
			}
		}

		// parse single characters
		if (breaks.Contains(c)) {
			com_token[len] = c;
			len++;
			com_token[len] = '\0';
			return data[1..];
		}

		// parse a regular word
		do {
			com_token[len] = c;
			data = data[1..];
			len++;
			c = data.Length > 0 ? data[0] : '\0';
			if (breaks.Contains(c))
				break;
		} while (c > 32);

		com_token[len] = '\0';
		return data;
	}
}
