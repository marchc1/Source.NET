using Source.Common;
using Source.Common.Audio;
using Source.Common.Client;
using Source.Common.Engine;
using Source.Engine.Client;

using System.Numerics;

namespace Source.Engine;

//-----------------------------------------------------------------------------
// Purpose: Engine implementation of services required by the audio subsystem
//-----------------------------------------------------------------------------
[EngineComponent]
public class EngineSoundServices : ISoundServices
{
	Host? host;
	Host Host => host ??= Singleton<Host>();
	CommonHostState? hostState;
	CommonHostState host_state => hostState ??= Singleton<CommonHostState>();
	IGame? gameImpl;
	IGame game => gameImpl ??= Singleton<IGame>();

	TimeUnit_t m_frameTime;
	readonly IClientEntityList? entitylist = OptionalSingleton<IClientEntityList>();

	public object? LevelAlloc(uint bytes, ReadOnlySpan<char> tag) {
		return new byte[bytes];
	}

	public void OnExtraUpdate() {
		if (g_ClientDLL != null && game.IsActiveApp())
			g_ClientDLL.IN_Accumulate();
	}

	public bool GetSoundSpatialization(int entIndex, ref SpatializationInfo info) {
		if (entitylist == null)
			return false;

		// Entity has been deleted
		IClientEntity? pClientEntity = entitylist.GetClientEntity(entIndex);
		if (pClientEntity == null) {
			// FIXME:  Should this assert?
			return false;
		}

		bool bResult = pClientEntity.GetSoundSpatialization(ref info);

		return bResult;
	}

	public bool GetToolSpatialization(int iUserData, int guid, ref SpatializationInfo info) {
		return false;
	}

	public TimeUnit_t GetClientTime() {
		return cl.GetTime();
	}

	// Filtered local time
	public TimeUnit_t GetHostTime() {
		return Host.Time;
	}

	public int GetViewEntity() => cl.ViewEntity;

	public void SetSoundFrametime(TimeUnit_t realDt, TimeUnit_t hostDt) {
		if (IsMovieRecording())
			m_frameTime = hostDt;
		else
			m_frameTime = realDt;
	}

	public TimeUnit_t GetHostFrametime() {
		return m_frameTime;
	}

	public int GetServerCount() => cl.ServerCount;

	public bool IsPlayer(int source) {
		return source == cl.PlayerSlot + 1;
	}

	public void OnChangeVoiceStatus(int entity, bool status) {
		g_ClientDLL?.VoiceStatus(entity, status);
	}

	public bool IsConnected() => cl.IsConnected();

	// Calls into client .dll with list of close caption tokens to construct a caption out of
	public void EmitSentenceCloseCaption(ReadOnlySpan<char> tokenstream) {
		g_ClientDLL?.EmitSentenceCloseCaption(tokenstream);
	}

	public void EmitCloseCaption(ReadOnlySpan<char> captionname, TimeUnit_t duration) {
		g_ClientDLL?.EmitCloseCaption(captionname, (float)duration);
	}

	public ReadOnlySpan<char> GetGameDir() {
		return Common.Gamedir;
	}

	// If the game is paused, certain audio will pause, too (anything with phoneme/sentence data for now)
	public bool IsGamePaused() {
		return cl.IsPaused();
	}

	public bool IsGameActive() {
		return game.IsActiveApp();
	}

	public void RestartSoundSystem() {
		Host.Snd_Restart_f();
	}

	public void CacheBuildingStart() {
		EngineVGui().ActivateGameUI();
		EngineVGui().StartCustomProgress();
		ReadOnlySpan<char> str = g_Localize.Find("#Valve_CreatingCache");
		if (!str.IsEmpty)
			EngineVGui().UpdateCustomProgressBar(0.0f, str);
	}

	public void CacheBuildingUpdateProgress(float percent, ReadOnlySpan<char> cachefile) {
		ReadOnlySpan<char> format = g_Localize.Find("Valve_CreatingSpecificSoundCache");
		if (!format.IsEmpty) {
			string constructed = new string(format).Replace("%s1", new string(cachefile));
			EngineVGui().UpdateCustomProgressBar(percent, constructed);
		}
	}

	public void CacheBuildingFinish() {
		EngineVGui().FinishCustomProgress();
		EngineVGui().HideGameUI();
	}

	public int GetPrecachedSoundCount() {
		if (!sv.IsActive())
			return 0;

		INetworkStringTable? table = sv.GetSoundPrecacheTable();
		if (table == null)
			return 0;

		return table.GetNumStrings();
	}

	public ReadOnlySpan<char> GetPrecachedSound(int index) {
		Assert(sv.IsActive());

		INetworkStringTable? table = sv.GetSoundPrecacheTable();
		if (table == null)
			return "";

		return table.GetString(index);
	}

	public void OnSoundStarted(int guid, ref StartSoundParams parms, ReadOnlySpan<char> soundname) {

	}

	public void OnSoundStopped(int guid, int soundsource, SoundEntityChannel channel, ReadOnlySpan<char> soundname) {

	}

	public bool ShouldSuppressNonUISounds() {
		return EngineVGui().IsGameUIVisible() || IsGamePaused();
	}

	public ReadOnlySpan<char> GetUILanguage() {
		return cvar.FindVar("cl_language")?.GetString() ?? "english";
	}

	public TimeUnit_t GetHostFrametimeUnbounded() => Host.FrameTimeUnbounded;
	public TimeUnit_t GetIntervalPerTick() => host_state.IntervalPerTick;
	public bool IsDedicated() => sv.IsDedicated();

	public long GetMemSize() {
		const long MINIMUM_WIN_MEMORY = 96L * 1024 * 1024;
		const long MAXIMUM_WIN_MEMORY = 512L * 1024 * 1024;
		long memsize = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
		if (memsize == 0)
			memsize = MAXIMUM_WIN_MEMORY;
		// just take one quarter, no cap
		memsize >>= 2;
		return Math.Clamp(memsize, MINIMUM_WIN_MEMORY, MAXIMUM_WIN_MEMORY);
	}

	public bool IsClientActive() => cl.IsActive();
	public bool IsServerActive() => sv.IsActive();
	public int LookupServerSoundIndex(ReadOnlySpan<char> name) => sv.LookupSoundIndex(name);
	public int LookupClientSoundIndex(ReadOnlySpan<char> name) => cl.LookupSoundIndex(name);
	public ReadOnlySpan<char> GetHostMap() => Host.host_map.GetString();
	public Vector3 MainViewOrigin() => RenderAccessors.MainViewOrigin();
	public bool IsStaticProp(IHandleEntity handleEntity) => StaticPropMgr().IsStaticProp(handleEntity);
	public ICollideable? GetStaticProp(IHandleEntity handleEntity) => StaticPropMgr().GetStaticProp(handleEntity);
	public bool IsConsoleVisible() => EngineVGui().IsConsoleVisible();
	public void Con_NPrintf(int pos, ReadOnlySpan<char> text) => Con.NPrintF(pos, text);
	public void Con_NXPrintf(in Con_NPrint_s info, ReadOnlySpan<char> text) => Con.NXPrintF(in info, text);
	public bool IsReslistLoggingToMap() => false;
	public MouthInfo? GetClientUIMouthInfo() => g_ClientDLL?.GetClientUIMouthInfo();

	public bool InToolMode() => false;
	public bool IsToolRecording() => false;

	public bool IsMovieRecording() => false;
	public bool MovieDoWav() => false;
	public bool MovieDoVideoSound() => false;
	public ReadOnlySpan<char> GetMovieName() => "";
	public void AppendMovieAudioSamples(ReadOnlySpan<short> samples) { }

	public int Voice_SamplesPerSec() => Voice.SamplesPerSec();
	public int Voice_AvgBytesPerSec() => Voice.AvgBytesPerSec();
	public int Voice_GetOutputData(int channel, Span<byte> copyBuf, int copyBufSize, int samplePosition, int sampleCount) => Voice.GetOutputData(channel, copyBuf, copyBufSize, samplePosition, sampleCount);
	public void Voice_OnAudioSourceShutdown(int channel) => Voice.OnAudioSourceShutdown(channel);
	public void Voice_Spatialize(int guid, ref int soundsource) => Voice.Spatialize(guid, ref soundsource);
	public void Voice_Deinit() => Voice.Deinit();
	public bool VoiceTweak_IsStillTweaking() => Voice.VoiceTweak_IsStillTweaking();
	public void VoiceTweak_EndVoiceTweakMode() => Voice.VoiceTweak_EndVoiceTweakMode();
}
