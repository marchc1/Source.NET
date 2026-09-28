namespace Source.Common.Audio;

public enum AudioSourceType
{
	AUDIO_SOURCE_UNK = 0,
	AUDIO_SOURCE_WAV,
	AUDIO_SOURCE_MP3,
	AUDIO_SOURCE_VOICE,

	AUDIO_SOURCE_MAXTYPE,
}

public enum AudioSourceCacheStatus
{
	AUDIO_NOT_LOADED = 0,
	AUDIO_IS_LOADED = 1,
	AUDIO_LOADING = 2,
}

public abstract class AudioSource
{
	public const int AUDIOSOURCE_COPYBUF_SIZE = 4096;

	// Serialization for caching
	public abstract AudioSourceType GetAudioSourceType();

	public abstract int SampleRate();

	// Returns true if the source is a voice source.
	// This affects the voice_overdrive behavior (all sounds get quieter when
	// someone is speaking).
	public abstract bool IsVoiceSource();

	// Sample size is in bytes.  It will not be accurate for compressed audio.  This is a best estimate.
	// The compressed audio mixers understand this, but in general do not assume that SampleSize() * SampleCount() = filesize
	// or even that SampleSize() is 100% accurate due to compression.
	public abstract int SampleSize();

	// Total number of samples in this source.  NOTE: Some sources are infinite (mic input), they should return
	// a count equal to one second of audio at their current rate.
	public abstract int SampleCount();

	public abstract int Format();
	public abstract int DataSize();

	public abstract bool IsLooped();
	public abstract bool IsStereoWav();
	public abstract bool IsStreaming();
	public abstract AudioSourceCacheStatus GetCacheStatus();
	public bool IsCached() => GetCacheStatus() == AudioSourceCacheStatus.AUDIO_IS_LOADED;
	public abstract void CacheLoad();
	public abstract void CacheUnload();
	public abstract Sentence? GetSentence();

	// these are used to find good splice/loop points.
	// If not implementing these, simply return sample
	public abstract int ZeroCrossingBefore(int sample);
	public abstract int ZeroCrossingAfter(int sample);

	// check reference count, return true if nothing is referencing this
	public abstract bool CanDelete();

	public abstract void Prefetch();

	public abstract bool IsAsyncLoad();

	// Make sure our data is rebuilt into the per-level cache
	public abstract void CheckAudioSourceCache();

	public abstract ReadOnlySpan<char> GetFileName();

	public abstract void SetPlayOnce(bool isPlayOnce);
	public abstract bool IsPlayOnce();

	// Used to identify a word that is part of a sentence mixing operation
	public abstract void SetSentenceWord(bool isWord);
	public abstract bool IsSentenceWord();

	public abstract int SampleToStreamPosition(int samplePosition);
	public abstract int StreamToSamplePosition(int streamPosition);
}
