using SDL;

using Source.Common.Formats.Keyvalues;

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Source.AudioSystem;

//-----------------------------------------------------------------------------
//
// NOTE: This only allows 16-bit, stereo wave out  (!!! FIXME: but SDL supports 7.1, etc, too!)
//
//-----------------------------------------------------------------------------
public unsafe class AudioDeviceSDLAudio : AudioDeviceBase
{
	// 64K is about 1/3 second at 16-bit, stereo, 44100 Hz
	// 44k: UNDONE - need to double buffers now that we're playing back at 44100?
	const int WAV_BUFFERS = 64;
	const int WAV_MASK = WAV_BUFFERS - 1;
	const int WAV_BUFFER_SIZE = 0x0400;

	static AudioDeviceSDLAudio? g_wave = null;

	SDL_AudioStream* stream;
	GCHandle selfHandle;

	int deviceSampleCount;

	int buffersSent;
	int pauseCount;
	int readPos;
	int partialWrite;

	// Memory for the wave data
	byte[]? buffer;
	byte[] callbackBuffer = [];

	//-----------------------------------------------------------------------------
	// Constructor (just lookup SDL entry points, real work happens in this->Init())
	//-----------------------------------------------------------------------------
	public AudioDeviceSDLAudio() {
		stream = null;
	}

	//-----------------------------------------------------------------------------
	// Class factory
	//-----------------------------------------------------------------------------
	public static IAudioDevice? Audio_CreateSDLAudioDevice() {
		g_wave ??= new AudioDeviceSDLAudio();

		if (g_wave != null && !g_wave.Init())
			g_wave = null;

		return g_wave;
	}

	//-----------------------------------------------------------------------------
	// Init, shutdown
	//-----------------------------------------------------------------------------
	public override bool Init() {
		// If we've already got a device open, then return. This allows folks to call
		//	Audio_CreateSDLAudioDevice() multiple times. CloseWaveOut() will free the
		//  device, and set m_devId to 0.
		if (stream != null)
			return true;

		Surround = false;
		SurroundCenter = false;
		Headphone = false;
		buffersSent = 0;
		pauseCount = 0;
		buffer = null;
		readPos = 0;
		partialWrite = 0;
		stream = null;

		OpenWaveOut();

		if (snd_firsttime)
			DevMsg("Wave sound initialized\n");

		return ValidWaveOut();
	}

	public override void Shutdown() {
		CloseWaveOut();
		if (g_wave == this)
			g_wave = null;
	}

	//-----------------------------------------------------------------------------
	// WAV out device
	//-----------------------------------------------------------------------------
	bool ValidWaveOut() => stream != null;

	//-----------------------------------------------------------------------------
	// Opens the windows wave out device
	//-----------------------------------------------------------------------------
	void OpenWaveOut() {
		if (!OperatingSystem.IsWindows()) {
			string appname;
			KeyValues modinfo = new KeyValues("ModInfo");

			if (modinfo.LoadFromFile(filesystem, "gameinfo.txt"))
				appname = new(modinfo.GetString("game"));
			else
				appname = "Source1 Game";

			// Set these environment variables, in case we're using PulseAudio.
			Environment.SetEnvironmentVariable("PULSE_PROP_application.name", appname);
			Environment.SetEnvironmentVariable("PULSE_PROP_media.role", "game");
		}

		// !!! FIXME: specify channel map, etc
		// !!! FIXME: set properties (role, icon, etc).

		if (SDL3.SDL_WasInit(SDL_InitFlags.SDL_INIT_AUDIO) == 0) {
			if (!SDL3.SDL_InitSubSystem(SDL_InitFlags.SDL_INIT_AUDIO)) {
				SDLAUDIO_FAIL("SDL_InitSubSystem(SDL_INIT_AUDIO)");
				return;
			}
		}

		// Open an audio device...
		//  !!! FIXME: let user specify a device?
		// !!! FIXME: we can handle quad, 5.1, 7.1, etc here.
		SDL_AudioSpec desired;
		desired.freq = SOUND_DMA_SPEED;
		desired.format = SDL_AudioFormat.SDL_AUDIO_S16LE;
		desired.channels = 2;

		selfHandle = GCHandle.Alloc(this);
		stream = SDL3.SDL_OpenAudioDeviceStream(SDL3.SDL_AUDIO_DEVICE_DEFAULT_PLAYBACK, &desired, &AudioCallbackEntry, GCHandle.ToIntPtr(selfHandle));

		if (stream == null) {
			SDLAUDIO_FAIL("SDL_OpenAudioDevice()");
			return;
		}

		// We're now ready to feed audio data to SDL!
		AllocateOutputBuffers();
		SDL3.SDL_ResumeAudioStreamDevice(stream);
	}

	void SDLAUDIO_FAIL(string fnstr) {
		string? err = SDL3.SDL_GetError();
		Msg($"SDLAUDIO: {fnstr} failed: {(string.IsNullOrEmpty(err) ? "???" : err)}\n");
		CloseWaveOut();
	}

	//-----------------------------------------------------------------------------
	// Closes the windows wave out device
	//-----------------------------------------------------------------------------
	void CloseWaveOut() {
		// none of these SDL_* functions are available to call if this is false.
		if (stream != null) {
			SDL3.SDL_DestroyAudioStream(stream);
			stream = null;
		}
		if (selfHandle.IsAllocated)
			selfHandle.Free();
		SDL3.SDL_QuitSubSystem(SDL_InitFlags.SDL_INIT_AUDIO);
		FreeOutputBuffers();
	}

	//-----------------------------------------------------------------------------
	// Allocate output buffers
	//-----------------------------------------------------------------------------
	void AllocateOutputBuffers() {
		// Allocate and lock memory for the waveform data.
		const int nBufferSize = WAV_BUFFER_SIZE * WAV_BUFFERS;
		buffer = new byte[nBufferSize];
		readPos = 0;
		partialWrite = 0;
		deviceSampleCount = nBufferSize / DeviceSampleBytes();
	}

	//-----------------------------------------------------------------------------
	// Free output buffers
	//-----------------------------------------------------------------------------
	void FreeOutputBuffers() {
		buffer = null;
		callbackBuffer = [];
	}

	//-----------------------------------------------------------------------------
	// Mixing setup
	//-----------------------------------------------------------------------------
	public override int PaintBegin(float mixAheadTime, int soundtime, int paintedtime) {
		//  soundtime - total samples that have been played out to hardware at dmaspeed
		//  paintedtime - total samples that have been mixed at speed
		//  endtime - target for samples in mixahead buffer at speed
		uint endtime = (uint)(soundtime + mixAheadTime * DeviceDmaSpeed());

		int samps = DeviceSampleCount() >> (DeviceChannels() - 1);

		if ((int)(endtime - soundtime) > samps)
			endtime = (uint)(soundtime + samps);

		if (((endtime - paintedtime) & 0x3) != 0) {
			// The difference between endtime and painted time should align on
			// boundaries of 4 samples.  This is important when upsampling from 11khz -> 44khz.
			endtime -= (uint)((endtime - paintedtime) & 0x3);
		}

		return (int)endtime;
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	static void AudioCallbackEntry(nint userdata, SDL_AudioStream* stream, int additional_amount, int total_amount) {
		if (additional_amount <= 0)
			return;

		AudioDeviceSDLAudio? self = (AudioDeviceSDLAudio?)GCHandle.FromIntPtr(userdata).Target;
		if (self == null)
			return;

		if (self.callbackBuffer.Length < additional_amount)
			self.callbackBuffer = new byte[additional_amount];

		self.AudioCallback(self.callbackBuffer, additional_amount);
		fixed (byte* callbackBuffer = self.callbackBuffer)
			SDL3.SDL_PutAudioStreamData(stream, (nint)callbackBuffer, additional_amount);
	}

	void AudioCallback(Span<byte> stream, int len) {
		if (this.stream == null)
			return;  // can this even happen?

		int totalWriteable = len;

		Assert(len <= (WAV_BUFFERS * WAV_BUFFER_SIZE));

		while (len > 0) {
			// spaceAvailable == bytes before we overrun the end of the ring buffer.
			int spaceAvailable = (WAV_BUFFERS * WAV_BUFFER_SIZE) - readPos;
			int writeLen = (len < spaceAvailable) ? len : spaceAvailable;

			if (writeLen > 0) {
				ReadOnlySpan<byte> buf = buffer.AsSpan(readPos, writeLen);
				buf.CopyTo(stream);
				stream = stream[writeLen..];
				len -= writeLen;
				Assert(len >= 0);
			}

			readPos = len != 0 ? 0 : (readPos + writeLen);  // if still bytes to write to stream, we're rolling around the ring buffer.
		}

		// Translate between bytes written and buffers written.
		partialWrite += totalWriteable;
		buffersSent += partialWrite / WAV_BUFFER_SIZE;
		partialWrite %= WAV_BUFFER_SIZE;
	}

	//-----------------------------------------------------------------------------
	// Actually performs the mixing
	//-----------------------------------------------------------------------------
	public override void PaintEnd() {
	}

	public override int GetOutputPosition() {
		return (readPos >> SAMPLE_16BIT_SHIFT) / DeviceChannels();
	}

	//-----------------------------------------------------------------------------
	// Pausing
	//-----------------------------------------------------------------------------
	public override void Pause() {
		pauseCount++;
		if (pauseCount == 1)
			SDL3.SDL_PauseAudioStreamDevice(stream);
	}

	public override void UnPause() {
		if (pauseCount > 0) {
			pauseCount--;
			if (pauseCount == 0)
				SDL3.SDL_ResumeAudioStreamDevice(stream);
		}
	}

	public override bool IsActive() {
		return pauseCount == 0;
	}

	public override float MixDryVolume() {
		return 0;
	}

	public override bool Should3DMix() {
		return false;
	}

	public override void ClearBuffer() {
		int clear;

		if (buffer == null)
			return;

		clear = 0;

		buffer.AsSpan(0, DeviceSampleCount() * DeviceSampleBytes()).Fill((byte)clear);
	}

	public override void MixBegin(int sampleCount) {
		MIX_ClearAllPaintBuffers(sampleCount, false);
	}

	public override void MixUpsample(int sampleCount, int filtertype) {
		PaintBuffer ppaint = MIX_GetCurrentPaintbufferPtr();
		int ifilter = ppaint.IFilter;

		Assert(ifilter < CPAINTFILTERS);

		S_MixBufferUpsample2x(sampleCount, ppaint.Buf, ppaint.GetFltMem(ifilter), CPAINTFILTERMEM, filtertype);

		ppaint.IFilter++;
	}

	public override void Mix8Mono(Channel channel, ReadOnlySpan<byte> data, int outputOffset, int inputOffset, fixedint rateScaleFix, int outCount, int timecompress) {
		Span<int> volume = stackalloc int[CCHANVOLUMES];
		PaintBuffer ppaint = MIX_GetCurrentPaintbufferPtr();

		if (!MIX_ScaleChannelVolume(ppaint, channel, volume, 1))
			return;

		Mix8MonoWavtype(channel, ppaint.Buf.AsSpan(outputOffset), volume, data, inputOffset, rateScaleFix, outCount);
	}

	public override void Mix8Stereo(Channel channel, ReadOnlySpan<byte> data, int outputOffset, int inputOffset, fixedint rateScaleFix, int outCount, int timecompress) {
		Span<int> volume = stackalloc int[CCHANVOLUMES];
		PaintBuffer ppaint = MIX_GetCurrentPaintbufferPtr();

		if (!MIX_ScaleChannelVolume(ppaint, channel, volume, 2))
			return;

		Mix8StereoWavtype(channel, ppaint.Buf.AsSpan(outputOffset), volume, data, inputOffset, rateScaleFix, outCount);
	}

	public override void Mix16Mono(Channel channel, ReadOnlySpan<short> data, int outputOffset, int inputOffset, fixedint rateScaleFix, int outCount, int timecompress) {
		Span<int> volume = stackalloc int[CCHANVOLUMES];
		PaintBuffer ppaint = MIX_GetCurrentPaintbufferPtr();

		if (!MIX_ScaleChannelVolume(ppaint, channel, volume, 1))
			return;

		Mix16MonoWavtype(channel, ppaint.Buf.AsSpan(outputOffset), volume, data, inputOffset, rateScaleFix, outCount);
	}

	public override void Mix16Stereo(Channel channel, ReadOnlySpan<short> data, int outputOffset, int inputOffset, fixedint rateScaleFix, int outCount, int timecompress) {
		Span<int> volume = stackalloc int[CCHANVOLUMES];
		PaintBuffer ppaint = MIX_GetCurrentPaintbufferPtr();

		if (!MIX_ScaleChannelVolume(ppaint, channel, volume, 2))
			return;

		Mix16StereoWavtype(channel, ppaint.Buf.AsSpan(outputOffset), volume, data, inputOffset, rateScaleFix, outCount);
	}

	public override void ChannelReset(int entnum, int channelIndex, float distanceMod) {
	}

	public override void TransferSamples(int end) {
		int lpaintedtime = g_paintedtime;
		int endtime = end;

		// resumes playback...

		if (buffer != null)
			S_TransferStereo16(MemoryMarshal.Cast<byte, short>(buffer.AsSpan()), PAINTBUFFER, lpaintedtime, endtime);
	}

	public override void StopAllSounds() {
	}

	public override void ApplyDSPEffects(int idsp, PortableSamplePair[] pbuffront, PortableSamplePair[]? pbufrear, PortableSamplePair[]? pbufcenter, int samplecount) {
		DSP_Process(idsp, pbuffront, pbufrear, pbufcenter, samplecount);
	}

	public override ReadOnlySpan<char> DeviceName() => "SDL";
	public override int DeviceChannels() => 2;
	public override int DeviceSampleBits() => 16;
	public override int DeviceSampleBytes() => 2;
	public override int DeviceDmaSpeed() => SOUND_DMA_SPEED;
	public override int DeviceSampleCount() => deviceSampleCount;
}
