namespace Bootil.String;

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
