
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

	readonly List<SendProxyRecipients> Recipients = [];
	byte[]? Data;
	int Bits;
	IChangeFrameList? ChangeFrameList;
	uint SnapshotCreationTick;
	bool _ShouldCheckCreationTick;

	public bool AllocAndCopyPadded(Span<byte> data) {
		FreeData();

		int bytes = PAD_NUMBER(data.Length, 4);
		Data = new byte[bytes];

		data.ClampedCopyTo(Data);
		SetNumBits(bytes * 8);

		return true;
	}

	public void FreeData() => Data = null;

	public void SetNumBits(int bits) => Bits = bits;
	public void SetCompressed() => Bits |= FLAG_IS_COMPRESSED;
	public bool IsCompressed() => (Bits & FLAG_IS_COMPRESSED) != 0;
	public int GetNumBits() => Bits & ~FLAG_IS_COMPRESSED;
	public int GetNumBytes() => (Bits + 7) >> 3;

	public byte[]? GetData() => Data;

	public void SetChangeFrameList(IChangeFrameList list) {
		Assert(ChangeFrameList == null);
		ChangeFrameList = list;
	}

	public IChangeFrameList? GetChangeFrameList() => ChangeFrameList;

	public IChangeFrameList? SnagChangeFrameList() {
		IChangeFrameList? ret = ChangeFrameList;
		ChangeFrameList = null;
		return ret;
	}

	public int GetPropsChangedAfterTick(int tick, Span<int> outProps) {
		if (ChangeFrameList != null)
			return ChangeFrameList.GetPropsChangedAfterTick(tick, outProps, Constants.MAX_DATATABLE_PROPS);

		return -1;
	}

	public List<SendProxyRecipients> GetRecipients() => Recipients;
	public int GetNumRecipients() => Recipients.Count;

	public void SetRecipients(ReadOnlySpan<SendProxyRecipients> recipients) {
		Recipients.Clear();
		Recipients.AddRange(recipients);
	}

	public bool CompareRecipients(ReadOnlySpan<SendProxyRecipients> recipients) {
		if (recipients.Length != Recipients.Count)
			return false;

		for (int i = 0; i < recipients.Length; i++) {
			if (!ReferenceEquals(recipients[i], Recipients[i]))
				return false;
		}

		return true;
	}

	public void SetSnapshotCreationTick(int tick) => SnapshotCreationTick = (uint)tick;
	public int GetSnapshotCreationTick() => (int)SnapshotCreationTick;

	public void SetShouldCheckCreationTick(bool state) => _ShouldCheckCreationTick = state;
	public bool ShouldCheckCreationTick() => _ShouldCheckCreationTick;

	public void SetServerAndClientClass(ServerClass? serverClass, ClientClass? clientClass) {
		ServerClass = serverClass;
		ClientClass = clientClass;
		if (serverClass != null) {
			Assert(serverClass.Table != null);
			SetShouldCheckCreationTick(serverClass.Table!.HasPropsEncodedAgainstCurrentTickCount);
		}
	}
}
