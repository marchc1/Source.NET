using Source.Common;
using Source.Common.Audio;
using Source.Common.Engine;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;
using Source.Common.SoundEmitterSystem;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using System.Threading.Channels;

namespace Source.Engine;

public partial class SV
{
	internal static void StartSound<T>(T filter, Edict? soundEmittingEntity, int channel, ReadOnlySpan<char> sample, float volume, SoundLevel soundLevel, SoundFlags flags, int pitch, int specialDSP, Vector3? origin, double soundTime, int speakerEntity, List<Vector3> origins) where T : IRecipientFilter {
		SoundInfo sound = default;
		sound.SetDefault();

		sound.EntityIndex = soundEmittingEntity != null ? NUM_FOR_EDICT(soundEmittingEntity) : 0;
		sound.Channel = (SoundEntityChannel)channel;
		sound.Volume = volume;
		sound.Soundlevel = soundLevel;
		sound.Flags = flags;
		sound.Pitch = pitch;
		sound.SpecialDSP = specialDSP;
		sound.SpeakerEntity = speakerEntity;

		if ((flags & SoundFlags.Stop) != 0)
			Assert(filter.IsReliable());

		// Compute the sound origin
		if (origin.HasValue) 
			MathLib.VectorCopy(origin.Value, out sound.Origin);
		else if (soundEmittingEntity != null) {
			IServerEntity? serverEntity = soundEmittingEntity.GetIServerEntity();
			if (serverEntity != null) 
				CM.WorldSpaceCenter(serverEntity.GetCollideable()!, out sound.Origin);
		}

		// Add actual sound origin to vector if requested
		if (origins != null)
			origins.Add(sound.Origin);
		

		// set sound delay
		if (soundTime != 0.0) {
			// add one tick since server time ends at the current tick
			// we'd rather delay sounds slightly than skip the beginning samples
			// so add one tick of latency
			soundTime += sv.GetTickInterval();

			sound.Delay = soundTime - sv.GetFinalTickTime();
			sound.Flags |= SoundFlags.Delay;
		}

		// find precache number for sound

		// if this is a sentence, get sentence number
		if (!sample.IsEmpty && SoundCharsUtils.TestSoundChar(sample, SoundChars.Sentence)) {
			sound.IsSentence = true;
			sound.SoundNum = atoi(SoundCharsUtils.SkipSoundChars(sample));
			// todo if (sound.SoundNum >= Vox.SentenceCount()) {
			// todo 	ConMsg("SV_StartSound: invalid sentence number: %s", PSkipSoundChars(pSample));
			// todo 	return;
			// todo }
			Warning("No VOX yet!\n");
			return;
		}
		else {
			sound.IsSentence = false;
			sound.SoundNum = sv.LookupSoundIndex(sample);
			if (0 == sound.SoundNum || sv.GetSound(sound.SoundNum).IsEmpty) {
				ConMsg($"SV.StartSound: {sample} not precached ({sound.SoundNum})\n");
				return;
			}
		}

		// now sound message is complete, send to clients in filter
		sv.BroadcastSound(sound, filter);
	}

	public void ClearWorld() {
#if !SWDS
		g_ShadowMgr.LevelShutdown();
#endif
		StaticPropMgr().LevelShutdown();

		for (int i = 0; i < 3; i++)
			if (host_state.WorldModel!.Mins[i] < MIN_COORD_INTEGER || host_state.WorldModel!.Maxs[i] > MAX_COORD_INTEGER)
				Host.EndGame(true, "Map coordinate extents are too large!!\nCheck for errors!\n");

		SpatialPartition().Init(host_state.WorldModel!.Mins, host_state.WorldModel!.Maxs);

		StaticPropMgr().LevelInit();
#if !SWDS
		g_ShadowMgr.LevelInit(host_state.WorldBrush!.NumSurfaces);
#endif
	}
}
