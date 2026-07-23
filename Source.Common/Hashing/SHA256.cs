using System.Runtime.InteropServices;

namespace Source.Common.Hashing;

[StructLayout(LayoutKind.Sequential)]
public struct SHA256 : IEquatable<SHA256>
{
	public ulong X, Y, Z, W;

	public SHA256(ulong x, ulong y, ulong z, ulong w) {
		X = x;
		Y = y;
		Z = z;
		W = w;
	}

	public readonly byte[] ToBytes() {
		byte[] result = new byte[32];
		BitConverter.TryWriteBytes(result.AsSpan(0, 8), X);
		BitConverter.TryWriteBytes(result.AsSpan(8, 8), Y);
		BitConverter.TryWriteBytes(result.AsSpan(16, 8), Z);
		BitConverter.TryWriteBytes(result.AsSpan(24, 8), W);
		return result;
	}

	public static SHA256 FromBytes(ReadOnlySpan<byte> bytes) {
		if (bytes.Length < 32)
			throw new ArgumentException("Span must be at least 32 bytes.", nameof(bytes));

		return new SHA256(
			BitConverter.ToUInt64(bytes.Slice(0, 8)),
			BitConverter.ToUInt64(bytes.Slice(8, 8)),
			BitConverter.ToUInt64(bytes.Slice(16, 8)),
			BitConverter.ToUInt64(bytes.Slice(24, 8))
		);
	}

	public static SHA256 Compute(ReadOnlySpan<byte> data) {
		Span<byte> hash = stackalloc byte[32];
		System.Security.Cryptography.SHA256.HashData(data, hash);
		return FromBytes(hash);
	}

	public readonly bool Equals(SHA256 other) =>
		X == other.X && Y == other.Y && Z == other.Z && W == other.W;

	public readonly override bool Equals(object? obj) =>
		obj is SHA256 other && Equals(other);

	public readonly override int GetHashCode() =>
		HashCode.Combine(X, Y, Z, W);

	public readonly override string ToString() {
		Span<char> chars = stackalloc char[64];
		Span<byte> bytes = stackalloc byte[32];
		BitConverter.TryWriteBytes(bytes.Slice(0, 8), X);
		BitConverter.TryWriteBytes(bytes.Slice(8, 8), Y);
		BitConverter.TryWriteBytes(bytes.Slice(16, 8), Z);
		BitConverter.TryWriteBytes(bytes.Slice(24, 8), W);
		for (int i = 0; i < 32; i++) {
			byte b = bytes[i];
			chars[i * 2] = GetHexChar(b >> 4);
			chars[i * 2 + 1] = GetHexChar(b & 0xF);
		}
		return new string(chars);
	}

	private static char GetHexChar(int value) =>
		(char)(value < 10 ? '0' + value : 'a' + (value - 10));

	public static bool operator ==(SHA256 left, SHA256 right) => left.Equals(right);
	public static bool operator !=(SHA256 left, SHA256 right) => !left.Equals(right);
}
