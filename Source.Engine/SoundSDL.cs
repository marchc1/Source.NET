using SDL;

using Source.Common.Audio;

using Steamworks;

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
	byte* Buffer;

	public override ReadOnlySpan<char> DeviceName() => "SDL";
	public override int DeviceChannels() => 2;
	public override int DeviceSampleBits() => 16;
	public override int DeviceSampleBytes() => 2;
	public override int DeviceDmaSpeed() => IAudioDevice.SOUND_DMA_SPEED;
	public override int DeviceSampleCount() => deviceSampleCount;

	SDL_AudioDeviceID devId;
	static void SDLAUDIO_FAIL(ReadOnlySpan<char> fnstr) => Log($"SDLAUDIO: {fnstr} failed: {SDL3.SDL_GetError()}\n");
	public AudioDeviceSDLAudio() : base() {
		
	}
	public override bool IsActive() {
		return ValidWaveOut();
	}
	private void AllocateOutputBuffers() {
		if (Buffer != null)
			FreeOutputBuffers();

		const int bufferSize = WAV_BUFFER_SIZE * WAV_BUFFERS;
		Buffer = (byte*)NativeMemory.Alloc(bufferSize);
		memset(Buffer, 0, bufferSize);
		ReadPos = 0;
		PartialWrite = 0;
		deviceSampleCount = bufferSize / DeviceSampleBytes();
	}

	private void FreeOutputBuffers() {
		if (Buffer == null)
			return;

		NativeMemory.Free(Buffer);
		Buffer = null;
	}


	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	static void CALLBACK(nint userdata, SDL_AudioStream* stream, int additionalAmount, int totalAmount) {
		GCHandle handle = GCHandle.FromIntPtr(userdata);
		AudioDeviceSDLAudio? sdlAudio = (AudioDeviceSDLAudio?)handle.Target;
		if (sdlAudio == null)
			return;
		// This is currently not plugged in because we stopped using SDL_OpenAudioDeviceStream for now
		// (and it might be garbage)
	}

	public override bool Init() {
		if (devId != 0)
			return true;

		Surround = false;
		SurroundCenter = false;
		Headphone = false;
		BuffersSent = 0;
		PauseCount = 0;
		Buffer = null;
		ReadPos = 0;
		PartialWrite = 0;
		devId = 0;

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

		devId = SDL3.SDL_OpenAudioDevice((SDL_AudioDeviceID)0xFFFFFFFFu, &spec);
		if (devId == 0) {
			SDLAUDIO_FAIL("SDL_OpenAudioDevice");
			return;
		}

		AllocateOutputBuffers();
		SDL3.SDL_PauseAudioDevice(devId);
	}

	public void CloseWaveOut() {
		if(devId != 0) {
			SDL3.SDL_CloseAudioDevice(devId);
			devId = 0;
		}
		SDL3.SDL_QuitSubSystem(SDL_InitFlags.SDL_INIT_AUDIO);
		FreeOutputBuffers();
	}

	public bool ValidWaveOut() {
		return devId != 0;
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
