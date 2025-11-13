using Source.Common.Filesystem;

using System.Collections;
using System.Diagnostics;
using System.Globalization;

namespace Source.Common.Formats.Keyvalues;


[DebuggerDisplay("Name = {Name}, Type = {Type}, Count = {Count}, Value = {Value}")]
public class KeyValues : IEnumerable<KeyValues>
{
	public enum Types
	{
		None = 0,
		String,
		Int,
		Double,
		Pointer,
		Color,
		Uint64,
	}

	public void Clear() {
		foreach (var child in children)
			child.node = null!; // << Intentionally break children
		children.Clear();
		Type = Types.None;
	}

	public string Name = "";
	public Types Type;
	public object? Value;

	bool useEscapeSequences = false;
	bool evaluateConditionals = false;

	LinkedListNode<KeyValues> node;
	LinkedList<KeyValues> children = [];

	public KeyValues() {
		node = new(this);
	}

	public KeyValues(ReadOnlySpan<char> name) : base() {
		node = new(this);
		Name = new(name);
	}

	public KeyValues(ReadOnlySpan<char> name, int value) : base() {
		node = new(this);
		Name = new(name);

		SetInt(value);
	}
	public KeyValues(ReadOnlySpan<char> name, object? instance) : base() {
		node = new(this);
		Name = new(name);

		SetPtr(instance);
	}

	public KeyValues(ReadOnlySpan<char> name, ReadOnlySpan<char> firstKey, ReadOnlySpan<char> firstValue) : base() {
		node = new(this);
		Name = new(name);

		SetString(firstKey, firstValue);
	}

	public KeyValues(ReadOnlySpan<char> name, ReadOnlySpan<char> firstKey, int firstValue) : base()
	{
		node = new(this);
		Name = new(name);

		SetInt(firstKey, firstValue);
	}

	public KeyValues(ReadOnlySpan<char> name, ReadOnlySpan<char> firstKey, int firstValue, ReadOnlySpan<char> secondKey, int secondValue) : base()
	{
		node = new(this);
		Name = new(name);

		SetInt(firstKey, firstValue);
		SetInt(secondKey, secondValue);
	}

	public bool LoadFromStream(Stream? stream) {
		// Clear();
		if (stream == null) return false;

		using StreamReader reader = new StreamReader(stream);
		return ReadKV(reader);
	}

	// Returns true if we did anything at all to skip whitespace.
	private bool SkipWhitespace(StreamReader reader) {
		bool didAnything = false;
		while (true) {
			int c = reader.Peek();
			if (c == -1)
				break;
			if (!char.IsWhiteSpace((char)c))
				break;
			reader.Read();
			didAnything = true;
		}

		return didAnything;
	}

	public override string ToString() {
		return $"{Type}<{Value}>";
	}

	// Returns true if we can read something. False if we can't.
	private bool SkipUntilParseableTextOrEOF(StreamReader reader) {
		// We read either
		//    1. A quote mark, in which case we need to read up to a quote
		//    2. Anything else, we read until whitespace

		while (true) {
			if (reader.Peek() == -1)
				return false;
			// If no whitespace was skipped and no comments were skipped, continue
			if (!SkipWhitespace(reader) && !SkipComments(reader))
				return true;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="reader"></param>
	/// <param name="condition"></param>
	/// <param name="match">If [$CONDITION], this value will be true, and if [!$CONDITION], this value will be false.</param>
	/// <returns></returns>
	private bool ReadConditional(StreamReader reader, Span<char> condition, out bool match) {
		// Zero out if it's existing memory
		for (int si = 0; si < condition.Length; si++)
			condition[si] = '\0';

		if (reader.Peek() != '[') {
			match = false;
			return false;
		}

		reader.Read();
		// Determine if we're inverted.
		char c = (char)reader.Read();
		match = true;
		if (c == '!') {
			match = false;
			c = (char)reader.Read();
		}

		if (c != '$') {
			Warning("ReadConditional failed.\n");
			return false;
		}

		int i = -1;
		while (++i < condition.Length) {
			char readIn = (char)reader.Read();
			if (readIn == ']')
				break;
			condition[i] = readIn;
		}
		return true;
	}

	private bool HandleConditional(ReadOnlySpan<char> condition, bool mustMatch) {
		int realStrLength = System.MemoryExtensions.IndexOf(condition, '\0');
		if (realStrLength == -1) {
			Debug.Assert(false, "String overflow!!!");
			return false;
		}
		condition = condition[..realStrLength];

		switch (condition) {
#if WIN32
			case "WIN32": return mustMatch;
			case "WINDOWS": return mustMatch;
			case "X360": return !mustMatch;
			case "OSX": return !mustMatch;
			case "POSIX": return !mustMatch;
			case "LINUX": return !mustMatch;
#elif OSX
			case "WIN32": return !mustMatch;
			case "WINDOWS": return !mustMatch;
			case "X360": return !mustMatch;
			case "OSX": return mustMatch;
			case "POSIX": return !mustMatch;
			case "LINUX": return !mustMatch;
#elif LINUX
			case "WIN32": return !mustMatch;
			case "WINDOWS": return !mustMatch;
			case "X360": return !mustMatch;
			case "OSX": return !mustMatch;
			case "POSIX": return mustMatch;
			case "LINUX": return mustMatch;
#else
#error Please define how KeyValues.HandleConditional should work on this platform.
#endif
		}
		// Other platforms are not applicable and we should just throw them away
		return !mustMatch;
	}

	private bool ReadKV(StreamReader reader) {
		SkipUntilParseableTextOrEOF(reader);

		bool quoteTerminated = (char)reader.Peek() == '"';

		string key = quoteTerminated ? ReadQuoteTerminatedString(reader, useEscapeSequences) : ReadWhitespaceTerminatedString(reader);
		Name = key;

		SkipUntilParseableTextOrEOF(reader);

		Span<char> conditional = stackalloc char[16];
		bool isBlockConditional = ReadConditional(reader, conditional, out bool mustMatch);
		bool matches = isBlockConditional ? HandleConditional(conditional, mustMatch) : true;

		SkipUntilParseableTextOrEOF(reader);

		// Determine what we're reading next.
		// If we run into a {, we read another KVObject and set our value to that.
		// If we run into a ", we read another string-based value terminated by quotes.
		// If we run into anything else, we read another string-based value, terminated by space or EOF.
		// The value will then be set based on the string. int.TryParse will try to make it an int, same for double, and Color.
		// We then will leave.

		char nextAction = (char)reader.Peek();
		string value;
		switch (nextAction) {
			case '{':
				// Ok, now we need to read every single key value until we hit a }
				ReadKVPairs(reader, matches);
				Type = Types.None;
				break;
			case '"':
				value = ReadQuoteTerminatedString(reader, useEscapeSequences);
				goto valueTypeSpecific;
			default:
				value = ReadWhitespaceTerminatedString(reader);
				goto valueTypeSpecific;
		}

		return !isBlockConditional || matches;

	valueTypeSpecific:

		DetermineValueType(value);
		SkipUntilParseableTextOrEOF(reader);
		bool isValueConditional = ReadConditional(reader, conditional, out bool valueMustMatch);
		bool valueMatches = isValueConditional ? HandleConditional(conditional, valueMustMatch) : true;
		return valueMatches;
	}

	void AddToTail(KeyValues kv) => children.AddLast(kv.node);

	private void ReadKVPairs(StreamReader reader, bool matches) {
		int rd = reader.Read();

		while (reader.Peek() != -1) {
			SkipWhitespace(reader);
			if (reader.Peek() == '}') {
				reader.Read();

				SkipUntilParseableTextOrEOF(reader);

				break;
			}
			SkipUntilParseableTextOrEOF(reader);
			// Start reading keyvalues.
			KeyValues kvpair = new() { evaluateConditionals = this.evaluateConditionals, useEscapeSequences = this.useEscapeSequences };

			if (kvpair.ReadKV(reader) && matches) // When conditional, still need to waste time on parsing, but we throw it away after
												  // There's definitely a better way to handle this, but it would need more testing scenarios
												  // The ReadKV call can also determine its condition and will return false if it doesnt want to be added.
				AddToTail(kvpair);
		}
	}

	// Returns true if we did anything at all to skip comments.
	private bool SkipComments(StreamReader reader) {
		bool didAnything = false;
		if (reader.Peek() == '/') {
			// We need to check the stream for another /
			reader.Read();
			if (reader.Peek() == '/') { // We got //, its a comment
										// We read until the end of the line.
				didAnything = true;
				while (true) {
					char c = (char)reader.Read();
					if (c == '\n')
						break;
				}
			}
			else {
				// What...
				throw new InvalidOperationException("Expected comment");
			}
		}

		return didAnything;
	}

	private void DetermineValueType(string input) {
		// Try Int32
		if (int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i32)) {
			Value = i32;
			Type = Types.Int;
		}

		// Try UInt64
		if (ulong.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong ui64)) {
			Value = ui64;
			Type = Types.Uint64;
		}

		// Try Double
		if (double.TryParse(input, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double d64)) {
			Value = d64;
			Type = Types.Double;
		}

		Value = input;
		Type = Types.String;
	}

	private string ReadWhitespaceTerminatedString(StreamReader reader) {
		Span<char> work = stackalloc char[1024];
		int i, len;
		for (i = 0, len = work.Length; i < len; i++) {
			int c = reader.Peek();
			if (c == -1) break;
			char ch = (char)c;
			if (char.IsWhiteSpace(ch)) break;
			work[i] = ch;
			reader.Read();
		}

		if (i >= len) {
			Warning("KeyValues: string overflow, ignoring (should we allocate more space?)\n");
			AssertMsg(false, "KeyValues string overflow");
		}

		return new(work[..i]);
	}

	private string ReadQuoteTerminatedString(StreamReader reader, bool useEscapeSequences) {
		int rd = reader.Read();
		Debug.Assert(rd == '"', "invalid quote-terminated string");
		Span<char> work = stackalloc char[1024];
		int i, len;
		bool lastCharacterWasEscape = false;
		for (i = 0, len = work.Length; i < len; i++) {
			char c = (char)reader.Peek();
			if (c == -1) break;
			if (lastCharacterWasEscape) {
				// Mutate c into the escape value or just insert the raw character.
				c = c switch {
					'n' => '\n',
					'r' => '\r',
					_ => c
				};
				lastCharacterWasEscape = false;
			}
			else {
				if (c == '"') {
					reader.Read();
					break;
				}
				else if (c == '\\' && useEscapeSequences) {
					lastCharacterWasEscape = true;
					reader.Read();
					continue;
				}
			}
			work[i] = c;
			reader.Read();
		}

		if (i >= len) {
			Warning("KeyValues: string overflow, ignoring (should we allocate more space?)\n");
			AssertMsg(false, "KeyValues string overflow");
		}

		return new(work[..i]);
	}

	public bool LoadFromFile(string? filepath) {
		if (filepath == null) return false;

		if (!Path.IsPathFullyQualified(filepath))
			filepath = Path.Combine(AppContext.BaseDirectory, filepath);

		FileInfo info = new(filepath);
		FileStream stream;
		try { stream = info.OpenRead(); }
		catch { return false; }

		bool ok = LoadFromStream(stream);
		stream.Dispose();
		return ok;
	}


	public KeyValues? FindKey(ReadOnlySpan<char> searchStr, bool create = false) {
		foreach (var child in this.children) {
			if (searchStr.Equals(child.Name, StringComparison.InvariantCulture))
				return child;
		}

		if (create) {
			KeyValues newKey = new(searchStr);
			newKey.useEscapeSequences = useEscapeSequences;

			AddToTail(newKey);
			return newKey;
		}

		return null;
	}

	public ReadOnlySpan<char> GetString() => Value is string str ? (str ?? "") : "";
	public ReadOnlySpan<char> GetString(ReadOnlySpan<char> key, ReadOnlySpan<char> defaultValue = default) {
		var keyob = FindKey(key);
		if (keyob == null) return defaultValue;
		return keyob.Value is string str ? (str ?? "") : "";
	}

	public void SetString(ReadOnlySpan<char> keyName, ReadOnlySpan<char> value) {
		KeyValues? dat = FindKey(keyName, true);
		if (dat != null) {
			if (dat.Type == Types.String && value.Equals(dat.Value?.ToString(), StringComparison.Ordinal))
				return;

			if (value.IsEmpty)
				value = "";
			dat.Value = new string(value);
			dat.Type = Types.String;
		}
	}
	public int GetInt(ReadOnlySpan<char> key, int defaultValue = default) {
		var keyob = FindKey(key);

		if (keyob == null)
			return defaultValue;

		return keyob.Value is int i
			? i
			: keyob.Value is string str
				? int.TryParse(str, out int r)
					? r
					: defaultValue
				: defaultValue;
	}

	public Color GetColor(ReadOnlySpan<char> key, Color defaultValue = default) {
		var keyob = FindKey(key);

		if (keyob == null)
			return defaultValue;

		return keyob.Value is Color c
			? c
			: default;
	}

	public float GetFloat(ReadOnlySpan<char> key, float defaultValue = default) {
		var keyob = FindKey(key);

		if (keyob == null)
			return defaultValue;

		return Convert.ToSingle(keyob.Value is double i
			? i
			: keyob.Value is string str
				? double.TryParse(str, out double r)
					? r
					: defaultValue
				: defaultValue);
	}

	public double GetDouble(ReadOnlySpan<char> key, double defaultValue = default) {
		var keyob = FindKey(key);

		if (keyob == null)
			return defaultValue;

		return keyob.Value is double i
			? i
			: keyob.Value is string str
				? double.TryParse(str, out double r)
					? r
					: defaultValue
				: defaultValue;
	}

	public void SetInt(int value) {
		Value = value;
		Type = Types.Int;
	}

	public void SetInt(ReadOnlySpan<char> keyName, int value) {
		KeyValues? dat = FindKey(keyName, true);
		if (dat != null) {
			dat.Value = value;
			dat.Type = Types.Int;
		}
	}

	public bool LoadFromFile(IFileSystem fileSystem, ReadOnlySpan<char> path, ReadOnlySpan<char> pathID) {
		return LoadFromStream(fileSystem.Open(path, FileOpenOptions.Read, pathID)?.Stream);
	}
	public bool LoadFromFile(IFileSystem fileSystem, ReadOnlySpan<char> path) {
		return LoadFromStream(fileSystem.Open(path, FileOpenOptions.Read, null)?.Stream);
	}

	public KeyValues? GetFirstSubKey() => children.First?.Value;
	public KeyValues? GetFirstTrueSubKey() {
		for (KeyValues? ret = GetFirstSubKey(); ret != null; ret = ret.GetNextKey()) {
			if (ret.Type == Types.None)
				return ret;
		}
		return null;
	}
	public KeyValues? GetNextTrueSubKey() {
		for (LinkedListNode<KeyValues>? ret = node.Next; ret != null; ret = ret.Next) {
			if (ret.Value.Type == Types.None)
				return ret.Value;
		}
		return null;
	}

	public KeyValues? GetFirstValue() {
		for (KeyValues? ret = GetFirstSubKey(); ret != null; ret = ret.GetNextKey()) {
			if (ret.Type != Types.None)
				return ret;
		}
		return null;
	}

	public KeyValues? GetNextValue() {
		for (LinkedListNode<KeyValues>? ret = node.Next; ret != null; ret = ret.Next) {
			if (ret.Value.Type != Types.None)
				return ret.Value;
		}
		return null;
	}

	// TODO: We should cache these!!!!
	public int GetInt() {
		if (Value is string str)
			return int.TryParse(str, out int i) ? i : float.TryParse(str, out float f) ? (int)f : 0;
		else if (Value is int i)
			return i;
		else
			return Convert.ToInt32(Value);
	}
	public float GetFloat() {
		if (Value is string str)
			return float.TryParse(str, out float f) ? f : 0;
		else if (Value is float f)
			return f;
		else
			return Convert.ToSingle(Value);
	}
	public double GetDouble() {
		if (Value is string str)
			return double.TryParse(str, out double d) ? d : 0;
		else if (Value is double d)
			return d;
		else
			return Convert.ToDouble(Value);
	}

	public void UsesEscapeSequences(bool value) {
		useEscapeSequences = value;
	}

	public void UsesConditionals(bool value) {
		evaluateConditionals = value;
	}

	public KeyValues? GetNextKey() {
		return node.Next?.Value;
	}

	public IEnumerator<KeyValues> GetEnumerator() {
		foreach (var key in children)
			yield return key;
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public KeyValues MakeCopy() {
		KeyValues newKeyValue = new(Name);

		newKeyValue.UsesEscapeSequences(useEscapeSequences);
		newKeyValue.UsesConditionals(evaluateConditionals);

		newKeyValue.Type = Type;
		newKeyValue.Value = Value;

		CopySubkeys(newKeyValue);
		return newKeyValue;
	}

	public void CopySubkeys(KeyValues parent) {
		for (KeyValues? sub = GetFirstSubKey(); sub != null; sub = sub.GetNextKey()) {
			KeyValues dat = sub.MakeCopy();
			parent.AddToTail(dat);
		}
	}

	// Untested...
	public unsafe bool LoadFromBuffer(ReadOnlySpan<char> resourceName, ReadOnlySpan<char> buffer) {
		fixed (char* bytes = buffer) {
			byte* input = (byte*)bytes;
			using UnmanagedMemoryStream stream = new(input, buffer.Length * sizeof(char));
			return LoadFromStream(stream);
		}
	}

	public KeyValues AddSubKey(KeyValues subkey) {
		Assert(subkey != null);
		AddToTail(subkey);
		return this;
	}

	public KeyValues AddSubKey(ReadOnlySpan<char> name, int value) {
		AddToTail(new KeyValues(name, value));
		return this;
	}

	public void SetPtr(object? ptr) {
		Value = ptr;
		Type = Types.Pointer;
	}

	public void SetPtr(ReadOnlySpan<char> keyName, object? ptr) {
		KeyValues? dat = FindKey(keyName, true);
		if (dat != null) {
			dat.Value = ptr;
			dat.Type = Types.Pointer;
		}
	}

	public Color GetColor() {
		if (Value is Color c) {
			return c;
		}
		return new(); // todo: proper implementation of this
	}

	public void SetFloat(ReadOnlySpan<char> keyName, float value) {
		KeyValues? dat = FindKey(keyName, true);
		if (dat != null) {
			dat.Value = (double)value;
			dat.Type = Types.Double;
		}
	}
	public void SetDouble(ReadOnlySpan<char> keyName, double value) {
		KeyValues? dat = FindKey(keyName, true);
		if (dat != null) {
			dat.Value = value;
			dat.Type = Types.Double;
		}
	}
	public void SetColor(ReadOnlySpan<char> keyName, Color value) {
		KeyValues? dat = FindKey(keyName, true);
		if (dat != null) {
			dat.Value = value;
			dat.Type = Types.Color;
		}
	}

	public bool GetBool(ReadOnlySpan<char> name, bool defaultValue) {
		return GetInt(name, defaultValue ? 1 : 0) != 0;
	}

	public object? GetPtr() {
		return Value;
	}

	public T? GetPtr<T>() {
		return (T?)Value;
	}

	public object? GetPtr(ReadOnlySpan<char> name) {
		KeyValues? key = FindKey(name, false);
		if (key != null)
			return key.Value;
		return default;
	}

	public T? GetPtr<T>(ReadOnlySpan<char> name) {
		KeyValues? key = FindKey(name, false);
		if (key != null)
			return (T?)key.Value;
		return default;
	}

	public void SetStringValue(ReadOnlySpan<char> str) {
		Value = null;

		if (str.IsEmpty)
			str = "";

		Value = new string(str);
		Type = Types.String;
	}

	public int Count => children.Count;

	public bool IsEmpty(ReadOnlySpan<char> keyName = default) {
		KeyValues? dat = FindKey(keyName, false);
		if (dat == null)
			return true;

		if (dat.Type == Types.None && dat.children.Count == 0)
			return true;

		return false;
	}
}
