// Valve's MD5Value struct
// TODO: Implement the algorithm here too, similar to the LZSS and CRC32 algos

namespace Source.Common.Hashing;

public struct MD5Value
{
	public const int DIGEST_LENGTH = 16;
	public const int BIT_LENGTH = DIGEST_LENGTH * sizeof(byte);

	public byte[] Bits;
	public MD5Value() {
		Bits = new byte[DIGEST_LENGTH];
	}

	public readonly override string ToString() => Convert.ToHexString(Bits);
}
