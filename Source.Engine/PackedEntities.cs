using Source.Common;
using Source.Common.Bitbuffers;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Engine.Server;

using System.Runtime.Intrinsics.Arm;

namespace Source.Engine;

struct PackWork
{
	public int Id;
	public Edict Edict;
	public FrameSnapshot Snapshot;
	public static void Process(PackWork item) {
		throw new NotImplementedException();
	}
}

static class PackedEntities
{
	static readonly ConVar sv_debugmanualmode = new("sv_debugmanualmode", "0", FCvar.None, "Make sure entities correctly report whether or not their network data has changed.");
	static readonly ConVar sv_parallel_packentities = new("sv_parallel_packentities", "1", FCvar.None);

	static bool EnsurePrivateData(Edict edict) {
		if (edict.GetUnknown() != null)
			return true;
		else {
			// Host.Error($"SV_EnsurePrivateData: pEdict->pvPrivateData==NULL (ent {edict.EdictIndex}).\n");
			return false;
		}
	}

	static void EnsureInstanceBasline(ServerClass serverClass, int Edict, ReadOnlySpan<byte> data, int bytes) {
		throw new NotImplementedException();
	}

	static void PackEntity(int edictId, Edict edict, ServerClass serverClass, FrameSnapshot snapshot) {
		throw new NotImplementedException();
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

	static void NetworkBackDoor(int clientCount, GameClient clients, FrameSnapshot snapshot) {
		throw new NotImplementedException();
	}

	static void Normal(int clientCount, GameClient clients, FrameSnapshot snapshot) {
		throw new NotImplementedException();
	}

	static void ComputeClientPacks(int clientCount, GameClient clients, FrameSnapshot snapshot) {
		throw new NotImplementedException();
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