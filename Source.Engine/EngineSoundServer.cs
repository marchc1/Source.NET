using Source.Common;
using Source.Common.Audio;
using Source.Common.Engine;
using Source.Common.MaterialSystem;
using Source.Common.SoundEmitterSystem;

using System.Numerics;
using System.Threading.Channels;

namespace Source.Engine;

public class EngineSoundServer : IEngineSound
{
	public void EmitAmbientSound(ReadOnlySpan<char> pSample, float volume, int pitch = 100, int flags = 0, double soundTime = 0) {
		throw new NotImplementedException();
	}

	public void EmitSentenceByIndex<T>(scoped in T filter, int entIndex, int channel, int iSentenceIndex, float volume, SoundLevel soundlevel, SoundFlags flags = SoundFlags.NoFlags, int pitch = 100, int specialDSP = 0, in Vector3? origin = default, in Vector3? direction = default, List<Vector3>? origins = default, bool updatePositions = true, double soundTime = 0, int speakerEntity = -1) where T : IRecipientFilter {
		throw new NotImplementedException();
	}

	public void EmitSound<T>(scoped in T filter, int entIndex, int channel, ReadOnlySpan<char> sample, float volume, float attenuation, SoundFlags flags = SoundFlags.NoFlags, int pitch = 100, int specialDSP = 0, in Vector3? origin = default, in Vector3? direction = default, List<Vector3>? origins = default, bool updatePositions = true, double soundTime = 0, int speakerEntity = -1) where T : IRecipientFilter {
		EmitSound(filter, entIndex, channel, sample, volume, ATTN_TO_SNDLVL(attenuation), flags,
		pitch, specialDSP, in origin, in direction, origins, updatePositions, soundTime, speakerEntity);
	}



	public void EmitSound<T>(scoped in T filter, int entIndex, int channel, ReadOnlySpan<char> sample, float volume, SoundLevel soundlevel, SoundFlags flags = SoundFlags.NoFlags, int pitch = 100, int specialDSP = 0, in Vector3? origin = default, in Vector3? direction = default, List<Vector3>? origins = default, bool updatePositions = true, double soundTime = 0, int speakerEntity = -1) where T : IRecipientFilter {
		if (!sample.IsEmpty && SoundCharsUtils.TestSoundChar(sample, SoundChars.Sentence)) {
			int iSentenceIndex = -1;
			// TODO Vox.LookupString(SoundCharsUtils.SkipSoundChars(sample), ref iSentenceIndex);
			if (iSentenceIndex >= 0) {
				EmitSentenceByIndex(filter, entIndex, channel, iSentenceIndex, volume,
					soundlevel, flags, pitch, specialDSP, origin, direction, origins, updatePositions, soundTime, speakerEntity);
			}
			else {
				DevWarning(2, $"Unable to find {SoundCharsUtils.SkipSoundChars(sample)} in sentences.txt\n");
			}
		}
		else {
			EmitSoundInternal(filter, entIndex, channel, sample, volume, soundlevel,
				flags, pitch, specialDSP, in origin, in direction, origins, updatePositions, soundTime, speakerEntity);
		}
	}

	private void EmitSoundInternal<T>(T filter, int entIndex, int channel, ReadOnlySpan<char> sample, float volume, SoundLevel soundLevel, SoundFlags flags, int pitch, int specialDSP, in Vector3? origin, in Vector3? direction, List<Vector3> origins, bool updatePositions, double soundTime, int speakerEntity) where T : IRecipientFilter {
		if (volume < 0 || volume > 1) {
			Warning($"EmitSound: volume out of bounds = {volume}\n");
			return;
		}

		if (((int)soundLevel < MIN_SNDLVL_VALUE) || ((int)soundLevel > MAX_SNDLVL_VALUE)) {
			Warning($"EmitSound: soundlevel out of bounds = {soundLevel}\n");
			return;
		}

		if (pitch < 0 || pitch > 255) {
			Warning($"EmitSound: pitch out of bounds = {pitch}\n");
			return;
		}

		Edict? edict = (entIndex >= 0) ? sv!.Edicts![entIndex] : null;
		SV.StartSound(filter, edict, channel, sample, volume, soundLevel,
			flags, pitch, specialDSP, origin, soundTime, speakerEntity, origins);
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

	public TimeUnit_t GetSoundDuration(ReadOnlySpan<char> sample) {
		return Host.GetSoundDuration(sample);
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
		throw new NotImplementedException();
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
