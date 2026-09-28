using Source.Common;
using Source.Common.Audio;
using Source.Common.Client;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Filesystem;

namespace Source.Engine;

public class Sound
{
	readonly IAudioSystem? AudioSystem = OptionalSingleton<IAudioSystem>();

	public bool Initialized => AudioSystem?.IsInitted() ?? false;

	public void Init() => AudioSystem?.Init();
	public void Shutdown() => AudioSystem?.Shutdown();
	public bool IsInitted() => AudioSystem?.IsInitted() ?? false;

	public void StopAllSounds(bool clear) => AudioSystem?.StopAllSounds(clear);
	public void Update() => AudioSystem?.Update(null);
	public void Update(in AudioState audioState) => AudioSystem?.Update(audioState);
	public void ExtraUpdate() => AudioSystem?.ExtraUpdate();
	public void ClearBuffer() => AudioSystem?.ClearBuffer();
	public void BlockSound() => AudioSystem?.BlockSound();
	public void UnblockSound() => AudioSystem?.UnblockSound();
	public float GetMasterVolume() => AudioSystem?.GetMasterVolume() ?? 0;
	public void SoundFade(float percent, float holdtime, float intime, float outtime) => AudioSystem?.SoundFade(percent, holdtime, intime, outtime);
	public void OnLoadScreen(bool value) => AudioSystem?.OnLoadScreen(value);
	public void EnableThreadedMixing(bool enable) => AudioSystem?.EnableThreadedMixing(enable);
	public void EnableMusic(bool enable) => AudioSystem?.EnableMusic(enable);

	public int StartSound(in StartSoundParams parms) {
		if (AudioSystem == null)
			return 0;
		StartSoundParams copy = parms;
		return AudioSystem.StartSound(ref copy);
	}
	public void StopSound(int entnum, int entchannel) => AudioSystem?.StopSound(entnum, entchannel);

	public float ComputeDelayForSoundtime(double soundtime, ClockSyncIndex syncIndex) => AudioSystem?.ComputeDelayForSoundtime((float)soundtime, syncIndex) ?? 0;

	public void StopSoundByGuid(int guid) => AudioSystem?.StopSoundByGuid(guid);
	public float SoundDurationByGuid(int guid) => AudioSystem?.SoundDurationByGuid(guid) ?? 0;
	public int GetGuidForLastSoundEmitted() => AudioSystem?.GetGuidForLastSoundEmitted() ?? 0;
	public bool IsSoundStillPlaying(int guid) => AudioSystem?.IsSoundStillPlaying(guid) ?? false;
	public void GetActiveSounds(List<SndInfo> sndlist) => AudioSystem?.GetActiveSounds(sndlist);
	public void SetVolumeByGuid(int guid, float fvol) => AudioSystem?.SetVolumeByGuid(guid, fvol);
	public float GetElapsedTimeByGuid(int guid) => AudioSystem?.GetElapsedTimeByGuid(guid) ?? 0;
	public bool IsLoopingSoundByGuid(int guid) => AudioSystem?.IsLoopingSoundByGuid(guid) ?? false;
	public void ReloadSound(ReadOnlySpan<char> sample) => AudioSystem?.ReloadSound(sample);
	public float GetMono16Samples(ReadOnlySpan<char> name, List<short> sampleList) => AudioSystem?.GetMono16Samples(name, sampleList) ?? 0;

	public SfxTable? DummySfx(ReadOnlySpan<char> name) => AudioSystem?.DummySfx(name);
	public SfxTable? PrecacheSound(ReadOnlySpan<char> sample) => AudioSystem?.PrecacheSound(sample);
	public void PrefetchSound(ReadOnlySpan<char> name, bool playOnce) => AudioSystem?.PrefetchSound(name, playOnce);
	public void MarkUISound(SfxTable sfx) => AudioSystem?.MarkUISound(sfx);
	public void ReloadFilesInList(IFileList filesToReload) => AudioSystem?.ReloadFilesInList(filesToReload);

	public float GetNominalClipDist() => AudioSystem?.GetNominalClipDist() ?? 0;

	public void MovieStart() => AudioSystem?.MovieStart();
	public void MovieEnd() => AudioSystem?.MovieEnd();

	public int GetCurrentStaticSounds(Span<SoundInfo> result, int sizeResult, int entchannel) => AudioSystem?.GetCurrentStaticSounds(result, sizeResult, entchannel) ?? 0;

	public void GetCurrentlyPlayingMusic(List<MusicSave> list) => AudioSystem?.GetCurrentlyPlayingMusic(list);
	public void RestartSong(in MusicSave song) => AudioSystem?.RestartSong(in song);

	public float GetSoundDuration(ReadOnlySpan<char> name) => AudioSystem?.GetSoundDuration(name) ?? 0;
}
