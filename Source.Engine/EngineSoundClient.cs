using Source.Common;
using Source.Common.Audio;
using Source.Common.Engine;
using Source.Common.MaterialSystem;

using System.Numerics;

namespace Source.Engine;

public class EngineSoundClient(Sound Sound) : IEngineSound
{
	public void EmitAmbientSound(ReadOnlySpan<char> pSample, float volume, int pitch = 100, int flags = 0, double soundTime = 0) {
		throw new NotImplementedException();
	}

	public void EmitSentenceByIndex<T>(scoped in T filter, int entIndex, int channel, int iSentenceIndex, float volume, SoundLevel soundlevel, SoundFlags flags = SoundFlags.NoFlags, int pitch = 100, int specialDSP = 0, in Vector3 origin = default, in Vector3 direction = default, ReadOnlySpan<Vector3> origins = default, bool updatePositions = true, double soundTime = 0, int speakerEntity = -1) where T : IRecipientFilter {
		throw new NotImplementedException();
	}

	public void EmitSound<T>(scoped in T filter, int entIndex, int channel, ReadOnlySpan<char> sample, float volume, float attenuation, SoundFlags flags = SoundFlags.NoFlags, int pitch = 100, int specialDSP = 0, in Vector3 origin = default, in Vector3 direction = default, ReadOnlySpan<Vector3> origins = default, bool updatePositions = true, double soundTime = 0, int speakerEntity = -1) where T : IRecipientFilter {
		throw new NotImplementedException();
	}

	public void EmitSound<T>(scoped in T filter, int entIndex, int channel, ReadOnlySpan<char> sample, float volume, SoundLevel soundlevel, SoundFlags flags = SoundFlags.NoFlags, int pitch = 100, int specialDSP = 0, in Vector3 origin = default, in Vector3 direction = default, ReadOnlySpan<Vector3> origins = default, bool updatePositions = true, double soundTime = 0, int speakerEntity = -1) where T : IRecipientFilter {
		throw new NotImplementedException();
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
		throw new NotImplementedException();
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
