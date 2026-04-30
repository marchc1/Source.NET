using Source.Common;
using Source.Common.Audio;
using Source.Common.Bitbuffers;
using Source.Common.Client;
using Source.Common.Engine;
using Source.Common.Formats.Keyvalues;
using Source.Common.Mathematics;
using Source.Common.Networking;
using Source.Common.Server;
using Source.Engine.Server;

using Steamworks;

using System.Numerics;

namespace Source.Engine;

internal class EngineServer(Cbuf Cbuf, Host host) : IEngineServer
{
	public readonly SharedEdictChangeInfo g_roSharedEdictChangeInfo = new();
	public void AddOriginToPVS(in Vector3 origin) {
		throw new NotImplementedException();
	}

	public void AllowImmediateEdictReuse() => ED.AllowImmediateReuse();

	public void BuildEntityClusterList(Edict edict, ref PVSInfo pvsInfo) {
		throw new NotImplementedException();
	}

	public void ChangeLevel(ReadOnlySpan<char> s1, ReadOnlySpan<char> s2) {
		Span<char> cmd = stackalloc char[256];
		Span<char> s1Escaped = stackalloc char[256];
		Span<char> s2Escaped = stackalloc char[256];

		if (!Cbuf.EscapeCommandArg(s1, s1Escaped) || (!s2.IsEmpty && !Cbuf.EscapeCommandArg(s2, s2Escaped))) {
			Warning("Illegal map name in ChangeLevel\n");
			return;
		}

		int cmdLen = 0;
		if (s2.IsEmpty)
			cmdLen = sprintf(cmd, "changelevel %s\n").S(s1Escaped);
		else
			cmdLen = sprintf(cmd, "changelevel %s %s\n").S(s1Escaped).S(s2Escaped);

		if (cmdLen >= 256) {
			Warning("Paramter overflow in ChangeLevel\n");
			return;
		}

		cbuf.AddText(cmd);
	}

	public void ChangeTeam(ReadOnlySpan<char> pTeamName) {
#if !SWDS
		throw new NotImplementedException();
#endif
	}

	public int CheckAreasConnected(int area1, int area2) => CM.AreasConnected(area1, area2);

	static bool warnedCheckBoxInPVS = false;
	public bool CheckBoxInPVS(in Vector3 mins, in Vector3 maxs, ReadOnlySpan<byte> checkpvs) {
		if (!warnedCheckBoxInPVS) {
			Console.WriteLine("CheckBoxInPVS not implemented");
			warnedCheckBoxInPVS = true;
		}
		return false;
	}

	public int CheckHeadnodeVisible(int nodenum, Span<byte> pvs) {
		throw new NotImplementedException();
	}

	public bool CheckOriginInPVS(in Vector3 org, ReadOnlySpan<byte> checkpvs) {
		throw new NotImplementedException();
	}

	public void CleanUpEntityClusterList(ref PVSInfo pvsInfo) {
		throw new NotImplementedException();
	}

	public void ClearSaveDir() {
		throw new NotImplementedException();
	}

	public void ClearSaveDirAfterClientLoad() {
		throw new NotImplementedException();
	}

	public void ClientCommand(Edict edict, ReadOnlySpan<char> cmd) {
		throw new NotImplementedException();
	}

	public void ClientCommandKeyValues(Edict edict, KeyValues command) {
		throw new NotImplementedException();
	}

	public void ClientPrintf(Edict edict, ReadOnlySpan<char> szMsg) {
		int entnum = NUM_FOR_EDICT(edict);
		if (entnum < 1 || entnum > sv.GetClientCount()) {
			ConMsg("tried to sprint to a non-client\n");
			return;
		}

		sv.Client(entnum - 1).ClientPrintf(szMsg);
	}

	public int CompareFileTime(ReadOnlySpan<char> filename1, ReadOnlySpan<char> filename2, ref int compare) {
		throw new NotImplementedException();
	}

#if SWDS
	public void Con_NPrintf(int pos, ReadOnlySpan<char> msg) {};
	public void Con_NXPrintf(in Con_NPrint_s info, ReadOnlySpan<char> msg) {};
#else
	public void Con_NPrintf(int pos, ReadOnlySpan<char> msg) {
		throw new NotImplementedException();
	}

	public void Con_NXPrintf(in Con_NPrint_s info, ReadOnlySpan<char> msg) {
		throw new NotImplementedException();
	}
#endif

	public bool CopyFile(ReadOnlySpan<char> source, ReadOnlySpan<char> destination) {
		throw new NotImplementedException();
	}

	public Edict? CreateEdict(int forceEdictIndex = -1) {
		Edict? edict = ED.Alloc(forceEdictIndex);

		serverPluginHandler.OnEdictAllocated(edict);

		return edict;
	}

	public Edict? CreateFakeClient(ReadOnlySpan<char> netname) {
		GameClient? fcl = (GameClient?)sv.CreateFakeClient(netname);
		return fcl?.Edict;
	}

	public Edict? CreateFakeClientEx(ReadOnlySpan<char> netname, bool reportFakeClient = true) {
		sv.SetReportNewFakeClients(reportFakeClient);
		Edict? ret = CreateFakeClient(netname);
		sv.SetReportNewFakeClients(true);
		return ret;
	}

	public ISpatialPartition CreateSpatialPartition(in Vector3 worldmin, in Vector3 worldmax) {
		throw new NotImplementedException();
	}

	public void CrosshairAngle(Edict pClient, float pitch, float yaw) {
		throw new NotImplementedException();
	}

	public void DestroySpatialPartition(ISpatialPartition spatialPartition) {
		throw new NotImplementedException();
	}

	public void EmitAmbientSound(int entindex, in Vector3 pos, ReadOnlySpan<char> samp, float vol, SoundLevel soundlevel, int fFlags, int pitch, float delay = 0) {
		throw new NotImplementedException();
	}

	public bf_write? EntityMessageBegin(int ent_index, ServerClass ent_class, bool reliable) {
		if (s_MsgData.Started) {
			Sys.Error("EntityMessageBegin:  New message started before matching call to EndMessage.\n ");
			return null;
		}

		s_MsgData.Reset();

		Assert(ent_class != null);

		s_MsgData.Filter = null;
		s_MsgData.Reliable = reliable;
		s_MsgData.Started = true;
		s_MsgData.CurrentMsg = s_MsgData.EntityMsg;

		s_MsgData.EntityMsg.EntityIndex = ent_index;
		s_MsgData.EntityMsg.ClassID = ent_class.ClassID;
		s_MsgData.EntityMsg.DataOut.Reset();

		return s_MsgData.EntityMsg.DataOut;
	}

	public void FadeClientVolume(Edict edict, float fadePercent, float fadeOutSeconds, float holdTime, float fadeInSeconds) {
		int entnum = NUM_FOR_EDICT(edict);

		if (entnum < 1 || entnum > sv.GetClientCount()) {
			ConMsg("tried to DLL_FadeClientVolume a non-client\n");
			return;
		}

		GameClient client = sv.Client(entnum - 1);

		NET_StringCmd sndMsg = new($"soundfade {fadePercent:F1} {holdTime:F1} {fadeOutSeconds:F1} {fadeInSeconds:F1}");
		client.SendNetMsg(sndMsg);
	}

	public void ForceExactFile(ReadOnlySpan<char> s) {
		throw new NotImplementedException();
	}

	public void ForceModelBounds(ReadOnlySpan<char> s, in Vector3 mins, in Vector3 maxs) {
		throw new NotImplementedException();
	}

	public void ForceSimpleMaterial(ReadOnlySpan<char> s) {
		throw new NotImplementedException();
	}

	public int GetAppID() {
		throw new NotImplementedException();
	}

	public int GetArea(in Vector3 origin) {
		throw new NotImplementedException();
	}

	public void GetAreaBits(int area, Span<byte> bits) {
		throw new NotImplementedException();
	}

	public bool GetAreaPortalPlane(in Vector3 viewOrigin, int portalKey, out VPlane plane) {
		throw new NotImplementedException();
	}

	public IChangeInfoAccessor GetChangeAccessor(Edict edict) {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> GetClientConVarValue(int clientIndex, ReadOnlySpan<char> name) {
		if (clientIndex < 1 || clientIndex > sv.GetClientCount()) {
			DevMsg(1, "GetClientConVarValue: player invalid index %i\n", clientIndex);
			return "";
		}

		return sv.GetClient(clientIndex - 1)!.GetUserSetting(name);
	}

	public CSteamID? GetClientSteamID(Edict playerEdict) {
		int entnum = NUM_FOR_EDICT(playerEdict);
		return GetClientSteamIDByPlayerIndex(entnum);
	}

	public CSteamID? GetClientSteamIDByPlayerIndex(int entNum) {
		if (entNum < 1 || entNum > sv.GetClientCount())
			return null;

		GameClient? client = sv.Client(entNum - 1);
		if (client == null)
			return null;

		if (!client.IsConnected() || !client.SteamID.IsValid())
			return null;

		return client.SteamID;
	}

	public int GetClusterCount() {
		throw new NotImplementedException();
	}

	public int GetClusterForOrigin(in Vector3 org) {
		throw new NotImplementedException();
	}

	public int GetEntityCount() => sv.NumEdicts - sv.FreeEdicts;

	public ref readonly MaxEdictsBitVec GetEntityTransmitBitsForClient(int iClientIndex) {
		throw new NotImplementedException();
	}

	public void GetGameDir(Span<char> getGameDir) => strcpy(getGameDir, Common.Gamedir);

	public CSteamID? GetGameServerSteamID() {
		CSteamID sid = Steam3Server().GetGSSteamID();

		if (!sid.IsValid())
			return null;

		return sid;
	}

	public ReadOnlySpan<char> GetMapEntitiesString() {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> GetMostRecentlyLoadedFileName() {
		throw new NotImplementedException();
	}

	public bool GetPlayerInfo(int entNum, out PlayerInfo info) => sv.GetPlayerInfo(entNum - 1, out info);

	public INetChannelInfo? GetPlayerNetInfo(int playerIndex) {
		if (playerIndex < 1 || playerIndex > sv.GetClientCount())
			return null;

		GameClient client = sv.Client(playerIndex - 1);

		return client.NetChannel;
	}

	public ReadOnlySpan<char> GetPlayerNetworkIDString(Edict e) {
		if (!sv.IsActive() || e == null)
			return null;

		for (int i = 0; i < sv.GetClientCount(); i++) {
			GameClient cl = sv.Client(i);

			if (cl.Edict == e)
				return cl.GetNetworkIDString();
		}

		return null;
	}

	public int GetPlayerUserId(Edict e) {
		if (!sv.IsActive() || e == null)
			return -1;

		for (int i = 0; i < sv.GetClientCount(); i++) {
			GameClient cl = sv.Client(i);

			if (cl.Edict == e)
				return cl.UserID;
		}

		return -1;
	}

	public int GetPVSForCluster(int cluster, Span<byte> outputpvs) {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> GetSaveFileName() {
		throw new NotImplementedException();
	}

	public int GetServerVersion() => GetSteamInfIDVersionInfo().ServerVersion;

	public int IndexOfEdict(Edict? edict) {
		if (edict == null)
			return 0;

		int index = Array.IndexOf(sv.Edicts!, edict);
		if (index < 0 || index > sv.MaxEdicts)
			Sys.Error($"Bad entity in IndexOfEdict() index {index} pEdict {edict} sv.edicts {sv.Edicts}\n");

		return index;
	}

	public void InsertServerCommand(ReadOnlySpan<char> str) {
		throw new NotImplementedException();
	}

	public bool IsClientFullyAuthenticated(Edict edict) {
		int entnum = NUM_FOR_EDICT(edict);
		if (entnum < 1 || entnum > sv.GetClientCount())
			return false;

		GameClient? client = sv.Client(entnum - 1);
		return client?.IsFullyAuthenticated() ?? false;
	}

	public bool IsDecalPrecached(ReadOnlySpan<char> s) {
		throw new NotImplementedException();
	}

	public bool IsDedicatedServer() => sv.IsDedicated();

	public bool IsGenericPrecached(ReadOnlySpan<char> s) {
		throw new NotImplementedException();
	}

	public int IsInCommentaryMode() {
		throw new NotImplementedException();
	}

	public int IsInEditMode() {
		throw new NotImplementedException();
	}

	public bool IsInternalBuild() => false;

	public bool IsLowViolence() {
		throw new NotImplementedException();
	}

	public int IsMapValid(ReadOnlySpan<char> filename) => modelloader.Map_IsValid(filename) ? 1 : 0;

	public bool IsModelPrecached(ReadOnlySpan<char> s) => SV.ModelIndex(s) != -1;

	public bool IsPaused() => sv.IsPaused();

	public void LightStyle(int style, ReadOnlySpan<char> val) {
		throw new NotImplementedException();
	}

	public void LoadAdjacentEnts(ReadOnlySpan<char> oldLevel, ReadOnlySpan<char> landmarkName) {
		throw new NotImplementedException();
	}

	public bool LoadGameState(ReadOnlySpan<char> mapName, bool createPlayers) {
		throw new NotImplementedException();
	}

	public bool LockNetworkStringTables(bool shouldLock) {
		throw new NotImplementedException();
	}

	public void LogPrint(ReadOnlySpan<char> msg) {
		throw new NotImplementedException();
	}

	int Message_CheckMessageLength() {
		if (s_MsgData.CurrentMsg == s_MsgData.UserMsg) {
			Span<char> msgname = stackalloc char[256];
			int msgsize = -1;
			int msgtype = s_MsgData.UserMsg.MessageType;

			if (!serverGameDLL.GetUserMessageInfo(msgtype, msgname, out msgsize)) {
				Warning($"Unable to find user message for index {msgtype}\n");
				return -1;
			}

			int bytesWritten = s_MsgData.UserMsg.DataOut.BytesWritten;

			if (msgsize == -1) {
				if (bytesWritten > Constants.MAX_USER_MSG_DATA) {
					Warning($"DLL_MessageEnd:  Refusing to send user message {msgname} of {bytesWritten} bytes to client, user message size limit is {Constants.MAX_USER_MSG_DATA} bytes\n");
					return -1;
				}
			}
			else if (msgsize != bytesWritten) {
				Warning($"User Msg '{msgname}': {bytesWritten} bytes written, expected {msgsize}\n");
				return -1;
			}

			return bytesWritten; // all checks passed, estimated final length
		}

		if (s_MsgData.CurrentMsg == s_MsgData.EntityMsg) {
			int bytesWritten = s_MsgData.EntityMsg.DataOut.BytesWritten;

			if (bytesWritten > Constants.MAX_ENTITY_MSG_DATA) // TODO use a define or so
			{
				Warning($"Entity Message to {s_MsgData.EntityMsg.EntityIndex}, {bytesWritten} bytes written (max is {Constants.MAX_ENTITY_MSG_DATA})\n");
				return -1;
			}

			return bytesWritten; // all checks passed, estimated final length
		}

		Warning("MessageEnd unknown message type.\n");
		return -1;

	}

	public void MessageEnd() {
		if (!s_MsgData.Started) {
			Sys.Error("MESSAGE_END called with no active message\n");
			return;
		}

		int length = Message_CheckMessageLength();

		// check to see if it's a valid message
		if (length < 0) {
			s_MsgData.Reset(); // clear message data
			return;
		}

		if (s_MsgData.Filter != null) {
			// send entity/user messages only to full connected clients in filter
			sv.BroadcastMessage(s_MsgData.CurrentMsg!, s_MsgData.Filter);
		}
		else {
			// send entity messages to all full connected clients 
			sv.BroadcastMessage(s_MsgData.CurrentMsg!, true, s_MsgData.Reliable);
		}

		s_MsgData.Reset(); // clear message data
	}

	public void Message_DetermineMulticastRecipients(bool usepas, in Vector3 origin, ref AbsolutePlayerLimitBitVec playerbits) {
		throw new NotImplementedException();
	}

	public void MultiplayerEndGame() {
		throw new NotImplementedException();
	}

	public void NotifyEdictFlagsChange(int edict) => CL.LocalNetworkBackdoor?.NotifyEdictFlagsChange((uint)edict);

	public ReadOnlySpan<char> ParseFile(ReadOnlySpan<char> data, Span<char> token) {
		throw new NotImplementedException();
	}

	public Edict? PEntityOfEntIndex(int iEntIndex) {
		if (iEntIndex >= 0 && iEntIndex < sv.MaxEdicts) {
			Edict? edict = sv.Edicts![iEntIndex];
			if (!edict.IsFree())
				return edict;
		}

		return null;
	}

	public void PlaybackTempEntity(IRecipientFilter filter, float delay, object sender, SendTable st, int classID) {
		throw new NotImplementedException();
	}

	public int PrecacheDecal(ReadOnlySpan<char> name, bool preload = false) {
		throw new NotImplementedException();
	}

	public int PrecacheGeneric(ReadOnlySpan<char> s, bool preload = false) {
		throw new NotImplementedException();
	}

	public int PrecacheModel(ReadOnlySpan<char> s, bool preload = false) {
		throw new NotImplementedException();
	}

	public int PrecacheSentenceFile(ReadOnlySpan<char> s, bool preload = false) {
		throw new NotImplementedException();
	}

	public void RemoveEdict(Edict e) {
		serverPluginHandler.OnEdictFreed(e);
		ED.Free(e);
	}

	public void ResetPVS(Span<byte> pvs) {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> SentenceGrounameFromIndex(int groupIndex) {
		throw new NotImplementedException();
	}

	public int SentenceGroupIndexFromName(ReadOnlySpan<char> pGrouname) {
		throw new NotImplementedException();
	}

	public int SentenceGroupPick(int groupIndex, Span<char> name) {
		throw new NotImplementedException();
	}

	public int SentenceGroupPickSequential(int groupIndex, Span<char> name, int sentenceIndex, int reset) {
		throw new NotImplementedException();
	}

	public int SentenceIndexFromName(ReadOnlySpan<char> pSentenceName) {
		throw new NotImplementedException();
	}

	public float SentenceLength(int sentenceIndex) {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> SentenceNameFromIndex(int sentenceIndex) {
		throw new NotImplementedException();
	}

	static bool ValidCmd(ReadOnlySpan<char> cmd) {
		int len = (int)strlen(cmd);
		return len != 0 && (cmd[len - 1] == '\n' || cmd[len - 1] == ';');
	}

	public void ServerCommand(ReadOnlySpan<char> str) {
		if (str.IsEmpty)
			Sys.Error("ServerCommand with NULL string\n");

		if (ValidCmd(str))
			Cbuf.AddText(str);
		else
			ConMsg($"Error, bad server command {str}\n");
	}

	public void ServerExecute() => Cbuf.Execute();

	public void SetAreaPortalState(int portalNumber, int isOpen) {
		throw new NotImplementedException();
	}

	public void SetAreaPortalStates(ReadOnlySpan<int> portalNumbers, ReadOnlySpan<int> isOpen) {
		throw new NotImplementedException();
	}

	public void SetDedicatedServerBenchmarkMode(bool benchmarkMode) {
		throw new NotImplementedException();
	}

	public void SetFakeClientConVarValue(Edict pEntity, ReadOnlySpan<char> cvar, ReadOnlySpan<char> value) {
		int clientnum = NUM_FOR_EDICT(pEntity);
		if (clientnum < 1 || clientnum > sv.GetClientCount())
			host.Error("DLL_SetView: not a client");

		GameClient client = sv.Client(clientnum - 1);
		if (client.IsFakeClient()) {
			client.SetUserCVar(cvar, value);
			client.ConVarsChanged = true;
		}
	}

	public void SetView(Edict client, Edict viewent) {
		int clientnum = NUM_FOR_EDICT(client);
		if (clientnum < 1 || clientnum > sv.GetClientCount())
			host.Error("DLL_SetView: not a client");

		GameClient cl = sv.Client(clientnum - 1);
		cl.ViewEntity = viewent;

		SVC_SetView view = new(NUM_FOR_EDICT(viewent));
		cl.SendNetMsg(view);
	}

	public void SolidMoved(Edict pSolidEnt, ICollideable pSolidCollide, in Vector3 prevAbsOrigin, bool testSurroundingBoundsOnly) {
		throw new NotImplementedException();
	}

	public void StaticDecal(in Vector3 originInEntitySpace, int decalIndex, int entityIndex, int modelIndex, bool lowpriority) {
		SVC_BSPDecal decal = new() {
			Pos = originInEntitySpace,
			DecalTextureIndex = decalIndex,
			EntityIndex = entityIndex,
			ModelIndex = modelIndex,
			LowPriority = lowpriority
		};

		if (sv.AllowSignOnWrites)
			decal.WriteToBuffer(sv.Signon);
		else
			sv.BroadcastMessage(decal, false, true);
	}

	public ref ClientTextMessage TextMessageGet(ReadOnlySpan<char> name) {
		throw new NotImplementedException();
	}

	public TimeUnit_t Time() => Sys.Time;

	public void TriggerMoved(Edict pTriggerEnt, bool testSurroundingBoundsOnly) {
		throw new NotImplementedException();
	}

	class MsgData
	{
		public MsgData() {
			Reset();

			EntityMsg.DataOut.StartWriting(EntityData, EntityData.Length);
			EntityMsg.DataOut.DebugName = "s_MsgData.EntityMsg.DataOut";

			UserMsg.DataOut.StartWriting(UserData, UserData.Length);
			UserMsg.DataOut.DebugName = "s_MsgData.UserMsg.DataOut";
		}

		public void Reset() {
			Filter = null;
			Reliable = false;
			SubType = 0;
			Started = false;
			UserMessageSize = -1;
			UserMessageName = null;
			CurrentMsg = null;
		}

		public readonly byte[] UserData = new byte[PAD_NUMBER(Constants.MAX_USER_MSG_DATA, 4)];    // buffer for outgoing user messages
		public readonly byte[] EntityData = new byte[PAD_NUMBER(Constants.MAX_ENTITY_MSG_DATA, 4)]; // buffer for outgoing entity messages

		public IRecipientFilter? Filter;       // clients who get this message
		public bool Reliable;

		public INetMessage? CurrentMsg;                // pointer to entityMsg or userMessage
		public int SubType;            // usermessage index
		public bool Started;           // IS THERE A MESSAGE IN THE PROCESS OF BEING SENT?
		public int UserMessageSize;
		public string? UserMessageName;

		public readonly SVC_EntityMessage EntityMsg = new();
		public readonly SVC_UserMessage UserMsg = new();
	}
	static readonly MsgData s_MsgData = new();

	public bf_write UserMessageBegin(in IRecipientFilter filter, int msg_index) {
		if (s_MsgData.Started) {
			Sys.Error("UserMessageBegin:  New message started before matching call to EndMessage.\n");
			return null!;
		}

		s_MsgData.Reset();

		Assert(filter);

		s_MsgData.Filter = filter;
		s_MsgData.Reliable = filter.IsReliable();
		s_MsgData.Started = true;

		s_MsgData.CurrentMsg = s_MsgData.UserMsg;

		s_MsgData.UserMsg.MessageType = msg_index;

		s_MsgData.UserMsg.DataOut.Reset();
		return s_MsgData.UserMsg.DataOut;
	}

	public CheckTransmitInfo GetPrevCheckTransmitInfo(Edict playerEdict) {
		int entnum = NUM_FOR_EDICT(playerEdict);
		if (entnum < 1 || entnum > sv.GetClientCount())
			Error("Invalid client specified in GetPrevCheckTransmitInfo\n");

		GameClient client = sv.Client(entnum - 1);
		return client.GetPrevPackInfo();
	}

	public SharedEdictChangeInfo GetSharedEdictChangeInfo() => g_roSharedEdictChangeInfo;
}
