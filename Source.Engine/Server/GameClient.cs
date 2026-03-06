global using static Source.Engine.Server.SvClientConvars;

using Source.Common;
using Source.Common.Bitbuffers;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Networking;
using Source.GUI.Controls;

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
			HLTV = false;
			Disconnect("ProcessClientInfo: SourceTV can not connect to game directly.\n");
			return false;
		}

		if (sv_allowupload.GetBool())
			DownloadCustomizations();

		return true;
	}

	protected override bool ProcessMove(CLC_Move m) {
		if (!IsActive())
			return true;

		if (LastMovementTick == sv.TickCount) {
			// Only one movement command per frame, someone is cheating.
			return true;
		}

		LastMovementTick = (int)sv.TickCount;

		int totalCmds = m.NewCommands + m.BackupCommands;
		int netDrop = NetChannel.GetDropNumber();

		bool ignore = !sv.IsActive();
#if SWDS
	bool paused = sv.IsPaused();
#else
		bool paused = sv.IsPaused() || (!sv.IsMultiplayer() && false /*Con_IsVisible todo*/);
#endif

		serverGlobalVariables.CurTime = sv.GetTime();
		serverGlobalVariables.FrameTime = host_state.IntervalPerTick;

		int startBit = m.DataIn.BitsRead;

		SV.ServerGameClients!.ProcessUsercmds(Edict, m.DataIn, m.NewCommands, totalCmds, netDrop, ignore, paused);

		if (m.DataIn.Overflowed) {
			Disconnect("ProcessUsercmds:  Overflowed reading usercmd data (check sending and receiving code for mismatches)!\n");
			return false;
		}

		int endBit = m.DataIn.BitsRead;
		if (m.Length != (endBit - startBit)) {
			Disconnect("ProcessUsercmds:  Incorrect reading frame (check sending and receiving code for mismatches)!\n");
			return false;
		}

		return true;
	}

	// bool ProcessVoiceData(CLC_VoiceData msg) { }

	// bool ProcessCmdKeyValues(CLC_CmdKeyValues msg) {
	// 	SV.ServerGameClients.ClientCommandKeyValues(Edict, msg.KeyValues);
	// 	return true;
	// }

	// bool ProcessRespondCvarValue(CLC_RespondCvarValue msg) { }

	// bool ProcessFileCRCCheck(CLC_FileCRCCheck msg) { }

	// bool ProcessFileMD5Check(CLC_FileMD5Check msg) { }

	// bool ProcessSaveReplay(CLC_SaveReplay pMsg) { }

	void DownloadCustomizations() { }

	public override void Connect(ReadOnlySpan<char> name, int userID, INetChannel netChannel, bool fakePlayer, int clientChallenge) {
		base.Connect(name, userID, netChannel, fakePlayer, clientChallenge);

		Edict = sv.Edicts![EntityIndex];

		// packinfo todo

		IGameEvent? evnt = gameEventManager.CreateEvent("player_connect");
		if (evnt != null) {
			evnt.SetInt("userid", GetUserID());
			evnt.SetInt("index", ClientSlot);
			evnt.SetString("name", name);
			evnt.SetString("networkid", GetNetworkIDString());
			evnt.SetString("address", netChannel != null ? netChannel.GetAddress() : "none");
			evnt.SetInt("bot", fakePlayer ? 1 : 0);
			gameEventManager.FireEvent(evnt);
		}

		evnt = gameEventManager.CreateEvent("player_connect_client");
		if (evnt != null) {
			evnt.SetInt("userid", GetUserID());
			evnt.SetInt("index", ClientSlot);
			evnt.SetString("name", name);
			evnt.SetString("networkid", GetNetworkIDString());
			evnt.SetInt("bot", fakePlayer ? 1 : 0);
			gameEventManager.FireEvent(evnt);
		}
	}

	public void SetupPackInfo(FrameSnapshot snapshot) {

		CurrentFrame = cl.AllocateFrame();
		CurrentFrame.Init(snapshot);

		int maxFrames = MAX_CLIENT_FRAMES;
		if (maxFrames < cl.AddClientFrame(CurrentFrame))
			cl.RemoveOldestFrame();

	}

	void SetupPrevPackInfo() { }

	// void SetRate(int nRate, bool force) { }

	// void SetUpdateRate(int udpaterate, bool force) { }

	void UpdateUserSettings() { }

	// bool IsHearingClient(int index) { }

	// bool IsProximityHearingClient(int index) { }

	public override void Inactivate() {
		if (Edict != null && !Edict.IsFree())
			Server.RemoveClientFromGame(this);

		if (IsHLTV()) {

		}

		base.Inactivate();

		Sounds.Clear();
		VoiceStreams.ClearAll();
		VoiceProximity.ClearAll();
	}

	protected override bool UpdateAcknowledgedFramecount(int tick) {
		if (tick != DeltaTick) {
			int removeTick = tick;

			if (removeTick > 0)
				cl.DeleteClientFrames(removeTick);
		}

		return base.UpdateAcknowledgedFramecount(tick);
	}

	public override void Clear() {
		if (HLTV) {

		}

		if (Replay) {

		}

		base.Clear();

		cl.DeleteClientFrames(-1);

		Sounds.Clear();
		VoiceStreams.ClearAll();
		VoiceProximity.ClearAll();
		Edict = null!;
		ViewEntity = null;
		VoiceLoopback = false;
		LastMovementTick = 0;
		SoundSequence = 0;
	}

	public override void Reconnect() {
		sv.RemoveClientFromGame(this);
		base.Reconnect();
	}

	// void Disconnect(ReadOnlySpan<char> fmt) { }

	protected override bool SetSignOnState(SignOnState state, int spawncount) {
		if (state == SignOnState.Connected) {
			if (!CheckConnect())
				return false;

			NetChannel!.SetTimeout(Source.Common.Networking.NetChannel.SIGNON_TIME_OUT);
			NetChannel.SetFileTransmissionMode(false);
			NetChannel.SetMaxBufferSize(true, Protocol.MAX_PAYLOAD);
		}
		else if (state == SignOnState.New) {
			if (!sv.IsMultiplayer())
				sv.InstallClientStringTableMirrors();
		}
		else if (state == SignOnState.Full) {
			if (sv.LoadGame) {
				// sv.FinishRestore();
			}

			NetChannel!.SetTimeout(sv_timeout.GetFloat());
			NetChannel.SetFileTransmissionMode(true);
		}

		return base.SetSignOnState(state, spawncount);
	}

	// void SendSound(SoundInfo sound, bool isReliable) { }

	void WriteGameSounds(bf_write buf) {
		if (Sounds.Count == 0)
			return;

		byte[] data = new byte[Protocol.MAX_PAYLOAD];
		SVC_Sounds msg = new();
		msg.DataOut.StartWriting(data, Protocol.MAX_PAYLOAD, 0);

		int soundCount = FillSoundsMessage(msg);
		msg.WriteToBuffer(buf);
	}

	int FillSoundsMessage(SVC_Sounds msg) {
		int i, count = Sounds.Count;

		int max = Server.IsMultiplayer() ? 32 : 255;

		if (count > max)
			count = max;

		if (count == 0)
			return 0;

		SoundInfo defaultSound = new();
		defaultSound.SetDefault();

		SoundInfo deltaSound = defaultSound;

		msg.NumSounds = count;
		msg.ReliableSound = false;
		msg.SetReliable(false);

		Assert(msg.DataOut.BitsLeft > 0);

		for (i = 0; i < count; i++) {
			SoundInfo sound = Sounds[i];
			sound.WriteDelta(ref deltaSound, msg.DataOut, Protocol.VERSION); // FIXME proto version
			deltaSound = sound;
		}

		int remove = Sounds.Count - (count + max);

		if (remove > 0) {
			DevMsg($"Warning! Dropped {remove} unreliable sounds for client {Name}.\n");
			count += remove;
		}

		if (count > 0)
			Sounds.RemoveRange(0, count);

		Assert(Sounds.Count <= max);

		return msg.NumSounds;
	}

	bool CheckConnect() {
		return true; // todo
	}

	protected override void ActivatePlayer() {
		base.ActivatePlayer();

		Common.TimestampedLog("CGameClient::ActivatePlayer -start");

		if (!sv.LoadGame) {
			serverGlobalVariables.CurTime = sv.GetTime();
			Common.TimestampedLog("g_pServerPluginHandler->ClientPutInServer");
			serverPluginHandler.ClientPutInServer(Edict, Name);
		}

		Common.TimestampedLog("g_pServerPluginHandler->ClientActivate");

		serverPluginHandler.ClientActive(Edict, sv.LoadGame);

		Common.TimestampedLog("g_pServerPluginHandler->ClientSettingsChanged");

		serverPluginHandler.ClientSettingsChanged(Edict);

		IGameEvent? evnt = gameEventManager.CreateEvent("player_activate");

		if (evnt != null) {
			evnt.SetInt("userid", GetUserID());
			gameEventManager.FireEvent(evnt);
		}

		Common.TimestampedLog("CGameClient::ActivatePlayer -end");
	}

	protected override bool SendSignonData() {
		bool clientHasDifferentTables = false;

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
			Assert(SV.ServerGameEnts);
			Edict.InitializeEntityDLLFields();
		}

		EntityIndex = ClientSlot + 1;
		IsInReplayMode = false;

		SVC_SetView msg = new() {
			EntityIndex = EntityIndex
		};
		SendNetMsg(msg);

		base.SpawnPlayer();

		// SV.ServerGameClients!.ClientSpawned(Edict);
	}

	protected override ClientFrame? GetDeltaFrame(int tick) {
		Assert(!IsHLTV());
		return cl.GetClientFrame(tick);
	}

	void WriteViewAngleUpdate() {
		if (IsFakeClient())
			return;

		PlayerState pl = SV.ServerGameClients!.GetPlayerState(Edict);
		Assert(pl != null);

		if (pl != null && pl.FixAngle != (int)FixAngle.None) {
			if (pl.FixAngle == (int)FixAngle.Relative) {
				SVC_FixAngle fixAngle = new(true, pl.AngleChange);
				NetChannel.SendNetMsg(fixAngle);
				pl.AngleChange.Init();
			}
			else {
				SVC_FixAngle fixAngle = new(false, pl.ViewingAngle);
				NetChannel.SendNetMsg(fixAngle);
			}

			pl.FixAngle = (int)FixAngle.None;
		}
	}

	static readonly string[] CLCommands = [ // Shouldn't be here
		"status",
		"pause",
		"setpause",
		"unpause",
		"ping",
		"rpt_server_enable",
		"rpt_client_enable",
#if !SWDS
		"rpt",
		"rpt_connect",
		"rpt_password",
		"rpt_screenshot",
		"rpt_download_log",
#endif
	];

	bool IsEngineClientCommand(in TokenizedCommand args) {
		if (args.ArgC() == 0)
			return false;

		for (int i = 0; i < CLCommands.Length; i++) {
			if (args[0].Equals(CLCommands[i], StringComparison.OrdinalIgnoreCase))
				return true;
		}

		return false;
	}

	// bool SendNetMsg(INetMessage msg, bool forceReliable) { }

	public override bool ExecuteStringCommand(ReadOnlySpan<char> c) {
		if (base.ExecuteStringCommand(c))
			return true;

		TokenizedCommand args = new();

		if (!args.Tokenize(c))
			return false;

		if (args.ArgC() == 0)
			return false;

		if (IsEngineClientCommand(args)) {
			cmd.ExecuteCommand(ref args, CommandSource.Client, ClientSlot);
			return true;
		}

		ConCommandBase? command = cvar.FindCommandBase(args[0]);

		if (command != null && command.IsCommand() && command.IsFlagSet(FCvar.GameDLL)) {
			// Allow cheat commands in singleplayer, debug, or multiplayer with sv_cheats on
			// NOTE: Don't bother with rpt stuff; commands that matter there shouldn't have FCVAR_GAMEDLL set
			if (command.IsFlagSet(FCvar.Cheat)) {
				if (sv.IsMultiplayer() && !Host.CanCheat())
					return false;
			}

			if (command.IsFlagSet(FCvar.SingleplayerOnly)) {
				if (sv.IsMultiplayer())
					return false;
			}

			// Don't allow clients to execute commands marked as development only.
			if (command.IsFlagSet(FCvar.DevelopmentOnly))
				return false;

			serverPluginHandler.SetCommandClient(ClientSlot);

			cmd.Dispatch(command, args);
		}
		else
			serverPluginHandler.ClientCommand(Edict, args);

		return true;
	}

	public override void SendSnapshot(ClientFrame frame) {
		if (HLTV) {

		}

		WriteViewAngleUpdate();

		base.SendSnapshot(frame);
	}

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

	public ClientFrame GetSendFrame() {
		ClientFrame? frame = CurrentFrame;
		return frame;
	}

	// bool IgnoreTempEntity(EventInfo evnt) { }

	// CheckTransmitInfo GetPrevPackInfo() { }
}
