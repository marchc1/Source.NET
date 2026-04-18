#if CLIENT_DLL || GAME_DLL
using Game.Shared;

using Source.Common.Audio;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

#if CLIENT_DLL
namespace Game.Client;
#else
namespace Game.Server;
#endif

public partial class
#if CLIENT_DLL
C_BaseEntity
#else
BaseEntity
#endif
{
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
}
#endif
