using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;

namespace Source.Common.Audio;

public class AudioSourceCachedInfo : IBaseCacheInfo
{
	AudioSourceType type;
	byte bits;
	byte channels;
	byte sampleSize;
	byte format;
	uint rate;

	bool sentence;
	bool cachedData;
	bool header;

	int loopStart;
	int sampleCount;
	int dataStart;
	int dataSize;

	uint cachedDataSize;
	uint headerSize;

	public void Rebuild(ReadOnlySpan<char> filename) {

	}

	public void Restore(Stream buf) {

	}

	public void Save(Stream buf) {

	}

	public AudioSourceType Type() {
		return type;
	}
}

public interface AudioMixer
{
	int MixDataToDevice(IAudioDevice device, AudioChannel channel, int sampleCount, int outputRate, int outputOffset);
	int SkipSamples(AudioChannel channel, int sampleCount, int outputRate, int outputOffset);
	bool ShouldContinueMixing();

	AudioSource GetSource();

	// get the current position (next sample to be mixed)
	int GetSamplePosition();

	// Allow the mixer to modulate pitch and volume. 
	// returns a floating point modulator
	float ModifyPitch(float pitch);
	float GetVolumeScale();

	// NOTE: Playback is optimized for linear streaming.  These calls will usually cost performance
	// It is currently optimal to call them before any playback starts, but some audio sources may not
	// guarantee this.  Also, some mixers may choose to ignore these calls for internal reasons (none do currently).

	// Move the current position to newPosition 
	// BUGBUG: THIS CALL DOES NOT SUPPORT MOVING BACKWARD, ONLY FORWARD!!!
	void SetSampleStart(int newPosition);

	// End playback at newEndPosition
	void SetSampleEnd(int newEndPosition);

	// How many samples to skip before commencing actual data reading ( to allow sub-frametime sound
	//  offsets and avoid synchronizing sounds to various 100 msec clock intervals throughout the
	//  engine and game code)
	void SetStartupDelaySamples(int delaySamples);
	int GetMixSampleSize();

	// Certain async loaded sounds lazilly load into memory in the background, use this to determine
	//  if the sound is ready for mixing
	bool IsReadyToMix();

	// NOTE: The "saved" position can be different than the "sample" position
	// NOTE: Allows mixer to save file offsets, loop info, etc
	int GetPositionForSave();
	void SetPositionFromSaved(int savedPosition);
	void Free();
}
