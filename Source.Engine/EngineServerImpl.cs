using Source.Common;
using Source.Common.Audio;
using Source.Common.Bitbuffers;
using Source.Common.Client;
using Source.Common.Engine;
using Source.Common.Formats.Keyvalues;
using Source.Common.Mathematics;
using Source.Common.Networking;
using Source.Common.Server;

using Steamworks;

using System.Numerics;

namespace Source.Engine;

internal class EngineServer(Cbuf Cbuf) : IEngineServer
{
	public void AddOriginToPVS(in Vector3 origin) {
		throw new NotImplementedException();
	}

	public void AllowImmediateEdictReuse() {
		throw new NotImplementedException();
	}

	public void BuildEntityClusterList(Edict edict, ref PVSInfo pvsInfo) {
		throw new NotImplementedException();
	}

	public void ChangeLevel(ReadOnlySpan<char> s1, ReadOnlySpan<char> s2) {
		throw new NotImplementedException();
	}

	public void ChangeTeam(ReadOnlySpan<char> pTeamName) {
		throw new NotImplementedException();
	}

	public int CheckAreasConnected(int area1, int area2) {
		throw new NotImplementedException();
	}

	public bool CheckBoxInPVS(in Vector3 mins, in Vector3 maxs, ReadOnlySpan<byte> checkpvs) {
		throw new NotImplementedException();
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
		throw new NotImplementedException();
	}

	public int CompareFileTime(ReadOnlySpan<char> filename1, ReadOnlySpan<char> filename2, ref int compare) {
		throw new NotImplementedException();
	}

	public void Con_NPrintf(int pos, ReadOnlySpan<char> msg) {
		throw new NotImplementedException();
	}

	public void Con_NXPrintf(in Con_NPrint_s info, ReadOnlySpan<char> msg) {
		throw new NotImplementedException();
	}

	public bool CopyFile(ReadOnlySpan<char> source, ReadOnlySpan<char> destination) {
		throw new NotImplementedException();
	}

	public Edict CreateEdict(int iForceEdictIndex = -1) {
		throw new NotImplementedException();
	}

	public Edict CreateFakeClient(ReadOnlySpan<char> netname) {
		throw new NotImplementedException();
	}

	public Edict CreateFakeClientEx(ReadOnlySpan<char> netname, bool bReportFakeClient = true) {
		throw new NotImplementedException();
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

	public bf_write EntityMessageBegin(int ent_index, ServerClass ent_class, bool reliable) {
		throw new NotImplementedException();
	}

	public void FadeClientVolume(Edict edict, float fadePercent, float fadeOutSeconds, float holdTime, float fadeInSeconds) {
		throw new NotImplementedException();
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
		throw new NotImplementedException();
	}

	public ref readonly CSteamID GetClientSteamID(Edict playerEdict) {
		throw new NotImplementedException();
	}

	public ref readonly CSteamID GetClientSteamIDByPlayerIndex(int entNum) {
		throw new NotImplementedException();
	}

	public int GetClusterCount() {
		throw new NotImplementedException();
	}

	public int GetClusterForOrigin(in Vector3 org) {
		throw new NotImplementedException();
	}

	public int GetEntityCount() {
		throw new NotImplementedException();
	}

	public ref readonly MaxEdictsBitVec GetEntityTransmitBitsForClient(int iClientIndex) {
		throw new NotImplementedException();
	}

	public void GetGameDir(Span<char> getGameDir) {
		throw new NotImplementedException();
	}

	public ref readonly CSteamID GetGameServerSteamID() {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> GetMapEntitiesString() {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> GetMostRecentlyLoadedFileName() {
		throw new NotImplementedException();
	}

	public bool GetPlayerInfo(int entNum, out PlayerInfo info) {
		throw new NotImplementedException();
	}

	public INetChannelInfo GetPlayerNetInfo(int playerIndex) {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> GetPlayerNetworkIDString(Edict e) {
		throw new NotImplementedException();
	}

	public int GetPlayerUserId(Edict e) {
		throw new NotImplementedException();
	}

	public int GetPVSForCluster(int cluster, Span<byte> outputpvs) {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> GetSaveFileName() {
		throw new NotImplementedException();
	}

	public int GetServerVersion() {
		throw new NotImplementedException();
	}

	public int IndexOfEdict(Edict edict) {
		throw new NotImplementedException();
	}

	public void InsertServerCommand(ReadOnlySpan<char> str) {
		throw new NotImplementedException();
	}

	public bool IsClientFullyAuthenticated(Edict edict) {
		throw new NotImplementedException();
	}

	public bool IsDecalPrecached(ReadOnlySpan<char> s) {
		throw new NotImplementedException();
	}

	public bool IsDedicatedServer() {
		throw new NotImplementedException();
	}

	public bool IsGenericPrecached(ReadOnlySpan<char> s) {
		throw new NotImplementedException();
	}

	public int IsInCommentaryMode() {
		throw new NotImplementedException();
	}

	public int IsInEditMode() {
		throw new NotImplementedException();
	}

	public bool IsInternalBuild() {
		throw new NotImplementedException();
	}

	public bool IsLowViolence() {
		throw new NotImplementedException();
	}

	public int IsMapValid(ReadOnlySpan<char> filename) {
		throw new NotImplementedException();
	}

	public bool IsModelPrecached(ReadOnlySpan<char> s) {
		throw new NotImplementedException();
	}

	public bool IsPaused() {
		throw new NotImplementedException();
	}

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

	public void MessageEnd() {
		throw new NotImplementedException();
	}

	public void Message_DetermineMulticastRecipients(bool usepas, in Vector3 origin, ref AbsolutePlayerLimitBitVec playerbits) {
		throw new NotImplementedException();
	}

	public void MultiplayerEndGame() {
		throw new NotImplementedException();
	}

	public void NotifyEdictFlagsChange(int iEdict) {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> ParseFile(ReadOnlySpan<char> data, Span<char> token) {
		throw new NotImplementedException();
	}

	public Edict PEntityOfEntIndex(int iEntIndex) {
		throw new NotImplementedException();
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
		throw new NotImplementedException();
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

	public void ServerExecute() {
		Cbuf.Execute();
	}

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
		throw new NotImplementedException();
	}

	public void SetView(Edict pClient, Edict pViewent) {
		throw new NotImplementedException();
	}

	public void SolidMoved(Edict pSolidEnt, ICollideable pSolidCollide, in Vector3 prevAbsOrigin, bool testSurroundingBoundsOnly) {
		throw new NotImplementedException();
	}

	public void StaticDecal(in Vector3 originInEntitySpace, int decalIndex, int entityIndex, int modelIndex, bool lowpriority) {
		throw new NotImplementedException();
	}

	public ref ClientTextMessage TextMessageGet(ReadOnlySpan<char> name) {
		throw new NotImplementedException();
	}

	public TimeUnit_t Time() {
		throw new NotImplementedException();
	}

	public void TriggerMoved(Edict pTriggerEnt, bool testSurroundingBoundsOnly) {
		throw new NotImplementedException();
	}

	public bf_write UserMessageBegin(IRecipientFilter filter, int msg_type) {
		throw new NotImplementedException();
	}
}
