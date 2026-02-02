// C# implementation of Valve's implementation of LZSS. -- Complete with C++ memory safety hell -- Now safe!!!

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Source.Common.Compression;

public struct lzss_header_t
{
	public uint ID;
	public uint ActualSize;

	public const int SIZEOF = sizeof(uint) * 2;
}

public class CLZSS
{
	public const uint LZSS_ID = 'S' << 24 | 'S' << 16 | 'Z' << 8 | 'L';

	public static uint WordSwap(uint value) {
		ushort temp = BitConverter.ToUInt16(BitConverter.GetBytes(value), 0);
		temp = (ushort)(temp >> 8 | temp << 8);
		return Unsafe.As<ushort, uint>(ref temp);
	}

	public static uint DWordSwap(uint value) {
		uint temp = BitConverter.ToUInt32(BitConverter.GetBytes(value), 0);
		temp = temp >> 24 | (temp & 0x00FF0000) >> 8 | (temp & 0x0000FF00) << 8 | temp << 24;
		return Unsafe.As<uint, uint>(ref temp);
	}

	public const int LZSS_LOOKSHIFT = 4;
	public const int LZSS_LOOKAHEAD = 1 << LZSS_LOOKSHIFT;

	/// <summary>
	/// Returns true if buffer is compressed.
	/// </summary>
	public static bool IsCompressed(Span<byte> input) {
		if (input.IsEmpty || input.Length < lzss_header_t.SIZEOF)
			return false;

		ref lzss_header_t header = ref MemoryMarshal.Cast<byte, lzss_header_t>(input)[0];

		if (header.ID == LZSS_ID)
			return true;

		return false;
	}

	public static uint GetActualSize(Span<byte> input) {
		if (input.IsEmpty || input.Length < lzss_header_t.SIZEOF)
			return 0;

		ref lzss_header_t header = ref MemoryMarshal.Cast<byte, lzss_header_t>(input)[0];
		if (header.ID == LZSS_ID) 
			return header.ActualSize;
		
		// unrecognized
		return 0;
	}


	//-----------------------------------------------------------------------------
	// Uncompress a buffer, Returns the uncompressed size. Caller must provide an
	// adequate sized output buffer or memory corruption will occur.
	//-----------------------------------------------------------------------------
	public static uint Uncompress(Span<byte> input, Span<byte> output) {
		uint totalBytes = 0;
		int cmdByte = 0;
		int getCmdByte = 0;

		uint actualSize = GetActualSize(input);
		if (actualSize == 0) {
			return 0;
		}

		int inputIndex = lzss_header_t.SIZEOF;
		int outputIndex = 0;

		while (true) {
			if (getCmdByte == 0) {
				cmdByte = input[inputIndex++];
			}
			getCmdByte = getCmdByte + 1 & 0x07;

			if ((cmdByte & 0x01) != 0) {
				int position = input[inputIndex++] << LZSS_LOOKSHIFT;
				position |= input[inputIndex] >> LZSS_LOOKSHIFT;
				int count = (input[inputIndex++] & 0x0F) + 1;
				if (count == 1) {
					break;
				}

				int sourceIndex = outputIndex - position - 1;
				for (int i = 0; i < count; i++) {
					output[outputIndex++] = output[sourceIndex++];
				}
				totalBytes += (uint)count;
			}
			else {
				output[outputIndex++] = input[inputIndex++];
				totalBytes++;
			}
			cmdByte >>= 1;
		}

		return totalBytes == actualSize ? totalBytes : 0;
	}

}
