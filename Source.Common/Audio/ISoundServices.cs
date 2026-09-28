using Source.Common.Engine;

using System.Numerics;

namespace Source.Common.Audio;

public interface ISoundServices
{
	object? LevelAlloc(uint bytes, ReadOnlySpan<char> tag);
	void OnExtraUpdate();
	bool GetSoundSpatialization(int entIndex, ref SpatializationInfo info);
	bool GetToolSpatialization(int userData, int guid, ref SpatializationInfo info);
	TimeUnit_t GetClientTime();
	TimeUnit_t GetHostTime();
	int GetViewEntity();
	TimeUnit_t GetHostFrametime();
	void SetSoundFrametime(TimeUnit_t realDt, TimeUnit_t hostDt);
	int GetServerCount();
	bool IsPlayer(SoundSource source);
	void OnChangeVoiceStatus(int entity, bool status);
	bool IsConnected();
	void EmitSentenceCloseCaption(ReadOnlySpan<char> tokenstream);
	void EmitCloseCaption(ReadOnlySpan<char> captionname, TimeUnit_t duration);
	ReadOnlySpan<char> GetGameDir();
	bool IsGamePaused();
	bool IsGameActive();
	void RestartSoundSystem();
	void CacheBuildingStart();
	void CacheBuildingUpdateProgress(float percent, ReadOnlySpan<char> cachefile);
	void CacheBuildingFinish();
	int GetPrecachedSoundCount();
	ReadOnlySpan<char> GetPrecachedSound(int index);
	void OnSoundStarted(int guid, ref StartSoundParams parms, ReadOnlySpan<char> soundname);
	void OnSoundStopped(int guid, int soundsource, SoundEntityChannel channel, ReadOnlySpan<char> soundname);
	bool ShouldSuppressNonUISounds();
	ReadOnlySpan<char> GetUILanguage();

	TimeUnit_t GetHostFrametimeUnbounded();
	TimeUnit_t GetIntervalPerTick();
	bool IsDedicated();
	long GetMemSize();
	bool IsClientActive();
	bool IsServerActive();
	int LookupServerSoundIndex(ReadOnlySpan<char> name);
	int LookupClientSoundIndex(ReadOnlySpan<char> name);
	ReadOnlySpan<char> GetHostMap();
	Vector3 MainViewOrigin();
	bool IsStaticProp(IHandleEntity handleEntity);
	ICollideable? GetStaticProp(IHandleEntity handleEntity);
	bool IsConsoleVisible();
	void Con_NPrintf(int pos, ReadOnlySpan<char> text);
	void Con_NXPrintf(in Con_NPrint_s info, ReadOnlySpan<char> text);
	bool IsReslistLoggingToMap();
	MouthInfo? GetClientUIMouthInfo();

	bool InToolMode();
	bool IsToolRecording();

	bool IsMovieRecording();
	bool MovieDoWav();
	bool MovieDoVideoSound();
	ReadOnlySpan<char> GetMovieName();
	void AppendMovieAudioSamples(ReadOnlySpan<short> samples);

	int Voice_SamplesPerSec();
	int Voice_AvgBytesPerSec();
	int Voice_GetOutputData(int channel, Span<byte> copyBuf, int copyBufSize, int samplePosition, int sampleCount);
	void Voice_OnAudioSourceShutdown(int channel);
	void Voice_Spatialize(int guid, ref int soundsource);
	void Voice_Deinit();
	bool VoiceTweak_IsStillTweaking();
	void VoiceTweak_EndVoiceTweakMode();
}
