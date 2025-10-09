using System.Buffers;

namespace Source.Common.Networking;

public unsafe class DataFragments
{
	public string? Filename;
	public byte[]? Buffer;
	public uint Bytes;
	public uint Bits;
	public uint TransferID;
	public bool Compressed;
	public uint UncompressedSize;
	public bool AsTCP;
	public int NumFragments;
	public int AckedFragments;
	public int PendingFragments;

	public int Count;

	public void Return() {
		if (Buffer == null) return;
		ArrayPool<byte>.Shared.Return(Buffer, true);
		Buffer = null;
	}
	public void Rent(int size) {
		Return();
		Buffer = ArrayPool<byte>.Shared.Rent(size);
	}
}
