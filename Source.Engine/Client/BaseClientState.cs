using Source.Common.Bitbuffers;
using Source.Common.Client;
using Source.Common.Commands;
using Source.Common.Compression;
using Source.Common.Engine;
using Source.Common.Entity;
using Source.Common.Filesystem;
using Source.Common.Networking;
using Source.Common.Networking.DataTable;
using Source.Engine.Server;

using Steamworks;

using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;
using System.Xml.Linq;
using static Source.Common.Networking.svc_ClassInfo;
using static Source.Dbg;

using GameServer = Source.Engine.Server.GameServer;

namespace Source.Engine.Client;
/// <summary>
/// Base client state, in CLIENT
/// </summary>
public abstract class BaseClientState(Host Host, IFileSystem fileSystem, Net Net, GameServer sv, Cbuf Cbuf, ICvar cvar, IEngineVGuiInternal? EngineVGui, IEngineAPI engineAPI) : INetChannelHandler, IConnectionlessPacketHandler, IServerMessageHandler
{
	public ConVar cl_connectmethod = new(nameof(cl_connectmethod), "", FCvar.UserInfo | FCvar.Hidden, "Method by which we connected to the current server.");
	public ConVar password = new(nameof(password), "privatelol", FCvar.Archive | FCvar.ServerCannotQuery | FCvar.DontRecord, "Current server access password");
	// RaphaelIT7: I really need this since I don't have steam everywhere
	public ConVar steamid = new(nameof(steamid), "76561198399203059", FCvar.Archive | FCvar.ServerCannotQuery | FCvar.DontRecord, "If Steam isn't available, we'll use this one instead");

	public const int CL_CONNECTION_RETRIES = 4;
	public const double CL_MIN_RESEND_TIME = 1.5;
	public const double CL_MAX_RESEND_TIME = 20;
	public const double MIN_CMD_RATE = 10;
	public const double MAX_CMD_RATE = 100;

	public ClockDriftMgr ClockDriftMgr;
	public NetSocket Socket;
	public NetChannel? NetChannel;
	public uint ChallengeNumber;
	public double ConnectTime;
	public int RetryNumber;
	public string? RetryAddress;
	public string? RetrySourceTag;
	public int RetryChallenge;
	public SignOnState SignOnState;
	public double NextCmdTime;
	public int ServerCount = -1;
	public ulong GameServerSteamID;
	public int CurrentSequence;
	public int DeltaTick;
	public bool Paused;
	public double PausedExpireTime;
	public int ViewEntity;
	public int PlayerSlot;
	public string? LevelFileName;
	public string? LevelBaseName;
	public int MaxClients;
	// PackedEntity entity baselines
	// ServerClassInfo

	public ServerClassInfo[]? ServerClassInfo = null;
	public int ServerClasses;
	public int ServerClassBits;
	public string EncryptionKey;
	public uint EncryptionKeySize;

	// NetworkStringTableContainer

	//NetworkStringTableContainer StringTableContainer;

	public bool RestrictServerCommands;
	public bool RestrictClientCommands;

	// Networking stuff
	public PackedEntity?[][] EntityBaselines = new PackedEntity?[2][];
	public ClientFrameManager FrameManager = new();
	public IClientEntityList ClientEntityList;
	public EntityReport EntityReport = new();

	// Source does it differently but who really cares, this works fine... I think
	public NetworkStringTableContainer? StringTableContainer;

	public virtual void Clear()
	{
		ServerCount = -1;
		DeltaTick = -1;

		ClockDriftMgr.Clear();

		CurrentSequence = 0;
		ServerClassInfo = null;
		ServerClasses = 0;
		ServerClassBits = 0;
		PlayerSlot = 0;
		LevelFileName = "";
		LevelBaseName = "";
		MaxClients = 0;

		if (StringTableContainer != null)
		{
			StringTableContainer.RemoveAllTables();
			StringTableContainer = null;
		}

		FreeEntityBaselines();

		// RecvTable_Term(false);

		if (NetChannel != null)
			NetChannel.Reset();

		Paused = false;
		PausedExpireTime = -1.0;
		ViewEntity = 0;
		ChallengeNumber = 0;
		ConnectTime = 0.0;

		// RaphaelIT7 - ToDo: Implement a way to clear the Entity reporting stuff
		// EntityReport
		FrameManager.DeleteClientFrames(-1);
		for (int i = 0; i < 2; i++)
		{
			EntityBaselines[i] = new PackedEntity?[Constants.MAX_EDICTS];
		}
	}

	public virtual bool ProcessConnectionlessPacket(ref NetPacket packet) {
		bf_read msg = packet.Message;

		int c = msg.ReadByte();
		switch (c) {
			case S2C.Connection:
				if (SignOnState == SignOnState.Challenge) {
					int myChallenge = msg.ReadLong();
					if (myChallenge != RetryChallenge) {
						ConWarning("incorrect challenge\n");
						return false;
					}
					FullConnect(packet.From);
				}
				break;
			case S2C.ConnectionRejected:
				if (SignOnState == SignOnState.Challenge) {
					int myChallenge = msg.ReadLong();
					if (myChallenge != RetryChallenge) {
						ConWarning("Connection rejection challenge mis-match, ignoring\n");
						return false;
					}

					string? why = msg.ReadString(Protocol.MAX_ROUTABLE_PAYLOAD);
					Disconnect(why ?? "<null>", true);
				}
				break;
			case S2C.Challenge:
				if (SignOnState == SignOnState.Challenge) {
					uint magicVersion = msg.ReadULong();
					ConWarning($"Server.MagicVersion: {magicVersion}\n");
					if (magicVersion != S2C.MagicVersion) {
						Disconnect("Server has not updated to the most recent version.", true);
						return false;
					}

					int challenge = msg.ReadLong();
					int myChallenge = msg.ReadLong();
					if (myChallenge != RetryChallenge) {
						ConWarning("Server challenge was not correct, ignoring.\n");
						return false;
					}

					int authProtocol = msg.ReadLong();
					ulong gameServerID = 0;
					bool secure = false;

					if (authProtocol == Protocol.PROTOCOL_STEAM) {
						if (msg.ReadShort() != 0) {
							Disconnect("Invalid Steam key size.", true);
							return false;
						}

						if (msg.BytesLeft > sizeof(ulong)) {
							if (!msg.ReadInto(out gameServerID)) {
								Disconnect("Invalid game-server Steam ID.", true);
								return false;
							}

							secure = msg.ReadByte() == 1;
						}

						if (secure && !Host.IsSecureServerAllowed()) {
							Disconnect("You are in insecure mode.  You must restart before you can connect to secure servers.", true);
						}
						SendConnectPacket(challenge, authProtocol, gameServerID, secure);
					}
				}
				break;
		}

		return false;
	}

	public virtual void ConnectionStart(NetChannel channel) {
		channel.RegisterMessage<NET_Tick>();
		channel.RegisterMessage<NET_SignonState>();
		channel.RegisterMessage<NET_SetConVar>();
		channel.RegisterMessage<NET_StringCmd>();
		channel.RegisterMessage<svc_Print>();
		channel.RegisterMessage<svc_ServerInfo>();
		channel.RegisterMessage<svc_CreateStringTable>();
		channel.RegisterMessage<svc_UpdateStringTable>();
		channel.RegisterMessage<svc_ClassInfo>();
		channel.RegisterMessage<svc_BSPDecal>();
		channel.RegisterMessage<svc_VoiceInit>();
		channel.RegisterMessage<svc_GameEventList>();
		channel.RegisterMessage<svc_FixAngle>();
		channel.RegisterMessage<svc_SetView>();
		channel.RegisterMessage<svc_UserMessage>();
		channel.RegisterMessage<svc_PacketEntities>();
		channel.RegisterMessage<svc_TempEntities>();
		channel.RegisterMessage<svc_GMod_ServerToClient>();
	}
	public virtual void ConnectionClosing(string reason) {
		Disconnect(reason, true);
	}
	public abstract void ConnectionCrashed(string reason);

	public virtual void PacketStart(int incomingSequence, int outgoingAcknowledged) { }
	public virtual void PacketEnd() { }

	public abstract void FileRequested(string fileName, uint transferID);
	public abstract void FileReceived(string fileName, uint transferID);
	public abstract void FileDenied(string fileName, uint transferID);
	public abstract void FileSent(string fileName, uint transferID);

	public virtual bool ProcessMessage(INetMessage message) {
		switch (message) {
			case NET_Tick msg: return ProcessTick(msg);
			case NET_SignonState msg: return ProcessSignonState(msg);
			case NET_SetConVar msg: return ProcessSetConVar(msg);
			case NET_StringCmd msg: return ProcessStringCmd(msg);
			case svc_Print msg: return ProcessPrint(msg);
			case svc_ServerInfo msg: return ProcessServerInfo(msg);
			case svc_CreateStringTable msg: return ProcessCreateStringTable(msg);
			case svc_UpdateStringTable msg: return ProcessUpdateStringTable(msg);
			case svc_ClassInfo msg: return ProcessClassInfo(msg);
			case svc_BSPDecal msg: return ProcessBSPDecal(msg);
			case svc_VoiceInit msg: return ProcessVoiceInit(msg);
			case svc_GameEventList msg: return ProcessGameEventList(msg);
			case svc_FixAngle msg: return ProcessFixAngle(msg);
			case svc_SetView msg: return ProcessSetView(msg);
			case svc_UserMessage msg: return ProcessUserMessage(msg);
			case svc_PacketEntities msg: return ProcessPacketEntities(msg);
			case svc_TempEntities msg: return ProcessTempEntities(msg);
			case svc_GMod_ServerToClient msg: return ProcessGMod_ServerToClient(msg);
		}
		// ignore
		return true;
	}

	private bool ProcessGMod_ServerToClient(svc_GMod_ServerToClient msg) {
		return true;
	}

	private bool ProcessTempEntities(svc_TempEntities msg) {
		return true;
	}

	virtual protected bool ProcessPacketEntities(svc_PacketEntities msg) {
		// Cheating; need to better implement all of this
		if (SignOnState < SignOnState.Spawn) {
			ConWarning("Received packet entities while connecting!\n");
			return false;
		}

		if (msg.UpdateBaseline)
		{
			var clcAck = new clc_BaselineAck(0, msg.Baseline);
			NetChannel.SendNetMsg(clcAck, true);
		}

		if (SignOnState == SignOnState.Spawn) {
			if (!msg.IsDelta) {
				SetSignonState(SignOnState.Full, ServerCount);
			} else {
				ConWarning("Received delta packet entities while spawning!\n");
				return false;
			}
		}

		if (DeltaTick >= 0 || !msg.IsDelta)
			DeltaTick = GetServerTickCount();

		return true;
	}

	private bool ProcessUserMessage(svc_UserMessage msg) {
		byte[] userdata = new byte[Constants.MAX_USER_MSG_DATA];

		bf_read userMsg = new bf_read("UserMessage(read)", userdata, Constants.MAX_USER_MSG_DATA);
		int bitsRead = msg.DataIn.ReadBitsClamped(userdata, (uint)msg.Length);
		userMsg.StartReading(userdata, Net.Bits2Bytes(bitsRead));

		if (!Host.clientDLL!.DispatchUserMessage(msg.MessageType, userMsg)) {
			ConWarning($"Couldn't dispatch user message ({userMsg})\n");
			return true;
		}

		return true;
	}

	private bool ProcessSetView(svc_SetView msg) {
		return true;
	}

	private bool ProcessFixAngle(svc_FixAngle msg) {
		return true;
	}

	private bool ProcessGameEventList(svc_GameEventList msg) {
		return true;
	}

	private bool ProcessBSPDecal(svc_BSPDecal msg) {
		return true;
	}

	private bool ProcessVoiceInit(svc_VoiceInit msg) {
		return true;
	}

	protected virtual bool ProcessClassInfo(svc_ClassInfo msg) {
		return true;
	}

	private bool ProcessUpdateStringTable(svc_UpdateStringTable msg) {
		int startbit = msg.DataIn.BitsRead;
		if(StringTableContainer != null) // RaphaelIT7: In the Source Engine during level transmission in rare cases the svc_UpdateStringTable could be received before the server info.
		{
			NetworkStringTable? table = (NetworkStringTable?)StringTableContainer.GetTable(msg.TableID);
			if (table != null)
			{
				table.ParseUpdate(msg.DataIn, msg.ChangedEntries);
			}
		} else {
			Warning("m_StringTableContainer is NULL in CBaseClientState::ProcessUpdateStringTable\n");
		}

		int endbit = msg.DataIn.BitsRead;
		return (endbit - startbit) == msg.Length;
	}

	private bool ProcessCreateStringTable(svc_CreateStringTable msg) {
#if !SWDS
		EngineVGui?.UpdateProgressBar(LevelLoadingProgress.ProcessStringTable);
#endif
		
		StringTableContainer?.SetAllowCreation(true);

		NetworkStringTable? table = (NetworkStringTable?)StringTableContainer?.CreateStringTableEx(msg.TableName, msg.MaxEntries, msg.UserDataSize, msg.UserDataSizeBits, msg.IsFilenames);
		
		StringTableContainer?.SetAllowCreation(false);
		
		if (table == null)
		{
			Error("Stringtable failed to be created!\n");
			return false;
		}

		table.SetTick(GetServerTickCount());

		HookClientStringTable(msg.TableName);

		if (msg.DataCompressed)
		{
			int msgUncompressedSize = msg.DataIn.ReadLong();
			int msgCompressedSize = msg.DataIn.ReadLong();
			int uncompressedSize = msgUncompressedSize;
			bool bSuccess = false;
			if ( msg.DataIn.BytesAvailable > 0 &&
				 msgCompressedSize <= (uint)msg.DataIn.BytesAvailable &&
				 msgCompressedSize < uint.MaxValue/2 &&
				 msgUncompressedSize < uint.MaxValue/2 )
			{
				byte[] compressedBuffer = new byte[msgCompressedSize];
				msg.DataIn.ReadBits( compressedBuffer, msgCompressedSize * 8 );

				byte[] uncompressedBuffer = new byte[msgUncompressedSize];
				uncompressedSize = (int)CLZSS.Uncompress(uncompressedBuffer, compressedBuffer);
				bSuccess &= (uncompressedSize == msgUncompressedSize);

				if (bSuccess)
				{
					bf_read data = new bf_read(uncompressedBuffer, uncompressedSize);
					table.ParseUpdate(data, msg.NumEntries);
				}
			}

			if (!bSuccess)
			{
				Warning("Malformed message in CBaseClientState::ProcessCreateStringTable\n");
			}
		} else {
			table.ParseUpdate(msg.DataIn, msg.NumEntries);
		}

		return true;
	}

	private bool ProcessServerInfo(svc_ServerInfo msg) {
#if !SWDS
		EngineVGui?.UpdateProgressBar(LevelLoadingProgress.ProcessServerInfo);
#endif

		if (msg.Protocol != Protocol.VERSION) {
			ConMsg($"Server returned version {msg.Protocol}, expected {Protocol.VERSION}\n");
			return false;
		}

		ServerCount = msg.ServerCount;
		MaxClients = msg.MaxClients;
		ServerClasses = msg.MaxClasses;
		ServerClassBits = (int)Math.Log2(ServerClasses) + 1;

		StringTableContainer = (NetworkStringTableContainer)INetworkStringTableContainer.networkStringTableContainerClient;

		if (MaxClients < 1 || MaxClients > Constants.ABSOLUTE_PLAYER_LIMIT) {
			ConMsg($"Bad maxclients ({MaxClients}) from server.\n");
			return false;
		}

		if (ServerClasses < 1 || ServerClasses > Constants.MAX_SERVER_CLASSES) {
			ConMsg($"Bad maxclasses ({MaxClients}) from server.\n");
			return false;
		}

#if !SWDS
		if(!sv.IsActive() && !(NetChannel!.IsLoopback() || NetChannel.IsNull)) {
			if(MaxClients <= 1) {
				ConMsg($"Bad maxclients ({MaxClients}) from server.\n");
				return false;
			}

			cvar.RevertFlaggedConVars(FCvar.Replicated);
			cvar.RevertFlaggedConVars(FCvar.Cheat);
			DevMsg("FCvar.Cheat cvars reverted to defaults.\n");
		}
#endif

		FreeEntityBaselines();
		PlayerSlot = msg.PlayerSlot;
		ViewEntity = PlayerSlot + 1;

		if(msg.TickInterval < Constants.MINIMUM_TICK_INTERVAL || msg.TickInterval > Constants.MAXIMUM_TICK_INTERVAL) {
			ConMsg($"Interval_per_tick {msg.TickInterval} out of range [{Constants.MINIMUM_TICK_INTERVAL} to {Constants.MAXIMUM_TICK_INTERVAL}]");
			return false;
		}

		LevelBaseName = msg.MapName;
		
		ConVar? skyname = cvar.FindVar("sv_skyname");
		skyname?.SetValue(msg.SkyName);

		DeltaTick = -1;

		// todo: Host_DefaultMapFileName

		return true;
	}

	private bool ProcessPrint(svc_Print msg) {
		Dbg.ConMsg(msg.Text);
		return true;
	}

	private bool ProcessStringCmd(NET_StringCmd msg) {
		if (!RestrictServerCommands || sv.IsActive()) {
			Cbuf.AddText(msg.Command);
			return true;
		}

		if (!Cbuf.HasRoomForExecutionMarkers(2)) {
			AssertMsg(false, "BaseClientState.ProcessStringCmd called, but there is no room for the execution markers. Ignoring command.");
			return true;
		}

		Cbuf.AddTextWithMarkers(CmdExecutionMarker.EnableServerCanExecute, msg.Command, CmdExecutionMarker.DisableServerCanExecute);
		return true;
	}

	private bool ProcessSetConVar(NET_SetConVar msg) {
		if (NetChannel == null) return false;
		// TODO: loopback netchannels

		foreach (var var in msg.ConVars) {
			ConVar? cv = cvar.FindVar(var.Name);
			if (cv == null) {
				ConMsg($"SetConVar: No such cvar ({var.Name} set to {var.Value})\n");
				continue;
			}

			if (!cv.IsFlagSet(FCvar.Replicated)) {
				ConMsg($"SetConVar: Can't set server cvar {var.Name} to {var.Value}, not marked as FCvar.Replicated on client\n");
				continue;
			}

			if (!sv.IsActive()) {
				cv.SetValue(var.Value);
				DevMsg($"SetConVar: {var.Name} = {var.Value}\n");
			}
		}

		return true;
	}

	private bool ProcessSignonState(NET_SignonState msg) {
		SetSignonState(msg.SignOnState, msg.SpawnCount);
		return true;
	}

	private bool ProcessTick(NET_Tick msg) {
		NetChannel.SetRemoteFramerate(msg.HostFrameTime, msg.HostFrameDeviation);
		SetClientTickCount(msg.Tick);
		SetServerTickCount(msg.Tick);
		// string tables?

		return GetServerTickCount() > 0;
	}

	public bool IsActive() => SignOnState == SignOnState.Full;
	public bool IsConnected() => SignOnState >= SignOnState.Connected;
	public virtual void FullConnect(NetAddress to) {
		NetChannel = Net.CreateNetChannel(NetSocketType.Client, to, "CLIENT", this) ?? throw new Exception("Failed to create networking channel");
		Debug.Assert(NetChannel != null);

		NetChannel.StartStreaming(ChallengeNumber);

		ConnectTime = Net.Time;

		DeltaTick = -1;

		NextCmdTime = Net.Time;

		SetSignonState(SignOnState.Connected, -1);
	}
	public virtual void Connect(string adr, string sourceTag) {
		RetryChallenge = (Random.Shared.Next(0, 0x0FFF) << 16) | (Random.Shared.Next(0, 0xFFFF));
		Net.ipname.SetValue(adr.Split(':')[0]);
		Net.SetMultiplayer(true);
		GameServerSteamID = 0;
		RetrySourceTag = sourceTag;
		RetryAddress = adr;
		cl_connectmethod.SetValue(sourceTag);

		SetSignonState(SignOnState.Challenge, -1);
		ConnectTime = -double.MaxValue;
		RetryNumber = 0;
	} // start a connection challenge
	public virtual bool SetSignonState(SignOnState state, int count) {
		if (state < SignOnState.None || state > SignOnState.ChangeLevel) {
			Debug.Assert(false, $"Received signon {state} when at {SignOnState}");
			return false;
		}

		if (state > SignOnState.Connected && state <= SignOnState) {
			Debug.Assert(false, $"Received signon {state} when at {SignOnState}");
			return false;
		}

		SignOnState = state;

		return true;
	}
	public virtual void Disconnect(string? reason, bool showMainMenu) {
		if (SignOnState == SignOnState.None)
			return;

		if (NetChannel != null) {
			NetChannel.Shutdown(reason ?? "Disconnect by user.");
			NetChannel = null;
		}

		Msg($"Disconnect: {reason}\n");
		SignOnState = SignOnState.None;
	}
	public virtual void SendConnectPacket(int challengeNr, int authProtocol, ulong gameServerSteamID, bool gameServerSecure) {
		string serverName;
		string cdKey = "NOCDKEY";

		if (RetryAddress == null || !Net.StringToAdr(RetryAddress, out IPEndPoint? addr)) {
			ConWarning($"Bad server address ({RetryAddress})\n");
			Disconnect("Bad server address", true);
			return;
		}

		if (addr.Port == 0) {
			addr.Port = Net.PORT_SERVER;
		}

		bf_write msg = new();
		byte[] packet = new byte[Protocol.MAX_ROUTABLE_PAYLOAD];
		msg.StartWriting(packet, packet.Length, 0);
		msg.WriteLong(Protocol.CONNECTIONLESS_HEADER);
		msg.WriteByte(C2S.Connect);
		msg.WriteLong(Protocol.VERSION);
		msg.WriteLong(authProtocol);
		msg.WriteLong(challengeNr);
		msg.WriteLong(RetryChallenge);
		msg.WriteUBitLong(2729496039, 32);
		msg.WriteString(GetClientName(), true, 256);
		msg.WriteString(password.GetString(), true, 256);
		msg.WriteString(SteamAppInfo.GetSteamInf(fileSystem).PatchVersion, true, 32);

		switch (authProtocol) {
			case Protocol.PROTOCOL_HASHEDCDKEY:
				throw new Exception("Cannot use CD key protocol");
			case Protocol.PROTOCOL_STEAM:
				if (!PrepareSteamConnectResponse(gameServerSteamID, gameServerSecure, addr, msg))
					return;
				break;
			default:

				break;
		}
		Socket.UDP!.SendTo(msg.BaseArray!.AsSpan()[..msg.BytesWritten], addr);


		this.ConnectTime = Net.Time;
		this.ChallengeNumber = (uint)challengeNr;
	}

	public virtual string GetCDKeyHash() => "123";
	public virtual void RunFrame() {
		if (SignOnState == SignOnState.Challenge) {
			CheckForResend();
		}
	}
	public virtual bool PrepareSteamConnectResponse(ulong gameServerSteamID, bool gameServerSecure, IPEndPoint addr, bf_write msg) {
		// Check steam user
		if (!SteamAPI.IsSteamRunning()) {
			if (addr.Port == 32030) // RaphaelIT7: Port I used for my testing server, should be fine for now.
			{ // For Testing without steam
				uint fakeKeysize = 10; // We can't just use 0 since that kicks us before we reach ConnectClient
				msg.WriteShort((int)(fakeKeysize + sizeof(ulong)));
				msg.WriteLongLong((long)long.Parse(steamid.GetString()));
				byte[] fakeKey = new byte[fakeKeysize];
				msg.WriteBytes(fakeKey, (int)fakeKeysize);
				return true;
			}

			Disconnect("The server requires Steam authentication.", true);
			return false;
		}

		byte[] steam3Cookie = new byte[Protocol.STEAM_KEYSIZE];
		var result = SteamUser.GetAuthSessionTicket(steam3Cookie, Protocol.STEAM_KEYSIZE, out uint keysize);

		msg.WriteShort((int)(keysize + sizeof(ulong)));
		msg.WriteLongLong((long)SteamUser.GetSteamID().m_SteamID);

		if (keysize > 0)
			msg.WriteBytes(steam3Cookie, (int)keysize);

		return true;
	}
	public virtual void CheckForResend() {
		if (SignOnState != SignOnState.Challenge) return;

		if ((Net.Time - ConnectTime) < 1)
			return;

		if (RetryAddress == null || !Net.StringToAdr(RetryAddress, out IPEndPoint? addr)) {
			ConMsg($"Bad server address ({RetryAddress})\n");
			Disconnect("Bad server address", true);
			return;
		}

		if (RetryNumber >= 5) {
			ConMsg($"Connection failed after {RetryNumber} retries.\n");
			Disconnect("Connection failed", true);
			return;
		}

		if (RetryNumber == 0)
			ConMsg($"Connecting to {RetryAddress}...\n");
		else
			ConMsg($"Retrying {RetryAddress}...\n");
		RetryNumber++;

		bf_write msg = new bf_write();
		msg.StartWriting(new byte[Protocol.MAX_ROUTABLE_PAYLOAD], Protocol.MAX_ROUTABLE_PAYLOAD, 0);
		msg.WriteLong(Protocol.CONNECTIONLESS_HEADER);
		msg.WriteByte(A2S.GetChallenge);
		msg.WriteLong(RetryChallenge);
		msg.WriteString("0000000000");

		Socket.UDP!.SendTo(msg.BaseArray!.AsSpan()[..msg.BytesWritten], addr);

		ConnectTime = Net.Time;
	}

	public virtual int GetConnectionRetryNumber() => CL_CONNECTION_RETRIES;

	public ConVar cl_name = new(nameof(cl_name), "unnamed", FCvar.Archive | FCvar.UserInfo | FCvar.PrintableOnly | FCvar.ServerCanExecute, "Current user name");

	public virtual string GetClientName() => cl_name.GetString();

	public virtual int GetClientTickCount() => 0;
	public virtual void SetClientTickCount(int tick) { }

	public virtual int GetServerTickCount() => 0;
	public virtual void SetServerTickCount(int tick) { }

	public virtual void SetClientAndServerTickCount(int tick) { }

	public void ForceFullUpdate() {
		if (DeltaTick == -1)
			return;
		FreeEntityBaselines();
		DeltaTick = -1;
		DevMsg("Requesting full game update...\n");
	}

	private void FreeEntityBaselines() {

	}

	public void SendStringCmd(ReadOnlySpan<char> str) {
		if (NetChannel != null) {
			NET_StringCmd stringCmd = new NET_StringCmd();
			stringCmd.Command = new(str);
			NetChannel.SendNetMsg(stringCmd);
		}
	}

	public void HookClientStringTable(string tableName)
	{
		// ToDo
	}

	public void CopyEntityBaseline(int From, int To)
	{
		for (int i=0; i<Constants.MAX_EDICTS; i++)
		{
			PackedEntity? blfrom = EntityBaselines[From][i];
			PackedEntity? blto = EntityBaselines[To][i];

			if(blfrom == null)
			{
				// make sure blto doesn't exists
				if (blto != null)
				{
					// ups, we already had this entity but our ack got lost
					// we have to remove it again to stay in sync
					EntityBaselines[To][i] = null;
				}
				continue;
			}

			if (blto == null)
			{
				// create new to baseline if none existed before
				blto = EntityBaselines[To][i] = new PackedEntity();
				blto.ClientClass = null;
				blto.ServerClass = null;
				blto.ReferenceCount = 0;
			}

			blto.EntityIndex = blfrom.EntityIndex; 
			blto.ClientClass = blfrom.ClientClass;
			blto.ServerClass = blfrom.ServerClass;
			blto.AllocAndCopyPadded(blfrom.GetData(), blfrom.GetNumBytes());
		}
	}

	public void ReadPacketEntities(CEntityReadInfo u)
	{
		u.NextOldEntity();
		while (u.UpdateType < UpdateType.Finished)
		{
			u.HeaderCount--;
			u.IsEntity = (u.HeaderCount >= 0) ? true : false;
			if (u.IsEntity)
			{
				EntsParse.ParseDeltaHeader(u);
			}

			u.UpdateType = UpdateType.PreserveEnt;
			while (u.UpdateType == UpdateType.PreserveEnt)
			{
				if (EntsParse.DetermineUpdateType(u))
				{
					switch (u.UpdateType)
					{
						case UpdateType.EnterPVS:
							EntsParse.ReadEnterPVS(this, u);
							break;
						case UpdateType.LeavePVS:
							EntsParse.ReadLeavePVS(this, u);
							break;
						case UpdateType.DeltaEnt:
							EntsParse.ReadDeltaPVS(this, u);
							break;
						case UpdateType.PreserveEnt:
							EntsParse.ReadPreservePVS(this, u);
							break;
					}
				}
			}
		}
	}

	public PackedEntity? GetEntityBaseline(int Baseline, int Entity)
	{
		return EntityBaselines[Baseline][Entity];
	}

	public INetworkStringTable? GetStringTable(string name)
	{
		if (StringTableContainer == null)
		{
			return null;
		}

		return StringTableContainer.FindTable( name );
	}

	public bool GetClassBaseline(int Class, out byte[]? Data, out int DataLength)
	{
		ErrorIfNot(Class >= 0 && Class < ServerClasses, "GetDynamicBaseline: invalid class index '{0}'", Class);

		// We lazily update these because if you connect to a server that's already got some dynamic baselines,
		// you'll get the baselines BEFORE you get the class descriptions.
		ServerClassInfo pInfo = ServerClassInfo[Class];
		INetworkStringTable? pBaselineTable = GetStringTable(INetworkStringTable.INSTANCE_BASELINE_TABLENAME);
		if (pBaselineTable == null)
		{
			Error("GetDynamicBaseline: NULL baseline table");
			Data = null;
			DataLength = -1;
			return false;
		}

		if (pInfo.InstanceBaselineIndex == INetworkStringTable.INVALID_STRING_INDEX)
		{
			// The key is the class index string.
			string strClass = Class.ToString();
			pInfo.InstanceBaselineIndex = pBaselineTable.FindStringIndex(strClass);
			if (pInfo.InstanceBaselineIndex == INetworkStringTable.INVALID_STRING_INDEX)
			{
				for (int i = 0; i < pBaselineTable.GetNumStrings(); ++i )
				{
					DevMsg("{0}: {1}\n", i, pBaselineTable.GetString(i));
				}

				// Gets a callstack, whereas ErrorIfNot(), does not.
				Assert(false);
			}

			ErrorIfNot(pInfo.InstanceBaselineIndex != INetworkStringTable.INVALID_STRING_INDEX, "GetDynamicBaseline: FindStringIndex({0}-{1}) failed.", strClass, pInfo.ClassName);
		}

		Data = pBaselineTable.GetStringUserData(pInfo.InstanceBaselineIndex, out DataLength);
		return Data != null;
	}

	public void SetEntityBaseline(int Baseline, ClientClass ClientClass, int index, byte[] packedData, int length)
	{
		Assert( index >= 0 && index < Constants.MAX_EDICTS );
		Assert( ClientClass != null );
		Assert( (Baseline == 0) || (Baseline == 1) );
	
		PackedEntity? entitybl = EntityBaselines[Baseline][index];
		if (entitybl == null)
		{
			entitybl = EntityBaselines[Baseline][index] = new PackedEntity();
		}

		entitybl.ClientClass = ClientClass;
		entitybl.EntityIndex = index;
		entitybl.ServerClass = null;

		// Copy out the data we just decoded.
		entitybl.AllocAndCopyPadded( packedData, length );
	}

	public ClientClass? FindClientClass(string? pClassName)
	{
		if ( pClassName == null )
			return null;

		for(ClientClass? pCur = Host.CL.ClientDLL.GetAllClientClasses(); pCur != null; pCur = pCur.Next)
		{
			if(pCur.NetworkName.Equals(pClassName))
				return pCur;
		}

		return null;
	}

	public virtual bool LinkClasses()
	{
		for (int i=0; i < ServerClasses; ++i)
		{
			ServerClassInfo pServerClass = ServerClassInfo[i];
			if (pServerClass.DatatableName == null)
				continue;

			// (this can be null in which case we just use default behavior).
			pServerClass.ClientClass = FindClientClass(pServerClass.ClassName);
			if (pServerClass.ClientClass != null)
			{
				// If the class names match, then their datatables must match too.
				// It's ok if the client is missing a class that the server has. In that case,
				// if the server actually tries to use it, the client will bomb out.
				string pServerName = pServerClass.DatatableName;
				string pClientName = pServerClass.ClientClass.pRecvTable.GetName();

				if (!pServerName.Equals(pClientName))
				{
					Host_Error("CL_ParseClassInfo_EndClasses: server and client classes for '{0}' use different datatables (server: {1}, client: {2})",
						pServerClass.ClassName, pServerName, pClientName );
				
					return false;
				}

				// copy class ID
				pServerClass.ClientClass.ClassID = i;
			} else {
				Msg("Client missing DT class {0}\n", pServerClass.ClassName);
			}
		}

		return true;
	}
}