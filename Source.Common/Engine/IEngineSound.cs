using Source.Common.Audio;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Source.Common.Engine;

public static class EngineSoundGlobals {
	public const int SOUND_FROM_UI_PANEL = -2;      // Sound being played inside a UI panel on the client
	public const int SOUND_FROM_LOCAL_PLAYER = -1;
	public const int SOUND_FROM_WORLD = 0;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static SoundLevel SNDLEVEL_TO_COMPATIBILITY_MODE(int x) => (SoundLevel)(int)(x + 256);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static SoundLevel SNDLEVEL_FROM_COMPATIBILITY_MODE(int x) => (SoundLevel)(int)(x - 256);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static SoundLevel SNDLEVEL_TO_COMPATIBILITY_MODE(SoundLevel x) => (SoundLevel)(int)(x + 256);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static SoundLevel SNDLEVEL_FROM_COMPATIBILITY_MODE(SoundLevel x) => (SoundLevel)(int)(x - 256);
}

public interface IEngineSound
{
	// Precache a particular sample
	bool PrecacheSound( ReadOnlySpan<char> sample, bool preload = false, bool isUISound = false );
	bool IsSoundPrecached( ReadOnlySpan<char> sample);
	void PrefetchSound( ReadOnlySpan<char> sample);

	// Just loads the file header and checks for duration (not hooked up for .mp3's yet)
	// Is accessible to server and client though
	float GetSoundDuration( ReadOnlySpan<char> sample );

	// Pitch of 100 is no pitch shift.  Pitch > 100 up to 255 is a higher pitch, pitch < 100
	// down to 1 is a lower pitch.   150 to 70 is the realistic range.
	// EmitSound with pitch != 100 should be used sparingly, as it's not quite as
	// fast (the pitchshift mixer is not native coded).

	// NOTE: setting iEntIndex to -1 will cause the sound to be emitted from the local
	// player (client-side only)
	void EmitSound<T>(scoped in T filter, int entIndex, int channel, ReadOnlySpan<char> sample,
		float volume, float attenuation, SoundFlags flags = 0, int pitch = PITCH_NORM, int specialDSP = 0,
		in Vector3 origin = default, in Vector3 direction = default, ReadOnlySpan<Vector3> origins = default, bool updatePositions = true, TimeUnit_t soundTime = 0.0f, int speakerEntity = -1 ) where T : IRecipientFilter;

	void EmitSound<T>(scoped in T filter, int entIndex, int channel, ReadOnlySpan<char> sample,
		float volume, SoundLevel soundlevel, SoundFlags flags = 0, int pitch = PITCH_NORM, int specialDSP = 0,
		in Vector3 origin = default, in Vector3 direction = default, ReadOnlySpan<Vector3> origins = default, bool updatePositions = true, TimeUnit_t soundTime = 0.0f, int speakerEntity = -1 ) where T : IRecipientFilter;

	void EmitSentenceByIndex<T>(scoped in T filter, int entIndex, int channel, int iSentenceIndex,
		float volume, SoundLevel soundlevel, SoundFlags flags = 0, int pitch = PITCH_NORM, int specialDSP = 0,
		in Vector3 origin = default, in Vector3 direction = default, ReadOnlySpan<Vector3> origins = default, bool updatePositions = true, TimeUnit_t soundTime = 0.0f, int speakerEntity = -1 ) where T : IRecipientFilter;

	void StopSound(int entIndex, int channel, ReadOnlySpan<char> pSample );

	// stop all active sounds (client only)
	void StopAllSounds(bool clearBuffers);

	// Set the room type for a player (client only)
	void SetRoomType<T>(scoped in T filter, int roomType) where T : IRecipientFilter;

	// Set the dsp preset for a player (client only)
	void SetPlayerDSP<T>(scoped in T filter, int dspType, bool fastReset) where T : IRecipientFilter;

	// emit an "ambient" sound that isn't spatialized
	// only available on the client, assert on server
	void EmitAmbientSound( ReadOnlySpan<char> pSample, float volume, int pitch = PITCH_NORM, int flags = 0, TimeUnit_t soundTime = 0.0f );


	//	EntChannel_t	CreateEntChannel();

	float GetDistGainFromSoundLevel(SoundLevel soundlevel, float dist);

	// Client .dll only functions
	int GetGuidForLastSoundEmitted();
	bool IsSoundStillPlaying(int guid);
	void StopSoundByGuid(int guid);
	// Set's master volume (0.0->1.0)
	void SetVolumeByGuid(int guid, float fvol);

	// Retrieves list of all active sounds
	// This differentiates from Source since C# doesn't like putting ref structs in spans etc
	int GetActiveSoundCount();
	ref SndInfo GetActiveSound();

	void PrecacheSentenceGroup( ReadOnlySpan<char> groupName );
	void NotifyBeginMoviePlayback();
	void NotifyEndMoviePlayback();
}
