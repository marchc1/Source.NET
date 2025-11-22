using Source.Common.Commands;
using Source.Common.Filesystem;

using System.Numerics;
using System.Runtime.CompilerServices;

namespace Source.Common.Audio;
public static class SoundSharedGlobals
{
	public const int IFRONT_LEFT = 0;      
	public const int IFRONT_RIGHT = 1;
	public const int IREAR_LEFT = 2;
	public const int IREAR_RIGHT = 3;
	public const int IFRONT_CENTER = 4;
	public const int IFRONT_CENTER0 = 5;   
	public const int IFRONT_LEFTD = 6;     
	public const int IFRONT_RIGHTD = 7;
	public const int IREAR_LEFTD = 8;
	public const int IREAR_RIGHTD = 9;
	public const int IFRONT_CENTERD = 10;
	public const int IFRONT_CENTERD0 = 11; 
	public const int CCHANVOLUMES = 12;

	static readonly ConVar snd_refdist = new("snd_refdist", "36", FCvar.Cheat);
	static readonly ConVar snd_refdb = new("snd_refdb", "60", FCvar.Cheat);
	static readonly ConVar snd_foliage_db_loss = new("snd_foliage_db_loss", "4", FCvar.Cheat);
	static readonly ConVar snd_gain = new("snd_gain", "1", FCvar.Cheat);
	static readonly ConVar snd_gain_max = new("snd_gain_max", "1", FCvar.Cheat);
	static readonly ConVar snd_gain_min = new("snd_gain_min", "0.01", FCvar.Cheat);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static SoundLevel SNDLEVEL_TO_COMPATIBILITY_MODE(int x) => ((SoundLevel)(int)((x) + 256));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static SoundLevel SNDLEVEL_TO_COMPATIBILITY_MODE(SoundLevel x) => ((SoundLevel)(int)((x) + 256));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static SoundLevel SNDLEVEL_FROM_COMPATIBILITY_MODE(int x) => ((SoundLevel)(int)((x) - 256));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static SoundLevel SNDLEVEL_FROM_COMPATIBILITY_MODE(SoundLevel x) => ((SoundLevel)(int)((x) - 256));
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool SNDLEVEL_IS_COMPATIBILITY_MODE(int x) => x >= 256;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool SNDLEVEL_IS_COMPATIBILITY_MODE(SoundLevel x) => x >= (SoundLevel)256;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float SNDLVL_TO_DIST_MULT(SoundLevel sndlvl) => (sndlvl != 0 ? ((MathF.Pow(10.0f, snd_refdb.GetFloat() / 20) / MathF.Pow(10.0f, (float)sndlvl / 20)) / snd_refdist.GetFloat()) : 0);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static SoundLevel DIST_MULT_TO_SNDLVL(float dist_mult) => (SoundLevel)(int)((dist_mult != 0) ? (20 * MathF.Log10(MathF.Pow(10.0f, snd_refdb.GetFloat() / 20) / ((dist_mult) * snd_refdist.GetFloat()))) : 0);
}


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

[InlineArray(CCHANVOLUMES)]
public struct InlineArrayChanVolumes<T>
{
	T first;
}

public interface ISfxTable {
	ReadOnlySpan<char> GetName();
	ReadOnlySpan<char> GetFileName();
	bool IsPrecachedSound();

	AudioSource? Source { get; set; }
	bool UseErrorFilename { get; set; }
	bool IsUISound { get; set; }
	bool IsLateLoad { get; set; }
	bool MixGroupsCached { get; set; }
	byte MixGroupCount { get; set; }
}

public struct StartSoundParams
{
	public bool StaticSound;
	public int UserData;
	public int SoundSource;
	public SoundEntityChannel EntChannel;
	public ISfxTable? Sfx;
	public Vector3 Origin;
	public Vector3 Direction;
	public bool UpdatePositions;
	public float Volume;
	public SoundLevel SoundLevel;
	public SoundFlags Flags;
	public int Pitch;
	public int SpecialDSP;
	public bool FromServer;
	public float Delay;
	public int SpeakerEntity;
	public bool SuppressRecording;
	public int InitialStreamPosition;

	public StartSoundParams() {
		UpdatePositions = true;
		Volume = 1;
		SoundLevel = SoundLevel.LvlNorm;
		Pitch = 100;
		SpeakerEntity = -1;
	}
}

public class AudioChannel
{
	public const int MAX_CHANNELS = 128;
	public const int MAX_DYNAMIC_CHANNELS = 64;

	public long GUID;
	public int UserData;
	public ISfxTable? Sfx;
	public AudioMixer? Mixer;

	public InlineArrayChanVolumes<float> Volume;
	public InlineArrayChanVolumes<float> VolumeTarget;
	public InlineArrayChanVolumes<float> VolumeInc;
	public uint FreeChannelAtSampleTime;

	public SoundSource SoundSource;
	public SoundEntityChannel EntChannel;
	public int SpeakerEntity;

	public short MasterVolume;
	public short BasePitch;
	public float Pitch;
	public InlineArray8<int> MixGroups;
	public int LastMixGroupID;
	public float LastVol;

	public Vector3 Origin;
	public Vector3 Direction;
	public float DistMult;

	public float DSPMix;
	public float DSPFace;
	public float DistMix;
	public float DSPMixMin;
	public float DSPMixMax;

	public float Radius;

	public float OBGain;
	public float OBGainTarget;
	public float OBGainInc;

	public short ActiveIndex;
	public SoundChars WavType;
	public byte Pad;

	public InlineArray8<byte> SamplePrev;

	public int InitialStreamPosition;

	public int SpecialDSP;

	public bool UpdatePositions;               // if true, assume sound source can move and update according to entity
	public bool IsSentence;                    // true if playing linked sentence
	public bool Dry;                           // if true, bypass all dsp processing for this sound (ie: music)	
	public bool Speaker;                       // true if sound is playing through in-game speaker entity.
	public bool StereoWav;                     // if true, a stereo .wav file is the sample data source

	public bool DelayedStart;                  // If true, sound had a delay and so same sound on same channel won't channel steal from it
	public bool FromServer;                    // for snd_show, networked sounds get colored differently than local sounds

	public bool FirstPass;                     // true if this is first time sound is spatialized
	public bool Traced;                        // true if channel was already checked this frame for obscuring
	public bool FastPitch;                     // true if using low quality pitch (fast, but no interpolation)

	public bool IsFreeingChannel;              // true when inside S_FreeChannel - prevents reentrance
	public bool CompatibilityAttenuation;      // True when we want to use goldsrc compatibility mode for the attenuation
											   // In that case, dist_mul is set to a relatively meaningful value in StartDynamic/StartStaticSound,
											   // but we interpret it totally differently in SND_GetGain.
	public bool ShouldPause;                   // if true, sound should pause when the game is paused
	public bool IgnorePhonemes;                // if true, we don't want to drive animation w/ phoneme data
}

public enum AudioSourceType : byte
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
	public abstract ReadOnlySpan<char> GetSentence();

	public void CheckAudioSourceCache() {
		throw new NotImplementedException();
	}

	public AudioMixer? CreateMixer(int initialStreamPosition) {
		throw new NotImplementedException();
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

public class InlineArray_2DPaintFilters
{
	public readonly PortableSamplePair[] elements = new PortableSamplePair[PaintBuffer.CPAINTFILTERS * PaintBuffer.CPAINTFILTERMEM];
	public unsafe ref PortableSamplePair this[int x, int y] => ref elements[(x * PaintBuffer.CPAINTFILTERMEM) + y];
}

public unsafe class PaintBuffer
{
	public const int PAINTBUFFER_SIZE = 1020;
	public const int CPAINTFILTERMEM = 3;
	public const int CPAINTFILTERS = 4;

	public bool Active;
	public bool Surround;
	public bool SurroundCenter;
	public int DSP_SpecialDSP;
	public int PrevSpecialDSP;
	public int SpecialDSP;
	public int Flags;
	public PortableSamplePair[]? Buf;
	public PortableSamplePair[]? BufRear;
	public PortableSamplePair[]? BufCenter;

	public int Filter;

	public readonly InlineArray_2DPaintFilters FilterMem = new();
	public readonly InlineArray_2DPaintFilters FilterMemRear = new();
	public readonly InlineArray_2DPaintFilters FilterMemCenter = new();
}


public interface IAudioDevice
{
	public const int SOUND_DMA_SPEED = 44100;
	public const int SOUND_11k = 11025;
	public const int SOUND_22k = 22050;
	public const int SOUND_44k = 44100;
	public const int SAMPLE_16BIT_SHIFT = 1;

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

	void Mix8Mono(AudioChannel channel, Span<byte> data, int outputOffset, int inputOffset, uint rateScaleFix, int outCount, int timecompress);
	void Mix8Stereo(AudioChannel channel, Span<byte> data, int outputOffset, int inputOffset, uint rateScaleFix, int outCount, int timecompress);
	void Mix16Mono(AudioChannel channel, Span<short> data, int outputOffset, int inputOffset, uint rateScaleFix, int outCount, int timecompress);
	void Mix16Stereo(AudioChannel channel, Span<short> data, int outputOffset, int inputOffset, uint rateScaleFix, int outCount, int timecompress);

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
