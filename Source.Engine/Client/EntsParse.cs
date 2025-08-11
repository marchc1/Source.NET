using Source.Common.Bitbuffers;
using Source.Common.Client;
using Source.Common.Entity;
using Source.Common.Networking;
using Source.Common.Networking.DataTable;
using static Source.Dbg;

namespace Source.Engine.Client;

// RaphaelIT7: We use static functions here since we don't save anything important at all, we just process a lot of shit.
public class EntsParse
{
	public static RecvTable? GetEntRecvTable(BaseClientState state, int Entity)
	{
		IClientNetworkable? networkable = state.ClientEntityList.GetClientNetworkable(Entity);
		if (networkable != null)
			return networkable.GetClientClass().pRecvTable;
		else
			return null;
	}

	public static void ParseDeltaHeader(CEntityReadInfo u)
	{
		u.UpdateFlags = FHDR.ZERO;

		u.NewEntity = u.HeaderBase + 1 + (int)u.Buf.ReadUBitVar();
		u.HeaderBase = u.NewEntity;

		if (u.Buf.ReadOneBit() == 0)
		{
			if (u.Buf.ReadOneBit() != 0)
			{
				u.UpdateFlags |= FHDR.ENTERPVS;
			}
		} else {
			u.UpdateFlags |= FHDR.LEAVEPVS;

			if (u.Buf.ReadOneBit() != 0)
			{
				u.UpdateFlags |= FHDR.DELETE;
			}
		}
	}

	public static bool DetermineUpdateType(CEntityReadInfo u)
	{
		if (!u.IsEntity || (u.NewEntity > u.OldEntity))
		{
			if ((u.From == null) || (u.OldEntity > u.From.LastEntity))
			{
				u.UpdateType = UpdateType.Finished;
				return false;
			}

			u.UpdateType = UpdateType.PreserveEnt;
		} else {
			if ((u.UpdateFlags & FHDR.ENTERPVS) != 0)
			{
				u.UpdateType = UpdateType.EnterPVS;
			} else if ((u.UpdateFlags & FHDR.LEAVEPVS) != 0) {
				u.UpdateType = UpdateType.LeavePVS;
			} else {
				u.UpdateType = UpdateType.DeltaEnt;
			}
		}

		return true;
	}

	public static void ReadEnterPVS(BaseClientState state, CEntityReadInfo u)
	{
		
	}

	public static void ReadLeavePVS(BaseClientState state, CEntityReadInfo u)
	{
		
	}

	public static void ReadDeltaPVS(BaseClientState state, CEntityReadInfo u)
	{
		
	}

	public static void ReadPreservePVS(BaseClientState state, CEntityReadInfo u)
	{
		
	}

	public static void CopyNewEntity(BaseClientState state, CEntityReadInfo u, int Class, int SerialNum)
	{
		if (u.NewEntity < 0 || u.NewEntity > Constants.MAX_EDICTS)
		{
			Host_Error("CL_CopyNewEntity: u.m_nNewEntity < 0 || m_nNewEntity >= MAX_EDICTS");
			return;
		}

		if (Class >= state.ServerClasses)
		{
			Host_Error("CL_CopyNewEntity: invalid class index (%d).\n", Class);
			return;
		}

		ClientClass clientClass = state.ServerClassInfo[Class].ClientClass;
		IClientNetworkable? ent = state.ClientEntityList.GetClientNetworkable(u.NewEntity);
		if (ent != null)
		{
			if (ent.GetIClientUnknown()?.GetRefEHandle().GetSerialNumber() != SerialNum)
			{
				DeleteDLLEntity(state, u.NewEntity, "CopyNewEntity");
				ent = null;
			}
		}

		bool NewlyCreated = false;
		if (ent == null)
		{
			ent = CreateDLLEntity(state, u.NewEntity, Class, SerialNum);
			if (ent == null)
			{
				string NetworkName = (state.ServerClassInfo[Class].ClientClass != null) ? state.ServerClassInfo[Class].ClientClass.NetworkName : "";
				Host_Error("CL_ParsePacketEntities:  Error creating entity %s(%i)\n", NetworkName, u.NewEntity);
				return;
			}

			NewlyCreated = true;
		}

		int startBit = u.Buf.BitsRead;
		DataUpdateType updateType = NewlyCreated ? DataUpdateType.CREATED : DataUpdateType.DATATABLE_CHANGED;
		ent.PreDataUpdate(updateType);

		byte[]? FromData = null;
		int FromBits = 0;

		PackedEntity? baseline = u.AsDelta ? state.GetEntityBaseline(u.Baseline, u.NewEntity) : null;
		if (baseline != null && baseline.ClientClass == clientClass)
		{
			FromData = baseline.GetData();
			FromBits = baseline.GetNumBits();
		} else {
			if (!state.GetClassBaseline(Class, out FromData, out FromBits))
			{
				Error("CL_CopyNewEntity: GetClassBaseline(%d) failed.", Class);
				return;
			}

			FromBits = 8;
		}

		bf_read fromBuf = new("CL_CopyNewEntity->fromBuf", FromData, Net.Bits2Bytes(FromBits), FromBits);
		RecvTable? RecvTable = GetEntRecvTable(state, u.NewEntity);

		if (RecvTable == null)
		{
			Host_Error("CL_ParseDelta: invalid recv table for ent %d.\n", u.NewEntity);
			return;
		}

		if (u.UpdateBaselines)
		{
			byte[] packedData = new byte[Constants.MAX_PACKEDENTITY_DATA];
			bf_write writeBuf = new("CL_CopyNewEntity->newBuf", packedData, Constants.MAX_PACKEDENTITY_DATA);

			int[] changedProps;
			DTRecv.MergeDeltas(RecvTable, fromBuf, u.Buf, writeBuf, -1, out changedProps, true );

			// set the other baseline
			state.SetEntityBaseline((u.Baseline==0) ? 1 : 0, clientClass, u.NewEntity, packedData, writeBuf.BytesWritten);

			fromBuf.StartReading(packedData, writeBuf.BytesWritten);

			DTRecv.Decode(RecvTable, ent.GetDataTableBasePtr(), fromBuf, u.NewEntity, false);
		}
	}

	public static void DeleteDLLEntity(BaseClientState state, int Entity, string Reason, bool OnRecreatingAllEntities = false)
	{
		IClientNetworkable? Networkable = state.ClientEntityList.GetClientNetworkable(Entity);

		if (Networkable != null)
		{
			ClientClass? clientClass = Networkable.GetClientClass();

			if (clientClass != null)
			{
				state.EntityReport.RecordDeleteEntity(Entity, clientClass);
			}

			if (OnRecreatingAllEntities)
			{
				Networkable.SetDestroyedOnRecreateEntities();
			}

			Networkable.Release();
		}
	}

	public static IClientNetworkable? CreateDLLEntity(BaseClientState state, int Entity, int Class, int SerialNum)
	{
		IClientNetworkable? Networkable = state.ClientEntityList.GetClientNetworkable(Entity);

		ClientClass? clientClass = null;
		if ((clientClass = state.ServerClassInfo[Class].ClientClass) != null)
		{
			state.EntityReport.RecordAddEntity(Entity);

			if (!state.IsActive())
			{
				// COM_TimestampedLog( "cl:  create '%s'", clientClass.GetName() );
			}

			return clientClass.CreateFn(Entity, SerialNum);
		}

		return null;
	}

	public static bool ProcessPacketEntities(ClientState state, svc_PacketEntities msg)
	{
		ClientFrame newFrame = state.FrameManager.AllocateFrame();
		newFrame.Init(state.GetServerTickCount());
		ClientFrame? oldFrame = null;

		state.EntityReport.renderTime = state.Host.RealTime; // RaphaelIT7: I hate this... Anyways.

		if (ClientState.cl_flushentitypacket.GetInt() > 0)
		{
			Warning("Forced by cvar\n");
			ClientState.cl_flushentitypacket.SetValue(ClientState.cl_flushentitypacket.GetInt() - 1);
			return false;
		}

		if (msg.IsDelta)
		{
			int DeltaTicks = state.GetServerTickCount() - msg.DeltaFrom;
			double DeltaSeconds = state.Host.TicksToTime(DeltaTicks);

			// RaphaelIT7: We don't need any of that cl_debug_player_perf stuff

			if (state.GetServerTickCount() == msg.DeltaFrom)
			{
				Host_Error("Update self-referencing, connection dropped.\n");
				return false;
			}

			oldFrame = state.FrameManager.GetClientFrame(msg.DeltaFrom);

			if (oldFrame == null)
			{
				Warning("Update detla not found.\n");
				return false;
			}
		} else {
			// Clear out the client's entity states..
			for (int i=0; i <= state.ClientEntityList.GetHighestEntityIndex(); i++)
			{
				DeleteDLLEntity(state, i, "ProcessPacketEntities", true);
			}
		}

		// RaphaelIT7 - ToDo: Fix this mess
		state.Host.CL.FrameStageNotify(ClientFrameStage.NetUpdateStart);

		if (msg.UpdateBaseline)
		{
			int UpdateBaseline = (msg.Baseline == 0) ? 1 : 0;
			state.CopyEntityBaseline(msg.Baseline, UpdateBaseline);

			clc_BaselineAck ackMsg = new clc_BaselineAck(state.GetServerTickCount(), msg.Baseline);
			state.NetChannel.SendNetMsg(ackMsg);
		}

		CEntityReadInfo u = new();
		u.Buf = msg.DataIn;
		u.From = oldFrame;
		u.To = newFrame;
		u.AsDelta = msg.IsDelta;
		u.HeaderCount = msg.UpdatedEntries;
		u.Baseline = msg.Baseline;
		u.UpdateBaselines = msg.UpdateBaseline;

		state.ReadPacketEntities(u);

		return true;
	}
}