using Source.Common.Audio;
using Source.Common.Bitbuffers;
using Source.Common.Client;
using Source.Common.Engine;
using Source.Common.Formats.Keyvalues;
using Source.Common.Mathematics;
using Source.Common.Networking;

using Steamworks;

using System.Numerics;

namespace Source.Common.Server;

/// <summary>
/// Interface the engine exposes to the game DLL.
/// </summary>
public interface IEngineServer
{
	void ChangeLevel(ReadOnlySpan<char> s1, ReadOnlySpan<char> s2);

	// Ask engine whether the specified map is a valid map file (exists and has valid version number).
	int IsMapValid(ReadOnlySpan<char> filename);

	// Is this a dedicated server?
	bool IsDedicatedServer();

	// Is in Hammer editing mode?
	int IsInEditMode();

	// Add to the server/client lookup/precache table, the specified string is given a unique index
	// NOTE: The indices for PrecacheModel are 1 based
	//  a 0 returned from those methods indicates the model or sound was not correctly precached
	// However, generic and decal are 0 based
	// If preload is specified, the file is loaded into the server/client's cache memory before level startup, otherwise
	//  it'll only load when actually used (which can cause a disk i/o hitch if it occurs during play of a level).
	int PrecacheModel(ReadOnlySpan<char> s, bool preload = false);
	int PrecacheSentenceFile(ReadOnlySpan<char> s, bool preload = false);
	int PrecacheDecal(ReadOnlySpan<char> name, bool preload = false);
	int PrecacheGeneric(ReadOnlySpan<char> s, bool preload = false);

	// Check's if the name is precached, but doesn't actually precache the name if not...
	bool IsModelPrecached(ReadOnlySpan<char> s);
	bool IsDecalPrecached(ReadOnlySpan<char> s);
	bool IsGenericPrecached(ReadOnlySpan<char> s);

	// Note that sounds are precached using the IEngineSound interface

	// Special purpose PVS checking
	// Get the cluster # for the specified position
	int GetClusterForOrigin(in Vector3 org);
	// Get the PVS bits for a specified cluster and copy the bits into outputpvs.  Returns the number of bytes needed to pack the PVS
	int GetPVSForCluster(int cluster, Span<byte> outputpvs);
	// Check whether the specified origin is inside the specified PVS
	bool CheckOriginInPVS(in Vector3 org, ReadOnlySpan<byte> checkpvs);
	// Check whether the specified worldspace bounding box is inside the specified PVS
	bool CheckBoxInPVS(in Vector3 mins, in Vector3 maxs, ReadOnlySpan<byte> checkpvs);

	// Returns the server assigned userid for this player.  Useful for logging frags, etc.  
	//  returns -1 if the edict couldn't be found in the list of players.
	int GetPlayerUserId(Edict e);
	ReadOnlySpan<char> GetPlayerNetworkIDString(Edict e);

	// Return the current number of used edict slots
	int GetEntityCount();
	// Given an edict, returns the entity index
	int IndexOfEdict(Edict? edict);
	// Given and entity index, returns the corresponding edict pointer
	Edict? PEntityOfEntIndex(int iEntIndex);

	// Get stats info interface for a client netchannel
	INetChannelInfo GetPlayerNetInfo(int playerIndex);

	// Allocate space for string and return index/offset of string in global string list
	// If iForceEdictIndex is not -1, then it will return the edict with that index. If that edict index
	// is already used, it'll return null.
	Edict CreateEdict(int iForceEdictIndex = -1);
	// Remove the specified edict and place back into the free edict list
	void RemoveEdict(Edict e);


	// Emit an ambient sound associated with the specified entity
	void EmitAmbientSound(int entindex, in Vector3 pos, ReadOnlySpan<char> samp, float vol, SoundLevel soundlevel, int fFlags, int pitch, float delay = 0.0f);

	// Fade out the client's volume level toward silence (or fadePercent)
	void FadeClientVolume(Edict edict, float fadePercent, float fadeOutSeconds, float holdTime, float fadeInSeconds);

	// Sentences / sentence groups
	int SentenceGroupPick(int groupIndex, Span<char> name);
	int SentenceGroupPickSequential(int groupIndex, Span<char> name, int sentenceIndex, int reset);
	int SentenceIndexFromName(ReadOnlySpan<char> pSentenceName);
	ReadOnlySpan<char> SentenceNameFromIndex(int sentenceIndex);
	int SentenceGroupIndexFromName(ReadOnlySpan<char> pGrouname);
	ReadOnlySpan<char> SentenceGrounameFromIndex(int groupIndex);
	float SentenceLength(int sentenceIndex);

	// Issue a command to the command parser as if it was typed at the server console.	
	void ServerCommand(ReadOnlySpan<char> str);
	// Execute any commands currently in the command parser immediately (instead of once per frame)
	void ServerExecute();
	// Issue the specified command to the specified client (mimics that client typing the command at the console).
	void ClientCommand(Edict edict, ReadOnlySpan<char> cmd);

	// Set the lightstyle to the specified value and network the change to any connected clients.  Note that val must not 
	//  change place in memory (use MAKE_STRING) for anything that's not compiled into your mod.
	void LightStyle(int style, ReadOnlySpan<char> val);

	// Project a static decal onto the specified entity / model (for level placed decals in the .bsp)
	void StaticDecal(in Vector3 originInEntitySpace, int decalIndex, int entityIndex, int modelIndex, bool lowpriority);

	// Given the current PVS(or PAS) and origin, determine which players should hear/receive the message
	void Message_DetermineMulticastRecipients(bool usepas, in Vector3 origin, ref AbsolutePlayerLimitBitVec playerbits);

	// Begin a message from a server side entity to its client side counterpart (func_breakable glass, e.g.)
	bf_write EntityMessageBegin(int ent_index, ServerClass ent_class, bool reliable);
	// Begin a usermessage from the server to the client .dll
	bf_write UserMessageBegin(in IRecipientFilter filter, int msg_type);
	// Finish the Entity or UserMessage and dispatch to network layer
	void MessageEnd();

	// Print szMsg to the client console.
	void ClientPrintf(Edict edict, ReadOnlySpan<char> szMsg);

	// SINGLE PLAYER/LISTEN SERVER ONLY (just matching the client .dll api for this)
	// Prints the formatted string to the notification area of the screen ( down the right hand edge
	//  numbered lines starting at position 0
	void Con_NPrintf(int pos, ReadOnlySpan<char> msg);
	// SINGLE PLAYER/LISTEN SERVER ONLY(just matching the client .dll api for this)
	// Similar to Con_NPrintf, but allows specifying custom text color and duration information
	void Con_NXPrintf(in Con_NPrint_s info, ReadOnlySpan<char> msg);

	// Change a specified player's "view entity" (i.e., use the view entity position/orientation for rendering the client view)
	void SetView(Edict pClient, Edict pViewent);

	// Get a high precision timer for doing profiling work
	TimeUnit_t Time();

	// Set the player's crosshair angle
	void CrosshairAngle(Edict pClient, float pitch, float yaw);

	// Get the current game directory (hl2, tf2, hl1, cstrike, etc.)
	void GetGameDir(Span<char> getGameDir);

	// Used by AI node graph code to determine if .bsp and .ain files are out of date
	int CompareFileTime(ReadOnlySpan<char> filename1, ReadOnlySpan<char> filename2, ref int compare);

	// Locks/unlocks the network string tables (.e.g, when adding bots to server, this needs to happen).
	// Be sure to reset the lock after executing your code!!!
	bool LockNetworkStringTables(bool shouldLock);

	// Create a bot with the given name.  Returns NULL if fake client can't be created
	Edict CreateFakeClient(ReadOnlySpan<char> netname);

	// Get a convar keyvalue for s specified client
	ReadOnlySpan<char> GetClientConVarValue(int clientIndex, ReadOnlySpan<char> name);

	// Parse a token from a file
	ReadOnlySpan<char> ParseFile(ReadOnlySpan<char> data, Span<char> token);
	// Copies a file
	bool CopyFile(ReadOnlySpan<char> source, ReadOnlySpan<char> destination);

	// Reset the pvs, pvssize is the size in bytes of the buffer pointed to by pvs.
	// This should be called right before any calls to AddOriginToPVS
	void ResetPVS(Span<byte> pvs);
	// Merge the pvs bits into the current accumulated pvs based on the specified origin ( not that each pvs origin has an 8 world unit fudge factor )
	void AddOriginToPVS(in Vector3 origin);

	// Mark a specified area portal as open/closed.
	// Use SetAreaPortalStates if you want to set a bunch of them at a time.
	void SetAreaPortalState(int portalNumber, int isOpen);

	// Queue a temp entity for transmission
	void PlaybackTempEntity(IRecipientFilter filter, float delay, object sender, SendTable st, int classID);
	// Given a node number and the specified PVS, return with the node is in the PVS
	int CheckHeadnodeVisible(int nodenum, Span<byte> pvs);
	// Using area bits, cheeck whether area1 flows into area2 and vice versa (depends on area portal state)
	int CheckAreasConnected(int area1, int area2);
	// Given an origin, determine which area index the origin is within
	int GetArea(in Vector3 origin);
	// Get area portal bit set
	void GetAreaBits(int area, Span<byte> bits);
	// Given a view origin (which tells us the area to start looking in) and a portal key,
	// fill in the plane that leads out of this area (it points into whatever area it leads to).
	bool GetAreaPortalPlane(in Vector3 viewOrigin, int portalKey, out VPlane plane);

	// Save/restore wrapper - FIXME:  At some point we should move this to it's own interface
	bool LoadGameState(ReadOnlySpan<char> mapName, bool createPlayers);
	void LoadAdjacentEnts(ReadOnlySpan<char> oldLevel, ReadOnlySpan<char> landmarkName);
	void ClearSaveDir();

	// Get the pristine map entity lump string.  (e.g., used by CS to reload the map entities when restarting a round.)
	ReadOnlySpan<char> GetMapEntitiesString();

	// Text message system -- lookup the text message of the specified name
	ref ClientTextMessage TextMessageGet(ReadOnlySpan<char> name);

	// Print a message to the server log file
	void LogPrint(ReadOnlySpan<char> msg);

	// Builds PVS information for an entity
	void BuildEntityClusterList(Edict edict, ref PVSInfo pvsInfo);

	// A solid entity moved, update spatial partition
	void SolidMoved(Edict pSolidEnt, ICollideable pSolidCollide, in Vector3 prevAbsOrigin, bool testSurroundingBoundsOnly);
	// A trigger entity moved, update spatial partition
	void TriggerMoved(Edict pTriggerEnt, bool testSurroundingBoundsOnly);

	// Create/destroy a custom spatial partition
	ISpatialPartition CreateSpatialPartition(in Vector3 worldmin, in Vector3 worldmax);
	void DestroySpatialPartition(ISpatialPartition spatialPartition);

	// scratch pad?

	// This returns which entities, to the best of the server's knowledge, the client currently knows about.
	// This is really which entities were in the snapshot that this client last acked.
	// This returns a bit Vector3 with one bit for each entity.
	//
	// USE WITH CARE. Whatever tick the client is really currently on is subject to timing and
	// ordering differences, so you should account for about a quarter-second discrepancy in here.
	// Also, this will return NULL if the client doesn't exist or if this client hasn't acked any frames yet.
	// 
	// iClientIndex is the CLIENT index, so if you use pPlayer->entindex(), subtract 1.
	ref readonly MaxEdictsBitVec GetEntityTransmitBitsForClient(int iClientIndex);

	// Is the game paused?
	bool IsPaused();

	// Marks the filename for consistency checking.  This should be called after precaching the file.
	void ForceExactFile(ReadOnlySpan<char> s);
	void ForceModelBounds(ReadOnlySpan<char> s, in Vector3 mins, in Vector3 maxs);
	void ClearSaveDirAfterClientLoad();

	// Sets a USERINFO client ConVar for a fakeclient
	void SetFakeClientConVarValue(Edict pEntity, ReadOnlySpan<char> cvar, ReadOnlySpan<char> value);

	// Marks the material (vmt file) for consistency checking.  If the client and server have different
	// contents for the file, the client's vmt can only use the VertexLitGeneric shader, and can only
	// contain $baseTexture and $bumpmap vars.
	void ForceSimpleMaterial(ReadOnlySpan<char> s);

	// Is the engine in Commentary mode?
	int IsInCommentaryMode();


	// Mark some area portals as open/closed. It's more efficient to use this
	// than a bunch of individual SetAreaPortalState calls.
	void SetAreaPortalStates(ReadOnlySpan<int> portalNumbers, ReadOnlySpan<int> isOpen);

	// Called when relevant edict state flags change.
	void NotifyEdictFlagsChange(int iEdict);

	// Tells the engine we can immdiately re-use all edict indices
	// even though we may not have waited enough time
	void AllowImmediateEdictReuse();

	// Returns true if the engine is an internal build. i.e. is using the internal bugreporter.
	bool IsInternalBuild();

	IChangeInfoAccessor GetChangeAccessor(Edict edict);

	// Name of most recently load .sav file
	ReadOnlySpan<char> GetMostRecentlyLoadedFileName();
	ReadOnlySpan<char> GetSaveFileName();

	// Matchmaking
	void MultiplayerEndGame();
	void ChangeTeam(ReadOnlySpan<char> pTeamName);

	// Cleans up the cluster list
	void CleanUpEntityClusterList(ref PVSInfo pvsInfo);

	// TODO: Achievements

	int GetAppID();

	bool IsLowViolence();

	// TODO: StartQueryCvarValue

	void InsertServerCommand(ReadOnlySpan<char> str);

	// Fill in the player info structure for the specified player index (name, model, etc.)
	bool GetPlayerInfo(int entNum, out PlayerInfo info);

	// Returns true if this client has been fully authenticated by Steam
	bool IsClientFullyAuthenticated(Edict edict);

	// This makes the host run 1 tick per frame instead of checking the system timer to see how many ticks to run in a certain frame.
	// i.e. it does the same thing timedemo does.
	void SetDedicatedServerBenchmarkMode(bool benchmarkMode);

	// TODO: GetGamestatsData/SetGamestatsData

	// Returns the SteamID of the specified player. It'll be NULL if the player hasn't authenticated yet.
	ref readonly CSteamID GetClientSteamID(Edict playerEdict);

	// Returns the SteamID of the game server
	ref readonly CSteamID GetGameServerSteamID();

	// Send a client command keyvalues
	// keyvalues are deleted inside the function
	void ClientCommandKeyValues(Edict edict, KeyValues command);

	// Returns the SteamID of the specified player. It'll be NULL if the player hasn't authenticated yet.
	ref readonly CSteamID GetClientSteamIDByPlayerIndex(int entNum);
	// Gets a list of all clusters' bounds.  Returns total number of clusters.
	int GetClusterCount();
	// TODO: GetAllClusterBounds

	// Create a bot with the given name.  Returns NULL if fake client can't be created
	Edict CreateFakeClientEx(ReadOnlySpan<char> netname, bool bReportFakeClient = true);

	// Server version from the steam.inf, this will be compared to the GC version
	int GetServerVersion();
}
