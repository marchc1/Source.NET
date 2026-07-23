using Source.Common.Audio;
using Source.Common.Commands;

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Source.Engine;

public class VoiceSfx : SfxTable
{

}

[EngineComponent]
public static class VoiceSE
{
	[Dependency] public static Sound Sound { get; set; } = null!;

	readonly static ConVar voice_overdrive = new("voice_overdrive", "2");
	readonly static ConVar voice_overdrivefadetime = new("voice_overdrivefadetime", "0.4"); // How long it takes to fade in and out of the voice overdrive.

	static float VoiceOverdriveDuration = 0;
	static bool VoiceOverdriveOn = false;

	static int SND_VoiceOverdriveInt = 256;

	static readonly VoiceSfx[] VoiceSfx = new VoiceSfx[VOICE_NUM_CHANNELS];

	static VoiceSE() {
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

	internal static void Idle(double frametime) {

	}

	internal static void InitMouth(int entity) {

	}

	internal static long StartChannel(int channel, int entity, bool proximity, int viewEntityIndex) {
		Assert(channel >= 0 && channel < VOICE_NUM_CHANNELS);

		// Start the sound.
		SfxTable sfx = VoiceSfx[channel];
		sfx.Source = null;
		Vector3 vOrigin = new(0, 0, 0);

		StartSoundParams parms = new();
		parms.StaticSound = false;
		parms.EntChannel = (SoundEntityChannel)((int)SoundEntityChannel.VoiceBase + channel);
		parms.Sfx = sfx;
		parms.Origin = vOrigin;
		parms.Volume = 1.0f;
		parms.Flags = 0;
		parms.Pitch = PITCH_NORM;


		if (proximity == true) {
			parms.UpdatePositions = true;
			parms.SoundLevel = SoundLevel.LvlTalking;
			parms.SoundSource = entity;
		}
		else {
			parms.SoundLevel = SoundLevel.LvlIdle;
			parms.SoundSource = viewEntityIndex;
		}


		return Sound.StartSound(parms);
	}
}
