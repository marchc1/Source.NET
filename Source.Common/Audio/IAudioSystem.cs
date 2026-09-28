using Source.Common.Client;
using Source.Common.Engine;
using Source.Common.Filesystem;

namespace Source.Common.Audio;

public enum ClockSyncIndex
{
	Client = 0,
	Server,
	Num
}

public struct MusicSave
{
	public string SongName;
	public int SamplePosition;
	public short MasterVolume;
}

public interface IAudioSystem
{
	void Init();
	void Shutdown();
	bool IsInitted();

	void StopAllSounds(bool clear);
	void Update(AudioState? audioState);
	void ExtraUpdate();
	void ClearBuffer();
	void BlockSound();
	void UnblockSound();
	float GetMasterVolume();
	void SoundFade(float percent, float holdtime, float intime, float outtime);
	void OnLoadScreen(bool value);
	void EnableThreadedMixing(bool enable);
	void EnableMusic(bool enable);

	int StartSound(ref StartSoundParams parms);
	void StopSound(int entnum, int entchannel);

	float ComputeDelayForSoundtime(float soundtime, ClockSyncIndex syncIndex);

	void StopSoundByGuid(int guid);
	float SoundDurationByGuid(int guid);
	int GetGuidForLastSoundEmitted();
	bool IsSoundStillPlaying(int guid);
	void GetActiveSounds(List<SndInfo> sndlist);
	void SetVolumeByGuid(int guid, float fvol);
	float GetElapsedTimeByGuid(int guid);
	bool IsLoopingSoundByGuid(int guid);
	void ReloadSound(ReadOnlySpan<char> sample);
	float GetMono16Samples(ReadOnlySpan<char> name, List<short> sampleList);

	SfxTable DummySfx(ReadOnlySpan<char> name);
	SfxTable? PrecacheSound(ReadOnlySpan<char> sample);
	void PrefetchSound(ReadOnlySpan<char> name, bool playOnce);
	void MarkUISound(SfxTable sfx);
	void ReloadFilesInList(IFileList filesToReload);

	float GetNominalClipDist();

	// for recording movies
	void MovieStart();
	void MovieEnd();

	int GetCurrentStaticSounds(Span<SoundInfo> result, int sizeResult, int entchannel);

	void GetCurrentlyPlayingMusic(List<MusicSave> list);
	void RestartSong(in MusicSave song);

	float GetSoundDuration(ReadOnlySpan<char> name);
	float GetSoundDuration(SfxTable? sfx);

	void MarkFirstTime();
	void LevelInit(ReadOnlySpan<char> mapname);
	void LevelShutdown();

	void ReadSentenceFile(ReadOnlySpan<char> sentenceFileName);
	int SentenceCount();
	float SentenceLength(int sentenceNum);
	ReadOnlySpan<char> SentenceNameFromIndex(int sentenceNum);
	string? LookupSentence(ReadOnlySpan<char> sentenceName, out int sentenceNum);
	int SentenceGroupIndexFromName(ReadOnlySpan<char> groupName);
	ReadOnlySpan<char> SentenceGroupNameFromIndex(int groupIndex);
	int SentenceGroupPick(int groupIndex, out string found);
	int SentenceGroupPickSequential(int groupIndex, out string found, int pick, bool reset);
	void PrecacheSentenceGroup(IEngineSound engineSound, ReadOnlySpan<char> groupName, ReadOnlySpan<char> pathOverride);

	void DSP_FastReset(int dspType);

	bool VoiceSE_Init();
	void VoiceSE_Term();
	void VoiceSE_Idle(float frametime);
	int VoiceSE_StartChannel(int channel, int entity, bool proximity, int viewEntityIndex);
	void VoiceSE_EndChannel(int channel, int entity);
	void VoiceSE_StartOverdrive();
	void VoiceSE_EndOverdrive();
	void VoiceSE_InitMouth(int entnum);
	void VoiceSE_CloseMouth(int entnum);
	void VoiceSE_MoveMouth(int entnum, Span<short> samples, int numSamples);
	IVoiceRecord? CreateVoiceRecord(int sampleRate);
}
