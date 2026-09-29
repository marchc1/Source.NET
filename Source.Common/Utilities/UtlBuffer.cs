using SevenZip.Buffer;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Source.Common.Utilities;

public delegate bool UtlBufferOverflowFunc(UtlBuffer buffer, int size);

public interface IUtlCharConversionImpl<T> where T : IUtlCharConversionImpl<T>
{
	static abstract char FindConversion(ref UtlCharConversion<T> conv, ReadOnlySpan<char> str, out int len);
}

public class DefaultConversion : IUtlCharConversionImpl<DefaultConversion>
{
	public static char FindConversion(ref UtlCharConversion<DefaultConversion> conv, ReadOnlySpan<char> str, out int len) {
		for (int i = 0; i < conv.m_nCount; ++i) {
			if (0 == strcmp(str, conv.m_pReplacements[(int)conv.m_pList[i]].ReplacementString)) {
				len = conv.m_pReplacements[(int)conv.m_pList[i]].Length;
				return conv.m_pList[i];
			}
		}

		len = 0;
		return '\0';
	}
}

public ref struct UtlCharConversion<T> where T : IUtlCharConversionImpl<T>
{
	public struct ConversionArray
	{
		public char ActualChar;
		public string ReplacementString;
	}

	public UtlCharConversion(char escapeChar, ReadOnlySpan<char> delimiter, Span<ConversionArray> array) {
		m_nEscapeChar = escapeChar;
		m_pDelimiter = delimiter;
		m_nCount = array.Length;
		m_nDelimiterLength = (int)strlen(delimiter);
		m_nMaxConversionLength = 0;

		for (int i = 0; i < m_nCount; ++i) {
			m_pList[i] = array[i].ActualChar;
			ref ConversionInfo info = ref m_pReplacements[(int)m_pList[i]];
			Assert(info.ReplacementString == null);
			info.ReplacementString = array[i].ReplacementString;
			info.Length = info.ReplacementString.Length;
			if (info.Length > m_nMaxConversionLength)
				m_nMaxConversionLength = info.Length;
		}
	}

	public readonly char GetEscapeChar() => m_nEscapeChar;
	public readonly ReadOnlySpan<char> GetDelimiter() => m_pDelimiter;
	public readonly int GetDelimiterLength() => m_nDelimiterLength;

	public readonly ReadOnlySpan<char> GetConversionString(char c) => m_pReplacements[(int)c].ReplacementString;
	public readonly int GetConversionLength(char c) => m_pReplacements[(int)c].Length;
	public readonly int MaxConversionLength() => m_nMaxConversionLength;

	// Finds a conversion for the passed-in string, returns length
	public char FindConversion(ReadOnlySpan<char> str, out int len) => T.FindConversion(ref this, str, out len);

	public struct ConversionInfo
	{
		public int Length;
		public string ReplacementString;
	}

	public char m_nEscapeChar;
	public ReadOnlySpan<char> m_pDelimiter;
	public int m_nDelimiterLength;
	public int m_nCount;
	public int m_nMaxConversionLength;
	public InlineArray256<char> m_pList;
	public InlineArray256<ConversionInfo> m_pReplacements;
}

public class UtlBuffer
{
	[Flags]
	public enum BufferFlags
	{
		TextBuffer = 0x1,          // Describes how get + put work (as strings, or binary)
		ExternalGrowable = 0x2,    // This is used w/ external buffers and causes the utlbuf to switch to reallocatable memory if an overflow happens when Putting.
		ContainsCRLF = 0x4,        // For text buffers only, does this contain \n or \n\r?
		ReadOnly = 0x8,            // For external buffers; prevents null termination from happening.
		AutoTabsDisabled = 0x10,   // Used to disable/enable push/pop tabs
	}

	// Overflow functions when a get or put overflows
	[Flags]
	public enum ErrorFlags
	{
		PutOverflow = 0x1,
		GetOverflow = 0x2,
		MaxErrorFlag = GetOverflow,
	}

	readonly UtlMemory<byte> m_Memory;
	int m_Get;
	int m_Put;
	ErrorFlags m_Error;
	BufferFlags m_Flags;
	int m_nTab;
	int m_nMaxPut;
	int m_nOffset;

	UtlBufferOverflowFunc getOverflowFunc;
	UtlBufferOverflowFunc putOverflowFunc;

	Byteswap byteswap;

	public UtlBuffer(int growSize = 0, int initSize = 0, BufferFlags flags = 0) {
		m_Error = 0;
		m_Memory = new();
		m_Memory.Init(growSize, initSize);
		m_Get = 0;
		m_Put = 0;
		m_nTab = 0;
		m_nOffset = 0;
		this.m_Flags = flags;
		if ((initSize != 0) && !IsReadOnly()) {
			m_nMaxPut = -1;
			AddNullTermination();
		}
		else
			m_nMaxPut = 0;

		SetOverflowFuncs(static (x, i) => x.GetOverflow(i), static (x, i) => x.PutOverflow(i));
	}

	public UtlBuffer(byte[] data, BufferFlags flags = 0) {
		m_Memory = new(data);

		m_Get = 0;
		m_Put = 0;
		m_nTab = 0;
		m_nOffset = 0;
		this.m_Flags = flags;
		if (IsReadOnly()) {
			m_nMaxPut = data.Length;
		}
		else {
			m_nMaxPut = -1;
			AddNullTermination();
		}

		SetOverflowFuncs(static (x, i) => x.GetOverflow(i), static (x, i) => x.PutOverflow(i));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsText() => (m_Flags & BufferFlags.TextBuffer) != 0;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsValid() => m_Error == 0;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public int TellGet() => m_Get;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public int TellPut() => m_Put;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public int TellMaxPut() => m_nMaxPut;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public byte[] Base() => m_Memory.Base();


	public void EnsureCapacity(int num) {
		num += 1;
		if (m_Memory.IsExternallyAllocated()) {
			if (IsGrowable() && (m_Memory.NumAllocated() < num))
				m_Memory.ConvertToGrowableMemory(0);
			else
				num -= 1;
		}

		m_Memory.EnsureCapacity(num);
	}

	bool CheckPut(int size) {
		if ((m_Error & ErrorFlags.PutOverflow) != 0 || IsReadOnly())
			return false;

		if ((m_Put < m_nOffset) || (m_Memory.NumAllocated() < m_Put - m_nOffset + size)) {
			if (!OnPutOverflow(size)) {
				m_Error |= ErrorFlags.PutOverflow;
				return false;
			}
		}
		return true;
	}

	public bool IsReadOnly() => (m_Flags & BufferFlags.ReadOnly) != 0;

	void AddNullTermination() {
		if (m_Put > m_nMaxPut) {
			if (!IsReadOnly() && ((m_Error & ErrorFlags.PutOverflow) == 0)) {
				// Add null termination value
				if (CheckPut(1)) {
					nint Index = m_Put - m_nOffset;
					Assert(m_Memory.IsIdxValid(Index));
					if (Index >= 0)
						m_Memory[Index] = 0;
				}
				else
					// Restore the overflow state, it was valid before...
					m_Error &= ~ErrorFlags.PutOverflow;
			}
			m_nMaxPut = m_Put;
		}
	}

	bool CheckGet(int size) {
		if (m_Error != 0)
			return false;

		if (m_Get + size <= m_nMaxPut)
			return true;

		m_Error |= ErrorFlags.GetOverflow;
		return false;
	}

	bool CheckPeekGet(int offset, int size) {
		if ((m_Error & ErrorFlags.GetOverflow) != 0)
			return false;

		return m_Get + offset + size <= m_nMaxPut;
	}

	public void SeekPut(SeekOrigin type, int offset) {
		switch (type) {
			case SeekOrigin.Begin:
				m_Put = offset;
				break;
			case SeekOrigin.Current:
				m_Put += offset;
				break;
			case SeekOrigin.End:
				m_Put = m_nMaxPut - offset;
				break;
		}

		if (m_Put > m_nMaxPut)
			m_nMaxPut = m_Put;
	}

	public void SeekGet(SeekOrigin type, int offset) {
		switch (type) {
			case SeekOrigin.Begin:
				m_Get = offset;
				break;
			case SeekOrigin.Current:
				m_Get += offset;
				break;
			case SeekOrigin.End:
				m_Get = m_nMaxPut - offset;
				break;
		}

		if (m_Get > m_nMaxPut)
			m_Error |= ErrorFlags.GetOverflow;
		else
			m_Error &= ~ErrorFlags.GetOverflow;
	}

	public void Put(ReadOnlySpan<byte> mem) {
		if (!mem.IsEmpty && CheckPut(mem.Length)) {
			nint index = m_Put - m_nOffset;
			Assert(m_Memory.IsIdxValid(index) && m_Memory.IsIdxValid(index + mem.Length - 1));
			if (index >= 0) {
				memcpy(m_Memory.Base().AsSpan()[(int)index..], mem, mem.Length);
				m_Put += mem.Length;

				AddNullTermination();
			}
		}
	}

	public void Put(ReadOnlySpan<char> mem) {
		if (mem.Length != 0 && CheckPut(mem.Length)) {
			int byteCount = Encoding.ASCII.GetByteCount(mem);
			byte[] rented = ArrayPool<byte>.Shared.Rent(byteCount + 1);
			Encoding.ASCII.GetBytes(mem, rented);
			Put(rented);
			ArrayPool<byte>.Shared.Return(rented);
		}
	}

	public void GetTypeBin<T>(out T dest) where T : unmanaged, ISpanFormattable {
		dest = default;
		int tSize = Unsafe.SizeOf<T>();
		if (CheckGet(tSize)) {
			if (!byteswap.IsSwappingBytes() || (tSize == 1))
				dest = const_reinterpret<byte, T>(PeekGet())[0];
			else
				byteswap.SwapBufferToTargetEndian(new(ref dest), const_reinterpret<byte, T>(PeekGet()));

			m_Get += tSize;
		}
		else {
			dest = default;
		}
	}

	public T GetType<T>() where T : unmanaged, ISpanFormattable {
		GetType(out T v);
		return v;
	}
	public void GetType<T>(out T dest) where T : unmanaged, ISpanFormattable {
		if (!IsText()) {
			GetTypeBin(out dest);
		}
		else {
			dest = default;
		}
	}

	public void PutType<T>(in T value) where T : unmanaged, ISpanFormattable {
		if (!IsText()) {
			PutTypeBin(in value);
		}
		else {
			Span<char> data = stackalloc char[1024]; // TODO: Probably should not do it this way...
			if (!value.TryFormat(data, out int chars, null, null))
				return;
			Printf(data[..chars]);
		}
	}

	Span<byte> PeekGet(int offset = 0) => m_Memory.Base().AsSpan()[(m_Get + offset - this.m_nOffset)..];
	Span<byte> PeekPut(int offset = 0) => m_Memory.Base().AsSpan()[(m_Put + offset - this.m_nOffset)..];

	private void PutTypeBin<T>(in T src) where T : unmanaged, ISpanFormattable {
		int tSize = Unsafe.SizeOf<T>();
		if (CheckPut(tSize)) {
			if (!byteswap.IsSwappingBytes() || tSize == 1)
				memcpy(PeekPut()[..tSize], const_reinterpret<T, byte>(new(in src)));
			else
				byteswap.SwapBufferToTargetEndian(reinterpret<byte, T>(PeekPut()[..tSize]), new(in src));

			m_Put += tSize;
			AddNullTermination();
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool WasLastCharacterCR() => (!IsText() || TellPut() == 0) ? false : PeekPut(-1)[0] == (byte)'\n';
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsGrowable() => (m_Flags & BufferFlags.ExternalGrowable) != 0;


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void PutTabs() {
		int tabCount = (m_Flags & BufferFlags.AutoTabsDisabled) != 0 ? 0 : m_nTab;
		for (int i = tabCount; --i >= 0;)
			PutTypeBin((sbyte)'\t');
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void PushTab() => m_nTab++;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void PopTab() => m_nTab = Math.Max(m_nTab - 1, 0);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void EnableTabs(bool enable) {
		if (enable)
			m_Flags &= ~BufferFlags.AutoTabsDisabled;
		else
			m_Flags |= BufferFlags.AutoTabsDisabled;
	}

	public void PutString(ReadOnlySpan<char> str, Encoding? encoding = null) {
		encoding ??= Encoding.ASCII;
		str = str.SliceNullTerminatedString();

		int byteCount = encoding.GetByteCount(str);
		byte[] rented = ArrayPool<byte>.Shared.Rent(byteCount + 1);
		encoding.GetBytes(str, rented);
		rented[byteCount] = 0;
		ReadOnlySpan<byte> pString = rented.AsSpan(0, byteCount);

		if (!IsText()) {
			if (!str.IsEmpty) {
				// Not text? append a null at the end.
				int nLen = pString.Length + 1;
				Put(rented.AsSpan(0, nLen * sizeof(byte)));
				goto freeReturn;
			}
			else {
				PutTypeBin<sbyte>(0);
			}
		}
		else if (!str.IsEmpty) {
			int nTabCount = (m_Flags & BufferFlags.AutoTabsDisabled) != 0 ? 0 : m_nTab;
			if (nTabCount > 0) {
				if (WasLastCharacterCR()) {
					PutTabs();
				}

				int pEndl = pString.IndexOf((byte)'\n');
				while (pEndl >= 0) {
					int nSize = pEndl + sizeof(byte);
					Put(pString[..nSize]);
					pString = pString[(pEndl + 1)..];
					if (!pString.IsEmpty) {
						PutTabs();
						pEndl = pString.IndexOf((byte)'\n');
					}
					else {
						pEndl = -1;
					}
				}
			}
			int nLen = pString.Length;
			if (nLen != 0) {
				Put(pString[..(nLen * sizeof(byte))]);
			}
		}

	freeReturn:
		ArrayPool<byte>.Shared.Return(rented);
		return;
	}

	void PutDelimitedCharInternal<T>(ref UtlCharConversion<T> conv, char c) where T : IUtlCharConversionImpl<T> {
		int l = conv.GetConversionLength(c);
		if (l == 0)
			PutChar(c);
		else {
			PutChar(conv.GetEscapeChar());
			Put(conv.GetConversionString(c)[..l]);
		}
	}

	public void PutDelimitedChar<T>(ref UtlCharConversion<T> conv, char c) where T : IUtlCharConversionImpl<T> {
		if (!IsText() || Unsafe.IsNullRef(ref conv)) {
			PutChar(c);
			return;
		}

		PutDelimitedCharInternal(ref conv, c);
	}

	public void PutDelimitedString<T>(ref UtlCharConversion<T> conv, ReadOnlySpan<char> str, Encoding? encoding = null) where T : IUtlCharConversionImpl<T> {
		if (!IsText() || Unsafe.IsNullRef(ref conv)) {
			PutString(str, encoding);
			return;
		}

		if (WasLastCharacterCR())
			PutTabs();

		Put(conv.GetDelimiter()[..conv.GetDelimiterLength()]);

		int nLen = !str.IsEmpty ? (int)strlen(str) : 0;
		for (int i = 0; i < nLen; ++i)
			PutDelimitedCharInternal(ref conv, str[i]);

		if (WasLastCharacterCR())
			PutTabs();

		Put(conv.GetDelimiter()[..conv.GetDelimiterLength()]);
	}

	public void Get(Span<byte> mem) {
		if (!mem.IsEmpty && CheckGet(mem.Length)) {
			nint idx = m_Get - m_nOffset;
			Assert(m_Memory.IsIdxValid(idx) && m_Memory.IsIdxValid(idx + mem.Length - 1));

			memcpy(mem, m_Memory.Base().AsSpan()[(int)idx..], mem.Length);
			m_Get += mem.Length;
		}
	}

	[MemberNotNull(nameof(getOverflowFunc))]
	[MemberNotNull(nameof(putOverflowFunc))]
	public void SetOverflowFuncs(UtlBufferOverflowFunc getFunc, UtlBufferOverflowFunc putFunc) {
		getOverflowFunc = getFunc;
		putOverflowFunc = putFunc;
	}

	bool OnPutOverflow(int size) => putOverflowFunc!.Invoke(this, size);
	bool OnGetOverflow(int size) => getOverflowFunc!.Invoke(this, size);

	public bool GetOverflow(int size) {
		return false;
	}

	public nint Size() => m_Memory.NumAllocated();

	public bool PutOverflow(int size) {
		if (m_Memory.IsExternallyAllocated()) {
			if (!IsGrowable())
				return false;

			m_Memory.ConvertToGrowableMemory(0);
		}

		while (Size() < m_Put - m_nOffset + size)
			m_Memory.Grow();

		return true;
	}

	//-----------------------------------------------------------------------------
	// Eats whitespace
	//-----------------------------------------------------------------------------
	public void EatWhiteSpace() {
		if (IsText() && IsValid()) {
			while (CheckGet(sizeof(byte))) {
				if (!char.IsWhiteSpace((char)m_Memory[m_Get]))
					break;
				m_Get += sizeof(byte);
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
			if (!char.IsWhiteSpace((char)m_Memory[m_Get + offset]))
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

			byte test = m_Memory[m_Get + offset];

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
			m_Error |= ErrorFlags.GetOverflow;
			return;
		}

		int charsToRead = Math.Min(len, str.Length) - 1;

		for (int i = 0; i < charsToRead; i++)
			str[i] = (char)m_Memory[m_Get + i];
		m_Get += charsToRead;
		str[charsToRead] = '\0';

		if (len > (charsToRead + 1))
			SeekGet(SeekOrigin.Current, len - (charsToRead + 1));

		// Read the terminating NULL in binary formats
		if (!IsText())
			GetChar();
	}

	public char GetChar() {
		if (!IsText()) {
			if (CheckGet(sizeof(byte)))
				return (char)(sbyte)m_Memory[m_Get++];
			return '\0';
		}

		if (m_Get >= TellMaxPut()) {
			m_Error |= ErrorFlags.GetOverflow;
			return '\0';
		}

		if (CheckPeekGet(0, sizeof(byte)))
			return (char)(sbyte)m_Memory[m_Get++];

		return '\0';
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void PutChar(char c) {
		if (WasLastCharacterCR())
			PutTabs();
		PutTypeBin((sbyte)c);
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void PutChar(sbyte c) => PutType(c);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void PutUnsignedChar(byte c) => PutType(c);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void PutUint64(ulong c) => PutType(c);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void PutInt16(short c) => PutType(c);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void PutShort(short c) => PutType(c);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void PutUnsignedShort(ushort c) => PutType(c);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void PutInt(int c) => PutType(c);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void PutInt64(long c) => PutType(c);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void PutUnsignedInt(uint c) => PutType(c);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void PutFloat(float c) => PutType(c);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void PutDouble(double c) => PutType(c);


	[MethodImpl(MethodImplOptions.AggressiveInlining)] public byte GetUnsignedChar() => GetType<byte>();
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public short GetShort() => GetType<short>();
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ushort GetUnsignedShort() => GetType<ushort>();
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public int GetInt() => GetType<int>();
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public long GetInt64() => GetType<long>();
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public uint GetUnsignedInt() => GetType<uint>();
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ulong GetUint64() => GetType<ulong>();
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public float GetFloat() => GetType<float>();
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public double GetDouble() => GetType<double>();


	public void Printf(ReadOnlySpan<char> str) {
		int count = Encoding.ASCII.GetByteCount(str);
		if (CheckPut(count)) {
			Encoding.ASCII.GetBytes(str, m_Memory.Base().AsSpan()[m_Put..]);
			m_Put += count;
			AddNullTermination();
		}
	}
}
