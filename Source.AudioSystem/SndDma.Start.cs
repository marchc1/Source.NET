using Source.Common;
using Source.Common.Audio;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Mathematics;

using System.Numerics;

using static Source.Common.Audio.SndGain;

namespace Source.AudioSystem;

public static partial class SndDma
{
	// remap contents of volumes[] arrary if sound originates from player, or is music, and is 100% 'mono'
	// ie: same volume in all channels

	static readonly float[] vol_dist_music_2 = [1.0f, 1.0f];  // FL, FR music volumes
	static readonly float[] vol_dist_player_2 = [1.0f, 1.0f]; // FL, FR player volumes

	//float vol_dist5[]   = {0.29, 0.29, 0.09, 0.09, 0.63};	// FL, FR, RL, RR, FC - 5 channel (mono source) volume distribution
	//float vol_dist5st[] = {0.29, 0.29, 0.09, 0.09, 0.63};	// FL, FR, RL, RR, FC - 5 channel (stereo source) volume distribution

	static readonly float[] vol_dist5_player = [0.30f, 0.30f, 0.09f, 0.09f, 0.59f];  // FL, FR, RL, RR, FC - 5 channel (mono source) volume distribution
	static readonly float[] vol_dist5st_player = [0.30f, 0.30f, 0.09f, 0.09f, 0.59f];    // FL, FR, RL, RR, FC - 5 channel (stereo source) volume distribution

	static readonly float[] vol_dist4_player = [0.50f, 0.50f, 0.15f, 0.15f, 0.00f];  // FL, FR, RL, RR, 0  - 4 channel (mono source) volume distribution
	static readonly float[] vol_dist4st_player = [0.50f, 0.50f, 0.15f, 0.15f, 0.00f];    // FL, FR, RL, RR, 0  - 4 channel (stereo source)volume distribution

	static readonly float[] vol_dist5_music = [0.5f, 0.5f, 0.25f, 0.25f, 0.0f];    // FL, FR, RL, RR, FC - 5 channel distribution
	static readonly float[] vol_dist4_music = [0.5f, 0.5f, 0.25f, 0.25f, 0.0f];    // FL, FR, RL, RR, 0  - 4 channel distribution

	static void RemapPlayerOrMusicVols(Channel ch, Span<int> volumes, bool fplayersound, bool fmusicsound, float mono) {
		if (!fplayersound && !fmusicsound)
			return; // no remapping

		if (ch.Flags.Speaker)
			return; // don't remap speaker sounds rebroadcast on player

		// get total volume

		float vol_total = 0.0f;
		int k;

		for (k = 0; k < CCHANVOLUMES / 2; k++)
			vol_total += (float)volumes[k];

		if (!g_AudioDevice!.IsSurround()) {
			if (mono < 1.0)
				return;

			// remap 2 chan non-spatialized versions of player and music sounds
			// note: this is required to keep volumes same as 4 & 5 ch cases!

			float[] pvol_dist2 = (fplayersound ? vol_dist_player_2 : vol_dist_music_2);

			for (k = 0; k < 2; k++)
				volumes[k] = Math.Clamp((int)(vol_total * pvol_dist2[k]), 0, 255);

			return;
		}

		// surround sound configuration...

		if (fplayersound) // && (ch->bstereowav && ch->wavtype != CHAR_DIRECTIONAL && ch->wavtype != CHAR_DISTVARIANT) )
		{
			// NOTE: player sounds also get n% overall volume boost.

			float[] pvol_dist;

			if (ch.Flags.StereoWav && (ch.WavType == (char)SoundChars.Omni || ch.WavType == (char)SoundChars.SpatialStereo || ch.WavType == 0))
				pvol_dist = (g_AudioDevice.IsSurroundCenter() ? vol_dist5st_player : vol_dist4st_player);
			else
				pvol_dist = (g_AudioDevice.IsSurroundCenter() ? vol_dist5_player : vol_dist4_player);

			for (k = 0; k < 5; k++)
				volumes[k] = Math.Clamp((int)(vol_total * pvol_dist[k]), 0, 255);

			return;
		}

		// Special case for music in surround mode

		if (fmusicsound) {
			float[] pvol_dist;

			pvol_dist = (g_AudioDevice.IsSurroundCenter() ? vol_dist5_music : vol_dist4_music);

			for (k = 0; k < 5; k++)
				volumes[k] = Math.Clamp((int)(vol_total * pvol_dist[k]), 0, 255);

			return;
		}

		return;
	}

	static int s_nSoundGuid = 0;

	static void SND_ActivateChannel(Channel pChannel) {
		pChannel.Clear();
		g_ActiveChannels.Add(pChannel);
		pChannel.Guid = ++s_nSoundGuid;
	}

	/*
	=================
	SND_Spatialize
	=================
	*/
	static void SND_Spatialize(Channel ch) {
		float dist;
		Vector3 source_vec = default;
		Vector3 source_vec_DL = default;
		Vector3 source_vec_DR = default;
		Vector3 source_doppler_left;
		Vector3 source_doppler_right;

		bool fdopplerwav = false;
		bool fplaydopplerwav = false;
		bool fvalidentity;
		float gain;
		float scale = 1.0f;
		bool fplayersound = false;
		bool fmusicsound = false;
		float mono = 0.0f;
		bool bAttenuated = true;
		Span<int> volumes = stackalloc int[CCHANVOLUMES / 2];

		ch.DspFace = 1.0f;              // default facing direction: always facing player
		ch.DspMix = 0;                  // default mix 0% dsp_room fx
		ch.DistMix = 0;             // default 100% left (near) wav

		if (ch.Sfx != null &&
			ch.Sfx.Source != null &&
			ch.Sfx.Source.GetAudioSourceType() == AudioSourceType.AUDIO_SOURCE_VOICE) {
			Voice_Spatialize(ch);
		}

		if (IsSoundSourceLocalPlayer(ch.SoundSource) && !soundServices.InToolMode()) {
			// sounds coming from listener actually come from a short distance directly in front of listener
			// in tool mode however, the view entity is meaningless, since we're viewing from arbitrary locations in space
			fplayersound = true;
		}

		// assume 'dry', playeverwhere sounds are 'music' or 'voiceover'

		if (ch.Flags.Dry && ch.DistMult <= 0) {
			fmusicsound = true;
			fplayersound = false;
		}

		// update channel's position in case ent that made the sound is moving.
		QAngle source_angles = new(0.0f, 0.0f, 0.0f);
		Vector3 entOrigin = ch.Origin;

		bool looping = false;

		AudioSource? pSource = ch.Sfx?.Source;
		if (pSource != null)
			looping = pSource.IsLooped();

		scoped SpatializationInfo si = default;
		si.Info.Set(
			ch.SoundSource,
			(SoundEntityChannel)ch.EntChannel,
			ch.Sfx != null ? ch.Sfx.GetFileNameHandle() : 0,
			ch.Origin,
			ch.Direction,
			ch.MasterVol,
			(SoundLevel)DIST_MULT_TO_SNDLVL(ch.DistMult),
			looping,
			(int)ch.Pitch,
			listener_origin,
			ch.SpeakerEntity);

		si.Type = SpatializationType.InSpatialization;
		si.Origin = ref entOrigin;
		si.Angles = ref source_angles;
		si.Radius = ref System.Runtime.CompilerServices.Unsafe.NullRef<float>();
		float radius = ch.Radius;
		if (ch.SoundSource != 0 && ch.Radius == 0)
			si.Radius = ref radius;

		fvalidentity = soundServices.GetSoundSpatialization(ch.SoundSource, ref si);
		ch.Radius = radius;

		if (ch.Flags.UpdatePositions) {
			MathLib.AngleVectors(source_angles, out ch.Direction);
			ch.Origin = entOrigin;
		}
		else
			MathLib.VectorAngles(ch.Direction, out source_angles);

		if (ch.UserData != 0) {
			soundServices.GetToolSpatialization(ch.UserData, ch.Guid, ref si);
			ch.Radius = radius;
			if (ch.Flags.UpdatePositions) {
				MathLib.AngleVectors(source_angles, out ch.Direction);
				ch.Origin = entOrigin;
			}
		}

#if false
		// !!!UNDONE - above code assumes the ENT hasn't been removed or respawned as another ent!
		// !!!UNDONE - fix this by flagging some entities (ie: glass) as immobile.  Don't spatialize them.
		if ( !fvalidendity)
		{
			// Turn off the sound while the entity doesn't exist or is not in the PVS.
			goto ClearAllVolumes;
		}
#endif // 0


		fdopplerwav = ((ch.WavType == (char)SoundChars.Doppler) && !fplayersound);
		if (fdopplerwav) {
			Vector3 vnearpoint;             // point of closest approach to listener,
											// along sound source forward direction (doppler wavs)

			vnearpoint = ch.Origin;     // default nearest sound approach point

			// calculate point of closest approach for CHAR_DOPPLER wavs, replace source_vec

			fplaydopplerwav = SND_GetClosestPoint(ch, source_angles, ref vnearpoint);

			// if doppler sound was 'shot' away from listener, don't play it

			if (!fplaydopplerwav)
				goto ClearAllVolumes;

			// find location of doppler left & doppler right points

			SND_GetDopplerPoints(ch, source_angles, vnearpoint, out source_doppler_left, out source_doppler_right);

			// source_vec_DL is vector from listener to doppler left point
			// source_vec_DR is vector from listener to doppler right point

			source_vec_DL = source_doppler_left - listener_origin;
			source_vec_DR = source_doppler_right - listener_origin;

			// normalized vectors to left and right doppler locations

			dist = MathLib.VectorNormalize(ref source_vec_DL);
			MathLib.VectorNormalize(ref source_vec_DR);

			// don't play doppler if out of range
			// unless recording in the tool, since we may play back in range
			if (dist > DOPPLER_RANGE_MAX && !soundServices.IsToolRecording())
				goto ClearAllVolumes;
		}
		else {
			// source_vec is vector from listener to sound source

			if (fplayersound) {
				// get 2d forward direction vector, ignoring pitch angle
				Vector3 listener_forward2d;

				ConvertListenerVectorTo2D(out listener_forward2d, in listener_right);

				// player sounds originate from 1' in front of player, 2d

				source_vec = listener_forward2d * 12.0f;
			}
			else
				source_vec = ch.Origin - listener_origin;

			// normalize source_vec and get distance from listener to source

			dist = MathLib.VectorNormalize(ref source_vec);
		}

		// calculate dsp mix based on distance to listener & sound level (linear approximation)

		ch.DspMix = SND_GetDspMix(ch, (int)dist);

		// calculate sound source facing direction for CHAR_DIRECTIONAL wavs

		if (!fplayersound) {
			ch.DspFace = SND_GetFacingDirection(ch, source_angles);

			// calculate mixing parameter for CHAR_DISTVAR wavs

			ch.DistMix = SND_GetDistanceMix(ch, (int)dist);
		}

		// for sounds with a radius, spatialize left/right/front/rear evenly within the radius

		if (ch.Radius > 0 && dist < ch.Radius && !fdopplerwav) {
			float interval = ch.Radius * 0.5F;
			mono = dist - interval;
			if (mono < 0.0)
				mono = 0.0f;
			mono /= interval;

			mono = 1.0F - mono;

			// mono is 0.0 -> 1.0 from radius 100% to radius 50%
		}

		// don't pan sounds with no attenuation
		if (ch.DistMult <= 0 && !fdopplerwav) {
			// sound is centered left/right/front/back

			mono = 1.0f;
			bAttenuated = false;
		}

		if (ch.WavType == (char)SoundChars.Omni) {
			// omni directional sound sources are mono mix, all speakers
			// ie: they only attenuate by distance, not by source direction.

			mono = 1.0f;
			bAttenuated = false;
		}

		// calculate gain based on distance, atmospheric attenuation, interposed objects
		// perform compression as gain approaches 1.0

		gain = SND_GetGain(ch, fplayersound, fmusicsound, looping, dist, bAttenuated);

		// map gain through global mixer by soundtype

		// gain *= SND_GetVolFromSoundtype( ch->soundtype );
		int last_mixgroupid;

		gain *= MXR_GetVolFromMixGroup(ch.MixGroups, out last_mixgroupid);

		// if playing a word, get volume scale of word - scale gain

		scale = VOX_GetChanVol(ch);

		gain *= scale;

		// save spatialized volume and mixgroupid for display later

		ch.LastMixGroupId = last_mixgroupid;

		if (fdopplerwav) {
			// fill out channel volumes for both doppler sound source locations

			// left doppler location

			g_AudioDevice!.SpatializeChannel(volumes, ch.MasterVol, source_vec_DL, gain, mono);

			// load volumes into channel as crossfade targets

			ChannelSetVolTargets(ch, volumes, IFRONT_LEFT, CCHANVOLUMES / 2);

			// right doppler location

			g_AudioDevice.SpatializeChannel(volumes, ch.MasterVol, source_vec_DR, gain, mono);

			// load volumes into channel as crossfade targets

			ChannelSetVolTargets(ch, volumes, IFRONT_LEFTD, CCHANVOLUMES / 2);
		}
		else {
			// fill out channel volumes for single sound source location

			g_AudioDevice!.SpatializeChannel(volumes, ch.MasterVol, source_vec, gain, mono);

			// Special case for stereo sounds originating from player in surround mode
			// and special case for musci: remap volumes directly to channels.

			RemapPlayerOrMusicVols(ch, volumes, fplayersound, fmusicsound, mono);

			// load volumes into channel as crossfade volume targets

			ChannelSetVolTargets(ch, volumes, IFRONT_LEFT, CCHANVOLUMES / 2);
		}


		// prevent left/right/front/rear/center volumes from changing too quickly & producing pops

		ChannelUpdateVolXfade(ch);

		// end of first time spatializing sound

		if (SND_IsInGame() || soundServices.InToolMode())
			ch.Flags.FirstPass = false;

		// calculate total volume for display later
		ch.LastVol = gain * (ch.MasterVol / 255.0F);

		return;

	ClearAllVolumes:

		// Clear all volumes and return.
		// This shuts the sound off permanently.

		ChannelClearVolumes(ch);

		// end of first time spatializing sound

		ch.Flags.FirstPass = false;
	}

	public static readonly ConVar snd_defer_trace = new("snd_defer_trace", "1");
	static void SND_SpatializeFirstFrameNoTrace(Channel pChannel) {
		if (snd_defer_trace.GetBool()) {
			// set up tracing state to be non-obstructed
			pChannel.Flags.FirstPass = false;
			pChannel.Flags.Traced = true;
			pChannel.ObGain = 1.0f;
			pChannel.ObGainInc = 1.0f;
			pChannel.ObGainTarget = 1.0f;
			// now spatialize without tracing
			SND_Spatialize(pChannel);
			// now reset tracing state to firstpass so the trace gets done on next spatialize
			pChannel.ObGain = 0.0f;
			pChannel.ObGainInc = 0.0f;
			pChannel.ObGainTarget = 0.0f;
			pChannel.Flags.FirstPass = true;
			pChannel.Flags.Traced = false;
		}
		else {
			pChannel.ObGain = 0.0f;
			pChannel.ObGainInc = 0.0f;
			pChannel.ObGainTarget = 0.0f;
			pChannel.Flags.FirstPass = true;
			pChannel.Flags.Traced = false;
			SND_Spatialize(pChannel);
		}
	}


	// search through all channels for a channel that matches this
	// soundsource, entchannel and sfx, and perform alteration on channel
	// as indicated by 'flags' parameter. If shut down request and
	// sfx contains a sentence name, shut off the sentence.
	// returns TRUE if sound was altered,
	// returns FALSE if sound was not found (sound is not playing)

	static bool S_AlterChannel(int soundsource, int entchannel, SfxTable sfx, int vol, int pitch, SoundFlags flags) {
		lock (g_SndMutex) {
			int ch_idx;

			ReadOnlySpan<char> name = sfx.GetName();
			if (!name.IsEmpty && TestSoundChar(name, SoundChars.Sentence)) {
				// This is a sentence name.
				// For sentences: assume that the entity is only playing one sentence
				// at a time, so we can just shut off
				// any channel that has ch->isentence >= 0 and matches the
				// soundsource.

				ChannelList list = new();
				g_ActiveChannels.GetActiveChannels(list);
				for (int i = 0; i < list.Count; i++) {
					ch_idx = list.GetChannelIndex(i);
					if (channels[ch_idx].SoundSource == soundsource
						&& channels[ch_idx].EntChannel == entchannel
						&& channels[ch_idx].Sfx != null) {

						if ((flags & SoundFlags.ChangePitch) != 0)
							channels[ch_idx].BasePitch = (short)pitch;

						if ((flags & SoundFlags.ChangeVolume) != 0)
							channels[ch_idx].MasterVol = (short)vol;

						if ((flags & SoundFlags.Stop) != 0)
							S_FreeChannel(channels[ch_idx]);

						return true;
					}
				}
				// channel not found
				return false;

			}

			// regular sound or streaming sound
			ChannelList list2 = new();
			g_ActiveChannels.GetActiveChannels(list2);

			bool bSuccess = false;

			for (int i = 0; i < list2.Count; i++) {
				ch_idx = list2.GetChannelIndex(i);
				if (channels[ch_idx].SoundSource == soundsource &&
					 (((flags & SoundFlags.IgnoreName) != 0) ||
					   (channels[ch_idx].EntChannel == entchannel && channels[ch_idx].Sfx == sfx))) {
					if ((flags & SoundFlags.ChangePitch) != 0)
						channels[ch_idx].BasePitch = (short)pitch;

					if ((flags & SoundFlags.ChangeVolume) != 0)
						channels[ch_idx].MasterVol = (short)vol;

					if ((flags & SoundFlags.Stop) != 0)
						S_FreeChannel(channels[ch_idx]);

					if ((flags & SoundFlags.IgnoreName) == 0)
						return true;
					else
						bSuccess = true;
				}
			}

			return bSuccess;
		}
	}

	// set channel flags during initialization based on
	// source name

	static void S_SetChannelWavtype(Channel target_chan, SfxTable pSfx) {
		// if 1st or 2nd character of name is CHAR_DRYMIX, sound should be mixed dry with no dsp (ie: music)

		if (TestSoundChar(pSfx.GetName(), SoundChars.DryMix))
			target_chan.Flags.Dry = true;
		else
			target_chan.Flags.Dry = false;

		if (TestSoundChar(pSfx.GetName(), SoundChars.FastPitch))
			target_chan.Flags.FastPitch = true;
		else
			target_chan.Flags.FastPitch = false;

		// get sound spatialization encoding

		target_chan.WavType = '\0';

		if (TestSoundChar(pSfx.GetName(), SoundChars.Doppler))
			target_chan.WavType = (char)SoundChars.Doppler;

		if (TestSoundChar(pSfx.GetName(), SoundChars.Directional))
			target_chan.WavType = (char)SoundChars.Directional;

		if (TestSoundChar(pSfx.GetName(), SoundChars.DistVariant))
			target_chan.WavType = (char)SoundChars.DistVariant;

		if (TestSoundChar(pSfx.GetName(), SoundChars.Omni))
			target_chan.WavType = (char)SoundChars.Omni;

		if (TestSoundChar(pSfx.GetName(), SoundChars.SpatialStereo))
			target_chan.WavType = (char)SoundChars.SpatialStereo;
	}


	// Sets bstereowav flag in channel if source is true stere wav
	// sets default wavtype for stereo wavs to CHAR_DISTVARIANT -
	// ie: sound varies with distance (left is close, right is far)
	// Must be called after S_SetChannelWavtype

	static void S_SetChannelStereo(Channel target_chan, AudioSource? pSource) {
		if (pSource == null) {
			target_chan.Flags.StereoWav = false;
			return;
		}

		// returns true only if source data is a stereo wav file.
		// ie: mp3, voice, sentence are all excluded.

		target_chan.Flags.StereoWav = pSource.IsStereoWav();

		// Default stereo wavtype:

		// just player standard stereo wavs on player entity - no override.

		if (IsSoundSourceLocalPlayer(target_chan.SoundSource))
			return;

		// default wavtype for stereo wavs is OMNI - except for drymix or sounds with 0 attenuation

		if (target_chan.Flags.StereoWav && target_chan.WavType == 0 && !target_chan.Flags.Dry && target_chan.DistMult != 0)
			// target_chan->wavtype = CHAR_DISTVARIANT;
			target_chan.WavType = (char)SoundChars.Omni;
	}

	// =======================================================================
	// Channel volume management routines:

	// channel volumes crossfade between values over time
	// to prevent pops due to rapid spatialization changes
	// =======================================================================

	// return true if all volumes and target volumes for channel are less/equal to 'vol'

	public static bool BChannelLowVolume(Channel pch, int vol_min) {
		int max = -1;
		int max_target = -1;
		int vol;
		int vol_target;

		for (int i = 0; i < CCHANVOLUMES; i++) {
			vol = (int)(pch.FVolume[i]);
			vol_target = (int)(pch.FVolumeTarget[i]);

			if (vol > max)
				max = vol;

			if (vol_target > max_target)
				max_target = vol_target;
		}

		return max <= vol_min && max_target <= vol_min;
	}

	// Get the loudest actual volume for a channel (not counting targets).
	public static float ChannelLoudestCurVolume(Channel pch) {
		float loudest = pch.FVolume[0];
		for (int i = 1; i < CCHANVOLUMES; i++)
			loudest = MathF.Max(loudest, pch.FVolume[i]);
		return loudest;
	}

	// clear all volumes, targets, crossfade increments

	public static void ChannelClearVolumes(Channel pch) {
		for (int i = 0; i < CCHANVOLUMES; i++) {
			pch.FVolume[i] = 0.0f;
			pch.FVolumeTarget[i] = 0.0f;
			pch.FVolumeInc[i] = 0.0f;
		}
	}

	// return current volume as integer

	public static int ChannelGetVol(Channel pch, int ivol) {
		Assert(ivol < CCHANVOLUMES);
		return (int)(pch.FVolume[ivol]);
	}

	// return maximum current output volume

	public static int ChannelGetMaxVol(Channel pch) {
		float max = 0.0f;

		for (int i = 0; i < CCHANVOLUMES; i++) {
			if (pch.FVolume[i] > max)
				max = pch.FVolume[i];
		}

		return (int)max;
	}

	// set current volume (clears crossfading - instantaneous value change)

	public static void ChannelSetVol(Channel pch, int ivol, int vol) {
		Assert(ivol < CCHANVOLUMES);

		pch.FVolume[ivol] = (float)(Math.Clamp(vol, 0, 255));

		pch.FVolumeTarget[ivol] = pch.FVolume[ivol];
		pch.FVolumeInc[ivol] = 0.0f;
	}

	// copy current channel volumes into target array, starting at ivol, copying cvol entries

	public static void ChannelCopyVolumes(Channel pch, Span<int> pvolume_dest, int ivol_start, int cvol) {
		Assert(ivol_start < CCHANVOLUMES);
		Assert(ivol_start + cvol <= CCHANVOLUMES);

		for (int i = 0; i < cvol; i++)
			pvolume_dest[i] = (int)(pch.FVolume[i + ivol_start]);
	}

	// volume has hit target, shut off crossfading increment

	static void ChannelStopVolXfade(Channel pch, int ivol) {
		pch.FVolume[ivol] = pch.FVolumeTarget[ivol];
		pch.FVolumeInc[ivol] = 0.0f;
	}

	const float VOL_XFADE_TIME = 0.070F;    // channel volume crossfade time in seconds

	const float VOL_INCR_MAX = 20.0F;   // never change volume by more than +/-N units per frame

	// set volume target and volume increment (for crossfade) for channel & speaker

	static void ChannelSetVolTarget(Channel pch, int ivol, int volume_target) {
		float frametime = (float)soundServices.GetHostFrametime();
		float speed;
		float vol_target = (float)(Math.Clamp(volume_target, 0, 255));
		float vol_current;

		Assert(ivol < CCHANVOLUMES);

		// set volume target

		pch.FVolumeTarget[ivol] = vol_target;

		// current volume

		vol_current = pch.FVolume[ivol];

		// if first time spatializing, set target = volume with no crossfade
		// if current & target volumes are close - don't bother crossfading

		if (pch.Flags.FirstPass || (MathF.Abs(vol_target - vol_current) < 5.0)) {
			// set current volume = target, no increment

			ChannelStopVolXfade(pch, ivol);
			return;
		}

		// get crossfade increment 'speed' (volume change per frame)

		speed = (frametime / VOL_XFADE_TIME) * (vol_target - vol_current);

		// make sure we never increment by more than +/- VOL_INCR_MAX volume units per frame

		speed = Math.Clamp(speed, (float)-VOL_INCR_MAX, (float)VOL_INCR_MAX);

		pch.FVolumeInc[ivol] = speed;
	}

	// set volume targets, using array pvolume as source volumes.
	// set into channel volumes starting at ivol_offset index
	// set cvol volumes

	public static void ChannelSetVolTargets(Channel pch, ReadOnlySpan<int> pvolumes, int ivol_offset, int cvol) {
		int volume_target;

		Assert(ivol_offset + cvol <= CCHANVOLUMES);

		for (int i = 0; i < cvol; i++) {
			volume_target = pvolumes[i];

			ChannelSetVolTarget(pch, ivol_offset + i, volume_target);
		}
	}


	// Call once per frame, per channel:
	// update all volume crossfades, from fvolume -> fvolume_target
	// if current volume reaches target, set increment to 0

	public static void ChannelUpdateVolXfade(Channel pch) {
		float fincr;

		for (int i = 0; i < CCHANVOLUMES; i++) {
			fincr = pch.FVolumeInc[i];

			if (fincr != 0.0) {
				pch.FVolume[i] += fincr;

				// test for hit target

				if (fincr > 0.0) {
					if (pch.FVolume[i] >= pch.FVolumeTarget[i])
						ChannelStopVolXfade(pch, i);
				}
				else {
					if (pch.FVolume[i] <= pch.FVolumeTarget[i])
						ChannelStopVolXfade(pch, i);
				}
			}
		}
	}

	// =======================================================================
	// S_StartDynamicSound
	// =======================================================================
	// Start a sound effect for the given entity on the given channel (ie; voice, weapon etc).
	// Try to grab a channel out of the 8 dynamic spots available.
	// Currently used for looping sounds, streaming sounds, sentences, and regular entity sounds.
	// NOTE: volume is 0.0 - 1.0 and attenuation is 0.0 - 1.0 when passed in.
	// Pitch changes playback pitch of wave by % above or below 100.  Ignored if pitch == 100

	// NOTE: it's not a good idea to play looping sounds through StartDynamicSound, because
	// if the looping sound starts out of range, or is bumped from the buffer by another sound
	// it will never be restarted.  Use StartStaticSound (pass CHAN_STATIC to EMIT_SOUND or
	// SV_StartSound.

	public static int S_StartDynamicSound(ref StartSoundParams parms) {
		Assert(parms.StaticSound == false);

		Channel? target_chan;
		int vol;

		if (g_AudioDevice == null || !g_AudioDevice.IsActive())
			return 0;

		if (parms.Sfx == null)
			return 0;

		// For debugging to see the actual name of the sound...
		string sndname = new(parms.Sfx.GetName());

		// Msg("Start sound %s\n", pSfx->getname() );

		// override the entchannel to CHAN_STREAM if this is a
		// non-voice stream sound.
		if (TestSoundChar(sndname, SoundChars.Stream) && parms.EntChannel != SoundEntityChannel.Voice && parms.EntChannel != SoundEntityChannel.Voice2)
			parms.EntChannel = SoundEntityChannel.Stream;

		vol = (int)(parms.Volume * 255);

		if (vol > 255) {
			DevMsg($"S_StartDynamicSound: {sndname} volume > 255");
			vol = 255;
		}

		lock (g_SndMutex) {
			if ((parms.Flags & (SoundFlags.Stop | SoundFlags.ChangeVolume | SoundFlags.ChangePitch)) != 0) {
				if (S_AlterChannel(parms.SoundSource, (int)parms.EntChannel, parms.Sfx, vol, parms.Pitch, parms.Flags))
					return 0;
				if ((parms.Flags & SoundFlags.Stop) != 0)
					return 0;
				// fall through - if we're not trying to stop the sound,
				// and we didn't find it (it's not playing), go ahead and start it up
			}

			if (parms.Pitch == 0) {
				DevMsg($"Warning: S_StartDynamicSound ({sndname}) Ignored, called with pitch 0\n");
				return 0;
			}

			// pick a channel to play on
			target_chan = SND_PickDynamicChannel(parms.SoundSource, (int)parms.EntChannel, parms.Origin, parms.Sfx, (float)parms.Delay, (parms.Flags & SoundFlags.DoNotOverwriteExistingOnChannel) != 0);
			if (target_chan == null)
				return 0;

			int channelIndex = target_chan.Index;
			g_AudioDevice.ChannelReset(parms.SoundSource, channelIndex, target_chan.DistMult);

			bool bIsSentence = TestSoundChar(sndname, SoundChars.Sentence);

			SND_ActivateChannel(target_chan);
			ChannelClearVolumes(target_chan);

			target_chan.UserData = parms.UserData;
			target_chan.InitialStreamPosition = parms.InitialStreamPosition;

			target_chan.Origin = parms.Origin;
			target_chan.Direction = parms.Direction;

			// never update positions if source entity is 0
			target_chan.Flags.UpdatePositions = parms.UpdatePositions && (parms.SoundSource == 0 ? false : true);

			// reference_dist / (reference_power_level / actual_power_level)
			target_chan.Flags.CompatibilityAttenuation = EngineSoundGlobals.SNDLEVEL_IS_COMPATIBILITY_MODE(parms.SoundLevel);
			if (target_chan.Flags.CompatibilityAttenuation) {
				// Translate soundlevel from its 'encoded' value to a real soundlevel that we can use in the sound system.
				parms.SoundLevel = EngineSoundGlobals.SNDLEVEL_FROM_COMPATIBILITY_MODE(parms.SoundLevel);
			}

			target_chan.DistMult = SNDLVL_TO_DIST_MULT((int)parms.SoundLevel);

			S_SetChannelWavtype(target_chan, parms.Sfx);

			target_chan.MasterVol = (short)vol;
			target_chan.SoundSource = parms.SoundSource;
			target_chan.EntChannel = (int)parms.EntChannel;
			target_chan.BasePitch = (short)parms.Pitch;
			target_chan.Flags.IsSentence = false;
			target_chan.Radius = 0;
			target_chan.Sfx = parms.Sfx;
			target_chan.SpecialDsp = parms.SpecialDSP;
			target_chan.Flags.FromServer = parms.FromServer;
			target_chan.Flags.Speaker = (parms.Flags & SoundFlags.Speaker) != 0;
			target_chan.SpeakerEntity = parms.SpeakerEntity;

			target_chan.Flags.ShouldPause = (parms.Flags & SoundFlags.ShouldPause) != 0;

			// initialize dsp room mixing params
			target_chan.DspMixMin = -1;
			target_chan.DspMixMax = -1;

			AudioSource? pSource = null;

			if (bIsSentence) {
				// this is a sentence
				// link all words and load the first word

				// NOTE: sentence names stored in the cache lookup are
				// prepended with a '!'.  Sentence names stored in the
				// sentence file do not have a leading '!'.
				VOX_LoadSound(target_chan, SkipSoundChars(sndname));
			}
			else {
				// regular or streamed sound fx
				pSource = S_LoadSound(parms.Sfx, target_chan);
				if (pSource != null && !IsValidSampleRate(pSource.SampleRate()))
					Warning($"*** Invalid sample rate ({pSource.SampleRate()}) for sound '{sndname}'.\n");

				if (pSource == null && !parms.Sfx.IsLateLoad)
					Warning($"Failed to load sound \"{sndname}\", file probably missing from disk/repository\n");

			}

			if (target_chan.Mixer == null) {
				// couldn't load the sound's data, or sentence has 0 words (this is not an error)
				S_FreeChannel(target_chan);
				return 0;
			}

			int nSndShowStart = snd_showstart.GetInt();

			// TODO: Support looping sounds through speakers.
			// If the sound is from a speaker, and it's looping, ignore it.
			if (target_chan.Flags.Speaker) {
				if (parms.Sfx.Source != null && parms.Sfx.Source.IsLooped()) {
					if (nSndShowStart > 0 && nSndShowStart < 7 && nSndShowStart != 4)
						DevMsg($"DynamicSound : Speaker ignored looping sound: {sndname}\n");

					S_FreeChannel(target_chan);
					return 0;
				}
			}

			S_SetChannelStereo(target_chan, pSource);

			if (nSndShowStart == 5) {
				snd_showstart.SetValue(6);      // debug: show gain for next spatialize only
				nSndShowStart = 6;
			}

			// get sound type before we spatialize
			MXR_GetMixGroupFromSoundsource(target_chan, parms.SoundSource, parms.SoundLevel);

			// skip the trace on the first spatialization.  This channel may be stolen
			// by another sound played this frame.  Defer the trace to the mix loop
			SND_SpatializeFirstFrameNoTrace(target_chan);

			if (nSndShowStart > 0 && nSndShowStart < 7 && nSndShowStart != 4) {
				Channel pTargetChan = target_chan;

				DevMsg($"DynamicSound {sndname} : src {parms.SoundSource} : channel {(int)parms.EntChannel} : {(int)parms.SoundLevel} dB : vol {parms.Volume:F2} : time {soundServices.GetHostTime():F3}\n");
				if (nSndShowStart == 2 || nSndShowStart == 5)
					DevMsg($"\t dspmix {pTargetChan.DspMix:F2} : distmix {pTargetChan.DistMix:F2} : dspface {pTargetChan.DspFace:F2} : lvol {pTargetChan.FVolume[IFRONT_LEFT]:F2} : cvol {pTargetChan.FVolume[IFRONT_CENTER]:F2} : rvol {pTargetChan.FVolume[IFRONT_RIGHT]:F2} : rlvol {pTargetChan.FVolume[IREAR_LEFT]:F2} : rrvol {pTargetChan.FVolume[IREAR_RIGHT]:F2}\n");
				if (nSndShowStart == 3)
					DevMsg($"\t x: {pTargetChan.Origin.X:F6} y: {pTargetChan.Origin.Y:F6} z: {pTargetChan.Origin.Z:F6}\n");

				if (snd_visualize.GetInt() != 0)
					debugoverlay?.AddTextOverlay(pTargetChan.Origin, 2.0f, sndname);
			}

			// If a client can't hear a sound when they FIRST receive the StartSound message,
			// the client will never be able to hear that sound. This is so that out of
			// range sounds don't fill the playback buffer.  For streaming sounds, we bypass this optimization.

			if (BChannelLowVolume(target_chan, 0) && !soundServices.IsToolRecording()) {
				// Looping sounds don't use this optimization because they should stick around until they're killed.
				// Also bypass for speech (GetSentence)
				if (parms.Sfx.Source == null || (!parms.Sfx.Source.IsLooped() && parms.Sfx.Source.GetSentence() == null)) {
					// if this is long sound, play the whole thing.
					if (!SND_IsLongWave(target_chan)) {
						// DevMsg("S_StartDynamicSound: spatialized to 0 vol & ignored %s", sndname);
						S_FreeChannel(target_chan);
						return 0;       // not audible at all
					}
				}
			}

			// Init client entity mouth movement vars
			target_chan.Flags.IgnorePhonemes = (parms.Flags & SoundFlags.IgnorePhonemes) != 0;
			SND_InitMouth(target_chan);

			// Pre-startup delay.  Compute # of samples over which to mix in zeros from data source before
			//  actually reading first set of samples
			if (parms.Delay != 0.0f) {
				Assert(target_chan.Sfx);
				Assert(target_chan.Sfx!.Source);

				// delay count is computed at the sampling rate of the source because the output rate will
				// match the source rate when the sound is mixed
				int rate = target_chan.Sfx.Source!.SampleRate();
				int delaySamples = (int)(parms.Delay * rate);

				if (parms.Delay > 0) {
					target_chan.Mixer.SetStartupDelaySamples(delaySamples);
					target_chan.Flags.DelayedStart = true;
				}
				else {
					int skipSamples = -delaySamples;
					int totalSamples = target_chan.Sfx.Source.SampleCount();
					if (target_chan.Sfx.Source.IsLooped())
						skipSamples = skipSamples % totalSamples;
					if (skipSamples >= totalSamples) {
						S_FreeChannel(target_chan);
						return 0;
					}
					target_chan.Pitch = target_chan.BasePitch * 0.01f;
					target_chan.Mixer.SkipSamples(target_chan, skipSamples, rate, 0);
					target_chan.ObGainTarget = 1.0f;
					target_chan.ObGain = 1.0f;
					target_chan.ObGainInc = 0.0f;
					target_chan.Flags.FirstPass = false;
					target_chan.Flags.DelayedStart = true;
				}
			}

			soundServices.OnSoundStarted(target_chan.Guid, ref parms, sndname);
			return target_chan.Guid;
		}
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	// Input  : *name -
	// Output : CSfxTable
	//-----------------------------------------------------------------------------
	public static SfxTable S_DummySfx(ReadOnlySpan<char> name) {
		dummySfx.SetName(name);
		return dummySfx;
	}

	/*
	=================
	S_StartStaticSound
	=================
	Start playback of a sound, loaded into the static portion of the channel array.
	Currently, this should be used for looping ambient sounds, looping sounds
	that should not be interrupted until complete, non-creature sentences,
	and one-shot ambient streaming sounds.  Can also play 'regular' sounds one-shot,
	in case designers want to trigger regular game sounds.
	Pitch changes playback pitch of wave by % above or below 100.  Ignored if pitch == 100

	  NOTE: volume is 0.0 - 1.0 and attenuation is 0.0 - 1.0 when passed in.
	*/

	public static int S_StartStaticSound(ref StartSoundParams parms) {
		Assert(parms.StaticSound == true);

		Channel? ch;
		AudioSource? pSource = null;

		if (!g_AudioDevice!.IsActive())
			return 0;

		if (parms.Sfx == null)
			return 0;

		// For debugging to see the actual name of the sound...
		string sndname = new(parms.Sfx.GetName());
		//	Msg("Start static sound %s\n", pSfx->getname() );

		int vol = (int)(parms.Volume * 255);
		if (vol > 255) {
			DevMsg($"S_StartStaticSound: {sndname} volume > 255");
			vol = 255;
		}

		int nSndShowStart = snd_showstart.GetInt();

		if ((parms.Flags & SoundFlags.Stop) != 0 && nSndShowStart > 0)
			DevMsg($"S_StartStaticSound: {sndname} Stopped.\n");

		if ((parms.Flags & SoundFlags.Stop) != 0 || (parms.Flags & SoundFlags.ChangeVolume) != 0 || (parms.Flags & SoundFlags.ChangePitch) != 0) {
			if (S_AlterChannel(parms.SoundSource, (int)parms.EntChannel, parms.Sfx, vol, parms.Pitch, parms.Flags) || (parms.Flags & SoundFlags.Stop) != 0)
				return 0;
		}

		if (parms.Pitch == 0) {
			DevMsg("Warning: S_StartStaticSound Ignored, called with pitch 0\n");
			return 0;
		}

		// First, make sure the sound source entity is even in the PVS.
		float flSoundRadius = 0.0f;

		bool looping = false;

		scoped SpatializationInfo si = default;
		si.Info.Set(
			parms.SoundSource,
			parms.EntChannel,
			parms.Sfx.GetFileNameHandle(),
			parms.Origin,
			parms.Direction,
			vol,
			parms.SoundLevel,
			looping,
			parms.Pitch,
			listener_origin,
			parms.SpeakerEntity);

		si.Type = SpatializationType.InCreation;

		si.Origin = ref System.Runtime.CompilerServices.Unsafe.NullRef<Vector3>();
		si.Angles = ref System.Runtime.CompilerServices.Unsafe.NullRef<QAngle>();
		si.Radius = ref flSoundRadius;

		soundServices.GetSoundSpatialization(parms.SoundSource, ref si);

		// pick a channel to play on from the static area
		lock (g_SndMutex) {
			ch = SND_PickStaticChannel(parms.SoundSource, parms.Sfx); // Autolooping sounds are always fixed origin(?)
			if (ch == null)
				return 0;

			SND_ActivateChannel(ch);
			ChannelClearVolumes(ch);

			ch.UserData = parms.UserData;
			ch.InitialStreamPosition = parms.InitialStreamPosition;

			if (ch.UserData != 0)
				soundServices.GetToolSpatialization(ch.UserData, ch.Guid, ref si);

			int channelIndex = ch.Index;
			g_AudioDevice.ChannelReset(parms.SoundSource, channelIndex, ch.DistMult);

			if (TestSoundChar(sndname, SoundChars.Sentence)) {
				// this is a sentence. link words to play in sequence.

				// NOTE: sentence names stored in the cache lookup are
				// prepended with a '!'.  Sentence names stored in the
				// sentence file do not have a leading '!'.

				// link all words and load the first word
				VOX_LoadSound(ch, SkipSoundChars(sndname));
			}
			else {
				// load regular or stream sound
				pSource = S_LoadSound(parms.Sfx, ch);
				if (pSource != null && !IsValidSampleRate(pSource.SampleRate()))
					Warning($"*** Invalid sample rate ({pSource.SampleRate()}) for sound '{sndname}'.\n");

				if (pSource == null && !parms.Sfx.IsLateLoad)
					Warning($"Failed to load sound \"{sndname}\", file probably missing from disk/repository\n");

				ch.Sfx = parms.Sfx;
				ch.Flags.IsSentence = false;
			}

			if (ch.Mixer == null) {
				// couldn't load sounds' data, or sentence has 0 words (not an error)
				S_FreeChannel(ch);
				return 0;
			}

			ch.Origin = parms.Origin;
			ch.Direction = parms.Direction;

			// never update positions if source entity is 0
			ch.Flags.UpdatePositions = parms.UpdatePositions && (parms.SoundSource == 0 ? false : true);

			ch.MasterVol = (short)vol;

			ch.Flags.CompatibilityAttenuation = EngineSoundGlobals.SNDLEVEL_IS_COMPATIBILITY_MODE(parms.SoundLevel);
			if (ch.Flags.CompatibilityAttenuation) {
				// Translate soundlevel from its 'encoded' value to a real soundlevel that we can use in the sound system.
				parms.SoundLevel = EngineSoundGlobals.SNDLEVEL_FROM_COMPATIBILITY_MODE(parms.SoundLevel);
			}

			ch.DistMult = SNDLVL_TO_DIST_MULT((int)parms.SoundLevel);

			S_SetChannelWavtype(ch, parms.Sfx);

			ch.BasePitch = (short)parms.Pitch;
			ch.SoundSource = parms.SoundSource;
			ch.EntChannel = (int)parms.EntChannel;
			ch.SpecialDsp = parms.SpecialDSP;
			ch.Flags.FromServer = parms.FromServer;
			ch.Flags.Speaker = (parms.Flags & SoundFlags.Speaker) != 0;
			ch.SpeakerEntity = parms.SpeakerEntity;

			ch.Flags.ShouldPause = (parms.Flags & SoundFlags.ShouldPause) != 0;

			// TODO: Support looping sounds through speakers.
			// If the sound is from a speaker, and it's looping, ignore it.
			if (ch.Flags.Speaker) {
				if (parms.Sfx.Source != null && parms.Sfx.Source.IsLooped()) {
					if (nSndShowStart > 0 && nSndShowStart < 7 && nSndShowStart != 4)
						DevMsg($"StaticSound : Speaker ignored looping sound: {sndname}\n");

					S_FreeChannel(ch);
					return 0;
				}
			}

			// set the default radius
			ch.Radius = flSoundRadius;

			S_SetChannelStereo(ch, pSource);

			// initialize dsp room mixing params
			ch.DspMixMin = -1;
			ch.DspMixMax = -1;

			if (nSndShowStart == 5) {
				snd_showstart.SetValue(6);      // display gain once only
				nSndShowStart = 6;
			}

			// get sound type before we spatialize

			MXR_GetMixGroupFromSoundsource(ch, parms.SoundSource, parms.SoundLevel);

			// skip the trace on the first spatialization.  This channel may be stolen
			// by another sound played this frame.  Defer the trace to the mix loop
			SND_SpatializeFirstFrameNoTrace(ch);

			// Init client entity mouth movement vars
			ch.Flags.IgnorePhonemes = (parms.Flags & SoundFlags.IgnorePhonemes) != 0;
			SND_InitMouth(ch);

			// Pre-startup delay.  Compute # of samples over which to mix in zeros from data source before
			// actually reading first set of samples
			if (parms.Delay != 0.0f) {
				Assert(ch.Sfx);
				Assert(ch.Sfx!.Source);

				int rate = ch.Sfx.Source!.SampleRate();

				int delaySamples = (int)(parms.Delay * rate * parms.Pitch * 0.01f);

				ch.Mixer.SetStartupDelaySamples(delaySamples);

				if (parms.Delay > 0) {
					ch.Mixer.SetStartupDelaySamples(delaySamples);
					ch.Flags.DelayedStart = true;
				}
				else {
					int skipSamples = -delaySamples;
					int totalSamples = ch.Sfx.Source.SampleCount();

					if (ch.Sfx.Source.IsLooped())
						skipSamples = skipSamples % totalSamples;

					if (skipSamples >= totalSamples) {
						S_FreeChannel(ch);
						return 0;
					}

					ch.Pitch = ch.BasePitch * 0.01f;
					ch.Mixer.SkipSamples(ch, skipSamples, rate, 0);
					ch.ObGainTarget = 1.0f;
					ch.ObGain = 1.0f;
					ch.ObGainInc = 0.0f;
					ch.Flags.FirstPass = false;
				}
			}

			if (S_IsMusic(ch)) {
				// See if we have "music" of same name playing from "world" which means we save/restored this sound already.  If so,
				//  kill the new version and update the soundsource
				ChannelList list = new();
				g_ActiveChannels.GetActiveChannels(list);
				for (int i = 0; i < list.Count; i++) {
					Channel pChannel = list.GetChannel(i);
					// Don't mess with the channel we just created, of course
					if (ch == pChannel)
						continue;
					if (ch.Sfx != pChannel.Sfx)
						continue;
					if (pChannel.SoundSource != EngineSoundGlobals.SOUND_FROM_WORLD)
						continue;
					if (!S_IsMusic(pChannel))
						continue;

					DevMsg(1, $"Hooking duplicate restored song track {sndname}\n");

					// the new channel will have an updated soundsource and probably
					// has an updated pitch or volume since we are receiving this sound message
					// after the sound has started playing (usually a volume change)
					// copy that data out of the source
					pChannel.SoundSource = ch.SoundSource;
					pChannel.MasterVol = ch.MasterVol;
					pChannel.BasePitch = ch.BasePitch;
					pChannel.Pitch = ch.Pitch;
					S_FreeChannel(ch);

					return 0;
				}
			}

			soundServices.OnSoundStarted(ch.Guid, ref parms, sndname);

			if (nSndShowStart > 0 && nSndShowStart < 7 && nSndShowStart != 4) {
				DevMsg($"StaticSound {sndname} : src {parms.SoundSource} : channel {(int)parms.EntChannel} : {(int)parms.SoundLevel} dB : vol {parms.Volume:F2} : radius {flSoundRadius:F0} : time {soundServices.GetHostTime():F3}\n");
				if (nSndShowStart == 2 || nSndShowStart == 5)
					DevMsg($"\t dspmix {ch.DspMix:F2} : distmix {ch.DistMix:F2} : dspface {ch.DspFace:F2} : lvol {ch.FVolume[IFRONT_LEFT]:F2} : cvol {ch.FVolume[IFRONT_CENTER]:F2} : rvol {ch.FVolume[IFRONT_RIGHT]:F2} : rlvol {ch.FVolume[IREAR_LEFT]:F2} : rrvol {ch.FVolume[IREAR_RIGHT]:F2}\n");
				if (nSndShowStart == 3)
					DevMsg($"\t x: {ch.Origin.X:F6} y: {ch.Origin.Y:F6} z: {ch.Origin.Z:F6}\n");
			}

			return ch.Guid;
		}
	}

	public static int S_StartSound(ref StartSoundParams parms) {

		if (parms.Sfx == null)
			return 0;

		if (parms.StaticSound)
			return S_StartStaticSound(ref parms);
		else
			return S_StartDynamicSound(ref parms);
	}

	// Restart all the sounds on the specified channel
	static bool IsChannelLooped(int iChannel) {
		return channels[iChannel].Sfx != null &&
				channels[iChannel].Sfx!.Source != null &&
				channels[iChannel].Sfx!.Source!.IsLooped();
	}

	public static int S_GetCurrentStaticSounds(Span<SoundInfo> pResult, int nSizeResult, int entchannel) {
		int nSpaceRemaining = nSizeResult;
		int resultIndex = 0;
		for (int i = MAX_DYNAMIC_CHANNELS; i < total_channels && nSpaceRemaining != 0; i++) {
			if (channels[i].EntChannel == entchannel && channels[i].Sfx != null) {
				pResult[resultIndex].Set(channels[i].SoundSource,
							  (SoundEntityChannel)channels[i].EntChannel,
							  channels[i].Sfx!.GetFileNameHandle(),
							  channels[i].Origin,
							  channels[i].Direction,
							  ((float)channels[i].MasterVol / 255.0F),
							  (SoundLevel)DIST_MULT_TO_SNDLVL(channels[i].DistMult),
							  IsChannelLooped(i),
							  channels[i].BasePitch,
							  listener_origin,
							  channels[i].SpeakerEntity);
				resultIndex++;
				nSpaceRemaining--;
			}
		}
		return nSizeResult - nSpaceRemaining;
	}


	// Stop all sounds for entity on a channel.
	public static void S_StopSound(int soundsource, int entchannel) {
		lock (g_SndMutex) {
			ChannelList list = new();
			g_ActiveChannels.GetActiveChannels(list);
			for (int i = 0; i < list.Count; i++) {
				Channel pChannel = list.GetChannel(i);
				if (pChannel.SoundSource == soundsource
					&& pChannel.EntChannel == entchannel) {
					S_FreeChannel(pChannel);
				}
			}
		}
	}

	public static Channel? S_FindChannelByGuid(int guid) {
		ChannelList list = new();
		g_ActiveChannels.GetActiveChannels(list);
		for (int i = 0; i < list.Count; i++) {
			Channel pChannel = list.GetChannel(i);
			if (pChannel.Guid == guid)
				return pChannel;
		}
		return null;
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	// Input  : guid -
	//-----------------------------------------------------------------------------
	public static void S_StopSoundByGuid(int guid) {
		lock (g_SndMutex) {
			Channel? pChannel = S_FindChannelByGuid(guid);
			if (pChannel != null)
				S_FreeChannel(pChannel);
		}
	}


	//-----------------------------------------------------------------------------
	// Purpose:
	// Input  : guid -
	//-----------------------------------------------------------------------------
	public static float S_SoundDurationByGuid(int guid) {
		Channel? pChannel = S_FindChannelByGuid(guid);
		if (pChannel == null || pChannel.Sfx == null)
			return 0.0f;

		// NOTE: Looping sounds will return the length of a single loop
		// Use S_IsLoopingSoundByGuid to see if they are looped
		float flRate = pChannel.Sfx.Source!.SampleRate() * pChannel.BasePitch * 0.01f;
		int nTotalSamples = pChannel.Sfx.Source.SampleCount();
		return (flRate != 0.0f) ? nTotalSamples / flRate : 0.0f;
	}


	//-----------------------------------------------------------------------------
	// Is this sound a looping sound?
	//-----------------------------------------------------------------------------
	public static bool S_IsLoopingSoundByGuid(int guid) {
		Channel? pChannel = S_FindChannelByGuid(guid);
		if (pChannel == null || pChannel.Sfx == null)
			return false;

		return pChannel.Sfx.Source!.IsLooped();
	}


	//-----------------------------------------------------------------------------
	// Purpose: Note that the guid is preincremented, so we can just return the current value as the "last sound" indicator
	// Input  :  -
	// Output : int
	//-----------------------------------------------------------------------------
	public static int S_GetGuidForLastSoundEmitted() {
		return s_nSoundGuid;
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	// Input  : guid -
	// Output : Returns true on success, false on failure.
	//-----------------------------------------------------------------------------
	public static bool S_IsSoundStillPlaying(int guid) {
		Channel? pChannel = S_FindChannelByGuid(guid);
		return pChannel != null;
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	// Input  : guid -
	//			fvol -
	//-----------------------------------------------------------------------------
	public static void S_SetVolumeByGuid(int guid, float fvol) {
		Channel? pChannel = S_FindChannelByGuid(guid);
		if (pChannel == null)
			return;

		pChannel.MasterVol = (short)(255.0f * Math.Clamp(fvol, 0.0f, 1.0f));
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	// Input  : guid -
	// Output : float
	//-----------------------------------------------------------------------------
	public static float S_GetElapsedTimeByGuid(int guid) {
		Channel? pChannel = S_FindChannelByGuid(guid);
		if (pChannel == null)
			return 0.0f;

		AudioMixer? mixer = pChannel.Mixer;
		if (mixer == null)
			return 0.0f;

		float elapsed = mixer.GetSamplePosition() / (mixer.GetSource().SampleRate() * pChannel.Pitch * 0.01f);
		return elapsed;
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	// Input  : sndlist -
	//-----------------------------------------------------------------------------
	public static void S_GetActiveSounds(List<SndInfo> sndlist) {
		ChannelList list = new();
		g_ActiveChannels.GetActiveChannels(list);
		for (int i = 0; i < list.Count; i++) {
			Channel ch = list.GetChannel(i);

			SndInfo info = new();

			info.Guid = ch.Guid;
			info.FilenameHandle = ch.Sfx != null ? ch.Sfx.GetFileNameHandle() : 0;
			info.SoundSource = ch.SoundSource;
			info.Channel = ch.EntChannel;
			// If a sound is being played through a speaker entity (e.g., on a monitor,), this is the
			//  entity upon which to show the lips moving, if the sound has sentence data
			info.SpeakerEntity = ch.SpeakerEntity;
			info.Volume = (float)ch.MasterVol / 255.0f;
			info.LastSpatializedVolume = ch.LastVol;
			// Radius of this sound effect (spatialization is different within the radius)
			info.Radius = ch.Radius;
			info.Pitch = ch.BasePitch;
			info.Origin = ch.Origin;
			info.Direction = ch.Direction;

			// if true, assume sound source can move and update according to entity
			info.UpdatePositions = ch.Flags.UpdatePositions;
			// true if playing linked sentence
			info.IsSentence = ch.Flags.IsSentence;
			// if true, bypass all dsp processing for this sound (ie: music)
			info.DryMix = ch.Flags.Dry;
			// true if sound is playing through in-game speaker entity.
			info.Speaker = ch.Flags.Speaker;
			// true if sound is using special DSP effect
			info.SpecialDSP = ch.SpecialDsp != 0;
			// for snd_show, networked sounds get colored differently than local sounds
			info.FromServer = ch.Flags.FromServer;

			sndlist.Add(info);
		}
	}

	public static void S_StopAllSounds(bool bClear) {
		lock (g_SndMutex) {
			int i;

			if (g_AudioDevice == null)
				return;

			if (!g_AudioDevice.IsActive())
				return;

			total_channels = MAX_DYNAMIC_CHANNELS;  // no statics

			ChannelList list = new();
			g_ActiveChannels.GetActiveChannels(list);
			for (i = 0; i < list.Count; i++) {
				Channel pChannel = list.GetChannel(i);
				if (channels[i].Sfx != null)
					DevMsg(1, $"{i,2}:Stopped sound {channels[i].Sfx!.GetName()}\n");
				S_FreeChannel(pChannel);
			}

			for (i = 0; i < MAX_CHANNELS; i++)
				channels[i].Clear();

			if (bClear)
				S_ClearBuffer();

			// Clear any remaining soundfade
			soundfade = default;

			g_AudioDevice.StopAllSounds();
			Assert(g_ActiveChannels.GetActiveCount() == 0);
		}
	}

	public static void S_StopAllSoundsC() {
		S_StopAllSounds(true);
	}

	public static void S_OnLoadScreen(bool value) {
		s_bOnLoadScreen = value;
	}

	public static void S_ClearBuffer() {
		if (g_AudioDevice == null)
			return;

		g_AudioDevice.ClearBuffer();
		DSP_ClearState();
		MIX_ClearAllPaintBuffers(PAINTBUFFER_SIZE, true);
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	// Input  : percent -
	//			holdtime -
	//			intime -
	//			outtime -
	//-----------------------------------------------------------------------------
	public static void S_SoundFade(float percent, float holdtime, float intime, float outtime) {
		soundfade.starttime = (float)soundServices.GetHostTime();

		soundfade.initial_percent = percent;
		soundfade.fadeouttime = outtime;
		soundfade.holdtime = holdtime;
		soundfade.fadeintime = intime;
	}

	//-----------------------------------------------------------------------------
	// Purpose: Modulates sound volume on the client.
	//-----------------------------------------------------------------------------
	static void S_UpdateSoundFade() {
		float totaltime;
		float f;
		// Determine current fade value.

		// Assume no fading remains
		soundfade.percent = 0;

		totaltime = soundfade.fadeouttime + soundfade.fadeintime + soundfade.holdtime;

		float elapsed = (float)soundServices.GetHostTime() - soundfade.starttime;

		// Clock wrapped or reset (BUG) or we've gone far enough
		if (elapsed < 0.0f || elapsed >= totaltime || totaltime <= 0.0f)
			return;

		// We are in the fade time, so determine amount of fade.
		if (soundfade.fadeouttime > 0.0f && (elapsed < soundfade.fadeouttime)) {
			// Ramp up
			f = elapsed / soundfade.fadeouttime;
		}
		// Inside the hold time
		else if (elapsed <= (soundfade.fadeouttime + soundfade.holdtime)) {
			// Stay
			f = 1.0f;
		}
		else {
			// Ramp down
			f = (elapsed - (soundfade.fadeouttime + soundfade.holdtime)) / soundfade.fadeintime;
			// backward interpolated...
			f = 1.0f - f;
		}

		// Spline it.
		f = MathLib.SimpleSpline(f);
		f = Math.Clamp(f, 0.0f, 1.0f);

		soundfade.percent = soundfade.initial_percent * f;
	}


	//=============================================================================

	// Global Voice Ducker - enabled in vcd scripts, when characters deliver important dialog.  Overrides all
	// other mixer ducking, and ducks all other sounds except dialog.

	public static readonly ConVar snd_ducktovolume = new("snd_ducktovolume", "0.55", FCvar.Archive);
	public static readonly ConVar snd_duckerattacktime = new("snd_duckerattacktime", "0.5", FCvar.Archive);
	public static readonly ConVar snd_duckerreleasetime = new("snd_duckerreleasetime", "2.5", FCvar.Archive);
	public static readonly ConVar snd_duckerthreshold = new("snd_duckerthreshold", "0.15", FCvar.Archive);

	static void S_UpdateVoiceDuck(int voiceChannelCount, int voiceChannelMaxVolume, float frametime) {
		float volume_when_ducked = snd_ducktovolume.GetFloat();
		int volume_threshold = (int)(snd_duckerthreshold.GetFloat() * 255.0);

		float duckTarget = 1.0f;
		if (voiceChannelCount > 0) {
			voiceChannelMaxVolume = Math.Clamp(voiceChannelMaxVolume, 0, 255);

			// duckTarget = RemapVal( voiceChannelMaxVolume, 0, 255, 1.0, volume_when_ducked );

			// KB: Change: ducker now active if any character is speaking above threshold volume.
			// KB: Active ducker drops all volumes to volumes * snd_duckvolume

			if (voiceChannelMaxVolume > volume_threshold)
				duckTarget = volume_when_ducked;
		}
		float rate = (duckTarget < g_DuckScale) ? snd_duckerattacktime.GetFloat() : snd_duckerreleasetime.GetFloat();
		g_DuckScale = MathLib.Approach(duckTarget, g_DuckScale, frametime * ((1 - volume_when_ducked) / rate));
	}

	// set 2d forward vector, given 3d right vector.
	// NOTE: this should only be used for a listener forward
	// vector from a listener right vector. It is not a general use routine.

	public static void ConvertListenerVectorTo2D(out Vector3 pvforward, in Vector3 pvright) {
		// get 2d forward direction vector, ignoring pitch angle
		QAngle angles2d;
		Vector3 source2d;
		Vector3 listener_forward2d;

		source2d = pvright;
		source2d.Z = 0.0f;

		MathLib.VectorNormalize(ref source2d);

		// convert right vector to euler angles (yaw & pitch)

		MathLib.VectorAngles(source2d, out angles2d);

		// get forward angle of listener

		angles2d[PITCH] = 0;
		angles2d[YAW] += 90; // rotate 90 ccw
		angles2d[ROLL] = 0;

		if (angles2d[YAW] >= 360)
			angles2d[YAW] -= 360;

		MathLib.AngleVectors(angles2d, out listener_forward2d);

		MathLib.VectorNormalize(ref listener_forward2d);

		pvforward = listener_forward2d;
	}
}
