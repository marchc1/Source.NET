using Source.Common.Audio;

namespace Source.AudioSystem;

public struct VoxWord
{
	public int Volume;          // increase percent, ie: 125 = 125% increase
	public int Pitch;           // pitch shift up percent
	public int Start;           // offset start of wave percent
	public int End;             // offset end of wave percent
	public int CbTrim;          // end of wave after being trimmed to 'end'
	public int KeepCached;      // 1 if this word was already in cache before sentence referenced it
	public int SampleFrac;      // if pitch shifting, this is position into wav * 256
	public int TimeCompress;    // % of wave to skip during playback (causes no pitch shift)
	public SfxTable? Sfx;       // name and cache pointer
}

//-----------------------------------------------------------------------------
// Purpose: This replaces the old sentence logic that was integrated with the
//			sound code.  Now it is a hierarchical mixer.
//-----------------------------------------------------------------------------
public class SentenceMixer : AudioMixer
{
	// identifies the active word
	int currentWordIndex;
	AudioMixer? currentWordMixer;

	// set when a transition to a new word occurs
	bool newWord;

	readonly VoxWord[] voxWords = new VoxWord[CVOXWORDMAX];
	readonly AudioMixer?[] wordMixers = new AudioMixer?[CVOXWORDMAX];
	int numWords;

	public static AudioMixer? CreateSentenceMixer(VoxWord[]? words) {
		if (words != null)
			return new SentenceMixer(words);

		return null;
	}

	public SentenceMixer(VoxWord[] words) {
		// count the expected number of words
		numWords = 0;
		while (numWords < words.Length && words[numWords].Sfx != null) {
			// get a private copy of the words
			voxWords[numWords] = words[numWords];
			numWords++;
			if (numWords >= voxWords.Length) {
				// very long sentence, prevent overflow
				break;
			}
		}

		// startup all the mixers now, this serves as a hint to the audio streamer
		// actual mixing will commence when they are ALL ready
		for (int word = 0; word < numWords; word++) {
			// it is possible to get a null mixer (due to wav error, etc)
			// the sentence will skip these words
			wordMixers[word] = LoadWord(word);
		}
		Assert(numWords < wordMixers.Length);

		// find first valid word mixer
		currentWordIndex = 0;
		currentWordMixer = null;
		for (int word = 0; word < numWords; word++) {
			if (wordMixers[word] != null) {
				currentWordIndex = word;
				currentWordMixer = wordMixers[word];
				break;
			}
		}

		newWord = currentWordMixer != null;
	}

	public override void Dispose() {
		// free all words
		for (int word = 0; word < numWords; word++)
			FreeWord(word);
		base.Dispose();
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	// Output : Returns true if mixing can commence, false otherwise
	//-----------------------------------------------------------------------------
	public override bool IsReadyToMix() {
		if (currentWordMixer == null) {
			// no word, but mixing has to commence in order to shutdown
			return true;
		}

		// all the words should be available before mixing the sentence
		for (int word = currentWordIndex; word < numWords; word++) {
			if (wordMixers[word] != null && !wordMixers[word]!.IsReadyToMix()) {
				// Still waiting for async data to arrive
				return false;
			}
		}

		if (newWord) {
			newWord = false;

			int start = voxWords[currentWordIndex].Start;
			int end = voxWords[currentWordIndex].End;

			// don't allow overlapped ranges
			if (end <= start)
				end = 0;

			if (start != 0 || end != 0) {
				int sampleCount = currentWordMixer.GetSource().SampleCount();
				if (start > 0 && start < 100)
					currentWordMixer.SetSampleStart((int)(sampleCount * 0.01f * start));
				if (end > 0 && end < 100)
					currentWordMixer.SetSampleEnd((int)(sampleCount * 0.01f * end));
			}
		}

		return true;
	}

	public override bool ShouldContinueMixing() {
		if (currentWordMixer != null) {
			// keep mixing until the words run out
			return true;
		}

		return false;
	}

	public override AudioSourceBase GetSource() {
		if (currentWordMixer != null)
			return currentWordMixer.GetSource();

		return null!;
	}

	// get the current position (next sample to be mixed)
	public override int GetSamplePosition() {
		if (currentWordMixer != null)
			return currentWordMixer.GetSamplePosition();

		return 0;
	}

	// BUGBUG: These are only applied to the current word, not the whole sentence!!!!
	public override void SetSampleStart(int newPosition) {
		currentWordMixer?.SetSampleStart(newPosition);
	}

	// End playback at newEndPosition
	public override void SetSampleEnd(int newEndPosition) {
		currentWordMixer?.SetSampleEnd(newEndPosition);
	}

	public override void SetStartupDelaySamples(int delaySamples) {
		currentWordMixer?.SetStartupDelaySamples(delaySamples);
	}

	public override int GetMixSampleSize() => currentWordMixer != null ? currentWordMixer.GetMixSampleSize() : 0;

	public override int GetPositionForSave() => GetSamplePosition();
	public override void SetPositionFromSaved(int savedPosition) => SetSampleStart(savedPosition);

	//-----------------------------------------------------------------------------
	// Purpose: Free a word
	//-----------------------------------------------------------------------------
	void FreeWord(int word) {
		if (wordMixers[word] != null) {
			wordMixers[word]!.Dispose();
			wordMixers[word] = null;
		}

		if (voxWords[word].Sfx != null) {
			// If this wave wasn't precached by the game code
			if (voxWords[word].KeepCached == 0) {
				// If this was the last mixer that had a reference
				if (voxWords[word].Sfx!.GetSource()!.CanDelete()) {
					// free the source
					voxWords[word].Sfx!.GetSource()!.Dispose();
					voxWords[word].Sfx!.Source = null;
				}
			}
		}
	}

	//-----------------------------------------------------------------------------
	// Purpose: Load a word
	//-----------------------------------------------------------------------------
	AudioMixer? LoadWord(int word) {
		AudioMixer? mixer = null;
		if (voxWords[word].Sfx != null) {
			AudioSourceBase? source = S_LoadSound(voxWords[word].Sfx!, null);
			if (source != null) {
				source.SetSentenceWord(true);
				mixer = source.CreateMixer();
			}
		}

		return mixer;
	}

	public override float ModifyPitch(float pitch) {
		if (currentWordMixer != null) {
			if (voxWords[currentWordIndex].Pitch > 0)
				pitch += (voxWords[currentWordIndex].Pitch - 100) * 0.01f;
		}
		return pitch;
	}

	public override float GetVolumeScale() {
		if (currentWordMixer != null) {
			if (voxWords[currentWordIndex].Volume != 0) {
				float volume = voxWords[currentWordIndex].Volume * 0.01f;
				if (volume < 1.0f)
					return volume;
			}
		}
		return 1.0f;
	}

	public override int SkipSamples(Channel channel, int sampleCount, int outputRate, int outputOffset) {
		Assert(false);
		return 0;
	}

	// return number of samples mixed
	public override int MixDataToDevice(IAudioDevice device, Channel channel, int sampleCount, int outputRate, int outputOffset) {
		if (currentWordMixer == null)
			return 0;

		// save this to compute total output
		int startingOffset = outputOffset;

		while (sampleCount > 0 && currentWordMixer != null) {
			int outputCount = currentWordMixer.MixDataToDevice(device, channel, sampleCount, outputRate, outputOffset);

			outputOffset += outputCount;
			sampleCount -= outputCount;

			if (!currentWordMixer.ShouldContinueMixing()) {
				bool mouth = SND_IsMouth(channel);
				if (mouth)
					SND_ClearMouth(channel);

				// advance to next valid word mixer
				do {
					currentWordIndex++;
					if (currentWordIndex >= numWords) {
						// end of sentence
						currentWordMixer = null;
						break;
					}
					currentWordMixer = wordMixers[currentWordIndex];
				}
				while (currentWordMixer == null);

				if (currentWordMixer != null) {
					newWord = true;

					channel.Sfx = voxWords[currentWordIndex].Sfx;
					if (mouth)
						SND_UpdateMouth(channel);
					if (!IsReadyToMix()) {
						// current word isn't ready, stop mixing
						break;
					}
				}
			}
		}

		return outputOffset - startingOffset;
	}
}
