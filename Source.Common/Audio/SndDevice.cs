using System.Numerics;

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

public struct AudioChannel
{
	public int GUID;
	public int UserData;
}

public enum AudioSourceType
{
	Unknown,
	WAV,
	MP3,
	Voice,
	Max
}

public enum AudioStatus
{
	NotLoaded,
	IsLoading,
	Loaded
}

public abstract class AudioSource
{
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

public class SfxTable
{
	public AudioSource? Source;
	public bool UseErrorFilename;
	public bool IsUISound;
	public bool IsLateLoad;
	public bool MixGroupsCached;
	public byte MixGroupCount;

	public FileNameHandle_t NamePoolIndex;
	public void SetNamePoolIndex(FileNameHandle_t handle) {
		NamePoolIndex = handle;
		if (NamePoolIndex != FileNameHandle_t.MaxValue) {
			// on name changed todo
		}
	}
}

public enum SoundBufferType
{
	Paint,
	Room,
	Facing,
	FacingAway,
	Dry,
	Speaker,
	BaseTotal,
	SpecialStart = BaseTotal,
}

public struct PortableSamplePair
{
	public int Left;
	public int Right;
}

public struct PaintBuffer
{
	public bool Active;
	public bool Surround;
	public bool SurroundCenter;
	public int DSP_SpecialDSP;
	public int PrevSpecialDSP;
	public int SpecialDSP;
	public int Flags;
	public Memory<PortableSamplePair> Buf;
	public Memory<PortableSamplePair> BufRear;
	public Memory<PortableSamplePair> BufCenter;
	public int Filter;
}

public interface IAudioDevice
{
	public const int SOUND_DMA_SPEED = 44100;
	public const int SOUND_11k = 11025;
	public const int SOUND_22k = 22050;
	public const int SOUND_44k = 44100;

	bool IsActive();
	bool Init();
	void Shutdown();
	void Pause();
	void UnPause();
	float MixDryVolume();
	bool Should3DMix();

	void StopAllSounds();

	int PaintBegin(TimeUnit_t mixAheadTime, int soundtime, int paintedtime);
	void PaintEnd();

	void SpatializeChannel(Span<int> volume, int master_vol, in Vector3 sourceDir, float gain, float mono);

	void ApplyDSPEffects(int idsp, Span<PortableSamplePair> bufFront, Span<PortableSamplePair> bufRear, Span<PortableSamplePair> bufCenter, int samplecount);

	int GetOutputPosition();

	void ClearBuffer();

	void UpdateListener(in Vector3 position, in Vector3 forward, in Vector3 right, in Vector3 up);

	void MixBegin(int sampleCount);
	void MixUpsample(int sampleCount, int filtertype);

	void Mix8Mono(ref AudioChannel channel, Span<byte> data, int outputOffset, int inputOffset, uint rateScaleFix, int outCount, int timecompress);
	void Mix8Stereo(ref AudioChannel channel, Span<byte> data, int outputOffset, int inputOffset, uint rateScaleFix, int outCount, int timecompress);
	void Mix16Mono(ref AudioChannel channel, Span<short> data, int outputOffset, int inputOffset, uint rateScaleFix, int outCount, int timecompress);
	void Mix16Stereo(ref AudioChannel channel, Span<short> data, int outputOffset, int inputOffset, uint rateScaleFix, int outCount, int timecompress);

	void ChannelReset(int entnum, int channelIndex, float distanceMod);
	void TransferSamples(int end);

	ReadOnlySpan<char> DeviceName();
	int DeviceChannels();
	int DeviceSampleBits();
	int DeviceSampleBytes();
	int DeviceDmaSpeed();
	int DeviceSampleCount();

	bool IsSurround();
	bool IsSurroundCenter();
	bool IsHeadphone();
}
