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
	readonly IAudioSystem? g_AudioSystem = OptionalSingleton<IAudioSystem>();

	public void EmitAmbientSound(ReadOnlySpan<char> pSample, float volume, int pitch = 100, int flags = 0, double soundTime = 0) {
		AssertMsg(false, "Not supported");
	}

	public void EmitSentenceByIndex<T>(scoped in T filter, int entIndex, int channel, int iSentenceIndex, float volume, SoundLevel soundlevel, SoundFlags flags = SoundFlags.NoFlags, int pitch = 100, int specialDSP = 0, in Vector3? origin = default, in Vector3? direction = default, List<Vector3>? origins = default, bool updatePositions = true, double soundTime = 0, int speakerEntity = -1) where T : IRecipientFilter {
		if (iSentenceIndex >= 0) {
			string pName = $"!{iSentenceIndex}";
			EmitSoundInternal(filter, entIndex, channel, pName, volume, soundlevel,
				flags, pitch, specialDSP, in origin, in direction, origins!, updatePositions, soundTime, speakerEntity);
		}
	}

	public void EmitSound<T>(scoped in T filter, int entIndex, int channel, ReadOnlySpan<char> sample, float volume, float attenuation, SoundFlags flags = SoundFlags.NoFlags, int pitch = 100, int specialDSP = 0, in Vector3? origin = default, in Vector3? direction = default, List<Vector3>? origins = default, bool updatePositions = true, double soundTime = 0, int speakerEntity = -1) where T : IRecipientFilter {
		EmitSound(filter, entIndex, channel, sample, volume, ATTN_TO_SNDLVL(attenuation), flags,
		pitch, specialDSP, in origin, in direction, origins, updatePositions, soundTime, speakerEntity);
	}



	public void EmitSound<T>(scoped in T filter, int entIndex, int channel, ReadOnlySpan<char> sample, float volume, SoundLevel soundlevel, SoundFlags flags = SoundFlags.NoFlags, int pitch = 100, int specialDSP = 0, in Vector3? origin = default, in Vector3? direction = default, List<Vector3>? origins = default, bool updatePositions = true, double soundTime = 0, int speakerEntity = -1) where T : IRecipientFilter {
		if (!sample.IsEmpty && SoundCharsUtils.TestSoundChar(sample, SoundChars.Sentence)) {
			int iSentenceIndex = -1;
			g_AudioSystem?.LookupSentence(SoundCharsUtils.SkipSoundChars(sample), out iSentenceIndex);
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

	// Retrieves list of all active sounds
	public void GetActiveSounds(List<SndInfo> sndlist) {
		Warning("Can't call GetActiveSounds from server\n");
		return;
	}

	public float GetDistGainFromSoundLevel(SoundLevel soundlevel, float dist) {
		return SndGain.S_GetGainFromSoundLevel(soundlevel, dist);
	}

	// Client .dll only functions
	public int GetGuidForLastSoundEmitted() {
		Warning("Can't call GetGuidForLastSoundEmitted from server\n");
		return 0;
	}

	public TimeUnit_t GetSoundDuration(ReadOnlySpan<char> sample) {
		return Host.GetSoundDuration(sample);
	}

	public bool IsSoundPrecached(ReadOnlySpan<char> sample) {
		if (!sample.IsEmpty && SoundCharsUtils.TestSoundChar(sample, SoundChars.Sentence))
			return true;

		return sv.LookupSoundIndex(sample) != -1;
	}

	public bool IsSoundStillPlaying(int guid) {
		Warning("Can't call IsSoundStillPlaying from server\n");
		return false;
	}

	public void NotifyBeginMoviePlayback() {
		AssertMsg(false, "Not supported");
	}

	public void NotifyEndMoviePlayback() {
		AssertMsg(false, "Not supported");
	}

	public void PrecacheSentenceGroup(ReadOnlySpan<char> groupName) {
		g_AudioSystem?.PrecacheSentenceGroup(this, groupName, null);
	}

	public bool PrecacheSound(ReadOnlySpan<char> sample, bool preload = false, bool isUISound = false) {
		if (!sample.IsEmpty && SoundCharsUtils.TestSoundChar(sample, SoundChars.Sentence))
			return true;

		if (sample.IsEmpty || sample[0] <= ' ') {
			Warning($"CEngineSoundServer::PrecacheSound:  Bad string: {sample}\n");
			return false;
		}

		// add the sound to the precache list
		Res flags = Res.FatalIfMissing;
		if (preload)
			flags |= Res.Preload;

		int i = sv.PrecacheSound(sample, flags);
		if (i >= 0)
			return true;

		Warning($"CEngineSoundServer::PrecacheSound: '{sample}' overflow\n");
		return false;
	}

	public void PrefetchSound(ReadOnlySpan<char> sample) {
		if (!sample.IsEmpty && SoundCharsUtils.TestSoundChar(sample, SoundChars.Sentence))
			return;

		// Clients prefetch on their own once precached; the server just needs the index to exist.
		sv.LookupSoundIndex(sample);
	}

	void BuildRecipientList<T>(List<Edict> list, in T filter) where T : IRecipientFilter {
		int c = filter.GetRecipientCount();
		for (int i = 0; i < c; i++) {
			int playerindex = filter.GetRecipientIndex(i);

			if (playerindex < 1 || playerindex > sv.GetClientCount())
				continue;

			Server.GameClient cl = sv.Client(playerindex - 1);
			// Never output to bots
			if (cl.IsFakeClient())
				continue;

			if (!cl.IsSpawned())
				continue;

			list.Add(cl.Edict);
		}
	}

	public void SetPlayerDSP<T>(scoped in T filter, int dspType, bool fastReset) where T : IRecipientFilter {
		Assert(!fastReset);
		if (fastReset)
			Warning("SetPlayerDSP:  fastReset only valid from client\n");

		List<Edict> players = [];
		BuildRecipientList(players, in filter);

		for (int i = 0; i < players.Count; i++)
			engine.ClientCommand(players[i], $"dsp_player {dspType}\n");
	}

	// Set the room type for a player
	public void SetRoomType<T>(scoped in T filter, int roomType) where T : IRecipientFilter {
		List<Edict> players = [];
		BuildRecipientList(players, in filter);

		for (int i = 0; i < players.Count; i++)
			engine.ClientCommand(players[i], $"room_type {roomType}\n");
	}

	public void SetVolumeByGuid(int guid, float fvol) {
		Warning("Can't call SetVolumeByGuid from server\n");
		return;
	}

	public void StopAllSounds(bool clearBuffers) {
		AssertMsg(false, "Not supported");
	}

	public void StopSound(int entIndex, int channel, ReadOnlySpan<char> pSample) {
		EngineRecipientFilter filter = new();
		filter.AddAllPlayers();
		filter.MakeReliable();

		EmitSound(filter, entIndex, channel, pSample, 0, SoundLevel.LvlNone, SoundFlags.Stop, PITCH_NORM, 0,
			null, null, null, true);
	}

	public void StopSoundByGuid(int guid) {
		Warning("Can't call StopSoundByGuid from server\n");
		return;
	}
}
