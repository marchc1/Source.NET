namespace Source.Common.Audio;

public enum SoundSampleRates
{
	Sound11khz = 11025,
	Sound22khz = 22050,
	Sound44khz = 44100
}

public enum SoundMix
{
	Wet,
	Dry,
	Speaker,
	SpecialDSP
}

public enum SoundBus
{
	Room = 1 << 0,
	Facing = 1 << 1,
	FacingAway = 1 << 2,
	Speaker = 1 << 3,
	Dry = 1 << 4,
	SpecialDSP = 1 << 5
}

public struct AudioChannel {
	public int GUID;
	public int UserData;
}

public enum AudioSourceType {
	Unknown,
	WAV,
	MP3,
	Voice,
	Max
}

public enum AudioStatus {
	NotLoaded,
	IsLoading,
	Loaded
}

public abstract class AudioSource {
	public abstract int SampleRate();
	public abstract bool IsVoiceSource();
	public abstract int SampleSize();
	public abstract int SampleCount();
	public abstract bool IsLooped();
	public abstract bool IsStereoWav();
	public abstract bool IsStreaming();
	public abstract AudioStatus GetCacheStatus();
	public bool IsCached() => GetCacheStatus() == AudioStatus.Loaded ? true : false;
	public abstract void CacheLoad();
	public abstract void CacheUnload();
}

public class SfxTable {
	public AudioSource? Source;
	public bool UseErrorFilename;
	public bool IsUISound;
	public bool IsLateLoad;
	public bool MixGroupsCached;
	public byte MixGroupCount;

	public FileNameHandle_t NamePoolIndex;
	public void SetNamePoolIndex(FileNameHandle_t handle) {
		NamePoolIndex = handle;
		if(NamePoolIndex != FileNameHandle_t.MaxValue) {
			// on name changed todo
		}
	}
}

public interface IAudioDevice {
	bool IsActive();
	bool Init();
	void Shutdown();
	void Pause();
	void UnPause();
	float MixDryVolume();
	bool Should3DMix();
	void StopAllSounds();
}
