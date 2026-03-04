using Source.Common;
using Source.Common.Bitbuffers;
using Source.Common.Commands;
using Source.Common.Engine;

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;

namespace Source.Engine.Server;

struct PackWork
{
	public int Id;
	public Edict Edict;
	public FrameSnapshot Snapshot;
	public static void Process(PackWork item) => PackedEntities.PackEntity(item.Id, item.Edict, item.Snapshot.Entities![item.Id].Class!, item.Snapshot);
}

static class PackedEntities
{
	static readonly ConVar sv_debugmanualmode = new("sv_debugmanualmode", "0", FCvar.None, "Make sure entities correctly report whether or not their network data has changed.");
	static readonly ConVar sv_parallel_packentities = new("sv_parallel_packentities", "0", FCvar.None); // SDN: Defaulted to 0 for now ~Callum

	static bool EnsurePrivateData(Edict edict) {
		if (edict.GetUnknown() != null)
			return true;
		else {
			// Host.Error($"SV_EnsurePrivateData: pEdict.pvPrivateData==NULL (ent {edict.EdictIndex}).\n");
			return false;
		}
	}

	static void EnsureInstanceBasline(ServerClass serverClass, int edictId, ReadOnlySpan<byte> data, int bytes) {
		throw new NotImplementedException();
	}

	public static void PackEntity(int edictId, Edict edict, ServerClass serverClass, FrameSnapshot snapshot) {
		Assert(edictId < snapshot.NumEntities);

#if false // TODO TODO SendTable, ChangeFrameList, SendProxyRecipients, SV.EnsureInstanceBaseline, AllocChangeFrameList

		int serialNum = snapshot.Entities![edictId].SerialNumber;

		// Check to see if this entity specifies its changes.
		// If so, then try to early out making the fullpack
		bool usedPrev = false;
		if (!edict.HasStateChanged()) {
			// Now this may not work if we didn't previously send a packet;
			// if not, then we gotta compute it
			usedPrev = framesnapshotmanager.UsePreviouslySentPacket(snapshot, edictId, serialNum);
		}

		if (usedPrev && !sv_debugmanualmode.GetBool()) {
			edict.ClearStateChanged();
			return;
		}

		// First encode the entity's data.
		byte[] packedData = ArrayPool<byte>.Shared.Rent(Constants.MAX_PACKEDENTITY_DATA);
		bf_write writeBuf = new(packedData, Constants.MAX_PACKEDENTITY_DATA);

		SendTable sendTable = serverClass.Table;

		// (avoid constructor overhead).
		Span<byte> tempData = stackalloc byte[Unsafe.SizeOf<SendProxyRecipients>() * Constants.MAX_DATATABLE_PROXIES];
		Span<SendProxyRecipients> recip = MemoryMarshal.Cast<byte, SendProxyRecipients>(tempData);

		if (!SendTable_Encode(sendTable, edict.GetUnknown(), &writeBuf, edictId, &recip, false)) {
			Host_Error("SV_PackEntity: SendTable_Encode returned false (ent %d).\n", edictId);
		}

		SV_EnsureInstanceBaseline(serverClass, edictId, packedData, writeBuf.BytesWritten);

		int nFlatProps = SendTable_GetNumFlatProps(sendTable);
		IChangeFrameList? changeFrame = null;

		// If this entity was previously in there, then it should have a valid IChangeFrameList
		// which we can delta against to figure out which properties have changed.
		//
		// If not, then we want to setup a new IChangeFrameList.
		PackedEntity? prevFrame = framesnapshotmanager.GetPreviouslySentPacket(edictId, snapshot.Entities[edictId].SerialNumber);
		if (prevFrame != null) {
			// Calculate a delta.
			Assert(!prevFrame.IsCompressed());

			int[] deltaProps = new int[Constants.MAX_DATATABLE_PROPS];

			int changes = SendTable_CalcDelta(
					sendTable,
					prevFrame.GetData(), prevFrame.GetNumBits(),
					packedData, writeBuf.GetNumBitsWritten(),

					deltaProps,
					ARRAYSIZE(deltaProps),

					edictId);

			// If it's non-manual-mode, but we detect that there are no changes here, then just
			// use the previous snapshot if it's available (as though the entity were manual mode).
			// It would be interesting to hook here and see how many non-manual-mode entities
			// are winding up with no changes.
			if (changes == 0) {
				if (prevFrame.CompareRecipients(recip)) {
					if (framesnapshotmanager.UsePreviouslySentPacket(snapshot, edictId, serialNum)) {
						edict.ClearStateChanged();
						return;
					}
				}
			}
			else {
				if (!edict.HasStateChanged()) {
					for (int iDeltaProp = 0; iDeltaProp < changes; iDeltaProp++) {
						Assert(sendTable.Precalc);
						Assert(deltaProps[iDeltaProp] < sendTable.Precalc!.GetNumProps());

						SendProp prop = sendTable.Precalc.GetProp(deltaProps[iDeltaProp])!;
						// If a field changed, but it changed because it encoded against tickcount,
						//   then it's just like the entity changed the underlying field, not an error, that is.
						if ((prop.GetFlags() & PropFlags.EncodedAgainstTickCount) != 0)
							continue;

						Msg("Entity %d (class '%s') reported ENTITY_CHANGE_NONE but '%s' changed.\n",
								edictId,
								edict.GetClassName(),
								prop.GetName());
					}
				}
			}

			if (false /*hltv && hltv.IsActive()*/) {
				// in HLTV or Replay mode every PackedEntity keeps it's own ChangeFrameList
				// we just copy the ChangeFrameList from prev frame and update it
				changeFrame = prevFrame.GetChangeFrameList();
				changeFrame = changeFrame.Copy(); // allocs and copies ChangeFrameList
			}
			else {
				// Ok, now snag the changeframe from the previous frame and update the 'last frame changed'
				// for the properties in the delta.
				changeFrame = prevFrame.SnagChangeFrameList();
			}

			ErrorIfNot(changeFrame, ("SV_PackEntity: SnagChangeFrameList returned null"));
			ErrorIfNot(changeFrame.GetNumProps() == nFlatProps, ("SV_PackEntity: SnagChangeFrameList mismatched number of props[%d vs %d]", nFlatProps, changeFrame.GetNumProps()));

			changeFrame.SetChangeTick(deltaProps, changes, snapshot.TickCount);
		}
		else {
			// Ok, init the change frames for the first time.
			changeFrame = AllocChangeFrameList(nFlatProps, snapshot.TickCount);
		}

		// Now make a PackedEntity and store the new packed data in there.
		PackedEntity packedEntity = framesnapshotmanager.CreatePackedEntity(snapshot, edictId);
		packedEntity.SetChangeFrameList(changeFrame);
		packedEntity.SetServerAndClientClass(serverClass, null);
		packedEntity.AllocAndCopyPadded(packedData);
		packedEntity.SetRecipients(recip);

		edict.ClearStateChanged();
#endif
	}

	static void FillHLTVData(FrameSnapshot snapshot, Edict edict, int validEdict) {
		throw new NotImplementedException();
	}

	static void FillReplayData(FrameSnapshot snapshot, Edict edict, int validEdict) {
		throw new NotImplementedException();
	}

	static SendTable GetEntSendTable(Edict edict) {
		throw new NotImplementedException();
	}

	static void NetworkBackDoor(int clientCount, GameClient[] clients, FrameSnapshot snapshot) {
		throw new NotImplementedException();
	}

	static void Normal(int clientCount, GameClient[] clients, FrameSnapshot snapshot) {
		Assert(snapshot.NumValidEntities >= 0 && snapshot.NumValidEntities <= Constants.MAX_EDICTS);

		List<PackWork> workItems = [];

		// check for all active entities, if they are seen by at least on client, if
		// so, bit pack them
		for (int iValidEdict = 0; iValidEdict < snapshot.NumValidEntities; ++iValidEdict) {
			int index = snapshot.ValidEntities![iValidEdict];

			Assert(index < snapshot.NumEntities);

			Edict edict = sv.Edicts![index];

			// if HLTV is running save PVS info for each entity
			FillHLTVData(snapshot, edict, iValidEdict);

			// if Replay is running save PVS info for each entity
			FillReplayData(snapshot, edict, iValidEdict);

			// Check to see if the entity changed this frame...
			// ServerDTI_RegisterNetworkStateChange( sendTable, ent.m_bStateChanged );

			for (int iClient = 0; iClient < clientCount; ++iClient) {
				// entities is seen by at least this client, pack it and exit loop
				GameClient client = clients[iClient]; // update variables cl, pInfo, frame for current client
				ClientFrame? frame = client.CurrentFrame;

				if (frame!.TransmitEntity.Get(index) != 0) {
					PackWork w;
					w.Id = index;
					w.Edict = edict;
					w.Snapshot = snapshot;

					workItems.Add(w);
					break;
				}
			}
		}

		if (sv_parallel_packentities.GetBool()) {
			// 	ParallelProcess("PackWork_t::Process", workItems.Base(), workItems.Count(), &PackWork_t::Process);
			Debugger.Break();
		}
		else {
			int c = workItems.Count();
			for (int i = 0; i < c; ++i) {
				PackWork w = workItems[i];
				PackEntity(w.Id, w.Edict, w.Snapshot.Entities![w.Id].Class!, w.Snapshot);
			}
		}

		// InvalidateSharedEdictChangeInfos(); todo
	}

	// todo call from CGameServer::SendClientMessages
	static void ComputeClientPacks(int clientCount, GameClient[] clients, FrameSnapshot snapshot) {
		for (int i = 0; i < clientCount; i++) {
			// todo transmit info
		}

		if (false /* g_LocalNetworkBackdoor */) {


		}
		else
			Normal(clientCount, clients, snapshot);
	}

	static void MaybeWriteSendTable(SendTable table, bf_write buffer, bool needDecover) {
		throw new NotImplementedException();
	}

	static void MaybeWriteSendTable_R(SendTable table, bf_write buffer) {
		throw new NotImplementedException();
	}

	static void WriteSendTables(ServerClass serverClass, bf_write buffer) {
		throw new NotImplementedException();
	}

	static void ComputeClassInfosCRC(Crc32 crc) {
		throw new NotImplementedException();
	}

	static void AssignClassIds() {
		throw new NotImplementedException();
	}

	static void WriteClassInfos(ServerClass clases, bf_write buffer) {
		throw new NotImplementedException();
	}

	static ReadOnlySpan<char> GetOjectClassName(int objectId) {
		throw new NotImplementedException();
	}
}