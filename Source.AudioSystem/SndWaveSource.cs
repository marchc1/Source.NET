global using static Source.AudioSystem.SndWaveSource;

using Source.Common;
using Source.Common.Audio;
using Source.Common.Commands;
using Source.Common.Formats;
using Source.Common.Hashing;
using Source.Common.Utilities;

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using static Source.Common.Formats.RiffConstants;

namespace Source.AudioSystem;

public static class SndWaveSource
{
	// This determines how much data to pre-cache (will invalidate per-map caches if changed).
	public const float SND_ASYNC_LOOKAHEAD_SECONDS = 0.125f;

	public static readonly ConVar snd_async_minsize = new("snd_async_minsize", "262144");

	public static readonly AudioSourceCache g_ASCache = new();
	public static IAudioSourceCache audiosourcecache => g_ASCache;

	//-----------------------------------------------------------------------------
	// Purpose: Report chunk error
	// Input  : id - chunk FOURCC
	//-----------------------------------------------------------------------------
	public static void ChunkError(uint id) {
	}

	//-----------------------------------------------------------------------------
	// Purpose: Determine a true sample count for an ADPCM blob
	//-----------------------------------------------------------------------------
	public static int ADPCMSampleCount(ReadOnlySpan<byte> format, int length) {
		// determine a true sample count
		int nChannels = BinaryPrimitives.ReadUInt16LittleEndian(format[2..]);
		int wSamplesPerBlock = BinaryPrimitives.ReadUInt16LittleEndian(format[18..]);

		int blockSize = ((wSamplesPerBlock - 2) * nChannels) / 2;
		blockSize += 7 * nChannels;

		int blockCount = length / blockSize;
		int blockRem = length % blockSize;

		// total samples in complete blocks
		int sampleCount = blockCount * wSamplesPerBlock;

		// add remaining in a short block
		if (blockRem != 0)
			sampleCount += wSamplesPerBlock - (((blockSize - blockRem) * 2) / nChannels);

		return sampleCount;
	}

	//-----------------------------------------------------------------------------
	// Purpose: Create a wave audio source (streaming or in memory)
	// Input  : *pName - file name (NOTE: CAUDIOSOURCE KEEPS A POINTER TO pSfx)
	//			streaming - if true, don't load, stream each instance
	// Output : CAudioSource * - a new source
	//-----------------------------------------------------------------------------
	public static AudioSourceBase? CreateWave(SfxTable sfx, bool streaming) {
		Assert(sfx);

		AudioSourceWave? wave = null;

		// Caching should always work, so if we failed to cache, it's a problem reading the file data, etc.
		bool isMapSound = sfx.IsPrecachedSound();
		AudioSourceCachedInfo? info = audiosourcecache.GetInfo(AudioSourceType.AUDIO_SOURCE_WAV, isMapSound, sfx);

		if (info != null && info.Type() != AudioSourceType.AUDIO_SOURCE_UNK) {
			// create the source from this file
			if (streaming)
				wave = new AudioSourceStreamWave(sfx, info);
			else
				wave = new AudioSourceMemWave(sfx, info);
		}

		if (wave != null && wave.Format() == 0) {
			// lack of format indicates failure
			wave.Dispose();
			wave = null;
		}

		return wave;
	}

	//-----------------------------------------------------------------------------
	// Purpose: Wrapper for CreateWave()
	//-----------------------------------------------------------------------------
	public static AudioSourceBase? Audio_CreateStreamedWave(SfxTable sfx) {
		if (Audio_IsMP3(sfx.GetFileName()))
			return Audio_CreateStreamedMP3(sfx);

		return CreateWave(sfx, true);
	}

	//-----------------------------------------------------------------------------
	// Purpose: Wrapper for CreateWave()
	//-----------------------------------------------------------------------------
	public static AudioSourceBase? Audio_CreateMemoryWave(SfxTable sfx) {
		if (Audio_IsMP3(sfx.GetFileName()))
			return Audio_CreateMemoryMP3(sfx);

		return CreateWave(sfx, false);
	}

	static float Audio_GetMP3Duration(ReadOnlySpan<char> name) {
		// Deduce from file
		return GetMP3Duration_Helper(name);
	}

	static float Audio_GetWaveDuration(ReadOnlySpan<char> name) {
		Span<byte> formatBuffer = stackalloc byte[1024];

		using InFileRIFF riff = new InFileRIFF(name, g_pSndIO);

		if (riff.RIFFName() != RIFF_WAVE) {
			AudioSourceWave.MaybeReportMissingWav(name);
			return 0.0f;
		}

		// set up the iterator for the whole file (root RIFF is a chunk)
		IterateRIFF walk = new IterateRIFF(riff, (int)riff.RIFFSize());

		int format = 0;
		int formatSize = 0;
		int sampleCount = 0;

		// This chunk must be first as it contains the wave's format
		// break out when we've parsed it
		while (walk.ChunkAvailable() && (format == 0 || sampleCount == 0)) {
			uint chunkName = walk.ChunkName();
			if (chunkName == WAVE_FMT) {
				if (walk.ChunkSize() <= formatBuffer.Length) {
					walk.ChunkRead(formatBuffer);
					formatSize = (int)walk.ChunkSize();
					format = BinaryPrimitives.ReadUInt16LittleEndian(formatBuffer);
				}
			}
			else if (chunkName == WAVE_DATA) {
				if (format != 0) {
					int dataSize = (int)walk.ChunkSize();
					if (format == WAVE_FORMAT_ADPCM) {
						// Dummy size for now
						sampleCount = dataSize;
					}
					else
						sampleCount = dataSize / (BinaryPrimitives.ReadUInt16LittleEndian(formatBuffer[14..]) >> 3);
				}
			}
			else
				ChunkError(walk.ChunkName());
			walk.ChunkNext();
		}

		// Not really a WAVE file or no format chunk, bail
		if (format == 0 || sampleCount == 0)
			return 0.0f;

		float sampleRate = BinaryPrimitives.ReadUInt32LittleEndian(formatBuffer[4..]);

		if (format == WAVE_FORMAT_ADPCM) {
			// Determine actual duration
			sampleCount = ADPCMSampleCount(formatBuffer, sampleCount);
		}

		return (float)sampleCount / sampleRate;
	}

	//-----------------------------------------------------------------------------
	// Purpose: Fast method for determining duration of .wav/.mp3, exposed to server as well
	// Input  : *pName -
	// Output : float
	//-----------------------------------------------------------------------------
	public static float AudioSource_GetSoundDuration(ReadOnlySpan<char> name) {
		if (Audio_IsMP3(name))
			return Audio_GetMP3Duration(name);

		SfxTable? sound = S_PrecacheSound(name);
		if (sound != null)
			return AudioSource_GetSoundDuration(sound);

		return Audio_GetWaveDuration(name);
	}

	public static float AudioSource_GetSoundDuration(SfxTable? sfx) {
		if (sfx != null && sfx.GetSource() != null)
			return (float)sfx.GetSource()!.SampleCount() / (float)sfx.GetSource()!.SampleRate();

		return 0;
	}

	public static void CheckCacheBuild() {
		g_ASCache.CheckCacheBuild();
	}

	[ConCommand(helpText: "<directory or VPK filename>  Rebulds sound cache for a given search path.\n")]
	static void snd_buildcache(in TokenizedCommand args) {
		if (args.ArgC() < 2) {
			ConMsg("Usage:  snd_buildcache <directory or VPK filename>\n");
			return;
		}

		// Allow them to eitehr specify multiple args, or comma-seperated list.
		// You cannot easily pas multiple args on the (OS) command line.
		for (int idxArg = 1; idxArg < args.ArgC(); ++idxArg) {
			foreach (string path in new string(args.Arg(idxArg)).Split(','))
				g_ASCache.BuildCache(path);
		}
	}
}

public class AudioSourceWave : AudioSourceBase
{
	protected int bits;
	protected int rate;
	protected int channels;
	protected int format;
	protected int sampleSize;
	protected int loopStart;
	protected int sampleCount;          // can be "samples" or "bytes", depends on format

	protected SfxTable? sfx;
	protected Sentence? tempSentence;

	protected int dataStart;            // offset of sample data
	protected int dataSize;             // size of sample data

	protected byte[]? header;
	protected int headerSize;

	protected AudioSourceCachedInfoHandle audioCacheHandle;

	protected int cachedDataSize;

	// number of actual samples (regardless of format)
	// compressed formats alter definition of m_sampleCount
	// used to spare expensive calcs by decoders
	protected int numDecodedSamples;

	// additional data needed by xma decoder to for looping
	protected ushort loopBlock;             // the block the loop occurs in
	protected ushort numLeadingSamples;     // number of leader samples in the loop block to discard
	protected ushort numTrailingSamples;    // number of trailing samples in the final block to discard

	protected bool noSentence;
	protected bool isPlayOnce;
	protected bool isSentenceWord;

	int refCount;

	//-----------------------------------------------------------------------------
	// Purpose: Init to empty wave
	//-----------------------------------------------------------------------------
	public AudioSourceWave(SfxTable? sfx) {
		format = 0;
		header = null;
		headerSize = 0;

		// no looping
		loopStart = -1;

		sampleSize = 1;
		sampleCount = 0;
		bits = 0;
		channels = 0;
		dataStart = 0;
		dataSize = 0;
		rate = 0;

		refCount = 0;

		this.sfx = sfx;

		noSentence = false;
		tempSentence = null;
		cachedDataSize = 0;
		isPlayOnce = false;
		isSentenceWord = false;

		numDecodedSamples = 0;
	}

	public AudioSourceWave(SfxTable sfx, AudioSourceCachedInfo info) {
		this.sfx = sfx;

		refCount = 0;

		header = null;
		headerSize = 0;

		if (info.HeaderData() != null) {
			header = ArrayCopy(info.HeaderData().AsSpan(0, info.HeaderSize()));
			headerSize = info.HeaderSize();
		}

		bits = info.Bits();
		channels = info.Channels();
		sampleSize = info.SampleSize();
		format = info.Format();
		dataStart = info.DataStart();
		dataSize = info.DataSize();
		rate = info.SampleRate();
		loopStart = info.LoopStart();
		sampleCount = info.SampleCount();
		numDecodedSamples = sampleCount;

		if (format == WAVE_FORMAT_ADPCM && header != null)
			numDecodedSamples = ADPCMSampleCount(header, sampleCount);

		noSentence = false;
		tempSentence = null;
		cachedDataSize = 0;
		isPlayOnce = false;
		isSentenceWord = false;
	}

	public override void Dispose() {
		// for non-standard waves, we store a copy of the header in RAM
		header = null;
		tempSentence = null;
		base.Dispose();
	}

	public override AudioSourceType GetAudioSourceType() {
		return AudioSourceType.AUDIO_SOURCE_WAV;
	}

	public override AudioMixer? CreateMixer(int initialStreamPosition = 0) => null;
	public override int GetOutputData(out ReadOnlySpan<byte> data, int samplePosition, int sampleCount, Span<byte> copyBuf) {
		data = default;
		return 0;
	}

	public override void GetCacheData(AudioSourceCachedInfo info) {
		Assert(info.Type() == AudioSourceType.AUDIO_SOURCE_WAV);

		byte[] tempbuf = new byte[32768];
		int datalen = 0;
		// NOTE GetStartupData has side-effects (...) hence the unconditional call
		if (GetStartupData(tempbuf, ref datalen) &&
			 AudioSourceCachedInfo.s_bIsPrecacheSound &&
			 datalen > 0) {
			byte[] data = ArrayCopy(tempbuf.AsSpan(0, datalen));
			info.SetCachedDataSize(datalen);
			info.SetCachedData(data);
		}

		info.SetBits(bits);
		info.SetChannels(channels);
		info.SetSampleSize(sampleSize);
		info.SetFormat(format);
		info.SetDataStart(dataStart);   // offset of wave data chunk
		info.SetDataSize(dataSize);     // size of wave data chunk
		info.SetSampleRate(rate);
		info.SetLoopStart(loopStart);
		info.SetSampleCount(sampleCount);

		if (tempSentence != null) {
			Sentence scopy = new Sentence();
			scopy.CopyFrom(tempSentence);
			info.SetSentence(scopy);

			// Wipe it down to basically nothing
			tempSentence = null;
		}

		if (header != null && headerSize > 0) {
			byte[] data = ArrayCopy(header.AsSpan(0, headerSize));
			info.SetHeaderSize(headerSize);
			info.SetHeaderData(data);
		}
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
	// Output : Returns true on success, false on failure.
	//-----------------------------------------------------------------------------
	public override bool IsAsyncLoad() {
		if (!audioCacheHandle.IsValid())
			audioCacheHandle.Get(GetAudioSourceType(), sfx!.IsPrecachedSound(), sfx, ref cachedDataSize);

		// If there's a bit of "cached data" then we don't have to lazy/async load (we still async load the remaining data,
		//  but we run from the cache initially)
		if (dataSize > snd_async_minsize.GetInt())
			return true;
		return cachedDataSize <= 0;
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	//-----------------------------------------------------------------------------
	public override void CheckAudioSourceCache() {
		Assert(sfx);

		if (sfx == null || !sfx.IsPrecachedSound())
			return;

		// This will "re-cache" this if it's not in this level's cache already
		audioCacheHandle.Get(GetAudioSourceType(), true, sfx, ref cachedDataSize);
	}

	//-----------------------------------------------------------------------------
	// Purpose: Init the wave data.
	// Input  : *pHeaderBuffer - the RIFF fmt chunk
	//			headerSize - size of that chunk
	//-----------------------------------------------------------------------------
	protected void Init(ReadOnlySpan<byte> headerBuffer, int headerSize) {
		// copy the relevant header data
		format = BinaryPrimitives.ReadUInt16LittleEndian(headerBuffer);
		bits = BinaryPrimitives.ReadUInt16LittleEndian(headerBuffer[14..]);
		rate = (int)BinaryPrimitives.ReadUInt32LittleEndian(headerBuffer[4..]);
		channels = BinaryPrimitives.ReadUInt16LittleEndian(headerBuffer[2..]);
		sampleSize = (bits * channels) / 8;

		// this can never be zero -- other functions divide by this.
		// this should never happen, but avoid crashing
		if (sampleSize <= 0)
			sampleSize = 1;

		if (format == WAVE_FORMAT_ADPCM) {
			// For non-standard waves (like ADPCM) store the header, it has the decoding coefficients
			header = ArrayCopy(headerBuffer[..headerSize]);
			this.headerSize = headerSize;

			// treat ADPCM sources as a file of bytes.  They are decoded by the mixer
			sampleSize = 1;
		}
	}

	public override int SampleRate() {
		return rate;
	}

	//-----------------------------------------------------------------------------
	// Purpose: Size of each sample
	// Output :
	//-----------------------------------------------------------------------------
	public override int SampleSize() {
		return sampleSize;
	}

	//-----------------------------------------------------------------------------
	// Purpose: Total number of samples in this source
	// Output : int
	//-----------------------------------------------------------------------------
	public override int SampleCount() {
		// caller wants real samples
		return numDecodedSamples;
	}

	public override int Format() {
		return format;
	}

	public override int DataSize() {
		return dataSize;
	}

	public override bool IsVoiceSource() {
		if (GetSentence() != null) {
			if (GetSentence()!.GetVoiceDuck())
				return true;
		}
		return false;
	}

	//-----------------------------------------------------------------------------
	// Purpose: Do any sample conversion
	//			For 8 bit PCM, convert to signed because the mixing routine assumes this
	// Input  : *pData - pointer to sample data
	//			sampleCount - number of samples
	//-----------------------------------------------------------------------------
	public void ConvertSamples(Span<byte> data, int sampleCount) {
		if (format == WAVE_FORMAT_PCM) {
			if (bits == 8) {
				for (int i = 0; i < sampleCount * channels; i++)
					data[i] = (byte)((int)data[i] - 128);
			}
		}
	}

	//-----------------------------------------------------------------------------
	// Purpose: Parse base chunks
	// Input  : &walk - riff file to parse
	//		  : chunkName - name of the chunk to parse
	//-----------------------------------------------------------------------------
	// UNDONE: Move parsing loop here and drop each chunk into a virtual function
	//			instead of this being virtual.
	public virtual void ParseChunk(IterateRIFF walk, uint chunkName) {
		if (chunkName == WAVE_CUE)
			ParseCueChunk(walk);
		else if (chunkName == WAVE_SAMPLER)
			ParseSamplerChunk(walk);
		else if (chunkName == WAVE_VALVEDATA)
			ParseSentence(walk);
		else {
			// unknown and don't care
			ChunkError(walk.ChunkName());
		}
	}

	public override bool IsLooped() {
		return loopStart >= 0;
	}

	public override bool IsStereoWav() {
		return channels == 2;
	}

	public override bool IsStreaming() {
		return false;
	}

	public override AudioSourceCacheStatus GetCacheStatus() {
		return AudioSourceCacheStatus.AUDIO_IS_LOADED;
	}

	public override void CacheLoad() {
	}

	public override void CacheUnload() {
	}

	public override int ZeroCrossingBefore(int sample) {
		return sample;
	}

	public override int ZeroCrossingAfter(int sample) {
		return sample;
	}

	public override void Prefetch() {
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	// Input  : &walk -
	//-----------------------------------------------------------------------------
	public virtual void ParseSentence(IterateRIFF walk) {
		byte[] buf = new byte[walk.ChunkSize()];
		walk.ChunkRead(buf);

		tempSentence = new Sentence();

		tempSentence.InitFromDataChunk(buf);

		// Throws all phonemes into one word, discards sentence memory, etc.
		tempSentence.MakeRuntimeOnly();
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	// Output : CSentence
	//-----------------------------------------------------------------------------
	public override Sentence? GetSentence() {
		// Already checked and this wav doesn't have sentence data...
		if (noSentence == true)
			return null;

		// Look up sentence from cache
		AudioSourceCachedInfo? info = audioCacheHandle.FastGet();
		if (info == null)
			info = audioCacheHandle.Get(AudioSourceType.AUDIO_SOURCE_WAV, sfx!.IsPrecachedSound(), sfx, ref cachedDataSize);
		Assert(info);
		if (info == null) {
			noSentence = true;
			return null;
		}

		Sentence? sentence = info.Sentence();
		if (sentence == null) {
			noSentence = true;
			return null;
		}

		if (sentence.IsValid)
			return sentence;

		noSentence = true;

		return null;
	}

	public ReadOnlySpan<char> GetName() {
		return sfx != null ? sfx.GetName() : null;
	}

	//-----------------------------------------------------------------------------
	// Purpose: Bastardized construction routine.  This is just to avoid complex
	//			constructor functions so code can be shared more easily by sub-classes
	// Input  : *pFormatBuffer - RIFF header
	//			formatSize - header size
	//			&walk - RIFF file
	//-----------------------------------------------------------------------------
	public void Setup(ReadOnlySpan<byte> formatBuffer, int formatSize, IterateRIFF walk) {
		Init(formatBuffer, formatSize);

		while (walk.ChunkAvailable()) {
			ParseChunk(walk, walk.ChunkName());
			walk.ChunkNext();
		}
	}

	protected bool GetStartupData(Span<byte> dest, ref int bytesCopied) {
		bytesCopied = 0;

		Span<byte> formatBuffer = stackalloc byte[1024];
		ReadOnlySpan<char> name = sfx!.GetFileName();
		using InFileRIFF riff = new InFileRIFF(name, g_pSndIO);

		if (riff.RIFFName() != RIFF_WAVE)
			return false;

		// set up the iterator for the whole file (root RIFF is a chunk)
		IterateRIFF walk = new IterateRIFF(riff, (int)riff.RIFFSize());

		int format = 0;
		int formatSize = 0;

		// This chunk must be first as it contains the wave's format
		// break out when we've parsed it
		while (walk.ChunkAvailable() && format == 0) {
			if (walk.ChunkName() == WAVE_FMT) {
				if (walk.ChunkSize() <= formatBuffer.Length) {
					walk.ChunkRead(formatBuffer);
					formatSize = (int)walk.ChunkSize();
					format = BinaryPrimitives.ReadUInt16LittleEndian(formatBuffer);
					if (BinaryPrimitives.ReadUInt16LittleEndian(formatBuffer[14..]) > 16)
						Warning($"Unsupported {(int)BinaryPrimitives.ReadUInt16LittleEndian(formatBuffer[14..])}-bit wave file {name}\n");
				}
			}
			else
				ChunkError(walk.ChunkName());
			walk.ChunkNext();
		}

		// Not really a WAVE file or no format chunk, bail
		if (format == 0)
			return false;

		Setup(formatBuffer, formatSize, walk);

		if (dataStart == 0 || dataSize == 0) {
			// failed during setup
			return false;
		}

		// requesting precache snippet as leader for streaming startup latency
		if (dest.Length != 0) {
			object? file = g_pSndIO.Open(sfx.GetFileName());
			if (file == null)
				return false;

			int bytesNeeded = (int)(channels * (bits >> 3) * rate * SND_ASYNC_LOOKAHEAD_SECONDS);

			// Round to multiple of 4
			bytesNeeded = (bytesNeeded + 3) & ~3;

			bytesCopied = Math.Min(dest.Length, dataSize);
			bytesCopied = Math.Min(bytesNeeded, bytesCopied);

			g_pSndIO.Seek(file, dataStart);
			g_pSndIO.Read(dest[..bytesCopied], file);
			g_pSndIO.Close(file);

			// some samples need to be converted
			ConvertSamples(dest, bytesCopied / sampleSize);
		}

		return true;
	}

	//-----------------------------------------------------------------------------
	// Purpose: parses loop information from a cue chunk
	// Input  : &walk - RIFF iterator
	// Output : int loop start position
	//-----------------------------------------------------------------------------
	protected void ParseCueChunk(IterateRIFF walk) {
		// Cue chunk as specified by RIFF format
		// see $/research/jay/sound/riffnew.htm
		Span<byte> cue_chunk = stackalloc byte[24];

		int cueCount;

		// assume that the cue chunk stored in the wave is the start of the loop
		// assume only one cue chunk, UNDONE: Test this assumption here?
		cueCount = walk.ChunkReadInt();
		if (cueCount > 0) {
			walk.ChunkReadPartial(cue_chunk);
			loopStart = BinaryPrimitives.ReadInt32LittleEndian(cue_chunk[20..]);
		}
	}

	//-----------------------------------------------------------------------------
	// Purpose: parses loop information from a 'smpl' chunk
	// Input  : &walk - RIFF iterator
	// Output : int loop start position
	//-----------------------------------------------------------------------------
	protected void ParseSamplerChunk(IterateRIFF walk) {
		// Sampler chunk for MIDI instruments
		// Parse loop info from this chunk too
		Span<byte> samplerChunk = stackalloc byte[36 + 24];

		// assume that the loop end is the sample end
		// assume that only the first loop is relevant

		walk.ChunkReadPartial(samplerChunk);
		if (BinaryPrimitives.ReadUInt32LittleEndian(samplerChunk[28..]) > 0) {
			// only support normal forward loops
			if (BinaryPrimitives.ReadUInt32LittleEndian(samplerChunk[(36 + 4)..]) == 0)
				loopStart = BinaryPrimitives.ReadInt32LittleEndian(samplerChunk[(36 + 8)..]);
#if DEBUG
			else
				Msg($"Unknown sampler chunk type {BinaryPrimitives.ReadUInt32LittleEndian(samplerChunk[(36 + 4)..])} on {sfx!.GetFileName()}\n");
#endif
		}
		// else discard - this is some other non-loop sampler data we don't support
	}

	//-----------------------------------------------------------------------------
	// Purpose: get the wave header
	//-----------------------------------------------------------------------------
	public byte[]? GetHeader() {
		return header;
	}

	//-----------------------------------------------------------------------------
	// Gets the looping information. Some parameters are interpreted based on format
	//-----------------------------------------------------------------------------
	public int GetLoopingInfo(out int loopBlock, out int numLeadingSamples, out int numTrailingSamples) {
		// for xma, the block that contains the loop point
		loopBlock = this.loopBlock;

		// for xma, the number of leading samples at the loop block to discard
		numLeadingSamples = this.numLeadingSamples;

		// for xma, the number of trailing samples at the final block to discard
		numTrailingSamples = this.numTrailingSamples;

		// the loop point in samples
		return loopStart;
	}

	//-----------------------------------------------------------------------------
	// Purpose: wrap the position wrt looping
	// Input  : samplePosition - absolute position
	// Output : int - looped position
	//-----------------------------------------------------------------------------
	public int ConvertLoopedPosition(int samplePosition) {
		// if the wave is looping and we're past the end of the sample
		// convert to a position within the loop
		// At the end of the loop, we return a short buffer, and subsequent call
		// will loop back and get the rest of the buffer
		if (loopStart >= 0 && samplePosition >= sampleCount) {
			// size of loop
			int loopSize = sampleCount - loopStart;
			// subtract off starting bit of the wave
			samplePosition -= loopStart;

			if (loopSize != 0) {
				// "real" position in memory (mod off extra loops)
				samplePosition = loopStart + (samplePosition % loopSize);
			}
			// ERROR? if no loopSize
		}

		return samplePosition;
	}

	//-----------------------------------------------------------------------------
	// Purpose: remove the reference for the mixer getting deleted
	// Input  : *pMixer -
	//-----------------------------------------------------------------------------
	public override void ReferenceRemove(AudioMixer mixer) {
		refCount--;

		if (refCount == 0 && IsPlayOnce()) {
			SetPlayOnce(false); // in case it gets used again
			CacheUnload();
		}
	}

	//-----------------------------------------------------------------------------
	// Purpose: Add a mixer reference
	// Input  : *pMixer -
	//-----------------------------------------------------------------------------
	public override void ReferenceAdd(AudioMixer mixer) {
		refCount++;
	}

	//-----------------------------------------------------------------------------
	// Purpose: return true if no mixers reference this source
	//-----------------------------------------------------------------------------
	public override bool CanDelete() {
		if (refCount > 0)
			return false;

		return true;
	}

	public override void SetPlayOnce(bool isPlayOnce) => this.isPlayOnce = isPlayOnce;
	public override bool IsPlayOnce() => isPlayOnce;

	public override void SetSentenceWord(bool isWord) => isSentenceWord = isWord;
	public override bool IsSentenceWord() => isSentenceWord;

	public override int SampleToStreamPosition(int samplePosition) => 0;
	public override int StreamToSamplePosition(int streamPosition) => 0;

	protected byte[]? GetCachedDataPointer() {
		AudioSourceCachedInfo? info = audioCacheHandle.Get(AudioSourceType.AUDIO_SOURCE_WAV, sfx!.IsPrecachedSound(), sfx, ref cachedDataSize);
		if (info == null) {
			AssertMsg(false, "CAudioSourceWave::GetCachedDataPointer info == NULL");
			return null;
		}

		return info.CachedData();
	}

	static readonly UtlSymbolTable wavErrors = new();

	public static void MaybeReportMissingWav(ReadOnlySpan<char> wav) {
		UtlSymId_t sym = wavErrors.Find(wav);
		if (UTL_INVAL_SYMBOL == sym) {
			// See if file exists
			if (filesystem.FileExists(wav))
				DevWarning($"Bad Audio file '{wav}'\n");
			else
				DevWarning($"Missing wav file '{wav}'\n");
			wavErrors.AddString(wav);
		}
	}
}

// CAudioSourceMemWave is a bunch of wave data that is all in memory.
// To use it:
// - derive from CAudioSourceMemWave
// - call CAudioSourceWave::Init with a WAVEFORMATEX
// - set m_sampleCount.
// - implement GetDataPointer
public class AudioSourceMemWave : AudioSourceWave
{
	protected memhandle_t cache;

	public AudioSourceMemWave() : base((SfxTable?)null) {
		cache = 0;
	}

	public AudioSourceMemWave(SfxTable sfx) : base(sfx) {
		cache = 0;
	}

	public AudioSourceMemWave(SfxTable sfx, AudioSourceCachedInfo info) : base(sfx, info) {
		cache = 0;
	}

	//-----------------------------------------------------------------------------
	// Purpose: Creates a mixer and initializes it with an appropriate mixer
	//-----------------------------------------------------------------------------
	public override AudioMixer? CreateMixer(int initialStreamPosition = 0) {
		AudioMixer? mixer = CreateWaveMixer(CreateWaveDataMemory(this), format, channels, bits, initialStreamPosition);

		return mixer;
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	// Input  : **pData - output pointer to samples
	//			samplePosition - position (in samples not bytes)
	//			sampleCount - number of samples (not bytes)
	// Output : int - number of samples available
	//-----------------------------------------------------------------------------
	public override int GetOutputData(out ReadOnlySpan<byte> data, int samplePosition, int sampleCount, Span<byte> copyBuf) {
		data = default;

		// handle position looping
		samplePosition = ConvertLoopedPosition(samplePosition);

		// how many samples are available (linearly not counting looping)
		int totalSampleCount = this.sampleCount - samplePosition;

		// may be asking for a sample out of range, clip at zero
		if (totalSampleCount < 0)
			totalSampleCount = 0;

		// clip max output samples to max available
		if (sampleCount > totalSampleCount)
			sampleCount = totalSampleCount;

		// byte offset in sample database
		samplePosition *= sampleSize;

		// if we are returning some samples, store the pointer
		if (sampleCount != 0) {
			// Starting past end of "preloaded" data, just use regular cache
			if (samplePosition >= cachedDataSize)
				data = GetDataPointer();
			else {
				// Start async loader if we haven't already done so
				CacheLoad();

				// Return less data if we are about to run out of uncached data
				if (samplePosition + (sampleCount * sampleSize) >= cachedDataSize)
					sampleCount = (cachedDataSize - samplePosition) / sampleSize;

				// Point at preloaded/cached data from .cache file for now
				data = GetCachedDataPointer();
			}

			if (!data.IsEmpty)
				data = data[samplePosition..];
			else {
				// End of data or some other problem
				sampleCount = 0;
			}
		}

		return sampleCount;
	}

	// Hardcoded macros to test for zero crossing
	static bool ZERO_X_8(sbyte b) => b < 2 && b > -2;
	static bool ZERO_X_16(short b) => b < 512 && b > -512;

	//-----------------------------------------------------------------------------
	// Purpose: Search backward for a zero crossing starting at sample
	// Input  : sample - starting point
	// Output : position of zero crossing
	//-----------------------------------------------------------------------------
	public override int ZeroCrossingBefore(int sample) {
		ReadOnlySpan<byte> waveData = GetDataPointer();

		if (format == WAVE_FORMAT_PCM) {
			if (bits == 8) {
				ReadOnlySpan<sbyte> data = MemoryMarshal.Cast<byte, sbyte>(waveData);
				int index = sample * sampleSize;
				bool zero = false;

				if (channels == 1) {
					while (sample > 0 && !zero && (uint)index < (uint)data.Length) {
						if (ZERO_X_8(data[index]))
							zero = true;
						else {
							sample--;
							index--;
						}
					}
				}
				else {
					while (sample > 0 && !zero && (uint)(index + 1) < (uint)data.Length) {
						if (ZERO_X_8(data[index]) && ZERO_X_8(data[index + 1]))
							zero = true;
						else {
							sample--;
							index--;
						}
					}
				}
			}
			else {
				ReadOnlySpan<short> data = MemoryMarshal.Cast<byte, short>(waveData);
				int index = sample * sampleSize / sizeof(short);
				bool zero = false;

				if (channels == 1) {
					while (sample > 0 && !zero && (uint)index < (uint)data.Length) {
						if (ZERO_X_16(data[index]))
							zero = true;
						else {
							index--;
							sample--;
						}
					}
				}
				else {
					while (sample > 0 && !zero && (uint)(index + 1) < (uint)data.Length) {
						if (ZERO_X_16(data[index]) && ZERO_X_16(data[index + 1]))
							zero = true;
						else {
							sample--;
							index--;
						}
					}
				}
			}
		}
		return sample;
	}

	//-----------------------------------------------------------------------------
	// Purpose: Search forward for a zero crossing
	// Input  : sample - starting point
	// Output : position of found zero crossing
	//-----------------------------------------------------------------------------
	public override int ZeroCrossingAfter(int sample) {
		ReadOnlySpan<byte> waveData = GetDataPointer();

		if (format == WAVE_FORMAT_PCM) {
			if (bits == 8) {
				ReadOnlySpan<sbyte> data = MemoryMarshal.Cast<byte, sbyte>(waveData);
				int index = sample * sampleSize;
				bool zero = false;

				if (channels == 1) {
					while (sample < SampleCount() && !zero && (uint)index < (uint)data.Length) {
						if (ZERO_X_8(data[index]))
							zero = true;
						else {
							sample++;
							index++;
						}
					}
				}
				else {
					while (sample < SampleCount() && !zero && (uint)(index + 1) < (uint)data.Length) {
						if (ZERO_X_8(data[index]) && ZERO_X_8(data[index + 1]))
							zero = true;
						else {
							sample++;
							index++;
						}
					}
				}
			}
			else {
				ReadOnlySpan<short> data = MemoryMarshal.Cast<byte, short>(waveData);
				int index = sample * sampleSize / sizeof(short);
				bool zero = false;

				if (channels == 1) {
					while (sample > 0 && !zero && (uint)index < (uint)data.Length) {
						if (ZERO_X_16(data[index]))
							zero = true;
						else {
							index++;
							sample++;
						}
					}
				}
				else {
					while (sample > 0 && !zero && (uint)(index + 1) < (uint)data.Length) {
						if (ZERO_X_16(data[index]) && ZERO_X_16(data[index + 1]))
							zero = true;
						else {
							sample++;
							index++;
						}
					}
				}
			}
		}
		return sample;
	}

	//-----------------------------------------------------------------------------
	// Purpose: parse chunks with unique processing to in-memory waves
	// Input  : &walk - RIFF file
	//-----------------------------------------------------------------------------
	public override void ParseChunk(IterateRIFF walk, uint chunkName) {
		// this is the audio data
		if (chunkName == WAVE_DATA) {
			ParseDataChunk(walk);
			return;
		}

		base.ParseChunk(walk, chunkName);
	}

	//-----------------------------------------------------------------------------
	// Purpose: reads the actual sample data and parses it
	// Input  : &walk - RIFF file
	//-----------------------------------------------------------------------------
	public void ParseDataChunk(IterateRIFF walk) {
		dataStart = walk.ChunkFilePosition() + 8;
		dataSize = (int)walk.ChunkSize();

		// 360 streaming model loads data later, but still needs critical member setup
		Span<byte> data = default;
		data = GetDataPointer();
		if (data.IsEmpty)
			Error($"CAudioSourceMemWave ({(sfx != null ? sfx.GetFileName() : "m_pSfx = NULL")}): GetDataPointer() failed.");

		// load them into memory (bad!!, this is a duplicate read of the data chunk)
		walk.ChunkRead(data[..dataSize]);

		if (format == WAVE_FORMAT_PCM) {
			// number of samples loaded
			sampleCount = dataSize / sampleSize;
			numDecodedSamples = sampleCount;
		}
		else if (format == WAVE_FORMAT_ADPCM) {
			// The ADPCM mixers treat the wave source as a flat file of bytes.
			// Since each "sample" is a byte (this is a flat file), the number of samples is the file size
			sampleCount = dataSize;
			sampleSize = 1;

			// file says 4, output is 16
			bits = 16;

			numDecodedSamples = ADPCMSampleCount(header, dataSize);
		}

		// some samples need to be converted
		if (!data.IsEmpty)
			ConvertSamples(data, sampleCount);
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	// Output : Returns true on success, false on failure.
	//-----------------------------------------------------------------------------
	public override AudioSourceCacheStatus GetCacheStatus() {
		// NOTE: This will start the load if it isn't started
		bool completed = wavedatacache.IsDataLoadCompleted(cache, out bool cacheValid);
		if (!cacheValid)
			wavedatacache.RestartDataLoad(ref cache, sfx!.GetFileName(), dataSize, dataStart);
		if (completed)
			return AudioSourceCacheStatus.AUDIO_IS_LOADED;
		if (wavedatacache.IsDataLoadInProgress(cache))
			return AudioSourceCacheStatus.AUDIO_LOADING;

		return AudioSourceCacheStatus.AUDIO_NOT_LOADED;
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	//-----------------------------------------------------------------------------
	public override void CacheLoad() {
		// Commence lazy load?
		if (cache != 0) {
			wavedatacache.IsDataLoadCompleted(cache, out bool cacheValid);
			if (!cacheValid)
				wavedatacache.RestartDataLoad(ref cache, sfx!.GetFileName(), dataSize, dataStart);
			return;
		}

		cache = wavedatacache.AsyncLoadCache(sfx!.GetFileName(), dataSize, dataStart);
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	//-----------------------------------------------------------------------------
	public override void CacheUnload() {
		if (cache != 0)
			wavedatacache.Unload(cache);
	}

	// by definition, should already be in memory
	public override void Prefetch() { }

	//-----------------------------------------------------------------------------
	// Purpose:
	// Output : char
	//-----------------------------------------------------------------------------
	protected virtual Span<byte> GetDataPointer() {
		Span<byte> waveData = default;

		if (cache == 0) {
			// not in cache, start loading
			CacheLoad();
		}

		// mount the requested data, blocks if necessary
		wavedatacache.GetDataPointer(
			ref cache,
			sfx!.GetFileName(),
			dataSize,
			dataStart,
			out waveData,
			0,
			out bool samplesConverted);

		// If we have reloaded data from disk (async) and we haven't converted the samples yet, do it now
		// FIXME:  Is this correct for stereo wavs?
		if (!waveData.IsEmpty && !samplesConverted) {
			ConvertSamples(waveData, dataSize / sampleSize);
			wavedatacache.SetPostProcessed(cache, true);
		}

		return waveData;
	}
}

//-----------------------------------------------------------------------------
// Purpose: Wave source for streaming wave files
// UNDONE: Handle looping
//-----------------------------------------------------------------------------
public class AudioSourceStreamWave : AudioSourceWave, IWaveStreamSource
{
	//-----------------------------------------------------------------------------
	// Purpose: Save a copy of the file name for instances to open later
	// Input  : *pFileName - filename
	//-----------------------------------------------------------------------------
	public AudioSourceStreamWave(SfxTable sfx) : base(sfx) {
		this.sfx = sfx;
		dataStart = -1;
		dataSize = 0;
		sampleCount = 0;
	}

	public AudioSourceStreamWave(SfxTable sfx, AudioSourceCachedInfo info) : base(sfx, info) {
		this.sfx = sfx;
		dataStart = info.DataStart();
		dataSize = info.DataSize();

		sampleCount = info.SampleCount();
	}

	public override bool IsStreaming() => true;

	// IWaveStreamSource
	public int UpdateLoopingSamplePosition(int samplePosition) {
		return ConvertLoopedPosition(samplePosition);
	}
	public void UpdateSamples(Span<byte> data, int sampleCount) {
		ConvertSamples(data, sampleCount);
	}
	int IWaveStreamSource.GetLoopingInfo(out int loopBlock, out int numLeadingSamples, out int numTrailingSamples) {
		return GetLoopingInfo(out loopBlock, out numLeadingSamples, out numTrailingSamples);
	}

	//-----------------------------------------------------------------------------
	// Purpose: Create an instance (mixer & wavedata) of this sound
	// Output : CAudioMixer * - pointer to the mixer
	//-----------------------------------------------------------------------------
	public override AudioMixer? CreateMixer(int initialStreamPosition = 0) {
		ReadOnlySpan<char> fileName = sfx!.GetFileName();

		// BUGBUG: Source constructs the IWaveData, mixer frees it, fix this?
		IWaveData? waveData = CreateWaveDataStream(this, this, fileName, dataStart, dataSize, sfx, initialStreamPosition);
		if (waveData != null) {
			AudioMixer? mixer = CreateWaveMixer(waveData, format, channels, bits, initialStreamPosition);
			if (mixer != null)
				return mixer;

			// no mixer, delete the stream buffer/instance
			waveData.Dispose();
		}

		return null;
	}

	public override void Prefetch() {
		PrefetchDataStream(sfx!.GetFileName(), dataStart, dataSize);
	}

	//-----------------------------------------------------------------------------
	//-----------------------------------------------------------------------------
	public override int SampleToStreamPosition(int samplePosition) {
		// not for PC
		Assert(false);
		return 0;
	}

	//-----------------------------------------------------------------------------
	//-----------------------------------------------------------------------------
	public override int StreamToSamplePosition(int streamPosition) {
		// not for PC
		Assert(false);
		return 0;
	}

	//-----------------------------------------------------------------------------
	// Purpose: Parse a stream wave file chunk
	//			unlike the in-memory file, don't load the data, just get a reference to it.
	// Input  : &walk - RIFF file
	//-----------------------------------------------------------------------------
	public override void ParseChunk(IterateRIFF walk, uint chunkName) {
		// NOTE: It would be nice to break out of parsing once we have the data start and
		//		save seeking over the whole file.  But to do so, the other needed chunks must occur
		//		before the DATA chunk.  But, that is not standard and breaks most other wav parsers.

		if (chunkName == WAVE_DATA) {
			// data starts at chunk + 8 (chunk name, chunk size = 2*4=8 bytes)
			// don't load the data, just know where it is so each instance
			// can load it later
			dataStart = walk.ChunkFilePosition() + 8;
			dataSize = (int)walk.ChunkSize();
			sampleCount = dataSize / sampleSize;
			return;
		}
		base.ParseChunk(walk, chunkName);
	}

	//-----------------------------------------------------------------------------
	// Purpose: This is not implemented here.  This source has no data.  It is the
	//			WaveData's responsibility to load/serve the data
	//-----------------------------------------------------------------------------
	public override int GetOutputData(out ReadOnlySpan<byte> data, int samplePosition, int sampleCount, Span<byte> copyBuf) {
		data = default;
		return 0;
	}

	public override AudioSourceCacheStatus GetCacheStatus() {
		if (dataSize == 0 || dataStart == 0) {
			// didn't get precached properly
			return AudioSourceCacheStatus.AUDIO_NOT_LOADED;
		}

		return AudioSourceCacheStatus.AUDIO_IS_LOADED;
	}
}

// Versions
//   3: The before time
//   4: Changed MP3 caching to ensure we store proper sample rate, removed hack to not cache vo/
//   5: Fixed bug that could result in incorrect mp3 datasizes in the sound cache
public class AudioSourceCache : IAudioSourceCache
{
	public const int AUDIOSOURCE_CACHE_VERSION = 5;

	readonly Dictionary<string, AudioSourceCachedInfo> cache = new(StringComparer.OrdinalIgnoreCase);

	int serverCount;
	bool sndCacheDebug;

	public AudioSourceCache() {
		serverCount = -1;
		sndCacheDebug = false;
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	// Output : Returns true on success, false on failure.
	//-----------------------------------------------------------------------------
	public bool Init(nuint memSize) {
#if DEBUG
		Msg("CAudioSourceCache: Init\n");
#endif

		sndCacheDebug = CommandLine.FindParm("-sndcachedebug") != 0;

		if (!wavedatacache.Init(memSize))
			Error("Unable to init wavedatacache system\n");

		return true;
	}

	//-----------------------------------------------------------------------------
	public void Shutdown() {
#if DEBUG
		Msg("CAudioSourceCache: Shutdown\n");
#endif

		cache.Clear();

		wavedatacache.Shutdown();
	}

	//-----------------------------------------------------------------------------
	// Purpose: Called by Host_Init on engine startup to rebuild everything if needed
	//-----------------------------------------------------------------------------
	public void CheckCacheBuild() {
		// !FIXME! We'll just do everything lazily for now!
	}

	//-----------------------------------------------------------------------------
	// Purpose: Static method
	// Output : unsigned int
	//-----------------------------------------------------------------------------
	public static uint AsyncLookaheadMetaChecksum() {
		CRC32_t crc = 0;
		CRC32.Init(ref crc);

		float f = SND_ASYNC_LOOKAHEAD_SECONDS;
		CRC32.ProcessBuffer(ref crc, in f);
		// Finish
		CRC32.Final(ref crc);

		return crc;
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	// Input  : *mapname -
	//-----------------------------------------------------------------------------
	public void LevelInit(ReadOnlySpan<char> mapname) {
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	//-----------------------------------------------------------------------------
	public void LevelShutdown() {
	}

	//-----------------------------------------------------------------------------
	static string GetSoundFilename(ReadOnlySpan<char> inputFilename) {
		return $"sound/{inputFilename}".Replace('\\', '/').Replace("/./", "/").ToLowerInvariant();
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	//-----------------------------------------------------------------------------
	public AudioSourceCachedInfo? GetInfo(AudioSourceType audiosourcetype, bool soundisprecached, SfxTable sfx) {
		Assert(sfx);

		string fn = GetSoundFilename(sfx.GetFileName());

		// Hack to remember the type of audiosource to create if we need to recreate it
		AudioSourceCachedInfo.s_CurrentType = audiosourcetype;
		AudioSourceCachedInfo.s_pSfx = sfx;
		AudioSourceCachedInfo.s_bIsPrecacheSound = soundisprecached;

		if (!filesystem.FileExists(fn, "game"))
			return null;

		if (!cache.TryGetValue(fn, out AudioSourceCachedInfo? info)) {
			info = new AudioSourceCachedInfo();
			info.Rebuild(fn);
			cache[fn] = info;
			if (sndCacheDebug)
				Msg($"CAudioSourceCache: Rebuilt {fn}\n");
		}

		return info;
	}

	public void RebuildCacheEntry(AudioSourceType audiosourcetype, bool soundisprecached, SfxTable sfx) {
		Assert(sfx);

		string fn = GetSoundFilename(sfx.GetFileName());

		// Hack to remember the type of audiosource to create if we need to recreate it
		AudioSourceCachedInfo.s_CurrentType = audiosourcetype;
		AudioSourceCachedInfo.s_pSfx = sfx;
		AudioSourceCachedInfo.s_bIsPrecacheSound = soundisprecached;

		if (!filesystem.FileExists(fn, "game"))
			return;

		AudioSourceCachedInfo info = new AudioSourceCachedInfo();
		info.Rebuild(fn);
		cache[fn] = info;
	}

	//-----------------------------------------------------------------------------
	public void ForceRecheckDiskInfo() {
		cache.Clear();
		AudioSourceCachedInfoHandle.InvalidateCache();
	}

	//-----------------------------------------------------------------------------
	public void BuildCache(ReadOnlySpan<char> searchPath) {
		Msg("Finding .wav files...\n");
		List<string> filenames = [];
		AddFilesToList(filenames, "sound", "wav");

		Msg("Finding .mp3 files...\n");
		AddFilesToList(filenames, "sound", "mp3");

		Msg($"Found {filenames.Count} audio files.\n");

		if (filenames.Count < 1) {
			Warning(" No audio files found.  Not building cache\n");
			return;
		}

		int lastShownPct = -1;
		for (int idxFilename = 0; idxFilename < filenames.Count; idxFilename++) {
			string filename = filenames[idxFilename];
			string name = filename[6..];

			// Show progress
			int pct = idxFilename * 100 / filenames.Count;
			if (pct != lastShownPct) {
				Msg($"  {pct,3}% {name}\n");
				lastShownPct = pct;
			}

			AudioSourceCachedInfo.s_bIsPrecacheSound = true;
			AudioSourceCachedInfo.s_CurrentType = AudioSourceType.AUDIO_SOURCE_WAV;
			if (Path.GetExtension(filename).Equals(".mp3", StringComparison.OrdinalIgnoreCase))
				AudioSourceCachedInfo.s_CurrentType = AudioSourceType.AUDIO_SOURCE_MP3;
			AudioSourceCachedInfo.s_pSfx = S_DummySfx(name);

			AudioSourceCachedInfo info = new AudioSourceCachedInfo();
			info.Rebuild(filename);
			cache[GetSoundFilename(name)] = info;
		}
	}

	static void AddFilesToList(List<string> list, string directory, string extension) {
		Span<char> search = stackalloc char[MAX_PATH];
		sprintf(search, "%s/*").S(directory);
		ReadOnlySpan<char> found = filesystem.FindFirstEx(search.SliceNullTerminatedString(), "GAME", out ulong handle);
		while (!found.IsEmpty) {
			string fullPath = $"{directory}/{found}";
			if (found[0] != '.') {
				if (filesystem.FindIsDirectory(handle))
					AddFilesToList(list, fullPath, extension);
				else if (Path.GetExtension(found).Equals("." + extension, StringComparison.OrdinalIgnoreCase))
					list.Add(fullPath);
			}
			found = filesystem.FindNext(handle);
		}
		filesystem.FindClose(handle);
	}
}
