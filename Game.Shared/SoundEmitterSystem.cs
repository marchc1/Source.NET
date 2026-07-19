#if CLIENT_DLL || GAME_DLL
#if CLIENT_DLL
global using static Game.Client.SoundEmitterSystemGlobals;

#else
global using static Game.Server.SoundEmitterSystemGlobals;
#endif
using Game.Shared;

using SharpCompress.Common;

using Source.Common;
using Source.Common.Audio;
using Source.Common.SoundEmitterSystem;

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;

#if CLIENT_DLL
namespace Game.Client;
#else
namespace Game.Server;
#endif

public class SoundEmitterSystem : BaseGameSystem
{
	public override ReadOnlySpan<char> Name() => "CSoundEmitterSystem";

	public override bool Init() {
		Assert(soundemitterbase != null);
		return soundemitterbase.ModInit();
	}

	public override void Shutdown() {
		Assert(soundemitterbase != null);
		soundemitterbase.ModShutdown();
	}

	internal void EmitSoundByHandle<T>(T filter, int entIndex, in EmitSound_t ep, ref HSOUNDSCRIPTHANDLE handle) where T : IRecipientFilter {
		SoundParameters parms = default;

		// Try to deduce the actor's gender
		Gender gender = Gender.None;
		BaseEntity? ent = BaseEntity.Instance(entIndex);
		if (ent != null) {
			ReadOnlySpan<char> actorModel = ent.GetModelName();
			gender = soundemitterbase.GetActorGender(actorModel);
		}

		if (!soundemitterbase.GetParametersForSoundEx(ep.SoundName, ref handle, ref parms, gender, true))
			return;

		if (((ReadOnlySpan<char>)parms.SoundName).IsStringEmpty)
			return;

		// handle SND_CHANGEPITCH/SND_CHANGEVOL and other sound flags.etc.
		if ((ep.Flags & SoundFlags.ChangePitch) != 0)
			parms.Pitch = ep.Pitch;

		if ((ep.Flags & SoundFlags.ChangeVolume) != 0)
			parms.Volume = ep.Volume;

		// TODO: CEnvMicrophone

		TimeUnit_t st = ep.SoundTime;
		if (st == 0 && parms.DelayMsec != 0)
			st = gpGlobals.CurTime + parms.DelayMsec / 1000d;

		enginesound.EmitSound(
			filter,
			entIndex,
			(int)parms.Channel,
			parms.SoundName,
			parms.Volume,
			(float)parms.SoundLevel,
			ep.Flags,
			parms.Pitch,
			ep.SpecialDSP,
			in ep.Origin,
			Unsafe.NullRef<Vector3>(),
			ep.SoundOrigin,
			true,
			st,
			ep.SpeakerEntity);

		if (!Unsafe.IsNullRef(ref ep.SoundDuration))
			ep.SoundDuration = enginesound.GetSoundDuration(parms.SoundName);

		// if (0 == (ep.Flags & (SoundFlags.ChangePitch | SoundFlags.ChangeVolume)))
		// EmitCloseCaption(filter, entindex, params, ep);
	}

	public void EmitSound<T>(scoped in T filter, int entindex, ref EmitSound_t ep) where T : IRecipientFilter {
		if (!ep.SoundName.IsEmpty &&
			(!stristr(ep.SoundName, ".wav").IsEmpty ||
			 !stristr(ep.SoundName, ".mp3").IsEmpty ||
			 ep.SoundName[0] == '!')) {
#if !CLIENT_DLL
			// TODO: CEnvMicrophone
#endif

			if (ep.WarnOnDirectWaveReference && !stristr(ep.SoundName, ".wav").IsEmpty) {
				// WaveTrace( ep.SoundName, "Emitsound" );
			}

			enginesound.EmitSound(
				filter,
				entindex,
				ep.Channel,
				ep.SoundName,
				ep.Volume,
				ep.SoundLevel,
				ep.Flags,
				ep.Pitch,
				ep.SpecialDSP,
				in ep.Origin,
				Unsafe.NullRef<Vector3>(),
				ep.SoundOrigin,
				true,
				ep.SoundTime,
				ep.SpeakerEntity);
			if (!Unsafe.IsNullRef(ref ep.SoundDuration))
				ep.SoundDuration = enginesound.GetSoundDuration(ep.SoundName);

			// TraceEmitSound( "EmitSound:  Raw wave emitted '%s' (ent %i)\n", ep.SoundName, entindex );
			return;
		}

		if (ep.SoundScriptHandle == SOUNDEMITTER_INVALID_HANDLE)
			ep.SoundScriptHandle = (HSOUNDSCRIPTHANDLE)soundemitterbase.GetSoundIndex(ep.SoundName);

		if (ep.SoundScriptHandle == -1)
			return;

		EmitSoundByHandle(filter, entindex, ep, ref ep.SoundScriptHandle);
	}

	internal void StopSoundByHandle(int entindex, ReadOnlySpan<char> soundname, ref HSOUNDSCRIPTHANDLE handle) {
		if (handle == SOUNDEMITTER_INVALID_HANDLE)
			handle = (HSOUNDSCRIPTHANDLE)soundemitterbase.GetSoundIndex(soundname);

		if (handle == SOUNDEMITTER_INVALID_HANDLE)
			return;

		ref SoundParametersInternal parms = ref soundemitterbase.InternalGetParametersForSound(handle);
		if (Unsafe.IsNullRef(ref parms))
			return;

		int c = parms.NumSoundNames();
		for (int i = 0; i < c; ++i) {
			ReadOnlySpan<char> wavename = soundemitterbase.GetWaveName(parms.GetSoundNames()[i].Symbol);
			Assert(!wavename.IsEmpty);

			enginesound.StopSound(
				entindex,
				(int)parms.GetChannel(),
				wavename);

			// TraceEmitSound( "StopSound:  '%s' stopped as '%s' (ent %i)\n", soundname, wavename, entindex );
		}
	}

	public void StopSound(int entindex, ReadOnlySpan<char> soundname) {
		HSOUNDSCRIPTHANDLE handle = (HSOUNDSCRIPTHANDLE)soundemitterbase.GetSoundIndex(soundname);
		if (handle == SOUNDEMITTER_INVALID_HANDLE)
			return;

		StopSoundByHandle(entindex, soundname, ref handle);
	}

	public void StopSound(int entindex, int channel, ReadOnlySpan<char> sample) {
		if (!sample.IsEmpty && (!stristr(sample, ".wav").IsEmpty || !stristr(sample, ".mp3").IsEmpty || sample[0] == '!')) {
			enginesound.StopSound(entindex, channel, sample);

			// TraceEmitSound( "StopSound:  Raw wave stopped '%s' (ent %i)\n", sample, entindex );
		}
		else
			StopSound(entindex, sample);
	}
}

public static class SoundEmitterSystemGlobals
{
	public static readonly SoundEmitterSystem g_SoundEmitterSystem = new();
}

public partial class
#if CLIENT_DLL
C_BaseEntity
#else
BaseEntity
#endif
{
	public static void StopSound(int entIndex, int channel, ReadOnlySpan<char> sample) => g_SoundEmitterSystem.StopSound(entIndex, channel, sample);

	public static SoundLevel LookupSoundLevel(ReadOnlySpan<char> soundname) {
		return soundemitterbase.LookupSoundLevel(soundname);
	}
	public static SoundLevel LookupSoundLevel(ReadOnlySpan<char> soundname, ref HSOUNDSCRIPTHANDLE handle) {
		return soundemitterbase.LookupSoundLevelByHandle(soundname, ref handle);
	}
	public void EmitSound(ReadOnlySpan<char> soundname, TimeUnit_t soundtime = 0) {
		PASAttenuationFilter filter = new(this, soundname);

		scoped EmitSound_t parms = default;
		parms.SoundName = soundname;
		parms.SoundTime = soundtime;
		parms.SoundDuration = ref Unsafe.NullRef<TimeUnit_t>();
		parms.WarnOnDirectWaveReference = true;

		BaseEntity.EmitSound(filter, EntIndex(), in parms);
	}

	public void EmitSound(ReadOnlySpan<char> soundname, TimeUnit_t soundtime, out TimeUnit_t duration) {
		duration = default;
		PASAttenuationFilter filter = new(this, soundname);

		scoped EmitSound_t parms = default;
		parms.SoundName = soundname;
		parms.SoundTime = soundtime;
		parms.SoundDuration = ref duration;
		parms.WarnOnDirectWaveReference = true;

		BaseEntity.EmitSound(filter, EntIndex(), in parms);
	}

	public static void EmitSound<T>(in T filter, int entIndex, ReadOnlySpan<char> soundname, in Vector3 origin, TimeUnit_t soundtime, out TimeUnit_t duration) where T : IRecipientFilter {
		duration = default;
		if (soundname.IsStringEmpty)
			return;

		scoped EmitSound_t parms = new();
		parms.SoundName = soundname;
		parms.SoundTime = soundtime;
		parms.Origin = ref origin;
		parms.SoundDuration = ref duration;
		parms.WarnOnDirectWaveReference = true;

		EmitSound(filter, entIndex, ref parms, ref parms.SoundScriptHandle);
	}

	public static void EmitSound<T>(in T filter, int entIndex, ref EmitSound_t parms, ref HSOUNDSCRIPTHANDLE handle) where T : IRecipientFilter {
#if GAME_DLL
		BaseEntity? entity = Util.EntityByIndex(entIndex);
#else
		C_BaseEntity? entity = cl_entitylist.GetEnt(entIndex);
#endif
#if CLIENT_DLL || GAME_DLL
		entity?.ModifyEmitSoundParams(ref parms);
#endif
		// VPROF( "CBaseEntity::EmitSound" );
		// Call into the sound emitter system...
		g_SoundEmitterSystem.EmitSoundByHandle(filter, entIndex, parms, ref handle);
	}

	public bool GetParametersForSound(ReadOnlySpan<char> soundName, ref SoundParameters parms, ReadOnlySpan<char> actorModel) {
		Gender gender = soundemitterbase.GetActorGender(actorModel);
		return soundemitterbase.GetParametersForSound(soundName, ref parms, gender);
	}
}
#endif
