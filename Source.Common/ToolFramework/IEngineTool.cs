using SharpCompress.Factories;

using Source.Common.Audio;
using Source.Common.Client;
using Source.Common.Engine;
using Source.Common.Formats.BSP;
using Source.Common.Formats.Keyvalues;
using Source.Common.Mathematics;

using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Source.Common.ToolFramework;

public interface IEngineToolFramework
{
	int GetToolCount();
	ReadOnlySpan<char> GetToolName(int index);
	void SwitchToTool(int index);
	bool IsTopmostTool(IToolSystem? sys);
	IToolSystem? GetToolSystem(int index);
	IToolSystem? GetTopmostTool();
	void ShowCursor(bool show);
	bool IsCursorVisible();
}

public interface IEngineTool
{
	void GetServerFactory(out IServiceProvider factory);
	void GetClientFactory(out IServiceProvider factory);

	float GetSoundDuration(ReadOnlySpan<char> pszName);
	bool IsSoundStillPlaying(int guid);
	// Returns the guid of the sound
	int StartSound(int userData, bool staticsound, int entIndex, SoundEntityChannel channel, ReadOnlySpan<char> sample,
		float volume, SoundLevel soundlevel, in Vector3 origin, in Vector3 direction,
		SoundFlags flags = 0, int pitch = PITCH_NORM, bool updatePositions = true, float delay = 0.0f,
		int speakerentity = -1);

	void StopSoundByGuid(int guid);

	// Returns how long the sound is
	float GetSoundDuration(int guid);

	// Returns if the sound is looping
	bool IsLoopingSound(int guid);
	void ReloadSound(ReadOnlySpan<char> pSample);
	void StopAllSounds();
	float GetMono16Samples(ReadOnlySpan<char> pszName, List<short> sampleList);
	void SetAudioState(in AudioState audioState);

	// Issue a console command
	void Command(ReadOnlySpan<char> cmd);
	// Flush console command buffer right away
	void Execute();

	ReadOnlySpan<char> GetCurrentMap();
	void ChangeToMap(ReadOnlySpan<char> mapname);
	bool IsMapValid(ReadOnlySpan<char> mapname);

	void RenderView(ref ViewSetup view, int flags, int whatToRender);

	// Returns true if the player is fully connected and active in game (i.e, not still loading)
	bool IsInGame();
	// Returns true if the player is connected, but not necessarily active in game (could still be loading)
	bool IsConnected();

	int GetMaxClients();

	bool IsGamePaused();
	void SetGamePaused(bool paused);

	TimeUnit_t GetTimescale();
	void SetTimescale(TimeUnit_t scale);
	TimeUnit_t GetRealTime();
	TimeUnit_t GetRealFrameTime();
	TimeUnit_t Time();
	TimeUnit_t HostFrameTime(); // host_frametime
	TimeUnit_t HostTime(); // host_time
	long HostTick(); // host_tickcount
	long HostFrameCount(); // total famecount
	TimeUnit_t ServerTime(); // gpGlobals->curtime on server
	TimeUnit_t ServerFrameTime(); // gpGlobals->frametime on server
	long ServerTick(); // gpGlobals->tickcount on server
	TimeUnit_t ServerTickInterval(); // tick interval on server
	TimeUnit_t ClientTime(); // gpGlobals->curtime on client
	TimeUnit_t ClientFrameTime(); // gpGlobals->frametime on client
	long ClientTick(); // gpGlobals->tickcount on client
	void SetClientFrameTime(TimeUnit_t frametime); // gpGlobals->frametime on client

	void ForceUpdateDuringPause();

	Model? GetModel(HTOOLHANDLE hEntity);
	// Get the .mdl file used by entity (if it's a cbaseanimating)
	StudioHeader? GetStudioModel(HTOOLHANDLE hEntity);

	// SINGLE PLAYER/LISTEN SERVER ONLY (just matching the client .dll api for this)
	// Prints the formatted string to the notification area of the screen ( down the right hand edge
	//  numbered lines starting at position 0
	void Con_NPrint(int pos, ReadOnlySpan<char> text);
	// SINGLE PLAYER/LISTEN SERVER ONLY(just matching the client .dll api for this)
	// Similar to Con_NPrintf, but allows specifying custom text color and duration information
	void Con_NXPrint(in Con_NPrint_s info, ReadOnlySpan<char> text);

	// Get the current game directory (hl2, tf2, hl1, cstrike, etc.)
	void GetGameDir(Span<char> getGameDir);

	// Do we need separate rects for the 3d "viewport" vs. the tools surface??? and can we control viewports from
	void GetScreenSize(out int width, out int height);

	// GetRootPanel(VPANEL)

	// Sets the location of the main view
	void SetMainView(in Vector3 origin, in QAngle angles);

	// Gets the player view
	bool GetPlayerView(out ViewSetup playerView, int x, int y, int w, int h);

	// From a location on the screen, figure out the vector into the world
	void CreatePickingRay(in ViewSetup viewSetup, int x, int y, ref Vector3 org, ref Vector3 forward);

	// precache methods
	bool PrecacheSound(ReadOnlySpan<char> name, bool preload = false);
	bool PrecacheModel(ReadOnlySpan<char> name, bool preload = false);

	// TODO: void InstallQuitHandler(object? userData, FnQuitHandler func);
	void TakeTGAScreenShot(ReadOnlySpan<char> filename, int width, int height);
	// Even if game is paused, force networking to update to get new server state down to client
	void ForceSend();

	bool IsRecordingMovie();

	// NOTE: Params can contain file name, frame rate, output avi, output raw, and duration
	void StartMovieRecording(KeyValues? movieParams);
	void EndMovieRecording();
	void CancelMovieRecording();
	// TODO: IVideoRecorder? GetActiveVideoRecorder();

	void StartRecordingVoiceToFile(ReadOnlySpan<char> filename, ReadOnlySpan<char> pathID = default);
	void StopRecordingVoiceToFile();
	bool IsVoiceRecording();

	// A version that simply accepts a ray (can work as a traceline or tracehull)
	void TraceRay<ITF>(in Ray ray, Mask mask, scoped in ITF traceFilter, ref Trace trace) where ITF : ITraceFilter; // client version
	void TraceRayServer<ITF>(in Ray ray, Mask mask, scoped in ITF traceFilter, ref Trace trace) where ITF : ITraceFilter;

	bool IsConsoleVisible();

	int GetPointContents(in Vector3 position);

	// TODO: int GetActiveDLights(dlight_t* pList[MAX_DLIGHTS] );
	// TODO: int GetLightingConditions( in Vector3 position, Span<Vector3> colors, Span<LightDesc> localLights );

	void GetWorldToScreenMatrixForView(in ViewSetup view, in Matrix4x4 matrix);

	SpatialPartitionHandle_t CreatePartitionHandle(IHandleEntity? entity, SpatialPartitionListMask_t listMask, in Vector3 mins, in Vector3 maxs);
	void DestroyPartitionHandle(SpatialPartitionHandle_t partition);
	void InstallPartitionQueryCallback<T>(T query) where T : IPartitionQueryCallback;
	void RemovePartitionQueryCallback<T>(T query) where T : IPartitionQueryCallback;
	void ElementMoved(SpatialPartitionHandle_t handle, in Vector3 mins, in Vector3 maxs);
}
