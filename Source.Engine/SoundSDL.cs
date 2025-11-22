using SDL;

using Source.Common;
using Source.Common.Audio;

using Steamworks;

using System.Drawing;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


namespace Source.Engine;

public unsafe class AudioDeviceSDLAudio : AudioDeviceBase {
	const int WAV_BUFFERS = 64;
	const int WAV_MASK = WAV_BUFFERS - 1;
	const int WAV_BUFFER_SIZE = 0x0400;
	int deviceSampleCount;
	int BuffersSent;
	int PauseCount;
	int ReadPos;
	int PartialWrite;
	UnmanagedHeapMemory? Buffer;

	readonly Sound Sound = Singleton<Sound>();

	public override ReadOnlySpan<char> DeviceName() => "SDL";
	public override int DeviceChannels() => 2;
	public override int DeviceSampleBits() => 16;
	public override int DeviceSampleBytes() => 2;
	public override int DeviceDmaSpeed() => IAudioDevice.SOUND_DMA_SPEED;
	public override int DeviceSampleCount() => deviceSampleCount;

	SDL_AudioStream* devId;
	static void SDLAUDIO_FAIL(ReadOnlySpan<char> fnstr) => Log($"SDLAUDIO: {fnstr} failed: {SDL3.SDL_GetError()}\n");
	public AudioDeviceSDLAudio() : base() {
		
	}
	public override bool IsActive() {
		return PauseCount == 0;
	}
	private void AllocateOutputBuffers() {
		if (Buffer != null)
			FreeOutputBuffers();

		const int bufferSize = WAV_BUFFER_SIZE * WAV_BUFFERS;
		Buffer = new UnmanagedHeapMemory(bufferSize);
		Buffer.Memset(0, bufferSize);
		ReadPos = 0;
		PartialWrite = 0;
		deviceSampleCount = bufferSize / DeviceSampleBytes();
	}

	private void FreeOutputBuffers() {
		if (Buffer == null)
			return;

		Buffer.Dispose();
		Buffer = null;
	}


	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	static void AudioCallbackEntry(nint userdata, SDL_AudioStream* stream, int additionalAmount, int totalAmount) {
		GCHandle handle = GCHandle.FromIntPtr(userdata);
		AudioDeviceSDLAudio? sdlAudio = (AudioDeviceSDLAudio?)handle.Target;
		if (sdlAudio == null)
			return;
		byte* data = stackalloc byte[additionalAmount];
		sdlAudio.AudioCallback(new(data, additionalAmount));
		SDL3.SDL_PutAudioStreamData(stream, (nint)data, additionalAmount);
	}

	private void AudioCallback(Span<byte> stream) {
		if(devId == null) {
			Msg("SDLAUDIO: uhoh, no audio device!\n");
			return;
		}

		int len = stream.Length;
		int totalWriteable = len;
		Assert(len <= (WAV_BUFFERS * WAV_BUFFER_SIZE));

		while (len > 0) {
			// spaceAvailable == bytes before we overrun the end of the ring buffer.
			int spaceAvailable = ((WAV_BUFFERS * WAV_BUFFER_SIZE) - ReadPos);
			int writeLen = (len < spaceAvailable) ? len : spaceAvailable;

			if (writeLen > 0) {
				Span<byte> buf = Buffer!.ToSpan()[ReadPos..];
				buf[..writeLen].CopyTo(stream);
				stream = stream[writeLen..];
				len -= writeLen;
				Assert(len >= 0);
			}

			ReadPos = len != 0 ? 0 : (ReadPos + writeLen); 
		}

		PartialWrite += totalWriteable;
		BuffersSent += PartialWrite / WAV_BUFFER_SIZE;
		PartialWrite %= WAV_BUFFER_SIZE;
	}

	public override bool Init() {
		if (devId != null)
			return true;

		Surround = false;
		SurroundCenter = false;
		Headphone = false;
		BuffersSent = 0;
		PauseCount = 0;
		Buffer = null;
		ReadPos = 0;
		PartialWrite = 0;
		devId = null;

		OpenWaveOut();

		return ValidWaveOut();
	}

	public void OpenWaveOut() {
		if (SDL3.SDL_WasInit(SDL_InitFlags.SDL_INIT_AUDIO) == 0) {
			if (!SDL3.SDL_InitSubSystem(SDL_InitFlags.SDL_INIT_AUDIO)) {
				SDLAUDIO_FAIL("SDL_InitSubSystem(SDL_INIT_AUDIO)");
				return;
			}
		}

		Log($"SDLAUDIO: Using SDL audio target '{SDL3.SDL_GetCurrentAudioDriver()}'\n");

		SDL_AudioSpec spec = new() {
			channels = 2,
			format = SDL_AudioFormat.SDL_AUDIO_F32LE,
			freq = IAudioDevice.SOUND_DMA_SPEED
		};

		// TODO: Is this a good way to pass the audio reference as userdata?
		devId = SDL3.SDL_OpenAudioDeviceStream(SDL3.SDL_AUDIO_DEVICE_DEFAULT_PLAYBACK, &spec, &AudioCallbackEntry, GCHandle.ToIntPtr(GCHandle.Alloc(this, GCHandleType.Weak)));
		if (devId == null) {
			SDLAUDIO_FAIL("SDL_OpenAudioDeviceStream");
			return;
		}

		AllocateOutputBuffers();
		SDL3.SDL_PauseAudioStreamDevice(devId);
	}

	public void CloseWaveOut() {
		if(devId != null) {
			var id = SDL3.SDL_GetAudioStreamDevice(devId);
			SDL3.SDL_DestroyAudioStream(devId);
			devId = null;
		}
		SDL3.SDL_QuitSubSystem(SDL_InitFlags.SDL_INIT_AUDIO);
		FreeOutputBuffers();
	}

	public bool ValidWaveOut() {
		return devId != null;
	}

	public override void Pause() {
		PauseCount++;
		if(PauseCount == 1) {
			SDL3.SDL_PauseAudioStreamDevice(devId);
		}
	}

	public override float MixDryVolume() {
		return 0;
	}

	public override bool Should3DMix() {
		return false;
	}

	public override void UnPause() {
		if(PauseCount > 0) {
			PauseCount--;
			if(PauseCount == 0) {
				SDL3.SDL_ResumeAudioStreamDevice(devId);
			}
		}
	}

	public override int PaintBegin(double mixAheadTime, int soundtime, int paintedtime) {
		uint endtime = (uint)(soundtime + mixAheadTime * DeviceDmaSpeed());
		int samps = DeviceSampleCount() >> (DeviceChannels() - 1);

		if ((int)(endtime - soundtime) > samps)
			endtime = (uint)(soundtime + samps);

		if (((endtime - paintedtime) & 0x3) != 0) 
			endtime -= (uint)((endtime - paintedtime) & 0x3);

		return (int)endtime;
	}
	public override int GetOutputPosition() {
		return ReadPos >> IAudioDevice.SAMPLE_16BIT_SHIFT;
	}
	public override void ClearBuffer() {
		int clear;
		if (Buffer == null)
			return;
		clear = 0;
		Buffer.Memset(clear, DeviceSampleCount() * DeviceSampleBytes());
	}

	public override void MixBegin(int sampleCount) {
		Sound.MIX_ClearAllPaintBuffers(sampleCount, false);
	}

	public override void MixUpsample(int sampleCount, int filtertype) {
		
	}

	public override void Mix8Mono(AudioChannel channel, Span<byte> data, int outputOffset, int inputOffset, uint rateScaleFix, int outCount, int timecompress) {

	}

	public override void Mix8Stereo(AudioChannel channel, Span<byte> data, int outputOffset, int inputOffset, uint rateScaleFix, int outCount, int timecompress) {

	}

	public override void Mix16Mono(AudioChannel channel, Span<short> data, int outputOffset, int inputOffset, uint rateScaleFix, int outCount, int timecompress) {

	}

	public override void Mix16Stereo(AudioChannel channel, Span<short> data, int outputOffset, int inputOffset, uint rateScaleFix, int outCount, int timecompress) {
		
	}

	public override void ChannelReset(int entnum, int channelIndex, float distanceMod) {
		
	}

	public override void TransferSamples(int end) {
		
	}

	public override void SpatializeChannel(Span<int> volume, int master_vol, in Vector3 sourceDir, float gain, float mono) {
		
	}

	public override void StopAllSounds() {
		
	}

	public override void ApplyDSPEffects(int idsp, Span<PortableSamplePair> bufFront, Span<PortableSamplePair> bufRear, Span<PortableSamplePair> bufCenter, int samplecount) {
		
	}

	public override void PaintEnd() { 
	
	}
}

public static partial class Audio
{
	static IAudioDevice? _sdl_wave;
	public static IAudioDevice? CreateSDLAudioDevice() {
		_sdl_wave ??= new AudioDeviceSDLAudio();
		
		if (!_sdl_wave.Init()) 
			_sdl_wave = null;

		return _sdl_wave;
	}
}
