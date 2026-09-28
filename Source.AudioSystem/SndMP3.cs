global using static Source.AudioSystem.SndMP3;

using NLayer;

using Source.Common;
using Source.Common.Audio;
using Source.Common.Filesystem;
using Source.Common.Utilities;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Source.AudioSystem;

public static class SndMP3
{
	// How many bytes initial data bytes of the mp3 should be saved in the soundcache, in addition to the small amount of
	// metadata (playbackrate, etc). This will increase memory usage by
	// ( N * number-of-precached-mp3-sounds-in-the-whole-game ) at all times, as the soundcache is held in memory.
	//
	// Right now we're setting this to zero. The IsReadyToMix() logic at the data layer will delay mixing of the sound until
	// it arrives. Setting this to anything above zero, however, will allow the sound to start, so it needs to either be
	// enough to cover SND_ASYNC_LOOKAHEAD_SECONDS or none at all.
	public const int MP3_STARTUP_DATA_SIZE_BYTES = 0;

	public const int MP3_BUFFER_SIZE = 16384;

	// vaudio DLL
	public static IVAudio? vaudio = null;

	public static readonly Dictionary<string, Sentence> g_PhonemeFileSentences = new(StringComparer.OrdinalIgnoreCase);
	public static bool g_bAllPhonemesLoaded;

	public static void PhonemeMP3Shutdown() {
		g_PhonemeFileSentences.Clear();
		g_bAllPhonemesLoaded = false;
	}

	public static void AddPhonemesFromFile(ReadOnlySpan<char> fileName) {
		// If all Phonemes are loaded, do not load anymore
		if (g_bAllPhonemesLoaded && g_PhonemeFileSentences.Count != 0)
			return;

		// Empty file name implies stop loading more phonemes
		if (fileName.IsEmpty) {
			g_bAllPhonemesLoaded = true;
			return;
		}

		// Load this file
		g_bAllPhonemesLoaded = false;

		IFileHandle? file = filesystem.Open(fileName, FileOpenOptions.Read, "MOD");
		if (file != null) {
			byte[] contents = new byte[file.Stream.Length];
			file.Stream.ReadExactly(contents);
			file.Dispose();

			UtlBuffer buf = new(contents, UtlBuffer.BufferFlags.TEXT_BUFFER);
			Span<char> token = stackalloc char[4096];
			while (true) {
				buf.GetString(token);

				string key = new string(token.SliceNullTerminatedString()).Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

				g_PhonemeFileSentences.Remove(key);

				Sentence sentence = new Sentence();
				g_PhonemeFileSentences[key] = sentence;

				buf.GetString(token);

				if (token[0] == '\0')
					break;

				if (stricmp(token.SliceNullTerminatedString(), "{") == 0)
					sentence.InitFromBuffer(buf);
			}
		}
	}

	public static bool Audio_IsMP3(ReadOnlySpan<char> name) {
		name = name.SliceNullTerminatedString();
		int len = name.Length;
		if (len > 4) {
			if (name[(len - 4)..].Equals(".mp3", StringComparison.OrdinalIgnoreCase))
				return true;
		}
		return false;
	}

	public static AudioSourceBase Audio_CreateStreamedMP3(SfxTable sfx) {
		AudioSourceStreamMP3? mp3 = null;
		AudioSourceCachedInfo? info = audiosourcecache.GetInfo(AudioSourceType.AUDIO_SOURCE_MP3, sfx.IsPrecachedSound(), sfx);
		if (info != null)
			mp3 = new AudioSourceStreamMP3(sfx, info);
		else
			mp3 = new AudioSourceStreamMP3(sfx);
		return mp3;
	}

	public static AudioSourceBase Audio_CreateMemoryMP3(SfxTable sfx) {
		AudioSourceMP3Cache? mp3 = null;
		AudioSourceCachedInfo? info = audiosourcecache.GetInfo(AudioSourceType.AUDIO_SOURCE_MP3, sfx.IsPrecachedSound(), sfx);
		if (info != null)
			mp3 = new AudioSourceMP3Cache(sfx, info);
		else
			mp3 = new AudioSourceMP3Cache(sfx);
		return mp3;
	}

	static readonly Dictionary<FileNameHandle_t, float> g_MP3Durations = [];

	public static float GetMP3Duration_Helper(ReadOnlySpan<char> filename) {
		float duration = 60.0f;

		// See if it's in the RB tree already...
		Span<char> fn = stackalloc char[512];
		sprintf(fn, "sound/%s").S(SoundCharsUtils.SkipSoundChars(filename));

		FileNameHandle_t h = filesystem.FindOrAddFileName(fn.SliceNullTerminatedString());

		if (g_MP3Durations.TryGetValue(h, out float cached))
			return cached;

		try {
			IFileHandle? file = filesystem.Open(fn.SliceNullTerminatedString(), FileOpenOptions.Read | FileOpenOptions.Binary, "GAME");
			if (file != null) {
				using (file) {
					using MpegFile mpaFile = new MpegFile(file.Stream);
					duration = (float)mpaFile.Duration.TotalSeconds;
				}
			}
		}
		catch (Exception e) {
			Warning($"Unable to get {filename} file length: {e.Message}.");
		}

		g_MP3Durations[h] = duration;

		return duration;
	}
}

public interface IAudioStream : IDisposable
{
	// Decode another bufferSize output bytes from the stream
	// returns number of bytes decoded
	int Decode(Span<byte> buffer);

	// output sampling bits (8/16)
	int GetOutputBits();
	// output sampling rate in Hz
	int GetOutputRate();
	// output channels (1=mono,2=stereo)
	int GetOutputChannels();

	// seek
	uint GetPosition();

	// NOTE: BUGBUG: Only supports seeking forward currently!
	void SetPosition(uint position);
}

public interface IVAudio
{
	IAudioStream? CreateMP3StreamDecoder(IAudioStreamEvent eventHandler);
	void DestroyMP3StreamDecoder(IAudioStream decoder);
}

public class VAudioNLayer : IVAudio
{
	class EventStream(IAudioStreamEvent eventHandler) : Stream
	{
		public long BytesRead;
		public int NextOffset = -1;

		public override bool CanRead => true;
		public override bool CanSeek => false;
		public override bool CanWrite => false;
		public override long Length => throw new NotSupportedException();
		public override long Position { get => BytesRead; set => throw new NotSupportedException(); }
		public override void Flush() { }
		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
		public override void SetLength(long value) => throw new NotSupportedException();
		public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

		public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

		public override int Read(Span<byte> buffer) {
			int read = eventHandler.StreamRequestData(buffer, buffer.Length, NextOffset);
			NextOffset = -1;
			BytesRead += read;
			return read;
		}
	}

	class MP3StreamDecoder : IAudioStream
	{
		readonly IAudioStreamEvent eventHandler;
		EventStream stream;
		MpegFile? file;
		float[] floatBuffer = [];
		int outputRate;
		int outputChannels;

		public MP3StreamDecoder(IAudioStreamEvent eventHandler) {
			this.eventHandler = eventHandler;
			stream = new EventStream(eventHandler);
			stream.NextOffset = 0;
			file = new MpegFile(stream);
			outputRate = file.SampleRate;
			outputChannels = file.Channels;
		}

		public bool IsValid() => file != null && outputRate > 0 && outputChannels > 0;

		public int Decode(Span<byte> buffer) {
			if (file == null)
				return 0;

			int samples = buffer.Length / sizeof(short);
			if (floatBuffer.Length < samples)
				floatBuffer = new float[samples];

			int read = file.ReadSamples(floatBuffer, 0, samples);
			Span<short> output = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, short>(buffer);
			for (int i = 0; i < read; i++)
				output[i] = (short)Math.Clamp((int)(floatBuffer[i] * 32767.0f), short.MinValue, short.MaxValue);

			return read * sizeof(short);
		}

		public int GetOutputBits() => 16;
		public int GetOutputRate() => outputRate;
		public int GetOutputChannels() => outputChannels;

		public uint GetPosition() => (uint)stream.BytesRead;

		public void SetPosition(uint position) {
			file?.Dispose();
			stream = new EventStream(eventHandler);
			stream.NextOffset = (int)position;
			stream.BytesRead = position;
			file = new MpegFile(stream);
		}

		public void Dispose() {
			file?.Dispose();
			file = null;
		}
	}

	public IAudioStream? CreateMP3StreamDecoder(IAudioStreamEvent eventHandler) {
		try {
			MP3StreamDecoder decoder = new MP3StreamDecoder(eventHandler);
			if (!decoder.IsValid()) {
				decoder.Dispose();
				return null;
			}
			return decoder;
		}
		catch {
			return null;
		}
	}

	public void DestroyMP3StreamDecoder(IAudioStream decoder) {
		decoder.Dispose();
	}
}

public class AudioMixerWaveMP3 : AudioMixerWave, IAudioStreamEvent
{
	// Lazily initialized, use GetStream
	IAudioStream? stream;
	bool streamInit;
	readonly byte[] samples = new byte[MP3_BUFFER_SIZE];
	int sampleCount;
	int samplePosition;
	int channelCount;
	int offset;
	int headerOffset;

	public AudioMixerWaveMP3(IWaveData data) : base(data) {
		sampleCount = 0;
		samplePosition = 0;
		offset = 0;
		delaySamples = 0;
		headerOffset = 0;
		stream = null;
		streamInit = false;
		channelCount = 0;
	}

	public override void Dispose() {
		stream?.Dispose();
		stream = null;
		base.Dispose();
	}

	public override void Mix(IAudioDevice device, Channel channel, ReadOnlySpan<byte> data, int outputOffset, int inputOffset, fixedint fracRate, int outCount, int timecompress) {
		Assert(IsReadyToMix());
		if (channelCount == 1)
			device.Mix16Mono(channel, MemoryMarshal.Cast<byte, short>(data), outputOffset, inputOffset, fracRate, outCount, timecompress);
		else
			device.Mix16Stereo(channel, MemoryMarshal.Cast<byte, short>(data), outputOffset, inputOffset, fracRate, outCount, timecompress);
	}

	// Some MP3 files are wrapped in ID3
	[SkipLocalsInit]
	void GetID3HeaderOffset() {
		Span<byte> copyBuf = stackalloc byte[AudioSource.AUDIOSOURCE_COPYBUF_SIZE];

		int bytesRead = this.data!.ReadSourceData(out ReadOnlySpan<byte> data, 0, 10, copyBuf);
		if (bytesRead < 10)
			return;

		headerOffset = 0;
		if ((data[0] == 0x49) &&
			(data[1] == 0x44) &&
			(data[2] == 0x33) &&
			(data[3] < 0xff) &&
			(data[4] < 0xff) &&
			(data[6] < 0x80) &&
			(data[7] < 0x80) &&
			(data[8] < 0x80) &&
			(data[9] < 0x80)) {
			// this is in id3 file
			// compute the size of the wrapper and skip it
			headerOffset = 10 + (data[9] | (data[8] << 7) | (data[7] << 14) | (data[6] << 21));
		}
	}

	public int StreamRequestData(Span<byte> buffer, int bytesRequested, int offset) {
		if (offset < 0)
			offset = this.offset;
		else
			this.offset = offset;
		// read the data out of the source
		int totalBytesRead = 0;

		if (offset == 0) {
			// top of file, check for ID3 wrapper
			GetID3HeaderOffset();
		}

		offset += headerOffset; // skip any id3 header/wrapper

		while (bytesRequested > 0) {
			Span<byte> outputBuffer = buffer;
			outputBuffer = outputBuffer[totalBytesRead..];

			int bytesRead = this.data!.ReadSourceData(out ReadOnlySpan<byte> data, offset + totalBytesRead, bytesRequested, outputBuffer);

			if (bytesRead == 0)
				break;
			if (bytesRead > bytesRequested)
				bytesRead = bytesRequested;
			// if the source is buffering it, copy it to the MP3 decomp buffer
			if (!data.Overlaps(outputBuffer, out int elementOffset) || elementOffset != 0)
				data[..bytesRead].CopyTo(outputBuffer);
			totalBytesRead += bytesRead;
			bytesRequested -= bytesRead;
		}

		this.offset += totalBytesRead;
		return totalBytesRead;
	}

	bool DecodeBlock() {
		IAudioStream? stream = GetStream();
		if (stream == null)
			return false;

		sampleCount = stream.Decode(samples);
		samplePosition = 0;
		return sampleCount > 0;
	}

	static string lastFileName = "";

	IAudioStream? GetStream() {
		if (!streamInit) {
			streamInit = true;

			if (vaudio != null)
				stream = vaudio.CreateMP3StreamDecoder(this);
			else
				Warning($"Attempting to play MP3 with no vaudio [ {this.data!.Source().GetFileName()} ]\n");

			if (stream != null)
				channelCount = stream.GetOutputChannels();

			if (stream == null) {
				string fileName = new(this.data!.Source().GetFileName());

				if (lastFileName != fileName) {
					Warning($"Failed to create decoder for MP3 [ {fileName} ]\n");

					lastFileName = fileName;
				}
			}
		}

		return stream;
	}

	public int GetStreamOutputRate() => GetStream()?.GetOutputRate() ?? 0;

	public override int GetMixSampleSize() => CalcSampleSize(16, channelCount);

	//-----------------------------------------------------------------------------
	// Purpose: Read existing buffer or decompress a new block when necessary
	// Input  : **pData - output data pointer
	//			sampleCount - number of samples (or pairs)
	// Output : int - available samples (zero to stop decoding)
	//-----------------------------------------------------------------------------
	public override int GetOutputData(out ReadOnlySpan<byte> data, int sampleCount, Span<byte> copyBuf) {
		data = default;
		if (samplePosition >= this.sampleCount) {
			if (!DecodeBlock())
				return 0;
		}

		IAudioStream? stream = GetStream();
		if (stream == null) {
			// Needed for channel count, and with a failed stream init we probably should fail to return data anyway.
			return 0;
		}

		if (samplePosition < this.sampleCount) {
			int sampleSize = stream.GetOutputChannels() * 2;
			data = samples.AsSpan(samplePosition);
			int available = this.sampleCount - samplePosition;
			int bytesRequired = sampleCount * sampleSize;
			if (available > bytesRequired)
				available = bytesRequired;

			samplePosition += available;

			int samples_loaded = available / sampleSize;

			// update count of max samples loaded in CAudioMixerWave

			sample_max_loaded += samples_loaded;

			// update index of last sample loaded

			sample_loaded_index += samples_loaded;

			return samples_loaded;
		}

		return 0;
	}

	//-----------------------------------------------------------------------------
	// Purpose: Seek to a new position in the file
	//			NOTE: In most cases, only call this once, and call it before playing
	//			any data.
	// Input  : newPosition - new position in the sample clocks of this sample
	//-----------------------------------------------------------------------------
	public override void SetSampleStart(int newPosition) {
		// UNDONE: Implement this?
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	// Input  : delaySamples -
	//-----------------------------------------------------------------------------
	public override void SetStartupDelaySamples(int delaySamples) {
		this.delaySamples = delaySamples;
	}

	public override int GetPositionForSave() => GetStream() != null ? (int)GetStream()!.GetPosition() : 0;
	public override void SetPositionFromSaved(int position) => GetStream()?.SetPosition((uint)position);
}

public abstract class AudioSourceMP3 : AudioSourceBase
{
	protected AudioSourceCachedInfoHandle audioCacheHandle;
	protected int cachedDataSize;

	protected SfxTable sfx;
	protected int sampleRate;
	protected int dataSize;
	protected int dataStart;
	protected int refCount;
	protected bool isPlayOnce;
	protected bool isSentenceWord;
	protected bool checkedForPendingSentence;

	public AudioSourceMP3(SfxTable sfx) {
		sampleRate = 0;
		this.sfx = sfx;
		refCount = 0;

		dataStart = 0;

		object? file = g_pSndIO.Open(sfx.GetFileName());
		if (file != null) {
			dataSize = (int)g_pSndIO.Size(file);
			g_pSndIO.Close(file);
		}
		else {
			// No sound cache, the file isn't here, print this so that the relatively deep failure points that are about to
			// spew make a little more sense
			Warning($"MP3 is completely missing, sound system will be upset to learn of this [ {sfx.GetFileName()} ]\n");
			dataSize = 0;
		}


		cachedDataSize = 0;
		isPlayOnce = false;
		isSentenceWord = false;
		checkedForPendingSentence = false;
	}

	public AudioSourceMP3(SfxTable sfx, AudioSourceCachedInfo info) {
		this.sfx = sfx;
		refCount = 0;

		sampleRate = info.SampleRate();
		dataSize = info.DataSize();
		dataStart = info.DataStart();

		cachedDataSize = 0;
		isPlayOnce = false;
		checkedForPendingSentence = false;

		CheckAudioSourceCacheInternal();
	}

	// mixer's references
	public override void ReferenceAdd(AudioMixer mixer) {
		refCount++;
	}

	public override void ReferenceRemove(AudioMixer mixer) {
		refCount--;
		if (refCount == 0 && IsPlayOnce()) {
			SetPlayOnce(false); // in case it gets used again
			CacheUnload();
		}
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	// Output : Returns true on success, false on failure.
	//-----------------------------------------------------------------------------
	public override bool IsAsyncLoad() {
		// If there's a bit of "cached data" then we don't have to lazy/async load (we still async load the remaining data,
		//  but we run from the cache initially)
		return cachedDataSize <= 0;
	}

	// check reference count, return true if nothing is referencing this
	public override bool CanDelete() {
		return refCount <= 0;
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	// Output : int
	//-----------------------------------------------------------------------------
	public override AudioSourceType GetAudioSourceType() {
		return AudioSourceType.AUDIO_SOURCE_MP3;
	}

	//-----------------------------------------------------------------------------
	public virtual void SetSentence(Sentence sentence) {
		AudioSourceCachedInfo? info = audioCacheHandle.FastGet();

		if (info == null)
			return;

		if (info != null && info.Sentence() != null)
			return;

		Sentence newSentence = new Sentence();

		newSentence.Append(0.0f, sentence);
		newSentence.MakeRuntimeOnly();

		info!.SetSentence(newSentence);
	}

	public override int SampleRate() {
		if (sampleRate == 0) {
			// This should've come from the sound cache. We can avoid sync I/O jank if and only if we've started streaming
			// data already for some other reason. (Despite the name, CreateWaveDataMemory is just creating a wrapper class
			// that manages access to the wave data cache)
			IWaveData data = CreateWaveDataMemory(this);
			if (!data.IsReadyToMix() && SND_IsInGame()) {
				// If you hit this, you're creating a sound source that isn't in the sound cache, and asking for its sample
				// rate before it has streamed enough data in to read it from the underlying file. Your options are:
				// - Rebuild sound cache or figure out why this sound wasn't included.
				// - Precache this sound at level load so this doesn't happen during gameplay.
				// - Somehow call CacheLoad() on this source earlier so it has time to get data into memory so the data
				//   shows up as IsReadyToMix here, and this crutch won't jank.
				Warning($"MP3 initialized with no sound cache, this may cause janking. [ {GetFileName()} ]\n");
				// The code below will still go fine, but the mixer will emit a jank warning that the data wasn't ready and
				// do sync I/O
			}
			AudioMixerWaveMP3 mixer = new AudioMixerWaveMP3(data);
			sampleRate = mixer.GetStreamOutputRate();
			// pData ownership is passed to, and free'd by, pMixer
			mixer.Dispose();
		}
		return sampleRate;
	}

	public override void GetCacheData(AudioSourceCachedInfo info) {
		// Don't want to replicate our cached sample rate back into the new cache, ensure we recompute it.
		AudioMixerWaveMP3 tempMixer = new AudioMixerWaveMP3(CreateWaveDataMemory(this));
		sampleRate = tempMixer.GetStreamOutputRate();
		tempMixer.Dispose();

		AssertMsg(sampleRate != 0, "Creating cache with invalid sample rate data");
		if (sampleRate == 0)
			Warning($"Failed to find sample rate creating cache data for MP3, cache will be invalid [ {GetFileName()} ]\n");

		info.SetSampleRate(sampleRate);
		info.SetDataStart(0);

		object? file = g_pSndIO.Open(sfx.GetFileName());
		if (file == null) {
			Warning($"Failed to find file for building soundcache [ {sfx.GetFileName()} ]\n");
			// Don't re-use old cached value
			dataSize = 0;
		}
		else
			dataSize = (int)g_pSndIO.Size(file);

		Assert(dataSize > 0);

		g_pSndIO.Close(file);

		// Data size gets computed in GetStartupData!!!
		info.SetDataSize(dataSize);
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	// Output : char const
	//-----------------------------------------------------------------------------
	public override ReadOnlySpan<char> GetFileName() {
		return sfx != null ? sfx.GetFileName() : "NULL m_pSfx";
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	//-----------------------------------------------------------------------------
	public override void CheckAudioSourceCache() => CheckAudioSourceCacheInternal();

	void CheckAudioSourceCacheInternal() {
		Assert(sfx);

		if (!sfx.IsPrecachedSound())
			return;

		// This will "re-cache" this if it's not in this level's cache already
		audioCacheHandle.Get(AudioSourceType.AUDIO_SOURCE_MP3, true, sfx, ref cachedDataSize);
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	// Output : byte
	//-----------------------------------------------------------------------------
	protected byte[]? GetCachedDataPointer() {
		AudioSourceCachedInfo? info = audioCacheHandle.Get(AudioSourceType.AUDIO_SOURCE_MP3, sfx.IsPrecachedSound(), sfx, ref cachedDataSize);
		if (info == null) {
			AssertMsg(false, "CAudioSourceMP3::GetCachedDataPointer info == NULL");
			return null;
		}

		return info.CachedData();
	}

	// Returns true if the source is a voice source.
	// This affects the voice_overdrive behavior (all sounds get quieter when
	// someone is speaking).
	public override bool IsVoiceSource() => false;
	public override int SampleSize() => 1;

	// Total number of samples in this source.  NOTE: Some sources are infinite (mic input), they should return
	// a count equal to one second of audio at their current rate.
	public override int SampleCount() => dataSize;

	public override int Format() => 0;
	public override int DataSize() => 0;

	public override bool IsLooped() => false;
	public override bool IsStereoWav() => false;
	public override bool IsStreaming() => false;
	public override AudioSourceCacheStatus GetCacheStatus() => AudioSourceCacheStatus.AUDIO_IS_LOADED;
	public override void CacheLoad() { }
	public override void CacheUnload() { }
	public override Sentence? GetSentence() => null;

	public override int ZeroCrossingBefore(int sample) => sample;
	public override int ZeroCrossingAfter(int sample) => sample;

	public override void SetPlayOnce(bool isPlayOnce) => this.isPlayOnce = isPlayOnce;
	public override bool IsPlayOnce() => isPlayOnce;

	public override void SetSentenceWord(bool isWord) => isSentenceWord = isWord;
	public override bool IsSentenceWord() => isSentenceWord;

	public override int SampleToStreamPosition(int samplePosition) => 0;
	public override int StreamToSamplePosition(int streamPosition) => 0;
}

//-----------------------------------------------------------------------------
// Purpose: Streaming MP3 file
//-----------------------------------------------------------------------------
public class AudioSourceStreamMP3 : AudioSourceMP3, IWaveStreamSource
{
	//-----------------------------------------------------------------------------
	// CAudioSourceStreamMP3
	//-----------------------------------------------------------------------------
	public AudioSourceStreamMP3(SfxTable sfx) : base(sfx) {
	}

	public AudioSourceStreamMP3(SfxTable sfx, AudioSourceCachedInfo info) : base(sfx, info) {
		dataSize = info.DataSize();
	}

	public override bool IsStreaming() => true;
	public override bool IsStereoWav() => false;

	// IWaveStreamSource
	public int UpdateLoopingSamplePosition(int samplePosition) {
		return samplePosition;
	}
	public void UpdateSamples(Span<byte> data, int sampleCount) { }

	public int GetLoopingInfo(out int loopBlock, out int numLeadingSamples, out int numTrailingSamples) {
		loopBlock = 0;
		numLeadingSamples = 0;
		numTrailingSamples = 0;
		return 0;
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	//-----------------------------------------------------------------------------
	public override void Prefetch() {
		PrefetchDataStream(sfx.GetFileName(), 0, dataSize);
	}

	public override AudioMixer? CreateMixer(int initialStreamPosition = 0) {
		// BUGBUG: Source constructs the IWaveData, mixer frees it, fix this?
		IWaveData? waveData = CreateWaveDataStream(this, this, sfx.GetFileName(), 0, dataSize, sfx, 0);
		if (waveData != null) {
			AudioMixer mixer = new AudioMixerWaveMP3(waveData);
			if (!checkedForPendingSentence) {
				if (g_PhonemeFileSentences.TryGetValue(new string(sfx.GetFileName()), out Sentence? sentence))
					SetSentence(sentence);

				checkedForPendingSentence = true;
			}

			return mixer;
		}

		return null;
	}

	public override int GetOutputData(out ReadOnlySpan<byte> data, int samplePosition, int sampleCount, Span<byte> copyBuf) {
		data = default;
		return 0;
	}
}

public class AudioSourceMP3Cache : AudioSourceMP3
{
	protected memhandle_t cache;

	bool noSentence;

	//-----------------------------------------------------------------------------
	// Purpose: NULL the wave data pointer (we haven't loaded yet)
	//-----------------------------------------------------------------------------
	public AudioSourceMP3Cache(SfxTable sfx) : base(sfx) {
		cache = 0;
	}

	public AudioSourceMP3Cache(SfxTable sfx, AudioSourceCachedInfo info) : base(sfx, info) {
		cache = 0;

		dataSize = info.DataSize();
		dataStart = info.DataStart();

		noSentence = false;
	}

	//-----------------------------------------------------------------------------
	// Purpose: Free any wave data we've allocated
	//-----------------------------------------------------------------------------
	public override void Dispose() {
		CacheUnload();
		base.Dispose();
	}

	public override AudioSourceCacheStatus GetCacheStatus() {
		AudioSourceCacheStatus loaded = wavedatacache.IsDataLoadCompleted(cache, out bool cacheValid) ? AudioSourceCacheStatus.AUDIO_IS_LOADED : AudioSourceCacheStatus.AUDIO_NOT_LOADED;
		if (!cacheValid)
			wavedatacache.RestartDataLoad(ref cache, sfx.GetFileName(), dataSize, dataStart);
		return loaded;
	}

	public override void CacheLoad() {
		// Commence lazy load?
		if (cache != 0) {
			GetCacheStatus();
			return;
		}

		cache = wavedatacache.AsyncLoadCache(sfx.GetFileName(), dataSize, dataStart);
	}

	public override void CacheUnload() {
		if (cache != 0)
			wavedatacache.Unload(cache);
	}

	public override void Prefetch() { }

	protected virtual Span<byte> GetDataPointer() {
		Span<byte> mp3Data = default;

		if (cache == 0)
			CacheLoad();

		wavedatacache.GetDataPointer(
			ref cache,
			sfx.GetFileName(),
			dataSize,
			dataStart,
			out mp3Data,
			0,
			out bool dummy);

		return mp3Data;
	}

	// NOTE: "samples" are bytes for MP3
	public override int GetOutputData(out ReadOnlySpan<byte> data, int samplePosition, int sampleCount, Span<byte> copyBuf) {
		data = default;

		// how many bytes are available ?
		int totalSampleCount = dataSize - samplePosition;

		// may be asking for a sample out of range, clip at zero
		if (totalSampleCount < 0)
			totalSampleCount = 0;

		// clip max output samples to max available
		if (sampleCount > totalSampleCount)
			sampleCount = totalSampleCount;

		// if we are returning some samples, store the pointer
		if (sampleCount != 0) {
			// Starting past end of "preloaded" data, just use regular cache
			if (samplePosition >= cachedDataSize)
				data = GetDataPointer();
			else {
				// Start async loader if we haven't already done so
				CacheLoad();

				// Return less data if we are about to run out of uncached data
				if (samplePosition + sampleCount >= cachedDataSize)
					sampleCount = cachedDataSize - samplePosition;

				// Point at preloaded/cached data from .cache file for now
				data = GetCachedDataPointer();
			}

			if (!data.IsEmpty)
				data = data[samplePosition..];
			else {
				// Out of data or file i/o problem
				sampleCount = 0;
			}
		}

		return sampleCount;
	}

	public override AudioMixer? CreateMixer(int initialStreamPosition = 0) {
		AudioMixer mixer = new AudioMixerWaveMP3(CreateWaveDataMemory(this));

		return mixer;
	}

	public override Sentence? GetSentence() {
		// Already checked and this wav doesn't have sentence data...
		if (noSentence == true)
			return null;

		// Look up sentence from cache
		AudioSourceCachedInfo? info = audioCacheHandle.FastGet();
		if (info == null)
			info = audioCacheHandle.Get(AudioSourceType.AUDIO_SOURCE_WAV, sfx.IsPrecachedSound(), sfx, ref cachedDataSize);
		Assert(info);
		if (info == null) {
			noSentence = true;
			return null;
		}

		Sentence? sentence = info.Sentence();
		if (sentence == null) {
			if (!checkedForPendingSentence) {
				if (g_PhonemeFileSentences.TryGetValue(new string(sfx.GetFileName()), out Sentence? pending)) {
					sentence = pending;
					SetSentence(sentence);
				}
				checkedForPendingSentence = true;
			}
		}

		if (sentence == null) {
			noSentence = true;
			return null;
		}

		if (sentence.IsValid)
			return sentence;

		noSentence = true;

		return null;
	}
}
