using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace Source.Common.Utilities;

public class UtlBuffer
{
	[Flags]
	public enum BufferFlags
	{
		TEXT_BUFFER = 0x1,          // Describes how get + put work (as strings, or binary)
		EXTERNAL_GROWABLE = 0x2,    // This is used w/ external buffers and causes the utlbuf to switch to reallocatable memory if an overflow happens when Putting.
		CONTAINS_CRLF = 0x4,        // For text buffers only, does this contain \n or \n\r?
		READ_ONLY = 0x8,            // For external buffers; prevents null termination from happening.
		AUTO_TABS_DISABLED = 0x10,  // Used to disable/enable push/pop tabs
	}

	// Overflow functions when a get or put overflows
	[Flags]
	public enum ErrorFlags
	{
		PUT_OVERFLOW = 0x1,
		GET_OVERFLOW = 0x2,
		MAX_ERROR_FLAG = GET_OVERFLOW,
	}

	public enum SeekType
	{
		SEEK_HEAD = 0,
		SEEK_CURRENT,
		SEEK_TAIL
	}

	byte[] memory;
	int get;
	int put;
	ErrorFlags error;
	BufferFlags flags;
	int maxPut;

	public UtlBuffer(int growSize = 0, int initSize = 0, BufferFlags flags = 0) {
		memory = new byte[Math.Max(initSize, 0)];
		this.flags = flags;
		get = 0;
		put = 0;
		maxPut = 0;
		error = 0;
	}

	public UtlBuffer(ReadOnlySpan<byte> data, BufferFlags flags = 0) {
		memory = data.ToArray();
		this.flags = flags;
		get = 0;
		put = data.Length;
		maxPut = data.Length;
		error = 0;
	}

	public bool IsText() => (flags & BufferFlags.TEXT_BUFFER) != 0;
	public bool IsValid() => error == 0;
	public int TellGet() => get;
	public int TellPut() => put;
	public int TellMaxPut() => maxPut;
	public ReadOnlySpan<byte> Base() => memory.AsSpan(0, maxPut);

	public void EnsureCapacity(int num) {
		if (memory.Length < num)
			Array.Resize(ref memory, num);
	}

	bool CheckPut(int size) {
		if ((error & ErrorFlags.PUT_OVERFLOW) != 0)
			return false;

		if (put + size > memory.Length)
			Array.Resize(ref memory, Math.Max(memory.Length * 2, put + size));

		return true;
	}

	void AddNullTermination() {
		if (put > maxPut)
			maxPut = put;
	}

	bool CheckGet(int size) {
		if (error != 0)
			return false;

		if (get + size <= maxPut)
			return true;

		error |= ErrorFlags.GET_OVERFLOW;
		return false;
	}

	bool CheckPeekGet(int offset, int size) {
		if ((error & ErrorFlags.GET_OVERFLOW) != 0)
			return false;

		return get + offset + size <= maxPut;
	}

	public void SeekPut(SeekType type, int offset) {
		switch (type) {
			case SeekType.SEEK_HEAD:
				put = offset;
				break;
			case SeekType.SEEK_CURRENT:
				put += offset;
				break;
			case SeekType.SEEK_TAIL:
				put = maxPut - offset;
				break;
		}

		if (put > maxPut)
			maxPut = put;
	}

	public void SeekGet(SeekType type, int offset) {
		switch (type) {
			case SeekType.SEEK_HEAD:
				get = offset;
				break;
			case SeekType.SEEK_CURRENT:
				get += offset;
				break;
			case SeekType.SEEK_TAIL:
				get = maxPut - offset;
				break;
		}

		if (get > maxPut)
			error |= ErrorFlags.GET_OVERFLOW;
		else
			error &= ~ErrorFlags.GET_OVERFLOW;
	}

	public void Put(ReadOnlySpan<byte> mem) {
		if (mem.Length != 0 && CheckPut(mem.Length)) {
			mem.CopyTo(memory.AsSpan(put));
			put += mem.Length;
			AddNullTermination();
		}
	}

	public void Get(Span<byte> mem) {
		if (mem.Length > 0 && CheckGet(mem.Length)) {
			memory.AsSpan(get, mem.Length).CopyTo(mem);
			get += mem.Length;
		}
	}

	//-----------------------------------------------------------------------------
	// Eats whitespace
	//-----------------------------------------------------------------------------
	public void EatWhiteSpace() {
		if (IsText() && IsValid()) {
			while (CheckGet(sizeof(byte))) {
				if (!char.IsWhiteSpace((char)memory[get]))
					break;
				get += sizeof(byte);
			}
		}
	}

	//-----------------------------------------------------------------------------
	// Peeks how much whitespace to eat
	//-----------------------------------------------------------------------------
	public int PeekWhiteSpace(int offset) {
		if (!IsText() || !IsValid())
			return 0;

		while (CheckPeekGet(offset, sizeof(byte))) {
			if (!char.IsWhiteSpace((char)memory[get + offset]))
				break;
			offset += sizeof(byte);
		}

		return offset;
	}

	//-----------------------------------------------------------------------------
	// Peek size of sting to come, check memory bound
	//-----------------------------------------------------------------------------
	public int PeekStringLength() {
		if (!IsValid())
			return 0;

		// Eat preceeding whitespace
		int offset = 0;
		if (IsText())
			offset = PeekWhiteSpace(offset);

		int startingOffset = offset;

		while (true) {
			if (!CheckPeekGet(offset, 1))
				return offset == startingOffset ? 0 : offset - startingOffset + 1;

			byte test = memory[get + offset];

			if (!IsText()) {
				// The +1 here is so we eat the terminating 0
				if (test == 0)
					return offset - startingOffset + 1;
			}
			else {
				// The +1 here is so we eat the terminating 0
				if (char.IsWhiteSpace((char)test) || test == 0)
					return offset - startingOffset + 1;
			}

			offset++;
		}
	}

	//-----------------------------------------------------------------------------
	// Reads a null-terminated string
	//-----------------------------------------------------------------------------
	public void GetString(Span<char> str) {
		if (!IsValid()) {
			if (str.Length > 0)
				str[0] = '\0';
			return;
		}

		if (str.Length == 0)
			return;

		// Remember, this *includes* the null character
		// It will be 0, however, if the buffer is empty.
		int len = PeekStringLength();

		if (IsText())
			EatWhiteSpace();

		if (len <= 0) {
			str[0] = '\0';
			error |= ErrorFlags.GET_OVERFLOW;
			return;
		}

		int charsToRead = Math.Min(len, str.Length) - 1;

		for (int i = 0; i < charsToRead; i++)
			str[i] = (char)memory[get + i];
		get += charsToRead;
		str[charsToRead] = '\0';

		if (len > (charsToRead + 1))
			SeekGet(SeekType.SEEK_CURRENT, len - (charsToRead + 1));

		// Read the terminating NULL in binary formats
		if (!IsText())
			GetChar();
	}

	public char GetChar() {
		if (!IsText()) {
			if (CheckGet(sizeof(byte)))
				return (char)(sbyte)memory[get++];
			return '\0';
		}

		if (get >= TellMaxPut()) {
			error |= ErrorFlags.GET_OVERFLOW;
			return '\0';
		}

		if (CheckPeekGet(0, sizeof(byte)))
			return (char)(sbyte)memory[get++];

		return '\0';
	}

	public short GetShort() {
		if (!IsText()) {
			if (CheckGet(sizeof(short))) {
				short v = BinaryPrimitives.ReadInt16LittleEndian(memory.AsSpan(get));
				get += sizeof(short);
				return v;
			}
			return 0;
		}

		return (short)ScanInteger();
	}

	public int GetInt() {
		if (!IsText()) {
			if (CheckGet(sizeof(int))) {
				int v = BinaryPrimitives.ReadInt32LittleEndian(memory.AsSpan(get));
				get += sizeof(int);
				return v;
			}
			return 0;
		}

		return (int)ScanInteger();
	}

	public float GetFloat() {
		if (!IsText()) {
			if (CheckGet(sizeof(float))) {
				float v = BinaryPrimitives.ReadSingleLittleEndian(memory.AsSpan(get));
				get += sizeof(float);
				return v;
			}
			return 0;
		}

		return (float)ScanFloat();
	}

	long ScanInteger() {
		if (error != 0 || get >= TellMaxPut()) {
			error |= ErrorFlags.GET_OVERFLOW;
			return 0;
		}

		int start = get;
		int end = get;
		if (end < maxPut && (memory[end] == '-' || memory[end] == '+'))
			end++;
		while (end < maxPut && memory[end] >= '0' && memory[end] <= '9')
			end++;

		if (!long.TryParse(Encoding.ASCII.GetString(memory, start, end - start), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long value))
			return 0;

		get = end;
		return value;
	}

	double ScanFloat() {
		if (error != 0 || get >= TellMaxPut()) {
			error |= ErrorFlags.GET_OVERFLOW;
			return 0;
		}

		int start = get;
		int end = get;
		while (end < maxPut && !char.IsWhiteSpace((char)memory[end]) && memory[end] != 0)
			end++;

		if (!double.TryParse(Encoding.ASCII.GetString(memory, start, end - start), NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
			return 0;

		get = end;
		return value;
	}

	public void PutChar(char c) {
		if (CheckPut(sizeof(byte))) {
			memory[put++] = (byte)c;
			AddNullTermination();
		}
	}

	public void PutShort(short s) {
		if (IsText()) {
			Printf(s.ToString(CultureInfo.InvariantCulture));
			return;
		}

		if (CheckPut(sizeof(short))) {
			BinaryPrimitives.WriteInt16LittleEndian(memory.AsSpan(put), s);
			put += sizeof(short);
			AddNullTermination();
		}
	}

	public void PutInt(int i) {
		if (IsText()) {
			Printf(i.ToString(CultureInfo.InvariantCulture));
			return;
		}

		if (CheckPut(sizeof(int))) {
			BinaryPrimitives.WriteInt32LittleEndian(memory.AsSpan(put), i);
			put += sizeof(int);
			AddNullTermination();
		}
	}

	public void PutFloat(float f) {
		if (IsText()) {
			Printf(f.ToString("F6", CultureInfo.InvariantCulture));
			return;
		}

		if (CheckPut(sizeof(float))) {
			BinaryPrimitives.WriteSingleLittleEndian(memory.AsSpan(put), f);
			put += sizeof(float);
			AddNullTermination();
		}
	}

	public void Printf(ReadOnlySpan<char> str) {
		int count = Encoding.ASCII.GetByteCount(str);
		if (CheckPut(count)) {
			Encoding.ASCII.GetBytes(str, memory.AsSpan(put));
			put += count;
			AddNullTermination();
		}
	}
}
