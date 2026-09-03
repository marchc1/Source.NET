using Source.Common;
using Source.Common.Bitbuffers;
using Source.Common.Commands;
using Source.Common.Networking;

namespace Source.Engine;

public partial class CL
{
	static readonly ConVar cl_showevents = new("cl_showevents", "0", FCvar.Cheat, "Print event firing info in the console");

	public void DescribeEvent(int slot, EventInfo evnt, ReadOnlySpan<char> eventname) {
		if (cl_showevents.GetInt() != 2 || eventname.IsEmpty)
			return;

		DevMsg($"{slot:D2} {cl.GetTime():F3}f {eventname.SliceNullTerminatedString(),20} {Protocol.Bits2Bytes(evnt.Bits):D3} bytes\n");
	}

	public void ParseEventDelta(byte[] rawData, object toData, RecvTable recvTable, uint readBufferSize) {
		bf_read fromBuf = new(rawData, readBufferSize);

		RecvTable.DecodeZeros(recvTable, toData, -1);
		RecvTable.Decode(recvTable, toData, fromBuf, -1);

		Assert(!fromBuf.Overflowed);
	}

	public void FireEvents() {
		if (!cl.IsActive()) {
			cl.Events.Clear();
			return;
		}

		int i = 0;
		LinkedListNode<EventInfo>? node = cl.Events.First;
		for (; node != null; i++) {
			LinkedListNode<EventInfo>? next = node.Next;

			EventInfo ei = node.Value;
			if (ei.ClassID == 0) {
				cl.Events.Remove(node);
				node = next;
				continue;
			}

			if (ei.FireDelay != 0 && ei.FireDelay > cl.GetTime()) {
				node = next;
				continue;
			}

			bool success = false;

			Assert(ei.ClientClass != null);

			if (ei.ClientClass!.CreateEventFn != null) {
				IClientNetworkable? pCE = ei.ClientClass.CreateEventFn();
				if (pCE != null) {
					pCE.PreDataUpdate(DataUpdateType.Created);

					uint buffer_size = (uint)PAD_NUMBER(Protocol.Bits2Bytes(ei.Bits), 4);
					ParseEventDelta(ei.Data!, pCE.GetDataTableBasePtr(), ei.ClientClass.RecvTable!, buffer_size);

					pCE.PostDataUpdate(DataUpdateType.Created);

					DescribeEvent(i, ei, ei.ClientClass.NetworkName);

					success = true;
				}
			}

			if (!success)
				ConDMsg($"Failed to execute event for classId {ei.ClassID - 1} ({ei.ClientClass?.NetworkName ?? "<NULL>"})\n");

			cl.Events.Remove(node);
			node = next;
		}
	}
}
