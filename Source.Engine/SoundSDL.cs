using SDL;

using Source.Common.Audio;

using Steamworks;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


namespace Source.Engine;

public unsafe class AudioDeviceSDLAudio : AudioDeviceBase {
	SDL_AudioDeviceID id;
	public AudioDeviceSDLAudio() : base() {
		SDL_AudioSpec spec = new() {
			channels = 2,
			format = SDL_AudioFormat.SDL_AUDIO_F32LE,
			freq = 44100
		};

		id = SDL3.SDL_OpenAudioDevice((SDL_AudioDeviceID)0xFFFFFFFFu, &spec);
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
