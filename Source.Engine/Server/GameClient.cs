global using static Source.Engine.Server.SvClientConvars;

using Source.Common;
using Source.Common.Bitbuffers;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Networking;

namespace Source.Engine.Server;

public static class SvClientConvars
{
	public static readonly ConVar sv_timeout = new("sv_timeout", "65", 0, "After this many seconds without a message from a client, the client is dropped");
	public static readonly ConVar sv_maxrate = new("sv_maxrate", "0", FCvar.Replicated, "Max bandwidth rate allowed on server, 0 == unlimited");
	public static readonly ConVar sv_minrate = new("sv_minrate", "3500", FCvar.Replicated, "Min bandwidth rate allowed on server, 0 == unlimited");
	public static readonly ConVar sv_maxupdaterate = new("sv_maxupdaterate", "66", FCvar.Replicated, "Maximum updates per second that the server will allow");
	public static readonly ConVar sv_minupdaterate = new("sv_minupdaterate", "10", FCvar.Replicated, "Minimum updates per second that the server will allow");
	public static readonly ConVar sv_stressbots = new("sv_stressbots", "0", 0, "If set to 1, the server calculates data and fills packets to bots. Used for perf testing.");
	public static readonly ConVar sv_allowdownload = new("sv_allowdownload", "1", 0, "Allow clients to download files");
	public static readonly ConVar sv_allowupload = new("sv_allowupload", "1", 0, "Allow clients to upload customizations files");
	public static readonly ConVar sv_sendtables = new("sv_sendtables", "0", FCvar.DevelopmentOnly, "Force full sendtable sending path.");
}

/// <summary>
/// Represents a player client in a game server
/// </summary>
public class GameClient : BaseClient
{
	public GameClient(int slot, BaseServer server) {
		Clear();

		ClientSlot = slot;
		EntityIndex = slot + 1;
		Server = server;
		CurrentFrame = null;
		IsInReplayMode = false;
	}
	public bool VoiceLoopback;
	public AbsolutePlayerLimitBitVec VoiceStreams;
	public AbsolutePlayerLimitBitVec VoiceProximity;
	public int LastMovementTick;
	public int SoundSequence;
	public Edict Edict = null!;
	public readonly List<SoundInfo> Sounds = [];
	public Edict? ViewEntity;
	public ClientFrame? CurrentFrame;
	// public CheckTransmitInfo PackInfo;
	public bool IsInReplayMode;
	// public CheckTransmitInfo PrevPackInfo;     
	public MaxEdictsBitVec PrevTransmitEdict;

	protected override bool ProcessClientInfo(CLC_ClientInfo msg) {
		base.ProcessClientInfo(msg);

		if (HLTV) {

		}

		if (sv_allowupload.GetBool()) {

		}

		return true;
	}

	protected override bool ProcessMove(CLC_Move msg) {
		if (!IsActive())
			return true;


		return true;
	}

	// bool ProcessVoiceData(CLC_VoiceData msg) { }

	// bool ProcessCmdKeyValues(CLC_CmdKeyValues msg) { }

	// bool ProcessRespondCvarValue(CLC_RespondCvarValue msg) { }

	// bool ProcessFileCRCCheck(CLC_FileCRCCheck msg) { }

	// bool ProcessFileMD5Check(CLC_FileMD5Check msg) { }

	// bool ProcessSaveReplay(CLC_SaveReplay pMsg) { }

	// void DownloadCustomizations() { }

	// void Connect(ReadOnlySpan<char> name, int nUserID, INetChannel netChannel, bool bFakePlayer, int clientChallenge) { }

	void SetupPackInfo(FrameSnapshot pSnapshot) { }

	void SetupPrevPackInfo() { }

	// void SetRate(int nRate, bool bForce) { }

	// void SetUpdateRate(int udpaterate, bool bForce) { }

	void UpdateUserSettings() { }

	// bool ProcessIncomingLogo(ReadOnlySpan<char> filename) { }

	// bool IsHearingClient(int index) { }

	// bool IsProximityHearingClient(int index) { }

	// void Inactivate() { }

	// bool UpdateAcknowledgedFramecount(int tick) { }

	// void Clear() { }

	public override void Reconnect() {
		sv.RemoveClientFromGame(this);
		base.Reconnect();
	}

	// void Disconnect(ReadOnlySpan<char> fmt) { }

	// bool SetSignonState(int state, int spawncount) { }

	// void SendSound(SoundInfo sound, bool isReliable) { }

	// void WriteGameSounds(bf_write buf) { }

	// int FillSoundsMessage(SVC_Sounds msg) { }

	// bool CheckConnect() { }

	// protected override void ActivatePlayer() { }

	protected override bool SendSignonData() {
		bool clientHasDirrentTables = false;

		if (false) {

		}
		else {
			SVC_ClassInfo msg = new() {
				NumServerClasses = Server.ServerClasses,
				CreateOnClient = true
			};
			SendNetMsg(msg);
		}

		if (!base.SendSignonData())
			return false;

		SoundSequence = 1;

		return true;
	}

	protected override void SpawnPlayer() {
		if (sv.LoadGame)
			sv.SetPaused(false);
		else {
			// Assert(SV.ServerGameEnts);
			Edict.InitializeEntityDLLFields();
		}

		EntityIndex = ClientSlot + 1;
		IsInReplayMode = false;

		SVC_SetView msg = new() {
			EntityIndex = EntityIndex
		};
		SendNetMsg(msg);
	}

	// ClientFrame GetDeltaFrame(int tick) { }

	// void WriteViewAngleUpdate() { }

	// bool IsEngineClientCommand(in TokenizedCommand args) { }

	// bool SendNetMsg(INetMessage msg, bool forceReliable) { }

	// bool ExecuteStringCommand(ReadOnlySpan<char> pCommandString) { }

	// void SendSnapshot(ClientFrame pFrame) { }

	// bool ShouldSendMessages() { }

	// void FileReceived(ReadOnlySpan<char> fileName, uint transferID) { }

	// void FileRequested(ReadOnlySpan<char> fileName, uint transferID) { }

	// void FileDenied(ReadOnlySpan<char> fileName, uint transferID) { }

	// void FileSent(ReadOnlySpan<char> fileName, uint transferID) { }

	public override void PacketStart(int incoming_sequence, int outgoing_acknowledged) {
		LastMovementTick = (int)(sv.TickCount - 1);
		// host_client = this;
		ReceivedPacket = true;
	}

	public override void PacketEnd() => serverGlobalVariables.FrameTime = host_state.IntervalPerTick;

	// void ConnectionClosing(ReadOnlySpan<char> reason) { }

	// void ConnectionCrashed(ReadOnlySpan<char> reason) { }

	// ClientFrame GetSendFrame() { }

	// bool IgnoreTempEntity(EventInfo evnt) { }

	// CheckTransmitInfo GetPrevPackInfo() { }
}
