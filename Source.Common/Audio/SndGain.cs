using Source.Common.Commands;
using Source.Common.Mathematics;

using System.Runtime.CompilerServices;

namespace Source.Common.Audio;

[EngineComponent]
public static class SndGain
{
	const float SND_GAIN_COMP_EXP_MAX = 2.5f; 
	const float SND_GAIN_COMP_EXP_MIN = 0.8f;
	const float SND_GAIN_COMP_THRESH = 0.5f;  
	const float SND_DB_MAX = 140.0f;          
	const float SND_DB_MED = 90.0f;           

	public const float SND_GAIN_PLAYER_WEAPON_DB = 2.0f; 

	public static readonly ConVar snd_refdist = new("snd_refdist", "36", FCvar.Cheat);
	public static readonly ConVar snd_refdb = new("snd_refdb", "60", FCvar.Cheat);
	public static readonly ConVar snd_foliage_db_loss = new("snd_foliage_db_loss", "4", FCvar.Cheat);
	public static readonly ConVar snd_gain = new("snd_gain", "1", FCvar.Cheat);
	public static readonly ConVar snd_gain_max = new("snd_gain_max", "1", FCvar.Cheat);
	public static readonly ConVar snd_gain_min = new("snd_gain_min", "0.01", FCvar.Cheat);

	public static readonly ConVar snd_musicvolume = new("snd_musicvolume", "1.0", FCvar.Archive, "Music volume", 0.0, 1.0);
	public static readonly ConVar volume_sfx = new("volume_sfx", "1.0", FCvar.Archive, "Sound effects volume", 0.0, 1.0);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float dB_To_Gain(float dB) => MathF.Pow(10.0f, dB / 20.0f);

	public static float SNDLVL_TO_DIST_MULT(int sndlvl) {
		return sndlvl != 0
			? (MathF.Pow(10.0f, snd_refdb.GetFloat() / 20) / MathF.Pow(10.0f, sndlvl / 20.0f)) / snd_refdist.GetFloat()
			: 0.0f;
	}

	public static int DIST_MULT_TO_SNDLVL(float distMult) {
		return distMult != 0.0f
			? (int)(20 * MathF.Log10(MathF.Pow(10.0f, snd_refdb.GetFloat() / 20) / (distMult * snd_refdist.GetFloat())))
			: 0;
	}

	public static float SND_GetGainFromMult(float gain, float distMult, float dist) {
		float additional_dB_loss = snd_foliage_db_loss.GetFloat() * (dist / 1200);
		float additional_dist_mult = MathF.Pow(10.0f, additional_dB_loss / 20);

		float relative_dist = dist * distMult * additional_dist_mult;

		if (relative_dist > 0.1f)
			gain *= (1 / relative_dist);
		else
			gain *= 10.0f;

		if (gain > SND_GAIN_COMP_THRESH) {
			float snd_gain_comp_power = SND_GAIN_COMP_EXP_MAX;
			int sndlvl = DIST_MULT_TO_SNDLVL(distMult);

			if (sndlvl > SND_DB_MED) {
				snd_gain_comp_power = (float)MathLib.RemapVal(sndlvl, SND_DB_MED, SND_DB_MAX, SND_GAIN_COMP_EXP_MAX, SND_GAIN_COMP_EXP_MIN);
			}

			float Y = -1.0f / (MathF.Pow(SND_GAIN_COMP_THRESH, snd_gain_comp_power) * (SND_GAIN_COMP_THRESH - 1));
			gain = 1.0f - 1.0f / (Y * MathF.Pow(gain, snd_gain_comp_power));
			gain = gain * snd_gain_max.GetFloat();
		}

		if (gain < snd_gain_min.GetFloat()) {
			gain = snd_gain_min.GetFloat() * (2.0f - relative_dist * snd_gain_min.GetFloat());
			if (gain <= 0.0f)
				gain = 0.001f; 
		}

		return gain;
	}

	public static float S_GetGainFromSoundLevel(SoundLevel soundlevel, float dist) {
		float gain = snd_gain.GetFloat();
		float distMult = SNDLVL_TO_DIST_MULT((int)soundlevel);
		if (distMult != 0.0f)
			gain = SND_GetGainFromMult(gain, distMult, dist);
		return gain;
	}
}
