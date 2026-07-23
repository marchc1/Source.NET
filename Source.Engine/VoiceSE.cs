using Source.Common.Audio;
using Source.Common.Commands;

using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Source.Engine;

public class VoiceSfx : SfxTable {

}

[EngineComponent]
public static class VoiceSE
{
	[Dependency] public static Sound Sound = null!;

	readonly static ConVar voice_overdrive = new("voice_overdrive", "2");
	readonly static ConVar voice_overdrivefadetime = new("voice_overdrivefadetime", "0.4"); // How long it takes to fade in and out of the voice overdrive.

	static float VoiceOverdriveDuration = 0;
	static bool VoiceOverdriveOn = false;

	static int SND_VoiceOverdriveInt = 256;

	static readonly VoiceSfx[] VoiceSfx = new VoiceSfx[VOICE_NUM_CHANNELS];

	static VoiceSE(){
		VoiceSfx.Initialize();
	}

	internal static bool Init() {
		if (!Sound.Initialized)
			return false;

		SND_VoiceOverdriveInt = 256;
		return true;
	}

	internal static void Term() {

	}

	internal static void CloseMouth(int ent) {

	}

	internal static void EndChannel(int idx, int ent) {
		Sound.StopSound(ent, (int)SoundEntityChannel.VoiceBase + idx);

		SfxTable sfx = VoiceSfx[idx];
		sfx.Source = null;
	}

	internal static void StartOverdrive() {
		VoiceOverdriveOn = true;
	}

	internal static void EndOverdrive() {
		VoiceOverdriveOn = false;
	}
}
