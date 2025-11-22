namespace Source.Common.Audio;

public interface ISoundServices
{
	object? LevelAlloc(uint bytes, ReadOnlySpan<char> tag);
	void OnExtraUpdate();
	TimeUnit_t GetClientTime();
	TimeUnit_t GetHostTime();
	int GetViewEntity();
	TimeUnit_t GetHostFrametime();
	void SetSoundFrametime(TimeUnit_t realDt, TimeUnit_t hostDt);
	int GetServerCount();
	bool IsPlayer(SoundSource source);
	void OnChangeVoiceStatus(int entity, bool status);
	bool IsConnected();
	void EmitSentenceCloseCaption(ReadOnlySpan<char> tokenstream );
	void EmitCloseCaption(ReadOnlySpan<char> captionname, TimeUnit_t duration );
	ReadOnlySpan<char> GetGameDir();
	bool IsGamePaused();
	bool IsGameActive();
	void RestartSoundSystem();
	void CacheBuildingStart();
	void CacheBuildingUpdateProgress(float percent, ReadOnlySpan<char> cachefile );
	void CacheBuildingFinish();
	int GetPrecachedSoundCount();
	ReadOnlySpan<char> GetPrecachedSound( int index );
	void OnSoundStarted(long guid, ref StartSoundParams parms, ReadOnlySpan<char> soundname  );
	void OnSoundStopped(long guid, int soundsource, SoundEntityChannel channel, ReadOnlySpan<char> soundname );
	ReadOnlySpan<char> GetUILanguage();
}
