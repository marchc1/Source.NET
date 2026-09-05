using Source.Common.Audio;

using static Source.Common.Audio.SndGain;

namespace Source.AudioSystem;

public static class ChannelGain
{
	public static float SND_GetGain(ref Channel ch, bool fplayersound, bool fmusicsound, bool flooping, float dist, bool bAttenuated) {
		float gain = snd_gain.GetFloat();

		gain *= fmusicsound ? snd_musicvolume.GetFloat() : volume_sfx.GetFloat();

		if (ch.DistMult != 0.0f)
			gain = SND_GetGainFromMult(gain, ch.DistMult, dist);

		if (fplayersound) {
			if (ch.EntChannel == (int)SoundEntityChannel.Weapon)
				gain *= dB_To_Gain(SND_GAIN_PLAYER_WEAPON_DB);
		}

		gain *= SND_GetGainObscured(ref ch, fplayersound, flooping, bAttenuated);

		return gain;
	}

	// SND_GetGainObscured / SND_ChannelOkToTrace: todo. We probably need an IEngineAPI binding for this (?) (maybe an entirely separate interface,
	// akin to what client/servers get?)
	public static float SND_GetGainObscured(ref Channel ch, bool fplayersound, bool flooping, bool bAttenuated) {
		return 1.0f;
	}
}
