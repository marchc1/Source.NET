global using static Source.AudioSystem.VoiceSoundEngineInterface;

using Source.Common;
using Source.Common.Audio;
using Source.Common.Commands;

using System.Buffers.Binary;
using System.Numerics;

using static Source.Common.Formats.RiffConstants;

namespace Source.AudioSystem;

// ------------------------------------------------------------------------- //
// CAudioSourceVoice.
// This feeds the data from an incoming voice channel (a guy on the server
// who is speaking) into the sound engine.
// ------------------------------------------------------------------------- //

public class AudioSourceVoice : AudioSourceWave
{
	public override AudioSourceType GetAudioSourceType() {
		return AudioSourceType.AUDIO_SOURCE_VOICE;
	}
	public override void GetCacheData(AudioSourceCachedInfo info) {
		Assert(false);
	}

	// Sample size is in bytes.  It will not be accurate for compressed audio.  This is a best estimate.
	// The compressed audio mixers understand this, but in general do not assume that SampleSize() * SampleCount() = filesize
	// or even that SampleSize() is 100% accurate due to compression.

	// Total number of samples in this source.  NOTE: Some sources are infinite (mic input), they should return
	// a count equal to one second of audio at their current rate.

	public override bool IsVoiceSource() => true;

	public override bool IsLooped() => false;
	public override bool IsStreaming() => true;
	public override bool IsStereoWav() => false;
	public override AudioSourceCacheStatus GetCacheStatus() => AudioSourceCacheStatus.AUDIO_IS_LOADED;
	public override void CacheLoad() { }
	public override void CacheUnload() { }
	public override Sentence? GetSentence() => null;

	public override int ZeroCrossingBefore(int sample) => sample;
	public override int ZeroCrossingAfter(int sample) => sample;

	public override void Prefetch() { }

	// Nothing, not a cache object...
	public override void CheckAudioSourceCache() { }

	class WaveDataVoice(AudioSourceWave source) : IWaveData
	{
		readonly AudioSourceWave m_source = source;    // pointer to source

		public void Dispose() { }

		public AudioSourceBase Source() {
			return m_source;
		}

		// this file is in memory, simply pass along the data request to the source
		public int ReadSourceData(out ReadOnlySpan<byte> pData, int sampleIndex, int sampleCount, Span<byte> copyBuf) {
			return m_source.GetOutputData(out pData, sampleIndex, sampleCount, copyBuf);
		}

		public bool IsReadyToMix() {
			return true;
		}
	}

	// Which entity's voice this is for.
	readonly int m_iChannel;

	// How many mixers are referencing us.
	int m_refCount;

	public AudioSourceVoice(SfxTable pSfx, int iChannel) : base(pSfx) {
		m_iChannel = iChannel;
		m_refCount = 0;

		Span<byte> tmp = stackalloc byte[WAVEFORMATEX_SIZE];
		BinaryPrimitives.WriteInt16LittleEndian(tmp, (short)WAVE_FORMAT_PCM);       // wFormatTag
		BinaryPrimitives.WriteInt16LittleEndian(tmp[2..], 1);                       // nChannels
		BinaryPrimitives.WriteInt32LittleEndian(tmp[4..], soundServices.Voice_SamplesPerSec());
		BinaryPrimitives.WriteInt32LittleEndian(tmp[8..], soundServices.Voice_AvgBytesPerSec());
		BinaryPrimitives.WriteInt16LittleEndian(tmp[12..], 2);                      // nBlockAlign
		BinaryPrimitives.WriteInt16LittleEndian(tmp[14..], 16);                     // wBitsPerSample
		BinaryPrimitives.WriteInt16LittleEndian(tmp[16..], WAVEFORMATEX_SIZE);      // cbSize
		Init(tmp, WAVEFORMATEX_SIZE);
		sampleCount = soundServices.Voice_SamplesPerSec();
	}

	const int WAVEFORMATEX_SIZE = 18;

	bool disposed;
	public override void Dispose() {
		if (disposed)
			return;
		disposed = true;
		soundServices.Voice_OnAudioSourceShutdown(m_iChannel);
		base.Dispose();
	}

	public override AudioMixer? CreateMixer(int initialStreamPosition = 0) {
		WaveDataVoice? pVoice = new WaveDataVoice(this);
		if (pVoice == null)
			return null;

		AudioMixer? pMixer = CreateWaveMixer(pVoice, WAVE_FORMAT_PCM, 1, BYTES_PER_SAMPLE * 8, 0);
		if (pMixer == null) {
			pVoice.Dispose();
			return null;
		}

		return pMixer;
	}

	public override int GetOutputData(out ReadOnlySpan<byte> pData, int samplePosition, int sampleCount, Span<byte> copyBuf) {
		int nSamplesGotten = soundServices.Voice_GetOutputData(
			m_iChannel,
			copyBuf,
			AUDIOSOURCE_COPYBUF_SIZE,
			samplePosition,
			sampleCount);

		// If there weren't enough bytes in the received data channel, pad it with zeros.
		if (nSamplesGotten < sampleCount) {
			copyBuf.Slice(nSamplesGotten * BYTES_PER_SAMPLE, (sampleCount - nSamplesGotten) * BYTES_PER_SAMPLE).Clear();
			nSamplesGotten = sampleCount;
		}

		pData = copyBuf;
		return nSamplesGotten;
	}

	public override int SampleRate() {
		return soundServices.Voice_SamplesPerSec();
	}

	public override int SampleSize() {
		return BYTES_PER_SAMPLE;
	}

	public override int SampleCount() {
		return soundServices.Voice_SamplesPerSec();
	}

	public override void ReferenceAdd(AudioMixer pMixer) {
		m_refCount++;
	}

	public override void ReferenceRemove(AudioMixer pMixer) {
		m_refCount--;
		if (m_refCount <= 0)
			Dispose();
	}

	public override bool CanDelete() {
		return m_refCount == 0;
	}
}

// ----------------------------------------------------------------------------- //
// Globals.
// ----------------------------------------------------------------------------- //

public class VoiceSfx : SfxTable
{
	public override ReadOnlySpan<char> GetName() {
		return "?VoiceSfx";
	}
}

public static class VoiceSoundEngineInterface
{
	public const int BYTES_PER_SAMPLE = 2;

	static readonly VoiceSfx[] g_CVoiceSfx = CreateVoiceSfx();

	static VoiceSfx[] CreateVoiceSfx() {
		VoiceSfx[] sfx = new VoiceSfx[VOICE_NUM_CHANNELS];
		for (int i = 0; i < sfx.Length; i++)
			sfx[i] = new();
		return sfx;
	}

	static float g_VoiceOverdriveDuration = 0;
	static bool g_bVoiceOverdriveOn = false;

	// When voice is on, all other sounds are decreased by this factor.
	public static readonly ConVar voice_overdrive = new("voice_overdrive", "2");
	public static readonly ConVar voice_overdrivefadetime = new("voice_overdrivefadetime", "0.4"); // How long it takes to fade in and out of the voice overdrive.

	// The sound engine uses this to lower all sound volumes.
	// All non-voice sounds are multiplied by this and divided by 256.
	public static int g_SND_VoiceOverdriveInt = 256;

	// ----------------------------------------------------------------------------- //
	// Interface implementation.
	// ----------------------------------------------------------------------------- //

	public static bool VoiceSE_Init() {
		if (!snd_initialized)
			return false;

		g_SND_VoiceOverdriveInt = 256;
		return true;
	}

	public static void VoiceSE_Term() {
		// Disable voice ducking.
		g_SND_VoiceOverdriveInt = 256;
	}


	public static void VoiceSE_Idle(float frametime) {
		g_SND_VoiceOverdriveInt = 256;

		if (g_bVoiceOverdriveOn)
			g_VoiceOverdriveDuration = Math.Min(g_VoiceOverdriveDuration + frametime, voice_overdrivefadetime.GetFloat());
		else {
			if (g_VoiceOverdriveDuration == 0)
				return;

			g_VoiceOverdriveDuration = Math.Max(g_VoiceOverdriveDuration - frametime, 0.0f);
		}

		float percent = g_VoiceOverdriveDuration / voice_overdrivefadetime.GetFloat();
		percent = (float)(-Math.Cos(percent * 3.1415926535) * 0.5 + 0.5);       // Smooth it out..
		float voiceOverdrive = 1 + (voice_overdrive.GetFloat() - 1) * percent;
		g_SND_VoiceOverdriveInt = (int)(256 / voiceOverdrive);
	}


	public static int VoiceSE_StartChannel(
		int iChannel,   //! Which channel to start.
		int iEntity,
		bool bProximity,
		int nViewEntityIndex) {
		Assert(iChannel >= 0 && iChannel < VOICE_NUM_CHANNELS);

		// Start the sound.
		SfxTable sfx = g_CVoiceSfx[iChannel];
		sfx.Source = null;
		Vector3 vOrigin = new(0, 0, 0);

		StartSoundParams parms = new();
		parms.StaticSound = false;
		parms.EntChannel = (SoundEntityChannel)((int)SoundEntityChannel.VoiceBase + iChannel);
		parms.Sfx = sfx;
		parms.Origin = vOrigin;
		parms.Volume = 1.0f;
		parms.Flags = 0;
		parms.Pitch = PITCH_NORM;


		if (bProximity == true) {
			parms.UpdatePositions = true;
			parms.SoundLevel = SoundLevel.LvlTalking;
			parms.SoundSource = iEntity;
		}
		else {
			parms.SoundLevel = SoundLevel.LvlIdle;
			parms.SoundSource = nViewEntityIndex;
		}


		return S_StartSound(ref parms);
	}

	public static void VoiceSE_EndChannel(
		int iChannel,   //! Which channel to stop.
		int iEntity
		) {
		Assert(iChannel >= 0 && iChannel < VOICE_NUM_CHANNELS);

		S_StopSound(iEntity, (int)SoundEntityChannel.VoiceBase + iChannel);

		// Start the sound.
		SfxTable sfx = g_CVoiceSfx[iChannel];
		sfx.Source = null;
	}

	public static void VoiceSE_StartOverdrive() {
		g_bVoiceOverdriveOn = true;
	}

	public static void VoiceSE_EndOverdrive() {
		g_bVoiceOverdriveOn = false;
	}


	public static void VoiceSE_InitMouth(int entnum) {
	}

	public static void VoiceSE_CloseMouth(int entnum) {
	}

	public static void VoiceSE_MoveMouth(int entnum, Span<short> pSamples, int nSamples) {
	}


	public static AudioSource? Voice_SetupAudioSource(int soundsource, int entchannel) {
		int iChannel = entchannel - (int)SoundEntityChannel.VoiceBase;
		if (iChannel >= 0 && iChannel < VOICE_NUM_CHANNELS) {
			SfxTable sfx = g_CVoiceSfx[iChannel];
			return new AudioSourceVoice(sfx, iChannel);
		}
		else
			return null;
	}

	// Only does anything for voice tweak channel so if view entity changes it doesn't fade out to zero volume
	public static void Voice_Spatialize(Channel channel) {
		int soundsource = channel.SoundSource;
		soundServices.Voice_Spatialize(channel.Guid, ref soundsource);
		channel.SoundSource = soundsource;
	}
}
