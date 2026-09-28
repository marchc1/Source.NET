global using static Source.AudioSystem.SndAudioSourceGlobals;

using Source.Common;
using Source.Common.Audio;
using Source.Common.Utilities;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Source.AudioSystem;

public static class SndAudioSourceGlobals
{
	public const string AUDIOSOURCE_CACHE_ROOTDIR = "maps/soundcache";

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int CalcSampleSize(int bitsPerSample, int channels) => (bitsPerSample >> 3) * channels;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static AudioSourceBase? GetSource(this SfxTable sfx) => (AudioSourceBase?)sfx.Source;

	public static byte[] AllocArray(int size) => new byte[Math.Max(size, 1)];

	public static byte[] ArrayCopy(ReadOnlySpan<byte> data) {
		byte[] copy = AllocArray(data.Length);
		data.CopyTo(copy);
		return copy;
	}
}

//-----------------------------------------------------------------------------
// Purpose: This is an instance of an audio source.
//			Mixers are attached to channels and reference an audio source.
//			Mixers are specific to the sample format and source format.
//			Mixers are never re-used, so they can track instance data like
//			sample position, fractional sample, stream cache, faders, etc.
//-----------------------------------------------------------------------------
public abstract class AudioMixer : IDisposable
{
	public virtual void Dispose() {
		GC.SuppressFinalize(this);
	}

	// return number of samples mixed
	public abstract int MixDataToDevice(IAudioDevice device, Channel channel, int sampleCount, int outputRate, int outputOffset);
	public abstract int SkipSamples(Channel channel, int sampleCount, int outputRate, int outputOffset);
	public abstract bool ShouldContinueMixing();

	public abstract AudioSourceBase GetSource();

	// get the current position (next sample to be mixed)
	public abstract int GetSamplePosition();

	// Allow the mixer to modulate pitch and volume.
	// returns a floating point modulator
	public abstract float ModifyPitch(float pitch);
	public abstract float GetVolumeScale();

	// NOTE: Playback is optimized for linear streaming.  These calls will usually cost performance
	// It is currently optimal to call them before any playback starts, but some audio sources may not
	// guarantee this.  Also, some mixers may choose to ignore these calls for internal reasons (none do currently).

	// Move the current position to newPosition
	// BUGBUG: THIS CALL DOES NOT SUPPORT MOVING BACKWARD, ONLY FORWARD!!!
	public abstract void SetSampleStart(int newPosition);

	// End playback at newEndPosition
	public abstract void SetSampleEnd(int newEndPosition);

	// How many samples to skip before commencing actual data reading ( to allow sub-frametime sound
	//  offsets and avoid synchronizing sounds to various 100 msec clock intervals throughout the
	//  engine and game code)
	public abstract void SetStartupDelaySamples(int delaySamples);
	public abstract int GetMixSampleSize();

	// Certain async loaded sounds lazilly load into memory in the background, use this to determine
	//  if the sound is ready for mixing
	public abstract bool IsReadyToMix();

	// NOTE: The "saved" position can be different than the "sample" position
	// NOTE: Allows mixer to save file offsets, loop info, etc
	public abstract int GetPositionForSave();
	public abstract void SetPositionFromSaved(int savedPosition);
}

public class AudioSourceCachedInfo
{
	// A hack, but will work okay
	public static AudioSourceType s_CurrentType = AudioSourceType.AUDIO_SOURCE_MAXTYPE;
	public static SfxTable? s_pSfx = null;
	public static bool s_bIsPrecacheSound = false;

	AudioSourceType type;
	int bits;
	int channels;
	int sampleSize;
	int format;
	int rate;

	bool hasSentence;
	bool hasCachedData;
	bool hasHeader;

	int loopStart;
	int sampleCount;
	int dataStart;  // offset of wave data chunk
	int dataSize;   // size of wave data chunk

	ushort cachedDataSize;
	ushort headerSize;

	Sentence? sentence;
	byte[]? cachedData;
	byte[]? header;

	public AudioSourceCachedInfo() { }

	public AudioSourceCachedInfo(AudioSourceCachedInfo src) {
		CopyFrom(src);
	}

	public AudioSourceCachedInfo CopyFrom(AudioSourceCachedInfo src) {
		if (this == src)
			return this;

		type = src.type;
		bits = src.bits;
		channels = src.channels;
		sampleSize = src.sampleSize;
		format = src.format;
		rate = src.rate;
		hasSentence = src.hasSentence;
		hasCachedData = src.hasCachedData;
		hasHeader = src.hasHeader;
		SetDataStart(src.DataStart());
		SetDataSize(src.DataSize());
		SetLoopStart(src.LoopStart());
		SetSampleCount(src.SampleCount());

		Sentence? scopy = null;
		if (src.Sentence() != null) {
			scopy = new Sentence();
			scopy.CopyFrom(src.Sentence()!);
		}
		SetSentence(scopy);

		byte[]? data = null;

		Assert(src.CachedDataSize() == 0 || src.CachedData() != null);

		cachedDataSize = 0;

		if (src.CachedData() != null && src.CachedDataSize() > 0) {
			SetCachedDataSize(src.CachedDataSize());
			data = ArrayCopy(src.CachedData().AsSpan(0, src.CachedDataSize()));
		}

		SetCachedData(data);

		data = null;

		Assert(src.HeaderSize() == 0 || src.HeaderData() != null);

		headerSize = 0;

		if (src.HeaderData() != null && src.HeaderSize() > 0) {
			SetHeaderSize(src.HeaderSize());
			data = ArrayCopy(src.HeaderData().AsSpan(0, src.HeaderSize()));
		}

		SetHeaderData(data);

		return this;
	}

	public void Clear() {
		type = 0;
		bits = 0;
		channels = 0;
		sampleSize = 0;
		format = 0;
		rate = 0;
		hasSentence = false;
		hasCachedData = false;
		hasHeader = false;
		dataStart = 0;
		dataSize = 0;
		loopStart = 0;
		sampleCount = 0;

		sentence = null;

		cachedData = null;
		cachedDataSize = 0;

		header = null;
		headerSize = 0;
	}

	public void RemoveData() {
		cachedData = null;
		cachedDataSize = 0;
		hasCachedData = false;
	}

	public void Save(UtlBuffer buf) {
		buf.PutInt(PackInfo());
		buf.PutChar((char)PackFlags());
		buf.PutInt(dataStart);
		buf.PutInt(dataSize);
		buf.PutInt(loopStart);
		buf.PutInt(sampleCount);

		if (hasSentence)
			sentence!.CacheSaveToBuffer(buf, Common.Sentence.CACHED_SENTENCE_VERSION);

		Assert(cachedDataSize < 65535);

		if (hasCachedData && cachedData != null) {
			buf.PutInt(cachedDataSize);
			buf.Put(cachedData.AsSpan(0, cachedDataSize));
		}

		Assert(headerSize <= 32767);

		if (hasHeader) {
			buf.PutShort((short)headerSize);
			buf.Put(header.AsSpan(0, headerSize));
		}
	}

	public void Restore(UtlBuffer buf) {
		// Wipe any old data!!!
		Clear();

		UnpackInfo(buf.GetInt());
		UnpackFlags((byte)buf.GetChar());
		dataStart = buf.GetInt();
		dataSize = buf.GetInt();
		loopStart = buf.GetInt();
		sampleCount = buf.GetInt();
		if (hasSentence) {
			sentence = new Sentence();
			sentence.CacheRestoreFromBuffer(buf);
		}

		if (hasCachedData) {
			cachedDataSize = (ushort)buf.GetInt();
			Assert(cachedDataSize > 0 && cachedDataSize < 65535);
			if (cachedDataSize > 0) {
				byte[] data = AllocArray(cachedDataSize);
				buf.Get(data.AsSpan(0, cachedDataSize));
				SetCachedData(data);
			}
		}

		if (hasHeader) {
			headerSize = (ushort)buf.GetShort();
			Assert(headerSize > 0 && headerSize <= 32767);
			if (headerSize > 0) {
				byte[] data = AllocArray(headerSize);
				buf.Get(data.AsSpan(0, headerSize));
				SetHeaderData(data);
			}
		}
	}

	int PackInfo() => (int)((uint)type & 0x3 | ((uint)bits & 0x1F) << 2 | ((uint)channels & 0x3) << 7 | ((uint)sampleSize & 0x7) << 9 | ((uint)format & 0x3) << 12 | ((uint)rate & 0x1FFFF) << 14);
	void UnpackInfo(int infolong) {
		uint info = (uint)infolong;
		type = (AudioSourceType)(info & 0x3);
		bits = (int)((info >> 2) & 0x1F);
		channels = (int)((info >> 7) & 0x3);
		sampleSize = (int)((info >> 9) & 0x7);
		format = (int)((info >> 12) & 0x3);
		rate = (int)((info >> 14) & 0x1FFFF);
	}
	byte PackFlags() => (byte)((hasSentence ? 1 : 0) | (hasCachedData ? 2 : 0) | (hasHeader ? 4 : 0));
	void UnpackFlags(byte flagsbyte) {
		hasSentence = (flagsbyte & 1) != 0;
		hasCachedData = (flagsbyte & 2) != 0;
		hasHeader = (flagsbyte & 4) != 0;
	}

	public void Rebuild(ReadOnlySpan<char> filename) {
		// Wipe any old data
		Clear();

		Assert(s_pSfx);
		Assert(s_CurrentType != AudioSourceType.AUDIO_SOURCE_MAXTYPE);

		SetType(s_CurrentType);

		AudioSourceBase? audioSource = null;

		// Note though these instantiate a specific AudioSource subclass, it doesn't matter, we just need one for .wav and one for .mp3
		switch (s_CurrentType) {
			default:
			case AudioSourceType.AUDIO_SOURCE_VOICE:
				break;
			case AudioSourceType.AUDIO_SOURCE_WAV:
				audioSource = new AudioSourceMemWave(s_pSfx!);
				break;
			case AudioSourceType.AUDIO_SOURCE_MP3:
				audioSource = new AudioSourceMP3Cache(s_pSfx!);
				break;
		}

		audioSource?.GetCacheData(this);
	}

	public AudioSourceType Type() => type;
	public void SetType(AudioSourceType type) => this.type = type;

	public int Bits() => bits;
	public void SetBits(int bits) => this.bits = bits & 0x1F;

	public int Channels() => channels;
	public void SetChannels(int channels) => this.channels = channels & 0x3;

	public int SampleSize() => sampleSize;
	public void SetSampleSize(int size) => sampleSize = size & 0x7;

	public int Format() => format;
	public void SetFormat(int format) => this.format = format & 0x3;

	public int SampleRate() => rate;
	public void SetSampleRate(int rate) => this.rate = rate & 0x1FFFF;

	public int CachedDataSize() => cachedDataSize;
	public void SetCachedDataSize(int size) => cachedDataSize = (ushort)size;

	public byte[]? CachedData() => cachedData;
	public void SetCachedData(byte[]? data) {
		cachedData = data;
		hasCachedData = data != null;
	}

	public int HeaderSize() => headerSize;
	public void SetHeaderSize(int size) => headerSize = (ushort)size;

	public byte[]? HeaderData() => header;
	public void SetHeaderData(byte[]? data) {
		header = data;
		hasHeader = data != null;
	}

	public int LoopStart() => loopStart;
	public void SetLoopStart(int start) => loopStart = start;

	public int SampleCount() => sampleCount;
	public void SetSampleCount(int count) => sampleCount = count;

	public int DataStart() => dataStart;
	public void SetDataStart(int start) => dataStart = start;

	public int DataSize() => dataSize;
	public void SetDataSize(int size) => dataSize = size;

	public Sentence? Sentence() => sentence;
	public void SetSentence(Sentence? sentence) {
		this.sentence = sentence;
		hasSentence = sentence != null;
	}
}

public interface IAudioSourceCache
{
	bool Init(nuint memSize);
	void Shutdown();
	void LevelInit(ReadOnlySpan<char> mapname);
	void LevelShutdown();

	// This invalidates the cached size/date info for sounds so it'll regenerate that next time it's accessed.
	// Used when you connect to a pure server.
	void ForceRecheckDiskInfo();

	AudioSourceCachedInfo? GetInfo(AudioSourceType audiosourcetype, bool soundisprecached, SfxTable sfx);
	void RebuildCacheEntry(AudioSourceType audiosourcetype, bool soundisprecached, SfxTable sfx);
}

public struct AudioSourceCachedInfoHandle
{
	public AudioSourceCachedInfo? Info;
	public uint FlushCount;

	public AudioSourceCachedInfo? Get(AudioSourceType audiosourcetype, bool soundisprecached, SfxTable sfx, ref int cacheddatasize) {
		if (FlushCount != s_nCurrentFlushCount) {
			// Reacquire
			Info = audiosourcecache.GetInfo(audiosourcetype, soundisprecached, sfx);

			cacheddatasize = Info != null ? Info.CachedDataSize() : 0;

			// Tag as current
			FlushCount = s_nCurrentFlushCount;
		}
		return Info;
	}

	public AudioSourceCachedInfo? Get(AudioSourceType audiosourcetype, bool soundisprecached, SfxTable sfx) {
		if (FlushCount != s_nCurrentFlushCount) {
			// Reacquire
			Info = audiosourcecache.GetInfo(audiosourcetype, soundisprecached, sfx);

			// Tag as current
			FlushCount = s_nCurrentFlushCount;
		}
		return Info;
	}

	public readonly bool IsValid() => FlushCount == s_nCurrentFlushCount;

	public readonly AudioSourceCachedInfo? FastGet() {
		if (FlushCount != s_nCurrentFlushCount)
			return null;
		return Info;
	}

	public static void InvalidateCache() {
		++s_nCurrentFlushCount;
	}

	public static uint s_nCurrentFlushCount = 1;
}

//-----------------------------------------------------------------------------
// Purpose: A source is an abstraction for a stream, cached file, or procedural
//			source of audio.
//-----------------------------------------------------------------------------
public abstract class AudioSourceBase : AudioSource, IDisposable
{
	public virtual void Dispose() {
		GC.SuppressFinalize(this);
	}

	// Create an instance (mixer) of this audio source
	public abstract AudioMixer? CreateMixer(int initialStreamPosition = 0);

	// Serialization for caching
	public abstract void GetCacheData(AudioSourceCachedInfo info);

	// Provide samples for the mixer. You can point pData at your own data, or if you prefer to copy the data,
	// you can copy it into copyBuf and set pData to copyBuf.
	public abstract int GetOutputData(out ReadOnlySpan<byte> data, int samplePosition, int sampleCount, Span<byte> copyBuf);

	// mixer's references
	public abstract void ReferenceAdd(AudioMixer mixer);
	public abstract void ReferenceRemove(AudioMixer mixer);
}

//-----------------------------------------------------------------------------
// Purpose: Linear iterator over source data.
//			Keeps track of position in source, and maintains necessary buffers
//-----------------------------------------------------------------------------
public interface IWaveData : IDisposable
{
	AudioSourceBase Source();
	int ReadSourceData(out ReadOnlySpan<byte> data, int sampleIndex, int sampleCount, Span<byte> copyBuf);
	bool IsReadyToMix();
}

public interface IWaveStreamSource
{
	int UpdateLoopingSamplePosition(int samplePosition);
	void UpdateSamples(Span<byte> data, int sampleCount);
	int GetLoopingInfo(out int loopBlock, out int numLeadingSamples, out int numTrailingSamples);
}
