using Source.Common.Audio;
using Source.Common.Bitbuffers;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Filesystem;
using Source.Common.Formats.BSP;
using Source.Common.Hashing;
using Source.Common.Input;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;
using Source.Common.Networking;

namespace Source.Common.Client;

/// <summary>
/// Interface exposed from the client DLL back to the engine
/// </summary>
public interface IBaseClientDLL
{
	bool Init();
	void PostInit();

	// Called once when the client DLL is being unloaded
	void Shutdown();

	// Called once the client is initialized to setup client-side replay interface pointers
	bool ReplayInit();
	bool ReplayPostInit();

	// Called at the start of each level change
	void LevelInitPreEntity(ReadOnlySpan<char> mapName);
	// Called at the start of a new level, after the entities have been received and created
	void LevelInitPostEntity();
	// Called at the end of a level
	void LevelShutdown();

	// Request a pointer to the list of client datatable classes
	ClientClass? GetAllClasses();

	// Called once per level to re-initialize any hud element drawing stuff
	int HudVidInit();
	// Called by the engine when gathering user input
	void HudProcessInput(bool bActive);
	// Called oncer per frame to allow the hud elements to think
	void HudUpdate(bool bActive);
	// Reset the hud elements to their initial states
	void HudReset();
	// Display a hud text message
	void HudText(ReadOnlySpan<char> message);

	// Mouse Input Interfaces
	// Activate the mouse (hides the cursor and locks it to the center of the screen)
	void IN_ActivateMouse();
	// Deactivates the mouse (shows the cursor and unlocks it)
	void IN_DeactivateMouse();
	// This is only called during extra sound updates and just accumulates mouse x, y offets and recenters the mouse.
	//  This call is used to try to prevent the mouse from appearing out of the side of a windowed version of the engine if 
	//  rendering or other processing is taking too long
	void IN_Accumulate();
	// Reset all key and mouse states to their initial, unpressed state
	void IN_ClearStates();
	// If key is found by name, returns whether it's being held down in isdown, otherwise function returns false
	bool IN_IsKeyDown(ReadOnlySpan<char> name, out bool isdown);
	// Notify the client that the mouse was wheeled while in game - called prior to executing any bound commands.
	void IN_OnMouseWheeled(int nDelta);
	// Raw keyboard signal, if the client .dll returns 1, the engine processes the key as usual, otherwise,
	//  if the client .dll returns 0, the key is swallowed.
	int IN_KeyEvent(int eventcode, ButtonCode keynum, ReadOnlySpan<char> currentBinding);

	// This function is called once per tick to create the player CUserCmd (used for prediction/physics simulation of the player)
	// Because the mouse can be sampled at greater than the tick interval, there is a separate input_sample_frametime, which
	//  specifies how much additional mouse / keyboard simulation to perform.
	void CreateMove(
								int sequence_number,            // sequence_number of this cmd
								float input_sample_frametime,   // Frametime for mouse input sampling
								bool active);               // True if the player is active (not paused)

	// If the game is running faster than the tick_interval framerate, then we do extra mouse sampling to avoid jittery input
	//  This code path is much like the normal move creation code, except no move is created
	void ExtraMouseSample(float frametime, bool active);

	// Encode the delta (changes) between the CUserCmd in slot from vs the one in slot to.  The game code will have
	//  matching logic to read the delta.
	bool WriteUsercmdDeltaToBuffer(bf_write buf, int from, int to, bool isnewcommand);
	// Demos need to be able to encode/decode CUserCmds to memory buffers, so these functions wrap that
	void EncodeUserCmdToBuffer(bf_write buf, int slot);
	void DecodeUserCmdFromBuffer(bf_read buf, int slot);

	// Set up and render one or more views (e.g., rear view window, etc.).  This called into RenderView below
	void View_Render(ref ViewRect rect);

	// Allow engine to expressly render a view (e.g., during timerefresh)
	// See IVRenderView.h, PushViewFlags_t for nFlags values
	void RenderView(in ViewSetup view, ClearFlags nClearFlags, int whatToDraw);

	// Apply screen fade directly from engine
	void View_Fade(ref ScreenFade pSF);

	// The engine has parsed a crosshair angle message, this function is called to dispatch the new crosshair angle
	void SetCrosshairAngle(in QAngle angle);

	// Sprite (.spr) model handling code
	// Load a .spr file by name
	void InitSprite(EngineSprite sprite, ReadOnlySpan<char> loadname);
	// Shutdown a .spr file
	void ShutdownSprite(EngineSprite sprite);
	// Returns sizeof( CEngineSprite ) so the engine can allocate appropriate memory
	int GetSpriteSize();

	// Called when a player starts or stops talking.
	// entindex is -1 to represent the local client talking (before the data comes back from the server). 
	// entindex is -2 to represent the local client's voice being acked by the server.
	// entindex is GetPlayer() when the server acknowledges that the local client is talking.
	void VoiceStatus(int entindex, bool talking);

	// Networked string table definitions have arrived, allow client .dll to 
	//  hook string changes with a callback function ( see INetworkStringTableClient.h )
	void InstallStringTableCallback(ReadOnlySpan<char> tableName);

	// Notification that we're moving into another stage during the frame.
	void FrameStageNotify(ClientFrameStage curStage);

	// The engine has received the specified user message, this code is used to dispatch the message handler
	bool DispatchUserMessage(int msg_type, bf_read data);

	// Save/restore system hooks
	SaveRestoreData? SaveInit(int size);
	void SaveWriteFields(SaveRestoreData data, ReadOnlySpan<char> name, ReadOnlySpan<byte> baseData, DataMap map, ReadOnlySpan<TypeDescription> fields);
	void SaveReadFields(SaveRestoreData data, ReadOnlySpan<char> name, ReadOnlySpan<byte> baseData, DataMap map, ReadOnlySpan<TypeDescription> fields);
	void PreSave(SaveRestoreData data);
	void Save(SaveRestoreData data);
	void WriteSaveHeaders(SaveRestoreData data);
	void ReadRestoreHeaders(SaveRestoreData data);
	void Restore(SaveRestoreData data, bool unk);
	void DispatchOnRestore();

	// Hand over the StandardRecvProxies in the client DLL's module.
	StandardRecvProxies? GetStandardRecvProxies();

	// save game screenshot writing
	void WriteSaveGameScreenshot(ReadOnlySpan<char> pFilename);

	// Given a list of "S(wavname) S(wavname2)" tokens, look up the localized text and emit
	//  the appropriate close caption if running with closecaption = 1
	void EmitSentenceCloseCaption(ReadOnlySpan<char> tokenstream);
	// Emits a regular close caption by token name
	void EmitCloseCaption(ReadOnlySpan<char> captionname, TimeUnit_t duration);

	// Returns true if the client can start recording a demo now.  If the client returns false,
	// an error message of up to length bytes should be returned in errorMsg.
	bool CanRecordDemo(Span<char> errorMsg);

	// Give the Client a chance to do setup/cleanup.
	void OnDemoRecordStart(ReadOnlySpan<char> pDemoBaseName);
	void OnDemoRecordStop();
	void OnDemoPlaybackStart(ReadOnlySpan<char> pDemoBaseName);
	void OnDemoPlaybackStop();

	// Draw the console overlay?
	bool ShouldDrawDropdownConsole();

	// Get client screen dimensions
	int GetScreenWidth();
	int GetScreenHeight();

	// Added interface

	// save game screenshot writing
	void WriteSaveGameScreenshotOfSize(ReadOnlySpan<char> pFilename, int width, int height, bool bCreatePowerOf2Padded = false, bool bWriteVTF = false);

	// Gets the current view
	bool GetPlayerView(ref ViewSetup playerView);

	// Matchmaking
	// void SetupGameProperties(List<XUSER_CONTEXT> contexts, List<XUSER_PROPERTY> properties);
	uint GetPresenceID(ReadOnlySpan<char> pIDName);
	ReadOnlySpan<char> GetPropertyIdString(uint id);
	void GetPropertyDisplayString(uint id, uint value, Span<char> output, int bytes);

	void InvalidateMdlCache();

	void IN_SetSampleTime(TimeUnit_t frametime);


	// For sv_pure mode. The filesystem figures out which files the client needs to reload to be "pure" ala the server's preferences.
	void ReloadFilesInList(IFileList filesToReload);

	// Let the client handle UI toggle - if this function returns false, the UI will toggle, otherwise it will not.
	bool HandleUiToggle();

	// Allow the console to be shown?
	bool ShouldAllowConsole();

	// Get renamed recv tables
	RenamedRecvTableInfo? GetRenamedRecvTableInfos();

	// Get the mouthinfo for the sound being played inside UI panels
	MouthInfo? GetClientUIMouthInfo();

	// Notify the client that a file has been received from the game server
	void FileReceived(ReadOnlySpan<char> fileName, uint transferID);

	ReadOnlySpan<char> TranslateEffectForVisionFilter(ReadOnlySpan<char> pchEffectType, ReadOnlySpan<char> pchEffectName);

	// Give the client a chance to modify sound settings however they want before the sound plays. This is used for
	// things like adjusting pitch of voice lines in Pyroland in TF2.
	void ClientAdjustStartSoundParams(ref StartSoundParams parms);

	// Returns true if the disconnect command has been handled by the client
	bool DisconnectAttempt();

#if !GMOD_DLL
	bool IsConnectedUserInfoChangeAllowed(IConVar cvar);
#else
	void GMOD_ReceiveServerMessage(bf_read buffer, int len);
	void GMOD_DoSnapshots();
	void GMOD_VoiceVolume(uint playerID, float volume);
	void GMOD_OnDrawSkybox();
	void IN_MouseWheelAnalog(int value);
	void GMOD_RequestLuaFiles(INetChannel netchan);
	void GMOD_ReceiveLuaFile(ReadOnlySpan<char> fileName, in SHA256Value sha256, ReadOnlySpan<byte> compressed);
	void GMOD_SignOnStateChanged(int userID, int oldState, int newState);
	void GMOD_OnAllSoundsStoppedCL();
#endif
}
