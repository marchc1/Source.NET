
using Source.Common;
using Source.Common.Engine;
using Source.Common.Networking;

namespace Source.Engine;

public class PackedEntity
{
	public const int FLAG_IS_COMPRESSED = 1 << 31;
	public ServerClass? ServerClass;
	public ClientClass? ClientClass;

	public int EntityIndex;
	public int ReferenceCount;

	readonly List<SendProxyRecipients> m_Recipients = [];
	byte[]? Data;
	int Bits;
	IChangeFrameList? ChangeFrameList;
	uint SnapshotCreationTick;
	bool ShouldCheckCreationTick;

	public bool AllocAndCopyPadded(Span<byte> data) {
		int bytes = PAD_NUMBER(data.Length, 4);
		Data = new byte[bytes];

		data.ClampedCopyTo(Data);
		SetNumBits(bytes * 8);

		return true;
	}

	public void SetNumBits(int bits)=> Bits = bits;
	public void SetCompressed() => Bits |= FLAG_IS_COMPRESSED;
	public bool IsCompressed() => (Bits & FLAG_IS_COMPRESSED) != 0;
	public int GetNumBits() => Bits & ~FLAG_IS_COMPRESSED;

	public byte[]? GetData() => Data;
}
