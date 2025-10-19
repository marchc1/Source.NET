
using Source.Common;
using Source.Common.Networking;

namespace Source.Engine;

public enum EventFlags
{
	NotHost = 1 << 0,
	Reliable = 1 << 1,
	Global = 1 << 2,
}

public class EventInfo
{
	public const int EVENT_INDEX_BITS = 8;
	public const int EVENT_DATA_LEN_BITS = 11;
	public const int MAX_EVENT_DATA = 192;

	public EventInfo() {
		ClassID = 0;
		FireDelay = 0.0f;
		Bits = 0;
		Flags = 0;
		SendTable = null;
		ClientClass = null;
		Data = null;
	}

	public EventInfo(EventInfo src) {
		ClassID = src.ClassID;
		FireDelay = src.FireDelay;
		Bits = src.Bits;
		Flags = src.Flags;
		SendTable = src.SendTable;
		ClientClass = src.ClientClass;
		if (src.Data != null) {
			int size = Net.Bits2Bytes(src.Bits);
			Data = new byte[size];
			src.Data.AsSpan().ClampedCopyTo(Data);
		}
		else 
			Data = null;
	}

	public short ClassID;
	public double FireDelay;
	public SendTable? SendTable;
	public ClientClass? ClientClass;

	public int Bits;
	public byte[]? Data;
	public EventFlags Flags;
	// TODO: EngineRecipientFilter
}
