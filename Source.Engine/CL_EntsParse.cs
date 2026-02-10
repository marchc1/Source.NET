using Source.Common.Client;
using Source.Common.Networking;

using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Engine;

public partial class CL
{
	public bool ProcessPacketEntities(SVC_PacketEntities entmsg) {
		ClientFrame newFrame = cl.AllocateFrame();
		newFrame.Init(cl.GetServerTickCount());
		ClientFrame? oldFrame = null;

		if (entmsg.IsDelta) {
			int deltaTicks = cl.GetServerTickCount() - entmsg.DeltaFrom;

			if (cl.GetServerTickCount() == entmsg.DeltaFrom) {
				Host.Error("Update self-referencing, connection dropped.\n");
				return false;
			}

			oldFrame = cl.GetClientFrame(entmsg.DeltaFrom);

			if (oldFrame == null) {
				FlushEntityPacket(newFrame, "Update delta not found.\n");
				return false;
			}
		}
		else {
			for (int i = 0; i <= ClientDLL.EntityList.GetHighestEntityIndex(); i++) {
				DeleteDLLEntity(i, "ProcessPacketEntities", true);
			}
		}

		ClientDLL.FrameStageNotify(ClientFrameStage.NetUpdateStart);

		PropsDecoded = 0;

		Assert(entmsg.Baseline >= 0 && entmsg.Baseline < 2);

		if (entmsg.UpdateBaseline) {
			int updateBaseline = (entmsg.Baseline == 0) ? 1 : 0;
			cl.CopyEntityBaseline(entmsg.Baseline, updateBaseline);

			var msg = new CLC_BaselineAck(cl.GetServerTickCount(), entmsg.Baseline);
			cl.NetChannel!.SendNetMsg(msg, true);
		}

		EntityReadInfo readInfo = EntityReadInfo.Alloc();
		readInfo.Buf = entmsg.DataIn;
		readInfo.From = oldFrame;
		readInfo.To = newFrame;
		readInfo.AsDelta = entmsg.IsDelta;
		readInfo.HeaderCount = entmsg.UpdatedEntries;
		readInfo.Baseline = entmsg.Baseline;
		readInfo.UpdateBaselines = entmsg.UpdateBaseline;

		cl.ReadPacketEntities(readInfo);

		ClientDLL.FrameStageNotify(ClientFrameStage.NetUpdatePostDataUpdateStart);

		CallPostDataUpdates(readInfo);

		ClientDLL.FrameStageNotify(ClientFrameStage.NetUpdatePostDataUpdateEnd);

		MarkEntitiesOutOfPVS(ref newFrame.TransmitEntity);

		cl.NetChannel?.UpdateMessageStats(NetChannelGroup.LocalPlayer, readInfo.LocalPlayerBits);
		cl.NetChannel?.UpdateMessageStats(NetChannelGroup.OtherPlayers, readInfo.OtherPlayerBits);
		cl.NetChannel?.UpdateMessageStats(NetChannelGroup.Entities, -(readInfo.LocalPlayerBits + readInfo.OtherPlayerBits));

		cl.DeleteClientFrames(entmsg.DeltaFrom);

		if (ClientFrame.MAX_CLIENT_FRAMES < cl.AddClientFrame(newFrame))
			DevMsg(1, "CL.ProcessPacketEntities: frame window too big (>%i)\n", ClientFrame.MAX_CLIENT_FRAMES);

		ClientDLL.FrameStageNotify(ClientFrameStage.NetUpdateEnd);

		EntityReadInfo.Free(readInfo);

		return true;
	}
}
