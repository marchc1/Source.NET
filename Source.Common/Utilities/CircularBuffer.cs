using System.Diagnostics;

namespace Source.Common.Utilities;

public class CircularBuffer
{
	public CircularBuffer(int size) {
		m_chData = new byte[size];
		SetSize(size);
	}

	public void SetSize(int size) {
		Assert(size <= m_chData.Length);

		m_nSize = size;
		m_nRead = 0;
		m_nWrite = 0;
		m_nCount = 0;
	}

	[Conditional("DBGFLAG_ASSERT")]
	void AssertValid() {
		Assert(m_nSize > 0);
		Assert(m_nCount >= 0);
		Assert(m_nCount <= m_nSize);
		Assert(m_nWrite < m_nSize);

		// Verify that m_nCount is correct.
		if (m_nRead == m_nWrite) {
			Assert(m_nCount == 0 || m_nCount == m_nSize);
		}
		else {
			int testCount = 0;
			if (m_nRead < m_nWrite)
				testCount = m_nWrite - m_nRead;
			else
				testCount = (m_nSize - m_nRead) + m_nWrite;

			Assert(testCount == m_nCount);
		}
	}

	public void Flush() {
		AssertValid();

		m_nRead = 0;
		m_nWrite = 0;
		m_nCount = 0;
	}

	public int GetSize() {
		AssertValid();

		return m_nSize;
	}

	public int GetWriteAvailable() {
		AssertValid();

		return m_nSize - m_nCount;
	}

	public int GetReadAvailable() {
		AssertValid();

		return m_nCount;
	}

	public int Peek(Span<byte> pchDest, int nCount) {
		// If no data available, just return.
		if (m_nCount == 0)
			return 0;

		//
		// Requested amount should not exceed the available amount.
		//
		nCount = Math.Min(m_nCount, nCount);

		//
		// Copy as many of the requested bytes as possible.
		// If buffer wrap occurs split the data into two chunks.
		//
		if (m_nRead + nCount > m_nSize) {
			int nCount1 = m_nSize - m_nRead;
			m_chData.AsSpan(m_nRead, nCount1).CopyTo(pchDest);

			int nCount2 = nCount - nCount1;
			m_chData.AsSpan(0, nCount2).CopyTo(pchDest[nCount1..]);
		}
		// Otherwise copy it in one go.
		else {
			m_chData.AsSpan(m_nRead, nCount).CopyTo(pchDest);
		}

		AssertValid();
		return nCount;
	}

	public int Advance(int nCount) {
		// If no data available, just return.
		if (m_nCount == 0)
			return 0;

		//
		// Requested amount should not exceed the available amount.
		//
		nCount = Math.Min(m_nCount, nCount);

		// Advance the read pointer, checking for buffer
		//wrap.
		//
		m_nRead = (m_nRead + nCount) % m_nSize;
		m_nCount -= nCount;

		//
		// If we have emptied the buffer, reset the read and write indices
		// to minimize buffer wrap.
		//
		if (m_nCount == 0) {
			m_nRead = 0;
			m_nWrite = 0;
		}

		AssertValid();
		return nCount;
	}

	public int Read(Span<byte> pchDest, int nCount) {
		int nPeeked;
		int nRead;

		nPeeked = Peek(pchDest, nCount);

		if (nPeeked != 0) {
			nRead = Advance(nPeeked);

			Assert(nRead == nPeeked);
		}
		else {
			nRead = 0;
		}

		AssertValid();
		return nRead;
	}

	public int Write(ReadOnlySpan<byte> pData, int nBytesRequested) {
		// Write all the data.
		int nBytesToWrite = nBytesRequested;
		ReadOnlySpan<byte> pDataToWrite = pData;

		while (nBytesToWrite != 0) {
			int from = m_nWrite;
			int to = m_nWrite + nBytesToWrite;

			if (to >= m_nSize) {
				to = m_nSize;
			}

			pDataToWrite[..(to - from)].CopyTo(m_chData.AsSpan(from));
			pDataToWrite = pDataToWrite[(to - from)..];

			m_nWrite = to % m_nSize;
			nBytesToWrite -= to - from;
		}

		// Did it cross the read pointer? Then slide the read pointer up.
		// This way, we will discard the old data.
		if (nBytesRequested > (m_nSize - m_nCount)) {
			m_nCount = m_nSize;
			m_nRead = m_nWrite;
		}
		else {
			m_nCount += nBytesRequested;
		}

		AssertValid();
		return nBytesRequested;
	}

	public int m_nCount;            // Space between the read and write pointers (how much data we can read).

	public int m_nRead;         // Read index into circular buffer
	public int m_nWrite;            // Write index into circular buffer

	public int m_nSize;         // Size of circular buffer in bytes (how much data it can hold).
	readonly byte[] m_chData;       // Circular buffer holding data
}
