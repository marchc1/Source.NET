using Source.Common;
using Source.Common.Audio;
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

		StartSoundParams parms = default;
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

	public void EmitSentenceByIndex<T>(scoped in T filter, int entIndex, int channel, int sentenceIndex, float volume, SoundLevel soundlevel, SoundFlags flags = SoundFlags.NoFlags, int pitch = 100, int specialDSP = 0, in Vector3 origin = default, in Vector3 direction = default, ReadOnlySpan<Vector3> origins = default, bool updatePositions = true, double soundTime = 0, int speakerEntity = -1) where T : IRecipientFilter {
		throw new NotImplementedException();
	}

	public void EmitSound<T>(scoped in T filter, int entIndex, int channel, ReadOnlySpan<char> sample, float volume, float attenuation, SoundFlags flags = SoundFlags.NoFlags, int pitch = 100, int specialDSP = 0, in Vector3 origin = default, in Vector3 direction = default, ReadOnlySpan<Vector3> origins = default, bool updatePositions = true, double soundTime = 0, int speakerEntity = -1) where T : IRecipientFilter {
		EmitSound(filter, entIndex, channel, sample, volume, ATTN_TO_SNDLVL(attenuation), flags,
			pitch, specialDSP, origin, direction, origins, updatePositions, soundTime, speakerEntity);
	}

	public void EmitSound<T>(scoped in T filter, int entIndex, int channel, ReadOnlySpan<char> sample, float volume, SoundLevel soundlevel, SoundFlags flags = SoundFlags.NoFlags, int pitch = 100, int specialDSP = 0, in Vector3 origin = default, in Vector3 direction = default, ReadOnlySpan<Vector3> origins = default, bool updatePositions = true, double soundTime = 0, int speakerEntity = -1) where T : IRecipientFilter {
		if (!sample.IsEmpty && SoundCharsUtils.TestSoundChar(sample, SoundChars.Sentence)) {
			int sentenceIndex = -1;
			// VOX_LookupString(SoundCharsUtils.SkipSoundChars(sample), &sentenceIndex); TODO
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

	private void EmitSoundInternal<T>(T filter, int entIndex, int channel, ReadOnlySpan<char> sample, float volume, SoundLevel soundlevel, SoundFlags flags, int pitch, int specialDSP, in Vector3 origin, in Vector3 direction, ReadOnlySpan<Vector3> origins, bool updatePositions, double soundTime, int speakerEntity) where T : IRecipientFilter {
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

		Vector3 startOrigin = Unsafe.IsNullRef(in origin) ? new(0) : origin;
		Vector3 startDirection = Unsafe.IsNullRef(in direction) ? new(0) : direction;
		if (soundSource == SOUND_FROM_UI_PANEL) {
			startOrigin = new(0);
			startDirection = new(0);
		}
		else {
			if (Unsafe.IsNullRef(in origin)) {
				IClientEntity? ent = entitylist.GetClientEntity(entIndex);
				if (ent != null && (flags & SoundFlags.Stop) == 0)
					startOrigin = ent.GetRenderOrigin();
				else
					startOrigin = new(0);
			}

			if (Unsafe.IsNullRef(in direction)) {
				IClientEntity? ent = entitylist.GetClientEntity(entIndex);
				if (ent != null && (flags & SoundFlags.Stop) == 0) {
					QAngle angles = ent.GetAbsAngles();
					AngleVectors(in angles, out startDirection);
				}
				else
					startDirection = new(0);
			}
		}

		float delay = 0.0f;
		if (soundTime != 0.0f) {
			delay = Sound.ComputeDelayForSoundtime(soundTime, ClockSyncIndex.Client);
			if (delay <= 0 && delay > -0.250f)
				delay = 1e-6f;
		}

		StartSoundParams parms = default;
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

	public ref SndInfo GetActiveSound() {
		throw new NotImplementedException();
	}

	public int GetActiveSoundCount() {
		throw new NotImplementedException();
	}

	public float GetDistGainFromSoundLevel(SoundLevel soundlevel, float dist) {
		throw new NotImplementedException();
	}

	public int GetGuidForLastSoundEmitted() {
		throw new NotImplementedException();
	}

	public float GetSoundDuration(ReadOnlySpan<char> sample) {
		// TODO: return AudioSource_GetSoundDuration(sample);
		return 0;
	}

	public bool IsSoundPrecached(ReadOnlySpan<char> sample) {
		throw new NotImplementedException();
	}

	public bool IsSoundStillPlaying(int guid) {
		throw new NotImplementedException();
	}

	public void NotifyBeginMoviePlayback() {
		throw new NotImplementedException();
	}

	public void NotifyEndMoviePlayback() {
		throw new NotImplementedException();
	}

	public void PrecacheSentenceGroup(ReadOnlySpan<char> groupName) {
		throw new NotImplementedException();
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
		throw new NotImplementedException();
	}

	public void SetPlayerDSP<T>(scoped in T filter, int dspType, bool fastReset) where T : IRecipientFilter {
		throw new NotImplementedException();
	}

	public void SetRoomType<T>(scoped in T filter, int roomType) where T : IRecipientFilter {
		throw new NotImplementedException();
	}

	public void SetVolumeByGuid(int guid, float fvol) {
		throw new NotImplementedException();
	}

	public void StopAllSounds(bool clearBuffers) {
		throw new NotImplementedException();
	}

	public void StopSound(int entIndex, int channel, ReadOnlySpan<char> pSample) {
		throw new NotImplementedException();
	}

	public void StopSoundByGuid(int guid) {
		throw new NotImplementedException();
	}
}
