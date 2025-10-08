using CommunityToolkit.HighPerformance;

using Microsoft.Extensions.DependencyInjection;

using Source.Common;
using Source.Common.Bitbuffers;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Filesystem;
using Source.Common.Networking;

using Steamworks;

using System.Diagnostics;
using System.Net;

using GameServer = Source.Engine.Server.GameServer;

namespace Source.Engine.Client;

public class C_ServerClassInfo() {
	public ClientClass? ClientClass;
	public string? ClassName;
	public string? DatatableName;
	public int InstanceBaselineIndex = INetworkStringTable.INVALID_STRING_INDEX;
}

/// <summary>
/// Base client state, in CLIENT
/// </summary>
public abstract class BaseClientState(
	Host Host, IFileSystem fileSystem, Net Net, GameServer sv, Cbuf Cbuf, ICvar cvar,
	IEngineVGuiInternal? EngineVGui, IEngineAPI engineAPI,
	[FromKeyedServices(Realm.Client)] NetworkStringTableContainer networkStringTableContainerClient
	) : INetChannelHandler, IConnectionlessPacketHandler, IServerMessageHandler
{
	public ConVar cl_connectmethod = new(nameof(cl_connectmethod), "", FCvar.UserInfo | FCvar.Hidden, "Method by which we connected to the current server.");
	public ConVar password = new(nameof(password), "", FCvar.Archive | FCvar.ServerCannotQuery | FCvar.DontRecord, "Current server access password");

	public const int CL_CONNECTION_RETRIES = 4;
	public const double CL_MIN_RESEND_TIME = 1.5;
	public const double CL_MAX_RESEND_TIME = 20;

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
	
	public InlineArray2<InlineArrayMaxEdicts<PackedEntity?>> EntityBaselines;

	public C_ServerClassInfo[] ServerClasses = new C_ServerClassInfo[Constants.TEMP_TOTAL_SERVER_CLASSES];
	public int NumServerClasses = Constants.TEMP_TOTAL_SERVER_CLASSES;
	public int ServerClassBits;
	public InlineArraySteamKeysize<char> EncryptionKey;
	public uint EncryptionKeySize;

	// Source does it differently but who really cares, this works fine... I think
	public NetworkStringTableContainer? StringTableContainer;

	public bool RestrictServerCommands;
	public bool RestrictClientCommands;


	protected virtual void ReadDeletions(EntityReadInfo u) {
		throw new NotImplementedException();
	}

	protected virtual void ReadPreserveEnt(EntityReadInfo u) {
		throw new NotImplementedException();
	}

	protected virtual void ReadDeltaEnt(EntityReadInfo u) {
		throw new NotImplementedException();
	}

	protected virtual void ReadLeavePVS(EntityReadInfo u) {
		throw new NotImplementedException();
	}

	protected virtual void ReadEnterPVS(EntityReadInfo u) {
		throw new NotImplementedException();
	}


	public bool GetClassBaseline(int iClass, out byte[]? fromData, out int fromBits) {
		ErrorIfNot( iClass >= 0 && iClass < ServerClasses.Length, $"GetDynamicBaseline: invalid class index '{iClass}'");

		C_ServerClassInfo pInfo = ServerClasses[iClass] ??= new();

		INetworkStringTable? pBaselineTable = GetStringTable(Protocol.INSTANCE_BASELINE_TABLENAME);

		ErrorIfNot(pBaselineTable != null, "GetDynamicBaseline: NULL baseline table");

		if (pInfo.InstanceBaselineIndex == INetworkStringTable.INVALID_STRING_INDEX) {
			Span<char> str = stackalloc char[64];
			sprintf(str, "%d").D(iClass);

			pInfo.InstanceBaselineIndex = pBaselineTable.FindStringIndex(str.SliceNullTerminatedString());

			if (pInfo.InstanceBaselineIndex == INetworkStringTable.INVALID_STRING_INDEX) {
				for (int i = 0; i < pBaselineTable.GetNumStrings(); ++i) 
					DevMsg($"{i}: {pBaselineTable.GetString(i)}\n");

				Assert(false);
			}
			ErrorIfNot(pInfo.InstanceBaselineIndex != INetworkStringTable.INVALID_STRING_INDEX, $"GetDynamicBaseline: FindStringIndex({str}-{pInfo.ClassName}) failed.");
		}
		fromData = pBaselineTable.GetStringUserData(pInfo.InstanceBaselineIndex);
		fromBits = fromData.Length;
		return fromData != null;
	}

	public void SetEntityBaseline(int baseline, ClientClass? clientClass, int index, byte[] packedData, int bytesWritten) {
		PackedEntity? entity = EntityBaselines[baseline][index];
		if (entity == null)
			entity = EntityBaselines[baseline][index] = new();

		entity.ClientClass = clientClass;
		entity.EntityIndex = index;
		entity.ServerClass = null;

		entity.AllocAndCopyPadded(packedData.AsSpan()[..bytesWritten]);
	}

	public virtual void Clear() {
		ServerCount = -1;
		DeltaTick = -1;

		ClockDriftMgr.Clear();

		CurrentSequence = 0;
		ServerClassBits = 0;
		PlayerSlot = 0;
		LevelFileName = "";
		LevelBaseName = "";
		MaxClients = 0;

		if (StringTableContainer != null) {
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

					string why = msg.ReadString(Protocol.MAX_ROUTABLE_PAYLOAD) ?? "<null>";
					ConWarning($"Disconnect: {why}\n"); // RaphaelIT7: Source Engine calls COM_ExplainDisconnection instead here.
					Disconnect(why, true);
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


	internal PackedEntity? GetEntityBaseline(int baseline, int newEntity) => EntityBaselines[baseline][newEntity];


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
		channel.RegisterMessage<svc_Sounds>();
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
			case svc_Sounds msg: return ProcessSounds(msg);
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

	private bool ProcessSounds(svc_Sounds msg) {
		return true;
	}

	private bool ProcessGMod_ServerToClient(svc_GMod_ServerToClient msg) {
		return true;
	}

	private bool ProcessTempEntities(svc_TempEntities msg) {
		return true;
	}

	protected virtual bool ProcessPacketEntities(svc_PacketEntities msg) {
		if (SignOnState < SignOnState.Spawn) {
			ConMsg("Received packet entities while connecting!\n");
			return false;
		}

		if (SignOnState == SignOnState.Spawn) {
			if (!msg.IsDelta) 
				SetSignonState(SignOnState.Full, ServerCount);
			else {
				ConMsg("Received delta packet entities while spawing!\n");
				return false;
			}
		}

		if ((DeltaTick >= 0) || !msg.IsDelta) 
			DeltaTick = GetServerTickCount();

		return true;
	}

	private bool ProcessUserMessage(svc_UserMessage msg) {
		byte[] userdata = new byte[Constants.MAX_USER_MSG_DATA];

		bf_read userMsg = new bf_read("UserMessage(read)", userdata, Constants.MAX_USER_MSG_DATA);
		int bitsRead = msg.DataIn.ReadBitsClamped(userdata, (uint)msg.Length);
		userMsg.StartReading(userdata, Net.Bits2Bytes(bitsRead));

		if (!Host.clientDLL!.DispatchUserMessage(msg.MessageType, userMsg)) {
			ConWarning($"Couldn't dispatch user message\n");
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

	public virtual bool ProcessClassInfo(svc_ClassInfo msg) {
		if (msg.CreateOnClient) {
			ConMsg("Can't create class tables.\n");
			Assert(false);
			return false;
		}

		Span<svc_ClassInfo.Class> classes = msg.Classes.AsSpan();
		ServerClasses = new C_ServerClassInfo[classes.Length];
		for (int i = 0; i < classes.Length; i++) {
			ref svc_ClassInfo.Class svclass = ref classes[i];
			if(svclass.ClassID >= classes.Length) {
				Host.EndGame(true, $"ProcessClassInfo: invalid class index ({svclass.ClassID}).\n");
				return false;
			}
			ServerClasses[i] = new();
		}

		return true;
	}

	private bool ProcessUpdateStringTable(svc_UpdateStringTable msg) {
		int startbit = msg.DataIn.BitsRead;
		if (StringTableContainer != null) // RaphaelIT7: In the Source Engine during level transmission in rare cases the svc_UpdateStringTable could be received before the server info.
		{
			NetworkStringTable? table = (NetworkStringTable?)StringTableContainer.GetTable(msg.TableID);
			if (table != null) {
				table.ParseUpdate(msg.DataIn, msg.ChangedEntries);
			}
		}
		else {
			Warning("m_StringTableContainer is NULL in BaseClientState.ProcessUpdateStringTable\n");
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

		if (table == null) {
			Error("Stringtable failed to be created!\n");
			return false;
		}

		table.SetTick(GetServerTickCount());

		HookClientStringTable(msg.TableName);

		if (msg.DataCompressed) {
			int msgUncompressedSize = msg.DataIn.ReadLong();
			int msgCompressedSize = msg.DataIn.ReadLong();
			uint uncompressedSize = (uint)msgUncompressedSize;
			bool success = false;
			if (msg.DataIn.BytesAvailable > 0 &&
				 msgCompressedSize <= (uint)msg.DataIn.BytesAvailable &&
				 msgCompressedSize < uint.MaxValue / 2 &&
				 msgUncompressedSize < uint.MaxValue / 2) {
				byte[] uncompressedBuffer = new byte[NetChannel.PAD_NUMBER(msgUncompressedSize, 4)];
				byte[] compressedBuffer = new byte[NetChannel.PAD_NUMBER(msgCompressedSize, 4)];
				msg.DataIn.ReadBits(compressedBuffer, msgCompressedSize * 8);

				unsafe {
					fixed (byte* uncompressedBfr = uncompressedBuffer)
					fixed (byte* compressedBfr = compressedBuffer)
						success = Net.BufferToBufferDecompress(uncompressedBfr, ref uncompressedSize, compressedBfr, uncompressedSize);
				}
				success &= (uncompressedSize == msgUncompressedSize);

				if (success) {
					bf_read data = new bf_read(uncompressedBuffer, uncompressedSize);
					table.ParseUpdate(data, msg.NumEntries);
				}
			}

			if (!success) {
				Assert(false);
				Warning("Malformed message in BaseClientState.ProcessCreateStringTable\n");
			}
		}
		else {
			table.ParseUpdate(msg.DataIn, msg.NumEntries);
		}

		return true;
	}

	public virtual bool ProcessServerInfo(svc_ServerInfo msg) {
#if !SWDS
		EngineVGui?.UpdateProgressBar(LevelLoadingProgress.ProcessServerInfo);
#endif

		if (msg.Protocol != Protocol.VERSION) {
			ConMsg($"Server returned version {msg.Protocol}, expected {Protocol.VERSION}\n");
			return false;
		}

		ServerCount = msg.ServerCount;
		MaxClients = msg.MaxClients;
		NumServerClasses = msg.MaxClasses;
		ServerClassBits = (int)Math.Log2(NumServerClasses) + 1;

		StringTableContainer = networkStringTableContainerClient;

		if (MaxClients < 1 || MaxClients > Constants.ABSOLUTE_PLAYER_LIMIT) {
			ConMsg($"Bad maxclients ({MaxClients}) from server.\n");
			return false;
		}

		if (NumServerClasses < 1 || NumServerClasses > Constants.MAX_SERVER_CLASSES) {
			ConMsg($"Bad maxclasses ({MaxClients}) from server.\n");
			return false;
		}

#if !SWDS
		if (!sv.IsActive() && !(NetChannel!.IsLoopback() || NetChannel.IsNull)) {
			if (MaxClients <= 1) {
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

		if (msg.TickInterval < Constants.MINIMUM_TICK_INTERVAL || msg.TickInterval > Constants.MAXIMUM_TICK_INTERVAL) {
			ConMsg($"Interval_per_tick {msg.TickInterval} out of range [{Constants.MINIMUM_TICK_INTERVAL} to {Constants.MAXIMUM_TICK_INTERVAL}]");
			return false;
		}

		LevelBaseName = msg.MapName;

		ConVar? skyname = cvar.FindVar("sv_skyname");
		skyname?.SetValue(msg.SkyName);

		DeltaTick = -1;

		Span<char> levelFileName = stackalloc char[MAX_PATH];
		Host.DefaultMapFileName(msg.MapName, levelFileName);
		LevelFileName = new(levelFileName.SliceNullTerminatedString());

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
		ConnectTime = -float.MaxValue;
		RetryNumber = 0;
		GameServerSteamID = 0;

		if (SignOnState == SignOnState.None)
			return;

		SignOnState = SignOnState.None;

		if (NetChannel != null) {
			NetChannel.Shutdown(reason ?? "Disconnect by user.");
			NetChannel = null;
		}
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
		msg.WriteUBitLong(msg.ChecksumXOR(), 32);
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
	Common Common = Singleton<Common>();
	public virtual void CheckForResend() {
		if (SignOnState != SignOnState.Challenge) return;

		if ((Net.Time - ConnectTime) < 1)
			return;

		if (RetryAddress == null || !Net.StringToAdr(RetryAddress, out IPEndPoint? addr)) {
			ConMsg($"Bad server address ({RetryAddress})\n");
			Disconnect("Bad server address", true);
			return;
		}

		if (RetryNumber >= GetConnectionRetryNumber()) {
			Common.ExplainDisconnection(true, $"Connection failed after {RetryNumber} retries.\n");
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

	public virtual bool LinkClasses() {
		for (int i = 0; i < NumServerClasses; i++) {
			C_ServerClassInfo serverClass = ServerClasses[i];
			if (serverClass == null || string.IsNullOrEmpty(serverClass.DatatableName))
				continue;

			serverClass.ClientClass = FindClientClass(serverClass.ClassName);
			if (serverClass.ClientClass != null) {
				ReadOnlySpan<char> serverName = serverClass.DatatableName;
				ReadOnlySpan<char> clientName = serverClass.ClientClass.RecvTable!.GetName();

				if (!serverName.Equals(clientName, StringComparison.OrdinalIgnoreCase)) {
					Host.EndGame(true, $"CL.ParseClassInfo_EndClasses: server and client classes for '{serverClass.ClassName}' " +
						$"use different datatables (server: {serverName}, client: {clientName})");

					return false;
				}

				serverClass.ClientClass.ClassID = i;
			}
			else
				Msg($"Client missing DT class {serverClass.ClassName}\n");
		}

		return true;
	}

	private ClientClass? FindClientClass(ReadOnlySpan<char> className) {
		if (className == null || className.IsEmpty)
			return null;

		for (ClientClass? cur = Host.ClientDLL.GetAllClasses(); cur != null; cur = cur.Next) 
			if (className.Equals(cur.NetworkName, StringComparison.OrdinalIgnoreCase))
				return cur;

		return null;
	}

	public virtual int GetConnectionRetryNumber() => CL_CONNECTION_RETRIES;

	public ConVar cl_name = new("name", "unnamed", FCvar.Archive | FCvar.UserInfo | FCvar.PrintableOnly | FCvar.ServerCanExecute, "Current user name");

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

	public INetworkStringTable? GetStringTable(ReadOnlySpan<char> tableName) {
		if(StringTableContainer == null) {
			Assert(StringTableContainer);
			return null;
		}

		return StringTableContainer.FindTable(tableName);
	}
	public virtual bool HookClientStringTable(ReadOnlySpan<char> tableName) {
		return false;
	}
}
