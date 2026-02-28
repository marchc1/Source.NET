namespace Game.Shared;

public class EntityMapData // fixme, why so string heavy
{
	public const int MAPKEY_MAXLENGTH = 2048;
	string EntData;
	int EntDataSize;
	string? CurrentKey;

	public EntityMapData(ReadOnlySpan<char> entBlock, int entBlockSize = -1) {
		EntData = entBlock.ToString();
		EntDataSize = entBlockSize;
	}

	public bool ExtractValue(ReadOnlySpan<char> keyName, Span<char> value) => MapEntity.ExtractValue(EntData, keyName, value);

	public bool GetFirstKey(ReadOnlySpan<char> keyName, ReadOnlySpan<char> value) {
		CurrentKey = EntData;
		return GetNextKey(keyName, value);
	}

	public ReadOnlySpan<char> CurrentBufferPosition() => CurrentKey;

	public bool GetNextKey(ReadOnlySpan<char> keyName, ReadOnlySpan<char> value) {
		Span<char> token = stackalloc char[MAPKEY_MAXLENGTH];

		string prevKey = CurrentKey;
		CurrentKey = MapEntity.ParseToken(CurrentKey, token);
		if (token.Length > 0 && token[0] == '}') {
			CurrentKey = prevKey;
			return false;
		}

		if (string.IsNullOrEmpty(CurrentKey)) {
			Warning("EntityMapData::GetNextKey: EOF without closing brace\n");
			Assert(false);
			return false;
		}

		keyName.CopyTo(token);

		int n = keyName.Length - 1;
		while (n >= 0 && keyName[n] == ' ')
			n--;

		if (n >= 0)
			keyName = keyName[..(n + 1)];

		CurrentKey = MapEntity.ParseToken(CurrentKey, token);
		if (string.IsNullOrEmpty(CurrentKey)) {
			Warning("EntityMapData::GetNextKey: EOF without closing brace\n");
			Assert(false);
			return false;
		}

		if (token.Length > 0 && token[0] == '}') {
			Warning("EntityMapData::GetNextKey: closing brace without data\n");
			Assert(false);
			return false;
		}

		value.CopyTo(token);

		return true;
	}

	bool SetValue(ReadOnlySpan<char> keyName, ReadOnlySpan<char> NewValue, int nKeyInstance) {
		if (EntDataSize == -1) {
			Assert(false);
			return false;
		}

		Span<char> token = stackalloc char[MAPKEY_MAXLENGTH];
		string? inputData = EntData;
		string? prevData;

		char[] newvaluebuf = new char[1024];
		int nCurrKeyInstance = 0;

		while (!string.IsNullOrEmpty(inputData)) {
			inputData = MapEntity.ParseToken(inputData, token);
			if (token.Length > 0 && token[0] == '}')
				break;

			if (token.SequenceEqual(keyName)) {
				nCurrKeyInstance++;
				if (nCurrKeyInstance > nKeyInstance) {
					int entLen = EntData.Length;
					char[] postData = new char[entLen];
					prevData = inputData;
					inputData = MapEntity.ParseToken(inputData, token);
					token.CopyTo(postData);

					if (NewValue.Length > 0 && NewValue[0] != '\"')
						newvaluebuf = $"\"{NewValue}\"".ToCharArray();
					else
						NewValue.CopyTo(newvaluebuf);

					int iNewValueLen = newvaluebuf.Length;
					int iPadding = iNewValueLen - token.Length - 2;

					Array.Copy(newvaluebuf, 0, prevData.ToCharArray(), 1, iNewValueLen + 1);
					Array.Copy(postData, 0, prevData.ToCharArray(), 1 + iNewValueLen, entLen - ((prevData.Length - inputData.Length) + 1));

					CurrentKey = CurrentKey[(iPadding)..];
					return true;
				}
			}

			inputData = MapEntity.ParseToken(inputData, token);
		}

		return false;
	}
}

public static class MapEntity
{
	public static ReadOnlySpan<char> SkipToNextEntity(ReadOnlySpan<char> mapData, Span<char> workBuffer) {
		if (mapData.IsEmpty)
			return null;

		int openBraceCount = 1;
		while (!mapData.IsEmpty) {
			mapData = ParseToken(mapData, workBuffer);

			if (workBuffer.Length > 0 && workBuffer[0] == '{')
				openBraceCount++;
			else if (workBuffer.Length > 0 && workBuffer[0] == '}') {
				openBraceCount--;
				if (openBraceCount == 0)
					return mapData;
			}
		}

		return null;
	}

	static readonly char[] s_BraceChars = "{}()\'".ToCharArray();
	static readonly bool[] s_BraceCharacters = new bool[256];
	static bool s_BuildReverseMap = true;

	public static string? ParseToken(ReadOnlySpan<char> data, Span<char> newToken) {
		int len = 0;
		newToken[0] = '\0';

		if (data == default || data.IsEmpty)
			return null;

		if (s_BuildReverseMap) {
			s_BuildReverseMap = false;
			Array.Clear(s_BraceCharacters, 0, s_BraceCharacters.Length);
			foreach (var chh in s_BraceChars)
				s_BraceCharacters[(byte)chh] = true;
		}

	skipwhite:
		while (true) {
			if (data.IsEmpty)
				return null;

			int c = data[0];
			if (c > ' ')
				break;

			if (c == 0)
				return null;

			data = data[1..];
		}

		int ch = data[0];

		if (ch == '/' && data.Length > 1 && data[1] == '/') {
			while (!data.IsEmpty && data[0] != '\n')
				data = data[1..];
			goto skipwhite;
		}

		if (ch == '"') {
			data = data[1..];

			while (len < EntityMapData.MAPKEY_MAXLENGTH) {
				if (data.IsEmpty)
					break;

				ch = data[0];
				data = data[1..];

				if (ch == '"' || ch == 0) {
					newToken[len] = '\0';
					return data.ToString();
				}

				newToken[len++] = (char)ch;
			}

			if (len >= EntityMapData.MAPKEY_MAXLENGTH) {
				len--;
				newToken[len] = '\0';
			}

			newToken[len] = '\0';
			return data.ToString();
		}

		if (ch < 256 && s_BraceCharacters[ch]) {
			newToken[len++] = (char)ch;
			newToken[len] = '\0';
			return data[1..].ToString();
		}

		do {
			newToken[len++] = (char)ch;
			data = data[1..];

			if (data.IsEmpty)
				break;

			ch = data[0];

			if (ch < 256 && s_BraceCharacters[ch])
				break;

			if (len >= EntityMapData.MAPKEY_MAXLENGTH) {
				len--;
				newToken[len] = '\0';
			}

		} while (ch > 32);

		newToken[len] = '\0';
		return data.ToString();
	}

	public static bool ExtractValue(ReadOnlySpan<char> entData, ReadOnlySpan<char> keyName, Span<char> value) {
		Span<char> token = stackalloc char[EntityMapData.MAPKEY_MAXLENGTH];
		ReadOnlySpan<char> inputData = entData;

		while (!inputData.IsEmpty) {
			var remainder = ParseToken(inputData, token);
			if (remainder == null)
				break;

			inputData = remainder.AsSpan();

			if (token[0] == '}')
				break;

			if (SequenceEquals(token, keyName)) {
				remainder = ParseToken(inputData, token);
				if (remainder == null)
					return false;

				inputData = remainder.AsSpan();
				int tokenLen = token.IndexOf('\0');
				if (tokenLen < 0) tokenLen = token.Length;
				value.Clear();
				token[..tokenLen].CopyTo(value);
				return true;
			}

			remainder = ParseToken(inputData, token);
			if (remainder == null)
				break;

			inputData = remainder.AsSpan();
		}

		return false;
	}

	static bool SequenceEquals(Span<char> token, ReadOnlySpan<char> key) {
		int len = token.IndexOf('\0');
		if (len < 0) len = token.Length;
		return token[..len].SequenceEqual(key);
	}
}