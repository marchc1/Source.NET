global using static Source.AudioSystem.SndWin;

namespace Source.AudioSystem;

public static class SndWin
{
	public static bool snd_firsttime = true;

	/*
	 * Global variables. Must be visible to window-procedure function
	 *  so it can unlock and free the data block after it has been played.
	 */
	public static IAudioDevice? g_AudioDevice = null;

	/*
	==================
	S_BlockSound
	==================
	*/
	public static void S_BlockSound() {
		if (g_AudioDevice == null)
			return;

		g_AudioDevice.Pause();
	}

	/*
	==================
	S_UnblockSound
	==================
	*/
	public static void S_UnblockSound() {
		if (g_AudioDevice == null)
			return;

		g_AudioDevice.UnPause();
	}

	/*
	==================
	AutoDetectInit

	Try to find a sound device to mix for.
	Returns a CAudioNULLDevice if nothing is found.
	==================
	*/
	public static IAudioDevice AutoDetectInit(bool waveOnly) {
		IAudioDevice? device = null;

		DevMsg("Trying SDL Audio Interface\n");
		device = AudioDeviceSDLAudio.Audio_CreateSDLAudioDevice();

		snd_firsttime = false;

		if (device == null) {
			if (snd_firsttime)
				DevMsg("No sound device initialized\n");

			return Audio_GetNullDevice();
		}

		return device;
	}

	/*
	==============
	SNDDMA_Shutdown

	Reset the sound device for exiting
	===============
	*/
	public static void SNDDMA_Shutdown() {
		if (g_AudioDevice != Audio_GetNullDevice()) {
			g_AudioDevice?.Shutdown();

			// the NULL device is always valid
			g_AudioDevice = Audio_GetNullDevice();
		}
	}
}
