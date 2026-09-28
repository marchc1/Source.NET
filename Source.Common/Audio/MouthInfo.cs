namespace Source.Common.Audio;

public class VoiceData
{
	float elapsed;
	AudioSource? audioSource;
	bool ignorePhonemes;

	public VoiceData() {
		elapsed = 0.0f;
		audioSource = null;
		ignorePhonemes = false;
	}

	public void SetElapsedTime(float t) {
		elapsed = t;
	}

	public float GetElapsedTime() {
		return elapsed;
	}

	public void SetSource(AudioSource? source, bool ignorePhonemes) {
		audioSource = source;
		this.ignorePhonemes = ignorePhonemes;
	}

	public bool ShouldIgnorePhonemes() {
		return ignorePhonemes;
	}

	public AudioSource? GetSource() {
		return audioSource;
	}

	public void CopyFrom(VoiceData other) {
		elapsed = other.elapsed;
		audioSource = other.audioSource;
		ignorePhonemes = other.ignorePhonemes;
	}
}

public class MouthInfo
{
	public const int UNKNOWN_VOICE_SOURCE = -1;

	// 0 = mouth closed, 255 = mouth agape
	public byte MouthOpen;
	// counter for running average
	public byte SndCount;
	// running average
	public int SndAvg;

	const int MAX_VOICE_DATA = 4;

	short voiceSources;
	short needsEnvelope;
	readonly VoiceData[] voiceSourceData = [new(), new(), new(), new()];

	public MouthInfo() {
		voiceSources = 0;
		needsEnvelope = 0;
	}

	public bool NeedsEnvelope() => needsEnvelope != 0;
	public void ActivateEnvelope() => needsEnvelope = 1;

	public bool IsActive() {
		return GetNumVoiceSources() > 0;
	}

	public int GetNumVoiceSources() {
		return voiceSources;
	}

	public VoiceData? GetVoiceSource(int number) {
		if (number < 0 || number >= voiceSources)
			return null;

		return voiceSourceData[number];
	}

	public void ClearVoiceSources() {
		voiceSources = 0;
	}

	public int GetIndexForSource(AudioSource? source) {
		for (int i = 0; i < voiceSources; i++) {
			VoiceData v = voiceSourceData[i];
			if (v == null)
				continue;

			if (v.GetSource() == source)
				return i;
		}

		return UNKNOWN_VOICE_SOURCE;
	}

	public bool IsSourceReferenced(AudioSource? source) {
		if (GetIndexForSource(source) != UNKNOWN_VOICE_SOURCE)
			return true;

		return false;
	}

	public void RemoveSource(AudioSource? source) {
		int idx = GetIndexForSource(source);
		if (idx == UNKNOWN_VOICE_SOURCE)
			return;

		RemoveSourceByIndex(idx);
	}

	public void RemoveSourceByIndex(int index) {
		if (index < 0 || index >= voiceSources)
			return;

		voiceSourceData[index].CopyFrom(voiceSourceData[--voiceSources]);
	}

	public VoiceData? AddSource(AudioSource? source, bool ignorePhonemes) {
		int idx = GetIndexForSource(source);
		if (idx == UNKNOWN_VOICE_SOURCE) {
			if (voiceSources < MAX_VOICE_DATA)
				idx = voiceSources++;
			else {
				// No room!
				return null;
			}
		}

		VoiceData data = voiceSourceData[idx];
		data.SetSource(source, ignorePhonemes);
		data.SetElapsedTime(0.0f);
		return data;
	}
}
