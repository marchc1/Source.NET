using Source.Common;
using Source.Common.Audio;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;

using System.Numerics;
using System.Runtime.CompilerServices;

using static Source.Common.Mathematics.MathLib;

namespace Source.Engine;

public class EngineSoundClient(Sound Sound) : IEngineSound
{
	public void EmitAmbientSound(ReadOnlySpan<char> pSample, float volume, int pitch = 100, int flags = 0, double soundTime = 0) {
		float delay = 0.0f;
		if (soundTime != 0.0f)
			delay = (float)(soundTime - cl.LastServerTickTime);

		SfxTable? sound = Sound.PrecacheSound(pSample);

		StartSoundParams parms = new();
		parms.StaticSound = true;
		parms.SoundSource = SOUND_FROM_LOCAL_PLAYER;
		parms.EntChannel = SoundEntityChannel.Static;
		parms.Sfx = sound;
		parms.Origin = vec3_origin;
		parms.Volume = volume;
		parms.SoundLevel = SoundLevel.LvlNone;
		parms.Flags = (SoundFlags)flags;
		parms.Pitch = pitch;
		parms.SpecialDSP = 0;
		parms.FromServer = false;
		parms.Delay = delay;

		Sound.StartSound(in parms);
	}

	//-----------------------------------------------------------------------------
	// Plays a sentence
	//-----------------------------------------------------------------------------
	public void EmitSentenceByIndex<T>(scoped in T filter, int entIndex, int channel, int sentenceIndex, float volume, SoundLevel soundlevel, SoundFlags flags = SoundFlags.NoFlags, int pitch = 100, int specialDSP = 0, in Vector3? origin = default, in Vector3? direction = default, List<Vector3>? origins = default, bool updatePositions = true, double soundTime = 0, int speakerEntity = -1) where T : IRecipientFilter {
		if (sentenceIndex >= 0) {
			string pName = $"!{sentenceIndex}";
			EmitSoundInternal(filter, entIndex, channel, pName, volume, soundlevel,
				flags, pitch, specialDSP, origin, direction, origins, updatePositions, soundTime, speakerEntity);
		}
	}

	public void EmitSound<T>(scoped in T filter, int entIndex, int channel, ReadOnlySpan<char> sample, float volume, float attenuation, SoundFlags flags = SoundFlags.NoFlags, int pitch = 100, int specialDSP = 0, in Vector3? origin = default, in Vector3? direction = default, List<Vector3>? origins = default, bool updatePositions = true, double soundTime = 0, int speakerEntity = -1) where T : IRecipientFilter {
		EmitSound(filter, entIndex, channel, sample, volume, ATTN_TO_SNDLVL(attenuation), flags,
			pitch, specialDSP, origin, direction, origins, updatePositions, soundTime, speakerEntity);
	}

	public void EmitSound<T>(scoped in T filter, int entIndex, int channel, ReadOnlySpan<char> sample, float volume, SoundLevel soundlevel, SoundFlags flags = SoundFlags.NoFlags, int pitch = 100, int specialDSP = 0, in Vector3? origin = default, in Vector3? direction = default, List<Vector3>? origins = default, bool updatePositions = true, double soundTime = 0, int speakerEntity = -1) where T : IRecipientFilter {
		if (!sample.IsEmpty && SoundCharsUtils.TestSoundChar(sample, SoundChars.Sentence)) {
#if !SWDS
			g_AudioSystem.LookupSentence(SoundCharsUtils.SkipSoundChars(sample), out int sentenceIndex);
#else
			int sentenceIndex = -1;
#endif
			if (sentenceIndex >= 0)
				EmitSentenceByIndex(filter, entIndex, channel, sentenceIndex, volume,
					soundlevel, flags, pitch, specialDSP, origin, direction, origins, updatePositions, soundTime, speakerEntity);
			else
				DevWarning(2, $"Unable to find {SoundCharsUtils.SkipSoundChars(sample).SliceNullTerminatedString()} in sentences.txt\n");
		}
		else
			EmitSoundInternal(filter, entIndex, channel, sample, volume, soundlevel,
				flags, pitch, specialDSP, origin, direction, origins, updatePositions, soundTime, speakerEntity);
	}

	private void EmitSoundInternal<T>(T filter, int entIndex, int channel, ReadOnlySpan<char> sample, float volume, SoundLevel soundlevel, SoundFlags flags, int pitch, int specialDSP, in Vector3? origin, in Vector3? direction, List<Vector3>? origins, bool updatePositions, double soundTime, int speakerEntity) where T : IRecipientFilter {
		if (volume < 0 || volume > 1) {
			Warning($"EmitSound: volume out of bounds = {volume}\n");
			return;
		}

		if (((int)soundlevel < MIN_SNDLVL_VALUE) || ((int)soundlevel > MAX_SNDLVL_VALUE)) {
			Warning($"EmitSound: soundlevel out of bounds = {(int)soundlevel}\n");
			return;
		}

		if (pitch < 0 || pitch > 255) {
			Warning($"EmitSound: pitch out of bounds = {pitch}\n");
			return;
		}

		int soundSource = entIndex;

		if (soundSource != SOUND_FROM_UI_PANEL) {
			if (soundSource < 0)
				soundSource = cl.ViewEntity;

			int i = 0;
			int c = filter.GetRecipientCount();
			for (; i < c; i++) {
				int index = filter.GetRecipientIndex(i);
				if (index == cl.PlayerSlot + 1)
					break;
			}

			if (i >= c)
				return;
		}

		SfxTable? sound = Sound.PrecacheSound(sample);
		if (sound == null)
			return;

		Vector3 startOrigin = !origin.HasValue ? new(0) : origin.Value;
		Vector3 startDirection = !direction.HasValue ? new(0) : direction.Value;
		if (soundSource == SOUND_FROM_UI_PANEL) {
			startOrigin = new(0);
			startDirection = new(0);
		}
		else {
			if (!origin.HasValue) {
				IClientEntity? ent = entitylist.GetClientEntity(entIndex);
				if (ent != null && (flags & SoundFlags.Stop) == 0)
					startOrigin = ent.GetRenderOrigin();
				else
					startOrigin = new(0);
			}

			if (!direction.HasValue) {
				IClientEntity? ent = entitylist.GetClientEntity(entIndex);
				if (ent != null && (flags & SoundFlags.Stop) == 0) {
					QAngle angles = ent.GetAbsAngles();
					AngleVectors(in angles, out startDirection);
				}
				else
					startDirection = new(0);
			}
		}

		origins?.Add(startOrigin);

		float delay = 0.0f;
		if (soundTime != 0.0f) {
			// this sound was played directly on the client, use its clock sync
			delay = Sound.ComputeDelayForSoundtime(soundTime, ClockSyncIndex.Client);
			// anything over 250ms is assumed to be intentional skipping
			if (delay <= 0 && delay > -0.250f)
				delay = 1e-6f;
		}

		StartSoundParams parms = new();
		parms.StaticSound = channel == (int)SoundEntityChannel.Static;
		parms.SoundSource = soundSource;
		parms.EntChannel = (SoundEntityChannel)channel;
		parms.Sfx = sound;
		parms.Origin = startOrigin;
		parms.Direction = startDirection;
		parms.UpdatePositions = updatePositions;
		parms.Volume = volume;
		parms.SoundLevel = soundlevel;
		parms.Flags = flags;
		parms.Pitch = pitch;
		parms.SpecialDSP = specialDSP;
		parms.FromServer = false;
		parms.Delay = delay;
		parms.SpeakerEntity = speakerEntity;

		Sound.StartSound(in parms);
	}

	//-----------------------------------------------------------------------------
	// Purpose: Retrieves list of all active sounds
	// Input  : sndlist - 
	//-----------------------------------------------------------------------------
	public void GetActiveSounds(List<SndInfo> sndlist) {
		Sound.GetActiveSounds(sndlist);
	}

	public float GetDistGainFromSoundLevel(SoundLevel soundlevel, float dist) {
		return SndGain.S_GetGainFromSoundLevel(soundlevel, dist);
	}

	public int GetGuidForLastSoundEmitted() {
		return Sound.GetGuidForLastSoundEmitted();
	}

	public TimeUnit_t GetSoundDuration(ReadOnlySpan<char> sample) {
		return Sound.GetSoundDuration(sample);
	}

	public bool IsSoundPrecached(ReadOnlySpan<char> sample) {
		if (!sample.IsEmpty && SoundCharsUtils.TestSoundChar(sample, SoundChars.Sentence))
			return true;

		int idx = cl.LookupSoundIndex(sample);
		if (idx == -1)
			return false;
		return true;
	}

	public bool IsSoundStillPlaying(int guid) {
		return Sound.IsSoundStillPlaying(guid);
	}

	public void NotifyBeginMoviePlayback() {
		StopAllSounds(true);
	}

	public void NotifyEndMoviePlayback() {
	}

	public void PrecacheSentenceGroup(ReadOnlySpan<char> groupName) {
#if !SWDS
		g_AudioSystem.PrecacheSentenceGroup(this, groupName, null);
#endif
	}

	public bool PrecacheSound(ReadOnlySpan<char> sample, bool preload = false, bool isUISound = false) {
		SfxTable? table = Sound.PrecacheSound(sample);
		if (table != null) {
			if (isUISound)
				Sound.MarkUISound(table);

			return true;
		}

		return false;
	}

	public void PrefetchSound(ReadOnlySpan<char> sample) {
		Sound.PrefetchSound(sample, true);
	}

	public void SetPlayerDSP<T>(scoped in T filter, int dspType, bool fastReset) where T : IRecipientFilter {
		dsp_player ??= cvar.FindVar("dsp_player");
		dsp_player?.SetValue(dspType);
#if !SWDS
		if (fastReset)
			g_AudioSystem.DSP_FastReset(dspType);
#endif
	}

	public void SetRoomType<T>(scoped in T filter, int roomType) where T : IRecipientFilter {
		dsp_room ??= cvar.FindVar("dsp_room");
		dsp_room?.SetValue(roomType);
	}

	[CvarIgnore] ConVar? dsp_player;
	[CvarIgnore] ConVar? dsp_room;

	public void SetVolumeByGuid(int guid, float fvol) {
		Sound.SetVolumeByGuid(guid, fvol);
	}

	public void StopAllSounds(bool clearBuffers) {
		Sound.StopAllSounds(clearBuffers);
	}

	//-----------------------------------------------------------------------------
	// Stops a sound
	//-----------------------------------------------------------------------------
	public void StopSound(int entIndex, int channel, ReadOnlySpan<char> pSample) {
		EngineSingleUserFilter filter = new(cl.PlayerSlot + 1);
		EmitSound(filter, entIndex, channel, pSample, 0, SoundLevel.LvlNone, SoundFlags.Stop, PITCH_NORM, 0,
			null, null, null, true);
	}

	public void StopSoundByGuid(int guid) {
		Sound.StopSoundByGuid(guid);
	}
}
