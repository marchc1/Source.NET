using Source.Common.Audio;

using SDL;

namespace Source.AudioSystem;

// ------------------------------------------------------------------------------
// VoiceRecord_SDL
// ------------------------------------------------------------------------------

public unsafe class VoiceRecord_SDL : IVoiceRecord
{
	SDL_AudioStream* m_Device;

	int m_nSampleRate;

	public VoiceRecord_SDL() {
		m_nSampleRate = 0;
		m_Device = null;
		ClearInterfaces();
	}

	~VoiceRecord_SDL() {
		ReleaseInterfaces();
	}

	public void Release() {
		ReleaseInterfaces();
		GC.SuppressFinalize(this);
	}

	public bool RecordStart() {

		// Re-initialize the capture buffer if neccesary (should always be)
		if (m_Device == null)
			InitalizeInterfaces();

		if (m_Device == null)
			return false;

		return SDL3.SDL_ResumeAudioStreamDevice(m_Device);
	}


	public void RecordStop() {
		// Stop capturing.
		if (m_Device != null)
			SDL3.SDL_PauseAudioStreamDevice(m_Device);

		// Release the capture buffer interface and any other resources that are no
		// longer needed
		ReleaseInterfaces();
	}

	bool InitalizeInterfaces() {
		if (SDL3.SDL_WasInit(SDL_InitFlags.SDL_INIT_AUDIO) == 0) {
			if (!SDL3.SDL_InitSubSystem(SDL_InitFlags.SDL_INIT_AUDIO))
				return false;
		}

		SDL_AudioSpec desired;
		desired.freq = m_nSampleRate;
		desired.format = SDL_AudioFormat.SDL_AUDIO_S16LE;
		desired.channels = 1;

		m_Device = SDL3.SDL_OpenAudioDeviceStream(SDL3.SDL_AUDIO_DEVICE_DEFAULT_RECORDING, &desired, null, 0);
		return m_Device != null;
	}

	// Initialize. The format of the data we expect from the provider is
	// 8-bit signed mono at the specified sample rate.
	public bool Init(int sampleRate) {
		m_nSampleRate = sampleRate;

		ReleaseInterfaces();

		return true;
	}


	void ReleaseInterfaces() {
		if (m_Device != null)
			SDL3.SDL_DestroyAudioStream(m_Device);
		ClearInterfaces();
	}


	void ClearInterfaces() {
		m_Device = null;
	}


	public void Idle() {
	}


	// Get the most recent N samples.
	public int GetRecordedData(Span<short> pOut) {
		if (m_Device == null)
			return 0;

		int nSamples = pOut.Length;
		int frameCount = SDL3.SDL_GetAudioStreamAvailable(m_Device) / sizeof(short);
		if (frameCount > 0) {
			frameCount = Math.Min(nSamples, frameCount);
			int bytes;
			fixed (short* pOutPtr = pOut)
				bytes = SDL3.SDL_GetAudioStreamData(m_Device, (IntPtr)pOutPtr, frameCount * sizeof(short));
			if (bytes < 0)
				return 0;
			return bytes / sizeof(short);
		}
		return 0;
	}

	public static IVoiceRecord? CreateVoiceRecord_SDL(int sampleRate) {
		VoiceRecord_SDL pRecord = new VoiceRecord_SDL();
		if (pRecord.Init(sampleRate))
			return pRecord;
		else {
			pRecord.Release();

			return null;
		}
	}
}
