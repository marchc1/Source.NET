global using static Source.AudioSystem.SndDma;

using Source.Common;
using Source.Common.Audio;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Filesystem;
using Source.Common.Formats.BSP;
using Source.Common.Mathematics;
using Source.Common.Physics;

using System.Numerics;
using System.Runtime.CompilerServices;

using static Source.Common.Audio.SndGain;

namespace Source.AudioSystem;

//-----------------------------------------------------------------------------
// Purpose: Main control for any streaming sound output device.
//-----------------------------------------------------------------------------
public static partial class SndDma
{
	public const int MAX_SFX = 2048;

	public static readonly IPhysicsSurfaceProps? physprop = OptionalSingleton<IPhysicsSurfaceProps>();

	static IEngineTrace? _engineTraceClient;
	static IEngineTrace g_pEngineTraceClient => _engineTraceClient ??= KeyedSingleton<IEngineTrace>(Realm.Client);

	static IVDebugOverlay? _debugOverlay;
	static IVDebugOverlay? debugoverlay => _debugOverlay ??= OptionalSingleton<IVDebugOverlay>();

	// =======================================================================
	// Internal sound data & structures
	// =======================================================================

	static double g_LastSoundFrame = 0.0f;      // last full frame of sound
	static double g_LastMixTime = 0.0f;         // last time we did mixing
	static float g_EstFrameTime = 0.1f;         // estimated frame time running average

	// x360 override to fade out game music when the user is playing music through the dashboard
	static float g_DashboardMusicMixValue = 1.0f;
	static float g_DashboardMusicMixTarget = 1.0f;
	const float g_DashboardMusicFadeRate = 0.5f;    // Fades one half full-scale volume per second (two seconds for complete fadeout)

	// sound mixers
	public static int g_csoundmixers = 0;                   // total number of soundmixers found
	public static int g_cgrouprules = 0;                    // total number of group rules found
	public static int g_cgroupclass = 0;

	// this is used to enable/disable music playback on x360 when the user selects his own soundtrack to play
	static void FixSlashesForward(Span<char> name) {
		for (int i = 0; i < name.Length; i++) {
			if (name[i] == '\\')
				name[i] = '/';
		}
	}

	public static void S_EnableMusic(bool bEnable) {
		if (bEnable)
			g_DashboardMusicMixTarget = 1.0f;
		else
			g_DashboardMusicMixTarget = 0.0f;
	}

	public static bool IsSoundSourceLocalPlayer(int soundsource) {
		if (soundsource == EngineSoundGlobals.SOUND_FROM_UI_PANEL)
			return true;

		return soundsource == soundServices.GetViewEntity();
	}

	static readonly Lock g_SndMutex = new();

	const Mask MASK_BLOCK_AUDIO = (Mask)(Contents.Solid | Contents.Moveable | Contents.Window);

	public static bool snd_initialized = false;

	public static Vector3 listener_origin;
	static Vector3 listener_forward;
	public static Vector3 listener_right;
	static Vector3 listener_up;
	static bool s_bIsListenerUnderwater;
	static float sound_nominal_clip_dist = SOUND_NORMAL_CLIP_DIST;

	public const float SOUND_NORMAL_CLIP_DIST = 1000.0f;

	// @TODO (toml 05-08-02): put this somewhere more reasonable
	public static float S_GetNominalClipDist() {
		return sound_nominal_clip_dist;
	}

	public static int g_soundtime = 0;      // sample PAIRS output since start
	public static int g_paintedtime = 0;        // sample PAIRS mixed since start

	public static float g_ReplaySoundTimeFracAccumulator = 0.0f;    // Used by replay

	static readonly float[] g_ClockSyncArray = new float[(int)ClockSyncIndex.Num];
	static readonly int[] g_SoundClockPaintTime = new int[(int)ClockSyncIndex.Num];

	// default 10ms
	public static readonly ConVar snd_delay_sound_shift = new("snd_delay_sound_shift", "0.01");
	// this forces the clock to resync on the next delayed/sync sound
	public static void S_SyncClockAdjust(ClockSyncIndex syncIndex) {
		g_ClockSyncArray[(int)syncIndex] = 0;
		g_SoundClockPaintTime[(int)syncIndex] = 0;
	}

	public static float S_ComputeDelayForSoundtime(float soundtime, ClockSyncIndex syncIndex) {
		// reset clock and return 0
		if (g_ClockSyncArray[(int)syncIndex] == 0) {
			// Put the current time marker one tick back to impose a minimum delay on the first sample
			// this shifts the drift over so the sounds are more likely to delay (rather than skip)
			// over the burst
			// NOTE: The first sound after a sync MUST have a non-zero delay for the delay channel
			// detection logic to work (otherwise we keep resetting the clock)
			g_ClockSyncArray[(int)syncIndex] = soundtime - (float)soundServices.GetIntervalPerTick();
			g_SoundClockPaintTime[(int)syncIndex] = g_paintedtime;
		}

		// how much time has passed in the game since we did a clock sync?
		float gameDeltaTime = soundtime - g_ClockSyncArray[(int)syncIndex];

		// how many samples have been mixed since we did a clock sync?
		int paintedSamples = g_paintedtime - g_SoundClockPaintTime[(int)syncIndex];
		int dmaSpeed = g_AudioDevice!.DeviceDmaSpeed();
		int gameSamples = (int)(gameDeltaTime * dmaSpeed);
		int delaySamples = gameSamples - paintedSamples;
		float delay = delaySamples / (float)dmaSpeed;

		if (gameDeltaTime < 0 || MathF.Abs(delay) > 0.500f) {
			// Note that the equations assume a correlation between game time and real time
			// some kind of clock error.  This can happen with large host_timescale or when the
			// framerate hitches drastically (game time is a smaller clamped value wrt real time).
			// The current sync estimate has probably drifted due to this or some other problem, recompute.
			//Msg("Clock ERROR!: %.2f %.2f\n", gameDeltaTime, delay);
			S_SyncClockAdjust(syncIndex);
			return 0;
		}
		return delay + snd_delay_sound_shift.GetFloat();
	}

	static int s_buffers = 0;
	static int s_oldsampleOutCount = 0;
	static float s_lastsoundtime = 0.0f;

	public static bool s_bOnLoadScreen = false;

	static readonly Lock g_pSoundPoolMutex = new(); // Mutex since s_SoundPool.Alloc is not threadsafe

	static readonly SortedDictionary<FileNameHandle_t, SfxTable> s_Sounds = [];

	class DummySfx : SfxTable
	{
		string name = "";

		public override ReadOnlySpan<char> GetName() {
			return name;
		}

		public void SetName(ReadOnlySpan<char> pName) {
			name = new(pName.SliceNullTerminatedString());
			OnNameChanged(name);
		}
	}

	static readonly DummySfx dummySfx = new();

	// returns true if ok to procede with TraceRay calls
	public static bool SND_IsInGame() {
		return soundServices.IsClientActive();
	}

	internal static void SfxTable_Init() {
		SfxTable.Impl.GetName = SfxTable_GetName;
		SfxTable.Impl.GetFileNameHandle = SfxTable_GetFileNameHandle;
		SfxTable.Impl.IsPrecachedSound = SfxTable_IsPrecachedSound;
		SfxTable.Impl.OnNameChanged = SfxTable_OnNameChanged;
		SfxTable.Impl.IsValidNamePoolIndex = SfxTable_IsValidNamePoolIndex;
	}

	static bool SfxTable_IsValidNamePoolIndex(FileNameHandle_t index) => s_Sounds.ContainsKey(index);

	static void SfxTable_OnNameChanged(SfxTable self, ReadOnlySpan<char> pName) {
		if (!pName.IsEmpty && g_cgrouprules != 0) {
			Span<char> szString = stackalloc char[MAX_PATH];
			strcpy(szString, pName);
			FixSlashesForward(szString);
			self.MixGroupCount = (byte)MXR_GetMixGroupListFromDirName(szString, self.MixGroupList, self.MixGroupList.Length);
			self.MixGroupsCached = true;
		}
	}

	//-----------------------------------------------------------------------------
	// Purpose: Wrapper for sfxtable->getname()
	// Output : char const
	//-----------------------------------------------------------------------------
	static ReadOnlySpan<char> SfxTable_GetName(SfxTable self) {
		if (s_Sounds.ContainsKey(self.NamePoolIndex)) {
			if (filesystem != null)
				return filesystem.String(self.NamePoolIndex);
			return "";
		}

		return null;
	}

	static FileNameHandle_t SfxTable_GetFileNameHandle(SfxTable self) {
		if (s_Sounds.ContainsKey(self.NamePoolIndex))
			return self.NamePoolIndex;

		return 0;
	}

	static bool SfxTable_IsPrecachedSound(SfxTable self) {
		ReadOnlySpan<char> pName = self.GetName();

		if (soundServices.IsServerActive()) {
			// Server uses zero to mark invalid sounds
			return soundServices.LookupServerSoundIndex(pName) != 0;
		}

		// Client uses -1
		// WE SHOULD FIX THIS!!!
		return soundServices.LookupClientSoundIndex(pName) != -1;
	}

	public static float g_DuckScale = 1.0f;

	// Structure used for fading in and out client sound volume.
	struct SoundFade
	{
		public float initial_percent;

		// How far to adjust client's volume down by.
		public float percent;

		// GetHostTime() when we started adjusting volume
		public float starttime;

		// # of seconds to get to faded out state
		public float fadeouttime;
		// # of seconds to hold
		public float holdtime;
		// # of seconds to restore
		public float fadeintime;
	}

	static SoundFade soundfade;  // Client sound fading singleton object

	// 0)headphones 2)stereo speakers 4)quad 5)5point1
	// autodetected from windows settings
	public static readonly ConVar snd_surround = new("snd_surround_speakers", "-1", FCvar.InternalUse);
	public static readonly ConVar snd_legacy_surround = new("snd_legacy_surround", "0", FCvar.Archive);
	public static readonly ConVar snd_noextraupdate = new("snd_noextraupdate", "0");
	public static readonly ConVar snd_show = new("snd_show", "0", FCvar.Cheat, "Show sounds info");
	public static readonly ConVar snd_visualize = new("snd_visualize", "0", FCvar.Cheat, "Show sounds location in world");
	public static readonly ConVar snd_pitchquality = new("snd_pitchquality", "1", FCvar.Archive);      // 1) use high quality pitch shifters

	// master volume
	static readonly ConVar volume = new("volume", "1.0", FCvar.Archive, "Sound volume", 0.0, 1.0);

	public static readonly ConVar snd_mixahead = new("snd_mixahead", "0.1", FCvar.Archive);
	public static readonly ConVar snd_mix_async = new("snd_mix_async", "0");
#if DEBUG
	[ConCommand("snd_mixvol", "Set named Mixgroup to mix volume.")]
	static void snd_mixvol(in TokenizedCommand args) => MXR_DebugSetMixGroupVolume(in args);
#endif

	//-----------------------------------------------------------------------------
	// Purpose:
	// Output : float
	//-----------------------------------------------------------------------------
	public static float S_GetMasterVolume() {
		float scale = 1.0f;
		if (soundfade.percent != 0) {
			scale = Math.Clamp(soundfade.percent / 100.0f, 0.0f, 1.0f);
			scale = 1.0f - scale;
		}
		return volume.GetFloat() * scale;
	}

	[ConCommand(helpText: "Describe the current sound device.")]
	static void soundinfo() {
		if (!g_AudioDevice!.IsActive()) {
			Msg("Sound system not started\n");
			return;
		}

		Msg($"Sound Device:   {g_AudioDevice.DeviceName()}\n");
		Msg($"  Channels:     {g_AudioDevice.DeviceChannels()}\n");
		Msg($"  Samples:      {g_AudioDevice.DeviceSampleCount()}\n");
		Msg($"  Bits/Sample:  {g_AudioDevice.DeviceSampleBits()}\n");
		Msg($"  Rate:         {g_AudioDevice.DeviceDmaSpeed()}\n");
		Msg($"total_channels: {total_channels}\n");
	}


	/*
	================
	S_Startup
	================
	*/

	public static void S_Startup() {
		if (!snd_initialized)
			return;

		if (g_AudioDevice == null || g_AudioDevice == Audio_GetNullDevice()) {
			g_AudioDevice = AutoDetectInit(false);
			if (g_AudioDevice == null)
				Error("Unable to init audio");
		}
	}

	[ConCommand(helpText: "Play a sound.", flags: FCvar.ServerCanExecute)]
	static void play(in TokenizedCommand args) => S_Play(in args);
	[ConCommand(helpText: "Play a sound, reloading from disk in case of changes.")]
	static void playflush(in TokenizedCommand args) => S_Play(in args);
	[ConCommand(helpText: "Play a sound at a specified volume.")]
	static void playvol(in TokenizedCommand args) => S_PlayVol(in args);
	[ConCommand(helpText: "Play a constructed sentence.")]
	static void speak(in TokenizedCommand args) => S_Say(in args);
	// Marked cheat because it gives an advantage to players minimising ambient noise.
	[ConCommand(flags: FCvar.Cheat)]
	static void stopsound() => S_StopAllSoundsC();
	[ConCommand(helpText: "List all known sounds.")]
	static void soundlist() => S_SoundList();

	public static bool IsValidSampleRate(int rate) {
		return rate == SOUND_11k || rate == SOUND_22k || rate == SOUND_44k;
	}

	public static void VAudioInit() {
		vaudio ??= new VAudioNLayer();
	}

	/*
	================
	S_Init
	================
	*/
	public static void S_Init() {
		if (soundServices.IsDedicated() && !CommandLine.CheckParm("-forcesound"))
			return;

		DevMsg("Sound Initialization: Start\n");

		// KDB: init sentence array
		VOX_Init();

		VAudioInit();

		if (CommandLine.CheckParm("-nosound")) {
			g_AudioDevice = Audio_GetNullDevice();
			audiosourcecache.Init((nuint)(soundServices.GetMemSize() >> 2));
			return;
		}

		snd_initialized = true;

		g_ActiveChannels.Init();
		S_Startup();

		MIX_InitAllPaintbuffers();

		SND_InitScaletable();

		MXR_LoadAllSoundMixers();

		S_StopAllSounds(true);

		audiosourcecache.Init((nuint)(soundServices.GetMemSize() >> 2));

		AllocDsps(true);

		DevMsg($"Sound Initialization: Finish, Sampling Rate: {g_AudioDevice!.DeviceDmaSpeed()} Hz\n");
	}


	// =======================================================================
	// Shutdown sound engine
	// =======================================================================
	public static void S_Shutdown() {
		if (soundServices.VoiceTweak_IsStillTweaking())
			soundServices.VoiceTweak_EndVoiceTweakMode();

		S_StopAllSounds(true);
		S_ShutdownMixThread();

		audiosourcecache.Shutdown();

		SNDDMA_Shutdown();

		foreach (var kvp in s_Sounds) {
			if (kvp.Value != null) {
				(kvp.Value.Source as IDisposable)?.Dispose();
				kvp.Value.Source = null;
			}
		}
		s_Sounds.Clear();

		// release DSP resources
		FreeDsps(true);

		MXR_ReleaseMemory();

		// release sentences resources
		VOX_Shutdown();

		// shutdown vaudio
		vaudio = null;

		MIX_FreeAllPaintbuffers();
		snd_initialized = false;
		g_paintedtime = 0;
		g_soundtime = 0;
		g_ReplaySoundTimeFracAccumulator = 0.0f;
		s_buffers = 0;
		s_oldsampleOutCount = 0;
		s_lastsoundtime = 0.0f;

		soundServices.Voice_Deinit();
	}

	public static bool S_IsInitted() {
		return snd_initialized;
	}

	// =======================================================================
	// Load a sound
	// =======================================================================

	//-----------------------------------------------------------------------------
	// Return sfx and set pfInCache to 1 if
	// name is in name cache. Otherwise, alloc
	// a new spot in name cache and return 0
	// in pfInCache.
	//-----------------------------------------------------------------------------
	public static SfxTable S_FindName(ReadOnlySpan<char> szName, Span<int> pfInCache) {
		SfxTable? sfx = null;

		if (szName.IsEmpty)
			Error("S_FindName: NULL\n");

		ReadOnlySpan<char> pName = szName;

		// see if already loaded
		FileNameHandle_t fnHandle = filesystem.FindOrAddFileName(pName);
		if (s_Sounds.TryGetValue(fnHandle, out sfx)) {
			Assert(sfx);
			if (!pfInCache.IsEmpty) {
				// indicate whether or not sound is currently in the cache.
				pfInCache[0] = (sfx.Source != null && sfx.Source.IsCached()) ? 1 : 0;
			}
			return sfx;
		}
		else {
			lock (g_pSoundPoolMutex) {
				sfx = new SfxTable();
				s_Sounds[fnHandle] = sfx;

				sfx.SetNamePoolIndex(fnHandle);
				sfx.Source = null;

				if (!pfInCache.IsEmpty)
					pfInCache[0] = 0;
			}
		}
		return sfx;
	}

	//-----------------------------------------------------------------------------
	// S_LoadSound
	//
	// Check to see if wave data is in the cache. If so, return pointer to data.
	// If not, allocate cache space for wave data, load wave file into temporary heap
	// space, and dump/convert file data into cache.
	//-----------------------------------------------------------------------------
	public static double g_flAccumulatedSoundLoadTime = 0.0f;
	public static AudioSourceBase? S_LoadSound(SfxTable pSfx, Channel? ch) {
		if (pSfx.Source == null) {
			double st = Platform.Time;

			bool bUserVox = false;

			// sound chars can explicitly categorize usage
			bool bStream = TestSoundChar(pSfx.GetName(), SoundChars.Stream);
			if (!bStream)
				bUserVox = TestSoundChar(pSfx.GetName(), SoundChars.UserVox);

			if (bStream) {
				// setup as a streaming resource
				pSfx.Source = Audio_CreateStreamedWave(pSfx);
			}
			else {
				if (bUserVox)
					pSfx.Source = Voice_SetupAudioSource(ch!.SoundSource, ch.EntChannel);
				else {
					// load all into memory directly
					pSfx.Source = Audio_CreateMemoryWave(pSfx);
				}
			}

			double ed = Platform.Time;
			g_flAccumulatedSoundLoadTime += (ed - st);
		}
		else
			pSfx.Source.CheckAudioSourceCache();

		if (pSfx.GetSource() == null)
			return null;

		// first time to load?  Create the mixer
		if (ch != null && ch.Mixer == null) {
			ch.Mixer = pSfx.GetSource()!.CreateMixer(ch.InitialStreamPosition);
			if (ch.Mixer == null)
				return null;
		}

		return pSfx.GetSource();
	}

	//-----------------------------------------------------------------------------
	//	S_PrecacheSound
	//
	//	Reserve space for the name of the sound in a global array.
	//	Load the data for the non-streaming sound. Streaming sounds
	//	defer loading of data until just before playback.
	//-----------------------------------------------------------------------------
	public static SfxTable? S_PrecacheSound(ReadOnlySpan<char> name) {
		if (g_AudioDevice == null)
			return null;

		if (!g_AudioDevice.IsActive())
			return null;

		SfxTable? sfx = S_FindName(name, null);
		if (sfx != null) {
			// cache sound
			S_LoadSound(sfx, null);
		}
		else
			AssertMsg(false, "S_PrecacheSound:  Failed to create sfx");

		return sfx;
	}


	static void S_InternalReloadSound(SfxTable? sfx) {
		if (sfx == null || sfx.Source == null)
			return;

		sfx.Source.CacheUnload();

		(sfx.Source as IDisposable)?.Dispose();
		sfx.Source = null;

		ReadOnlySpan<char> pExt = sfx.GetName().GetFileExtension();
		AudioSourceType nSource = stricmp(pExt, "mp3") == 0 ? AudioSourceType.AUDIO_SOURCE_MP3 : AudioSourceType.AUDIO_SOURCE_WAV;
		//	audiosourcecache->RebuildCacheEntry( nSource, sfx->IsPrecachedSound(), sfx );
		audiosourcecache.GetInfo(nSource, sfx.IsPrecachedSound(), sfx); // Do a size/date check and rebuild the cache entry if necessary.
	}


	//-----------------------------------------------------------------------------
	//	Refresh a sound in the cache
	//-----------------------------------------------------------------------------
	public static void S_ReloadSound(ReadOnlySpan<char> name) {
		if (g_AudioDevice == null)
			return;

		if (!g_AudioDevice.IsActive())
			return;

		SfxTable? sfx = S_FindName(name, null);
#if DEBUG
		if (sfx != null)
			Assert(stricmp(sfx.GetName(), name) == 0);
#endif

		S_InternalReloadSound(sfx);
	}


	// See comments on CL_HandlePureServerWhitelist for details of what we're doing here.
	public static void S_ReloadFilesInList(IFileList pFilesToReload) {
		S_StopAllSounds(true);
		wavedatacache.Flush();
		audiosourcecache.ForceRecheckDiskInfo();    // Force all cached audio data to recheck size/date info next time it's accessed.


		List<SfxTable> processed = [];

		foreach (var kvp in s_Sounds.ToArray()) {
			FileNameHandle_t fnHandle = kvp.Key;
			ReadOnlySpan<char> filename = filesystem.String(fnHandle);
			if (filename.IsEmpty) {
				AssertMsg(false, "S_HandlePureServerWhitelist - can't get a filename.");
				continue;
			}

			// If the file isn't cached in yet, then the filesystem hasn't touched its file, so don't bother.
			SfxTable? sfx = kvp.Value;
			if (sfx != null && !processed.Contains(sfx)) {
				string fullFilename;
				if (IsSoundChar(filename[0]))
					fullFilename = $"sound/{filename[1..]}";
				else
					fullFilename = $"sound/{filename}";


				if (!pFilesToReload.IsFileInList(fullFilename))
					continue;

				processed.Add(sfx);

				S_InternalReloadSound(sfx);
			}
		}
	}

	//-----------------------------------------------------------------------------
	// Unfortunate confusing terminology.
	// Here prefetching means hinting to the audio source (which may be a stream)
	// to get its async data in flight.
	//-----------------------------------------------------------------------------
	public static void S_PrefetchSound(ReadOnlySpan<char> name, bool bPlayOnce) {
		SfxTable? sfx;

		if (g_AudioDevice == null)
			return;

		if (!g_AudioDevice.IsActive())
			return;

		sfx = S_FindName(name, null);
		if (sfx != null) {
			// cache sound
			S_LoadSound(sfx, null);
		}

		if (sfx == null || sfx.Source == null)
			return;

		// hint the sound to start loading
		sfx.Source.Prefetch();

		if (bPlayOnce)
			sfx.Source.SetPlayOnce(true);
	}

	public static void S_MarkUISound(SfxTable pSfx) {
		pSfx.IsUISound = true;
	}

	public static uint RemainingSamples(Channel? pChannel) {
		if (pChannel == null || pChannel.Sfx == null || pChannel.Sfx.Source == null)
			return 0;

		uint timeleft = (uint)pChannel.Sfx.Source.SampleCount();

		if (pChannel.Sfx.Source.IsLooped())
			return (uint)pChannel.Sfx.Source.SampleRate();

		if (pChannel.Mixer != null)
			timeleft -= (uint)pChannel.Mixer.GetSamplePosition();

		return timeleft;
	}

	// chooses the voice stealing algorithm
	public static readonly ConVar voice_steal = new("voice_steal", "2");

	/*
	=================
	SND_StealDynamicChannel
	Select a channel from the dynamic channel allocation area.  For the given entity,
	override any other sound playing on the same channel (see code comments below for
	exceptions).
	=================
	*/
	static Channel? SND_StealDynamicChannel(int soundsource, int entchannel, in Vector3 origin, SfxTable sfx, float flDelay, bool bDoNotOverwriteExisting) {
		Span<int> canSteal = stackalloc int[MAX_DYNAMIC_CHANNELS];
		int canStealCount = 0;

		int sameSoundCount = 0;
		uint sameSoundRemaining = 0xFFFFFFFF;
		int sameSoundIndex = -1;
		int sameVol = 0xFFFF;
		int availableChannel = -1;
		bool bDelaySame = false;

		Span<int> nExactMatch = stackalloc int[MAX_DYNAMIC_CHANNELS];
		int nExactCount = 0;
		// first pass to replace sounds on same ent/channel, and search for free or stealable channels otherwise
		for (int ch_idx = 0; ch_idx < MAX_DYNAMIC_CHANNELS; ch_idx++) {
			Channel ch = channels[ch_idx];

			if (ch.ActiveIndex != 0) {
				// channel CHAN_AUTO never overrides sounds on same channel
				if (entchannel != (int)SoundEntityChannel.Auto) {
					int checkChannel = entchannel;
					if (checkChannel == -1) {
						if (ch.EntChannel != (int)SoundEntityChannel.Stream && ch.EntChannel != (int)SoundEntityChannel.Voice && ch.EntChannel != (int)SoundEntityChannel.Voice2)
							checkChannel = ch.EntChannel;
					}
					if (ch.SoundSource == soundsource && (soundsource != -1) && ch.EntChannel == checkChannel) {
						// we found an exact match for this entity and this channel, but the sound we want to play is considered
						// low priority so instead of stomping this entry pretend we couldn't find a free slot to play and let
						// the existing sound keep going
						if (bDoNotOverwriteExisting)
							return null;

						if (ch.Flags.DelayedStart) {
							nExactMatch[nExactCount] = ch_idx;
							nExactCount++;
							continue;
						}
						return ch;  // always override sound from same entity
					}
				}

				// Never steal the channel of a streaming sound that is currently playing or
				// voice over IP data that is playing or any sound on CHAN_VOICE( acting )
				if (ch.EntChannel == (int)SoundEntityChannel.Stream || ch.EntChannel == (int)SoundEntityChannel.Voice || ch.EntChannel == (int)SoundEntityChannel.Voice2)
					continue;

				// don't let monster sounds override player sounds
				if (soundServices.IsPlayer(ch.SoundSource) && !soundServices.IsPlayer(soundsource))
					continue;

				if (ch.Sfx == sfx) {
					bDelaySame = ch.Flags.DelayedStart ? true : bDelaySame;
					sameSoundCount++;
					int maxVolume = ChannelGetMaxVol(ch);
					uint remaining = RemainingSamples(ch);
					if (maxVolume < sameVol || (maxVolume == sameVol && remaining < sameSoundRemaining)) {
						sameSoundIndex = ch_idx;
						sameVol = maxVolume;
						sameSoundRemaining = remaining;
					}
				}
				canSteal[canStealCount++] = ch_idx;
			}
			else {
				if (availableChannel < 0)
					availableChannel = ch_idx;
			}
		}


		// coalesce the timeline for this channel
		if (nExactCount > 0) {
			uint nFreeSampleTime = (uint)(g_paintedtime + (flDelay * SOUND_DMA_SPEED));
			Channel pReturn = channels[nExactMatch[0]];
			uint nMinRemaining = RemainingSamples(pReturn);
			if (pReturn.FreeChannelAtSampleTime == 0 || pReturn.FreeChannelAtSampleTime > nFreeSampleTime)
				pReturn.FreeChannelAtSampleTime = nFreeSampleTime;
			for (int i = 1; i < nExactCount; i++) {
				Channel pChannel = channels[nExactMatch[i]];
				if (pChannel.FreeChannelAtSampleTime == 0 || pChannel.FreeChannelAtSampleTime > nFreeSampleTime)
					pChannel.FreeChannelAtSampleTime = nFreeSampleTime;
				uint nRemain = RemainingSamples(pChannel);
				if (nRemain < nMinRemaining) {
					pReturn = pChannel;
					nMinRemaining = nRemain;
				}
			}
			// if there's only one, mark it to be freed but don't reuse it.
			// otherwise mark all others to be freed and use the closest one to being done
			if (nExactCount > 1)
				return pReturn;
		}

		// Limit the number of times a given sfx/wave can play simultaneously
		if (voice_steal.GetInt() > 1 && sameSoundIndex >= 0) {
			// if sounds of this type are normally delayed, then add an extra slot for stealing
			// NOTE: In HL2 these are usually NPC gunshot sounds - and stealing too soon will cut
			// them off early.  This is a safe heuristic to avoid that problem.  There's probably a better
			// long-term solution involving only counting channels that are actually going to play (delay included)
			// at the same time as this one.
			int maxSameSounds = bDelaySame ? 5 : 4;
			float distSqr = 0.0f;
			if (sfx.Source != null) {
				distSqr = Vector3.DistanceSquared(origin, listener_origin);
				if (sfx.Source.IsLooped())
					maxSameSounds = 3;
			}

			// don't play more than N copies of the same sound, steal the quietest & closest one otherwise
			if (sameSoundCount >= maxSameSounds) {
				Channel ch = channels[sameSoundIndex];
				// you're already playing a closer version of this sound, don't steal
				if (distSqr > 0.0f && Vector3.DistanceSquared(ch.Origin, listener_origin) < distSqr && entchannel != (int)SoundEntityChannel.Weapon)
					return null;

				//Msg("Sound playing %d copies, stole %s (%d)\n", sameSoundCount, ch->sfx->getname(), sameVol );
				return ch;
			}
		}

		// if there's a free channel, just take that one - don't steal
		if (availableChannel >= 0)
			return channels[availableChannel];

		// Still haven't found a suitable channel, so choose the one with the least amount of time left to play
		float life_left = float.MaxValue;
		int first_to_die = -1;
		bool bAllowVoiceSteal = voice_steal.GetBool();

		for (int i = 0; i < canStealCount; i++) {
			int ch_idx = canSteal[i];
			Channel ch = channels[ch_idx];
			float timeleft = 0;
			if (bAllowVoiceSteal) {
				int maxVolume = ChannelGetMaxVol(ch);
				if (maxVolume < 5) {
					//Msg("Sound quiet, stole %s for %s\n", ch->sfx->getname(), sfx->getname() );
					return ch;
				}

				if (ch.Sfx != null && ch.Sfx.Source != null) {
					uint sampleCount = RemainingSamples(ch);
					timeleft = (float)sampleCount / (float)ch.Sfx.Source.SampleRate();
				}
			}
			else {
				// UNDONE: Kill this when voice_steal 0,1,2 has been tested
				// UNDONE: This is the old buggy code that we're trying to replace
				if (ch.Sfx != null) {
					// basically steals the first one you come to
					timeleft = 1;   //ch->end - paintedtime
				}
			}

			if (timeleft < life_left) {
				life_left = timeleft;
				first_to_die = ch_idx;
			}
		}
		if (first_to_die >= 0) {
			//Msg("Stole %s, timeleft %d\n", channels[first_to_die].sfx->getname(), life_left );
			return channels[first_to_die];
		}

		return null;
	}

	static Channel? SND_PickDynamicChannel(int soundsource, int entchannel, in Vector3 origin, SfxTable sfx, float flDelay, bool bDoNotOverwriteExisting) {
		Channel? pChannel = SND_StealDynamicChannel(soundsource, entchannel, origin, sfx, flDelay, bDoNotOverwriteExisting);
		if (pChannel == null)
			return null;

		if (pChannel.Sfx != null) {
			// Don't restart looping sounds for the same entity
			AudioSource? pSource = pChannel.Sfx.Source;
			if (pSource != null) {
				if (pSource.IsLooped()) {
					if (pChannel.SoundSource == soundsource && pChannel.EntChannel == entchannel && pChannel.Sfx == sfx) {
						// same looping sound, same ent, same channel, don't restart the sound
						return null;
					}
				}
			}
			// be sure and release previous channel
			// if sentence.
			//	("Stealing channel from %s\n", channels[first_to_die].sfx->getname() );
			S_FreeChannel(pChannel);
		}

		return pChannel;
	}



	/*
	=====================
	SND_PickStaticChannel
	=====================
	Pick an empty channel from the static sound area, or allocate a new
	channel.  Only fails if we're at max_channels (128!!!) or if
	we're trying to allocate a channel for a stream sound that is
	already playing.

	*/
	static Channel? SND_PickStaticChannel(int soundsource, SfxTable pSfx) {
		int i;
		Channel? ch = null;

		// Check for replacement sound, or find the best one to replace
		for (i = MAX_DYNAMIC_CHANNELS; i < total_channels; i++)
			if (channels[i].Sfx == null)
				break;

		if (i < total_channels) {
			// reuse an empty static sound channel
			ch = channels[i];
		}
		else {
			// no empty slots, alloc a new static sound channel
			if (total_channels == MAX_CHANNELS) {
				DevMsg("total_channels == MAX_CHANNELS\n");
				return null;
			}

			// get a channel for the static sound
			ch = channels[total_channels];
			total_channels++;
		}

		return ch;
	}


	public static void S_SpatializeChannel(Span<int> pVolume, int master_vol, in Vector3 psourceDir, float gain, float mono) {
		float lscale, rscale, scale;
		float dotRight;
		Vector3 sourceDir = psourceDir;

		dotRight = Vector3.Dot(listener_right, sourceDir);

		// clear volumes
		for (int i = 0; i < CCHANVOLUMES / 2; i++)
			pVolume[i] = 0;

		if (mono > 0.0F) {
			// sound has radius, within which spatialization becomes mono:

			// mono is 0.0 -> 1.0, from radius 100% to radius 50%

			// at radius * 0.5, dotRight is 0 (ie: sound centered left/right)
			// at radius * 1.0, dotRight == dotRight

			dotRight *= (1.0F - mono);
		}

		rscale = 1.0F + dotRight;
		lscale = 1.0F - dotRight;

		// add in distance effect
		scale = gain * rscale / 2;
		pVolume[IFRONT_RIGHT] = (int)(master_vol * scale);

		scale = gain * lscale / 2;
		pVolume[IFRONT_LEFT] = (int)(master_vol * scale);

		pVolume[IFRONT_RIGHT] = Math.Clamp(pVolume[IFRONT_RIGHT], 0, 255);
		pVolume[IFRONT_LEFT] = Math.Clamp(pVolume[IFRONT_LEFT], 0, 255);

	}

	static bool S_IsMusic(Channel pChannel) {
		if (!pChannel.Flags.Dry)
			return false;

		SfxTable? sfx = pChannel.Sfx;
		if (sfx == null)
			return false;

		AudioSource? source = sfx.Source;
		if (source == null)
			return false;

		// Don't save restore looping sounds as you can end up with an entity restarting them again and have
		//  them accumulate, etc.
		if (source.IsLooped())
			return false;

		AudioMixer? pMixer = pChannel.Mixer;
		if (pMixer == null)
			return false;

		for (int i = 0; i < 8; i++) {
			if (pChannel.MixGroups[i] != -1) {
				ReadOnlySpan<char> pGroupName = MXR_GetGroupnameFromId(pChannel.MixGroups[i]);
				if (strcmp(pGroupName, "Music") == 0)
					return true;
			}
		}
		return false;
	}

	//-----------------------------------------------------------------------------
	// Purpose: For save/restore of currently playing music
	// Input  : list -
	//-----------------------------------------------------------------------------
	public static void S_GetCurrentlyPlayingMusic(List<MusicSave> musiclist) {
		ChannelList list = new();
		g_ActiveChannels.GetActiveChannels(list);
		for (int i = 0; i < list.Count; i++) {
			Channel pChannel = channels[list.GetChannelIndex(i)];
			if (!S_IsMusic(pChannel))
				continue;

			MusicSave song = new();
			song.SongName = new(pChannel.Sfx!.GetName());
			song.SamplePosition = pChannel.Mixer!.GetPositionForSave();
			song.MasterVolume = pChannel.MasterVol;

			musiclist.Add(song);
		}
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	// Input  : *song -
	//-----------------------------------------------------------------------------
	public static void S_RestartSong(in MusicSave song) {
		// Start the song
		SfxTable? pSound = S_PrecacheSound(song.SongName);
		if (pSound != null) {
			StartSoundParams parms = new();
			parms.StaticSound = true;
			parms.SoundSource = EngineSoundGlobals.SOUND_FROM_WORLD;
			parms.EntChannel = SoundEntityChannel.Static;
			parms.Sfx = pSound;
			parms.Origin = vec3_origin;
			parms.Volume = song.MasterVolume / 255.0f;
			parms.SoundLevel = SoundLevel.LvlNone;
			parms.Flags = SoundFlags.NoFlags;
			parms.Pitch = PITCH_NORM;
			parms.InitialStreamPosition = song.SamplePosition;

			S_StartSound(ref parms);

			// Now find the channel this went on and skip ahead in the mixer
			for (int i = 0; i < total_channels; i++) {
				Channel ch = channels[i];

				if (ch.Mixer == null || ch.Mixer.GetSource() == null)
					continue;

				if (ch.Mixer.GetSource() != pSound.Source)
					continue;

				ch.Mixer.SetPositionFromSaved(song.SamplePosition);
				break;
			}
		}
	}

	// calculate ammount of sound to be mixed to dsp, based on distance from listener


	public static readonly ConVar dsp_dist_min = new("dsp_dist_min", "0.0", FCvar.Demo | FCvar.Cheat);      // range at which sounds are mixed at dsp_mix_min
	public static readonly ConVar dsp_dist_max = new("dsp_dist_max", "1440.0", FCvar.Demo | FCvar.Cheat);   // range at which sounds are mixed at dsp_mix_max

	public static readonly ConVar dsp_mix_min = new("dsp_mix_min", "0.2", FCvar.Demo);      // dsp mix at dsp_dist_min distance "near"
	public static readonly ConVar dsp_mix_max = new("dsp_mix_max", "0.8", FCvar.Demo);      // dsp mix at dsp_dist_max distance "far"
	public static readonly ConVar dsp_db_min = new("dsp_db_min", "80", FCvar.Demo);            // sounds with sndlvl below this get dsp_db_mixdrop % less dsp mix
	public static readonly ConVar dsp_db_mixdrop = new("dsp_db_mixdrop", "0.5", FCvar.Demo);    // sounds with sndlvl below dsp_db_min get dsp_db_mixdrop % less mix

	public static float DSP_ROOM_MIX = 1.0f;    // mix volume of dsp_room sounds when added back to 'dry' sounds
	public static float DSP_NOROOM_MIX = 1.0f;  // mix volume of facing + facing away sounds. added to dsp_room_mix sounds

	// returns 0-1.0 dsp mix value.  If sound source is at a range >= DSP_DIST_MAX, return a mix value of
	// DSP_MIX_MAX.  This mix value is used later to determine wet/dry mix ratio of sounds.

	// This ramp changes with db level of sound source,  and is set in the dsp room presets by room size
	// empirical data: 0.78 is nominal mix for sound 100% at far end of room, 0.24 is mix for sound 25% into room

	static float SND_GetDspMix(Channel pchannel, int idist) {
		float mix;
		float dist = (float)idist;
		float dist_min = dsp_dist_min.GetFloat();
		float dist_max = dsp_dist_max.GetFloat();
		float mix_min;
		float mix_max;

		// only set dsp mix_min & mix_max when sound is first started

		if (pchannel.DspMixMin < 0 && pchannel.DspMixMax < 0) {
			mix_min = dsp_mix_min.GetFloat();       // set via dsp_room preset
			mix_max = dsp_mix_max.GetFloat();       // set via dsp_room preset

			// set mix_min & mix_max based on db level of sound:
			// sounds below dsp_db_min decrease dsp_mix_min & dsp_mix_max by N%
			// ie: quiet sounds get less dsp mix than loud sounds

			int sndlvl = SND_GetSndlvl(pchannel);
			int sndlvl_min = dsp_db_min.GetInt();

			if (sndlvl <= sndlvl_min) {
				mix_min *= dsp_db_mixdrop.GetFloat();
				mix_max *= dsp_db_mixdrop.GetFloat();
			}

			pchannel.DspMixMin = mix_min;
			pchannel.DspMixMax = mix_max;
		}
		else {
			mix_min = pchannel.DspMixMin;
			mix_max = pchannel.DspMixMax;
		}

		// dspmix is 0 (100% mix to facing buffer) if dsp_off

		if (dsp_off.GetInt() != 0)
			return 0.0f;

		// doppler wavs are mixed dry

		if (pchannel.WavType == (char)SoundChars.Doppler)
			return 0.0f;

		// linear ramp - get dry mix %

		// dist: 0->(max - min)

		dist = Math.Clamp(dist, dist_min, dist_max) - dist_min;

		// dist: 0->1.0

		dist = dist / (dist_max - dist_min);

		// mix: min->max

		mix = ((mix_max - mix_min) * dist) + mix_min;

		return mix;
	}

	// calculate crossfade between wav left (close sound) and wav right (far sound) based on
	// distance fron listener

	const float DVAR_DIST_MIN = 20.0F * 12.0F;      // play full 'near' sound at 20' or less
	const float DVAR_DIST_MAX = 110.0F * 12.0F;     // play full 'far' sound at 110' or more
	const float DVAR_MIX_MIN = 0.0F;
	const float DVAR_MIX_MAX = 1.0F;

	// calculate mixing parameter for CHAR_DISTVAR wavs
	// returns 0 - 1.0, 1.0 is 100% far sound (wav right)

	static float SND_GetDistanceMix(Channel pchannel, int idist) {
		float mix;
		float dist = (float)idist;

		// doppler wavs are 100% near - their spatialization is calculated later.

		if (pchannel.WavType == (char)SoundChars.Doppler)
			return 0.0f;

		// linear ramp - get dry mix %

		// dist 0->(max - min)

		dist = Math.Clamp(dist, (float)DVAR_DIST_MIN, (float)DVAR_DIST_MAX) - (float)DVAR_DIST_MIN;

		// dist 0->1.0

		dist = dist / (DVAR_DIST_MAX - DVAR_DIST_MIN);

		// mix min->max

		mix = ((DVAR_MIX_MAX - DVAR_MIX_MIN) * dist) + DVAR_MIX_MIN;

		return mix;
	}

	// given facing direction of source, and channel,
	// return -1.0 - 1.0, where -1.0 is source facing away from listener
	// and 1.0 is source facing listener


	static float SND_GetFacingDirection(Channel pChannel, in QAngle source_angles) {
		Vector3 SF;             // sound source forward direction unit vector
		Vector3 SL;             // sound -> listener unit vector
		float dotSFSL;

		// no facing direction unless wavtyp CHAR_DIRECTIONAL

		if (pChannel.WavType != (char)SoundChars.Directional)
			return 1.0f;

		SL = listener_origin - pChannel.Origin;
		MathLib.VectorNormalize(ref SL);

		// compute forward vector for sound entity

		MathLib.AngleVectors(source_angles, out SF);

		// dot source forward unit vector with source to listener unit vector to get -1.0 - 1.0 facing.
		// ie: projection of SF onto SL

		dotSFSL = Vector3.Dot(SF, SL);

		return dotSFSL;
	}

	// calculate point of closest approach - caller must ensure that the
	// forward facing vector of the entity playing this sound points in exactly the direction of
	// travel of the sound. ie: for bullets or tracers, forward vector must point in traceline direction.
	// return true if sound is to be played, false if sound cannot be heard (shot away from player)

	static bool SND_GetClosestPoint(Channel pChannel, in QAngle source_angles, ref Vector3 vnearpoint) {
		// S - sound source origin
		// L - listener origin

		Vector3 SF;             // sound source forward direction unit vector
		Vector3 SL;             // sound -> listener vector
		Vector3 SD;             // sound->closest point vector
		float dSLSF;            // magnitude of project of SL onto SF

		// P = SF (SF . SL) + S

		// only perform this calculation for doppler wavs

		if (pChannel.WavType != (char)SoundChars.Doppler)
			return false;

		// get vector 'SL' from sound source to listener

		SL = listener_origin - pChannel.Origin;

		// compute sound->forward vector 'SF' for sound entity

		MathLib.AngleVectors(source_angles, out SF);
		MathLib.VectorNormalize(ref SF);

		dSLSF = Vector3.Dot(SL, SF);


		if (dSLSF <= 0 && !soundServices.IsToolRecording()) {
			// source is pointing away from listener, don't play anything
			// unless we're recording in the tool, since we may play back from in front of the source
			return false;
		}

		// project dSLSF along forward unit vector from sound source

		SD = SF * dSLSF;

		// output vector - add SD to sound source origin

		vnearpoint = SD + pChannel.Origin;

		return true;
	}


	// given point of nearest approach and sound source facing angles,
	// return vector pointing into quadrant in which to play
	// doppler left wav (incomming) and doppler right wav (outgoing).

	// doppler left is point in space to play left doppler wav
	// doppler right is point in space to play right doppler wav

	// Also modifies channel pitch based on distance to nearest approach point

	const float DOPPLER_DIST_LEFT_TO_RIGHT = 4 * 12;        // separate left/right sounds by 4'

	const float DOPPLER_DIST_MAX = 20 * 12;     // max distance - causes min pitch
	const float DOPPLER_DIST_MIN = 1 * 12;      // min distance - causes max pitch
	const float DOPPLER_PITCH_MAX = 1.5F;           // max pitch change due to distance
	const float DOPPLER_PITCH_MIN = 0.25F;      // min pitch change due to distance

	const float DOPPLER_RANGE_MAX = 10 * 12;        // don't play doppler wav unless within this range
													// UNDONE: should be set by caller!

	static void SND_GetDopplerPoints(Channel pChannel, in QAngle source_angles, in Vector3 vnearpoint, out Vector3 source_doppler_left, out Vector3 source_doppler_right) {
		Vector3 SF;         // direction sound source is facing (forward)
		Vector3 LN;         // vector from listener to closest approach point
		Vector3 DL;
		Vector3 DR;

		// nearpoint is closest point of approach, when playing CHAR_DOPPLER sounds

		// SF is normalized vector in direction sound source is facing

		MathLib.AngleVectors(source_angles, out SF);
		MathLib.VectorNormalize(ref SF);

		// source_doppler_left - location in space to play doppler left wav (incomming)
		// source_doppler_right	- location in space to play doppler right wav (outgoing)

		DL = SF * (-1 * DOPPLER_DIST_LEFT_TO_RIGHT);
		DR = SF * DOPPLER_DIST_LEFT_TO_RIGHT;

		source_doppler_left = vnearpoint + DL;
		source_doppler_right = vnearpoint + DR;

		// set pitch of channel based on nearest distance to listener

		// LN is vector from listener to closest approach point

		LN = vnearpoint - listener_origin;

		float pitch;
		float dist = LN.Length();

		// dist varies 0->1

		dist = Math.Clamp(dist, (float)DOPPLER_DIST_MIN, (float)DOPPLER_DIST_MAX);
		dist = (dist - DOPPLER_DIST_MIN) / (DOPPLER_DIST_MAX - DOPPLER_DIST_MIN);

		// pitch varies from max to min

		pitch = DOPPLER_PITCH_MAX - dist * (DOPPLER_PITCH_MAX - DOPPLER_PITCH_MIN);

		pChannel.BasePitch = (short)(int)(pitch * 100.0);
	}

	// console variables used to construct gain curve - don't change these!

	public static readonly ConVar snd_showstart = new("snd_showstart", "0", FCvar.Cheat);  // showstart always skips info on player footsteps!
																						// 1 - show sound name, channel, volume, time
																						// 2 - show dspmix, distmix, dspface, l/r/f/r vols
																						// 3 - show sound origin coords
																						// 4 - show gain of dsp_room
																						// 5 - show dB loss due to obscured sound
																						// 6 - reserved
																						// 7 - show 2 and total gain & dist in ft. to sound source

	const float SND_DB_MAX = 140.0F;    // max db of any sound source
	const float SND_DB_MED = 90.0F; // db at which compression curve changes
	const float SND_DB_MIN = 60.0F; // min db of any sound source

	// dB = 20 log (amplitude/32768)		0 to -90.3dB
	// amplitude = 32768 * 10 ^ (dB/20)		0 to +/- 32768
	// gain = amplitude/32768				0 to 1.0

	public static float Gain_To_dB(float gain) {
		float dB = 20 * MathF.Log(gain);
		return dB;
	}

	public static float Gain_To_Amplitude(float gain) {
		return gain * 32768;
	}

	public static float Amplitude_To_Gain(float amplitude) {
		return amplitude / 32768;
	}

	public static int SND_GetSndlvl(Channel pchannel) {
		return DIST_MULT_TO_SNDLVL(pchannel.DistMult);
	}


	// The complete gain calculation, with SNDLVL given in dB is:
	//
	// GAIN = 1/dist * snd_refdist * 10 ^ ( ( SNDLVL - snd_refdb - (dist * snd_foliage_db_loss / 1200)) / 20 )
	//
	//		for gain > SND_GAIN_THRESH, start curve smoothing with
	//
	// GAIN = 1 - 1 / (Y * GAIN ^ SND_GAIN_POWER)
	//
	//		 where Y = -1 / ( (SND_GAIN_THRESH ^ SND_GAIN_POWER) * (SND_GAIN_THRESH - 1) )
	//

	// gain curve construction

	static float SND_GetGain(Channel ch, bool fplayersound, bool fmusicsound, bool flooping, float dist, bool bAttenuated) {
		if (ch.Flags.CompatibilityAttenuation) {
			// Convert to the original attenuation value.
			int soundlevel = DIST_MULT_TO_SNDLVL(ch.DistMult);
			float flAttenuation = AttenuationValues.SNDLVL_TO_ATTN(soundlevel);

			// Now get the goldsrc dist_mult and use the same calculation it uses in SND_Spatialize.
			// Straight outta Goldsrc!!!
			float nominal_clip_dist = 1000.0f;
			float flGoldsrcDistMult = flAttenuation / nominal_clip_dist;
			dist *= flGoldsrcDistMult;
			float flReturnValue = 1.0f - dist;
			flReturnValue = Math.Clamp(flReturnValue, 0.0f, 1.0f);
			return flReturnValue;
		}
		else {
			float gain = snd_gain.GetFloat();

			if (fmusicsound) {
				gain = gain * snd_musicvolume.GetFloat();
				gain = gain * g_DashboardMusicMixValue;
			}

			if (ch.DistMult != 0)
				gain = SND_GetGainFromMult(gain, ch.DistMult, dist);

			if (fplayersound) {

				// player weapon sounds get extra gain - this compensates
				// for npc distance effect weapons which mix louder as L+R into L,R
				// Hack.

				if (ch.EntChannel == (int)SoundEntityChannel.Weapon)
					gain = gain * dB_To_Gain(SND_GAIN_PLAYER_WEAPON_DB);
			}

			// modify gain if sound source not visible to player

			gain = gain * SND_GetGainObscured(ch, fplayersound, flooping, bAttenuated);

			if (snd_showstart.GetInt() == 6) {
				DevMsg($"(gain {gain:F3} : dist ft {(float)dist / 12.0:F1}) ");
				snd_showstart.SetValue(5);  // display once
			}

			return gain;
		}
	}

	// always ramp channel gain changes over time
	// returns ramped gain, given new target gain

	const float SND_GAIN_FADE_TIME = 0.25F;     // xfade seconds between obscuring gain changes

	static float SND_FadeToNewGain(Channel ch, float gain_new) {

		if (gain_new == -1.0) {
			// if -1 passed in, just keep fading to existing target

			gain_new = ch.ObGainTarget;
		}

		// if first time updating, store new gain into gain & target, return
		// if gain_new is close to existing gain, store new gain into gain & target, return

		if (ch.Flags.FirstPass || (MathF.Abs(gain_new - ch.ObGain) < 0.01)) {
			ch.ObGain = gain_new;
			ch.ObGainTarget = gain_new;
			ch.ObGainInc = 0.0f;
			return gain_new;
		}

		// set up new increment to new target

		float frametime = (float)soundServices.GetHostFrametime();
		float speed;
		speed = (frametime / SND_GAIN_FADE_TIME) * (gain_new - ch.ObGain);

		ch.ObGainInc = MathF.Abs(speed);

		// ch->ob_gain_inc = fabs(gain_new - ch->ob_gain) / 10.0;

		ch.ObGainTarget = gain_new;

		// if not hit target, keep approaching

		if (MathF.Abs(ch.ObGain - ch.ObGainTarget) > 0.01F)
			ch.ObGain = MathLib.Approach(ch.ObGainTarget, ch.ObGain, ch.ObGainInc);
		else {
			// close enough, set gain = target
			ch.ObGain = ch.ObGainTarget;
		}

		return ch.ObGain;
	}

	const int SND_TRACE_UPDATE_MAX = 2;         // max of N channels may be checked for obscured source per frame

	static int g_snd_trace_count = 0;       // total tracelines for gain obscuring made this frame

	// All new sounds must traceline once,
	// but cap the max number of tracelines performed per frame
	// for longer or looping sounds to SND_TRACE_UPDATE_MAX.

	static bool SND_ChannelOkToTrace(Channel ch) {
		// always trace first time sound is spatialized (doesn't update counter)

		if (ch.Flags.FirstPass) {
			ch.Flags.Traced = true;
			return true;
		}

		// if already traced max channels this frame, return

		if (g_snd_trace_count >= SND_TRACE_UPDATE_MAX)
			return false;

		// ok to trace if this sound hasn't yet been traced in this round

		if (ch.Flags.Traced)
			return false;

		// set flag - don't traceline this sound again until all others have
		// been traced

		ch.Flags.Traced = true;

		g_snd_trace_count++;                // total traces this frame

		return true;
	}

	// determine if we need to reset all flags for traceline limiting -
	// this happens if we hit a frame whein no tracelines occur ie: all currently
	// playing sounds are blocked.

	static void SND_ChannelTraceReset() {
		if (g_snd_trace_count != 0)
			return;

		// if no tracelines performed this frame, then reset all
		// trace flags

		for (int i = 0; i < total_channels; i++)
			channels[i].Flags.Traced = false;
	}

	static bool SND_IsLongWave(Channel pChannel) {
		AudioSource? pSource = pChannel.Sfx?.Source;
		if (pSource != null) {
			if (pSource.IsStreaming())
				return true;

			// UNDONE: Do this on long wave files too?
#if false
			float length = (float)pSource.SampleCount() / (float)pSource.SampleRate();
			if (length > 0.75f)
				return true;
#endif
		}

		return false;
	}


	public static readonly ConVar snd_obscured_gain_db = new("snd_obscured_gain_dB", "-2.70", FCvar.Cheat); // dB loss due to obscured sound source

	static float g_drop_prev = 0;

	// drop gain on channel if sound emitter obscured by
	// world, unbroken windows, closed doors, large solid entities etc.

	static float SND_GetGainObscured(Channel ch, bool fplayersound, bool flooping, bool bAttenuated) {
		float gain = 1.0f;
		int count = 1;
		float snd_gain_db;                  // dB loss due to obscured sound source

		// Unattenuated sounds don't get obscured.
		if (!bAttenuated)
			return 1.0f;

		if (fplayersound)
			return gain;

		// During signon just apply regular state machine since world hasn't been
		//  created or settled yet...

		if (!SND_IsInGame()) {
			if (!soundServices.InToolMode())
				gain = SND_FadeToNewGain(ch, -1.0f);

			return gain;
		}

		// don't do gain obscuring more than once on short one-shot sounds

		if (!ch.Flags.FirstPass && !ch.Flags.IsSentence && !flooping && !SND_IsLongWave(ch)) {
			gain = SND_FadeToNewGain(ch, -1.0f);
			return gain;
		}

		snd_gain_db = snd_obscured_gain_db.GetFloat();

		// if long or looping sound, process N channels per frame - set 'processed' flag, clear by
		// cycling through all channels - this maintains a cap on traces per frame

		if (!SND_ChannelOkToTrace(ch)) {
			// just keep updating fade to existing target gain - no new trace checking

			gain = SND_FadeToNewGain(ch, -1.0f);
			return gain;
		}
		// set up traceline from player eyes to sound emitting entity origin

		Vector3 endpoint = ch.Origin;

		TraceFilterWorldOnly filter = new();    // UNDONE: also test for static props?
		Ray ray = default;
		ray.Init(soundServices.MainViewOrigin(), endpoint);
		g_pEngineTraceClient.TraceRay(in ray, MASK_BLOCK_AUDIO, ref filter, out Trace tr);

		if (tr.DidHit() && tr.Fraction < 0.99) {
			// can't see center of sound source:
			// build extents based on dB sndlvl of source,
			// test to see how many extents are visible,
			// drop gain by snd_gain_db per extent hidden

			Span<Vector3> endpoints = stackalloc Vector3[4];
			int sndlvl = DIST_MULT_TO_SNDLVL(ch.DistMult);
			float radius;
			Vector3 vsrc_forward;
			Vector3 vsrc_right;
			Vector3 vsrc_up;
			Vector3 vecl;
			Vector3 vecr;
			Vector3 vecl2;
			Vector3 vecr2;
			int i;

			// get radius

			if (ch.Radius > 0)
				radius = ch.Radius;
			else
				radius = dB_To_Radius(sndlvl);      // approximate radius from soundlevel

			// set up extent endpoints - on upward or downward diagonals, facing player

			for (i = 0; i < 4; i++)
				endpoints[i] = endpoint;

			// vsrc_forward is normalized vector from sound source to listener

			vsrc_forward = listener_origin - endpoint;
			MathLib.VectorNormalize(ref vsrc_forward);
			MathLib.VectorVectors(vsrc_forward, out vsrc_right, out vsrc_up);

			vecl = vsrc_up + vsrc_right;

			// if src above listener, force 'up' vector to point down - create diagonals up & down

			if (endpoint.Z > listener_origin.Z + (10 * 12))
				vsrc_up.Z = -vsrc_up.Z;

			vecr = vsrc_up - vsrc_right;
			MathLib.VectorNormalize(ref vecl);
			MathLib.VectorNormalize(ref vecr);

			// get diagonal vectors from sound source

			vecl2 = radius * vecl;
			vecr2 = radius * vecr;
			vecl = (radius / 2.0F) * vecl;
			vecr = (radius / 2.0F) * vecr;

			// endpoints from diagonal vectors

			endpoints[0] += vecl;
			endpoints[1] += vecr;
			endpoints[2] += vecl2;
			endpoints[3] += vecr2;

			// drop gain for each point on radius diagonal that is obscured

			for (count = 0, i = 0; i < 4; i++) {
				// UNDONE: some endpoints are in walls - in this case, trace from the wall hit location

				ray.Init(soundServices.MainViewOrigin(), endpoints[i]);
				g_pEngineTraceClient.TraceRay(in ray, MASK_BLOCK_AUDIO, ref filter, out tr);

				if (tr.DidHit() && tr.Fraction < 0.99 && !tr.StartSolid) {
					count++;    // skip first obscured point: at least 2 points + center should be obscured to hear db loss
					if (count > 1)
						gain = gain * dB_To_Gain(snd_gain_db);
				}
			}
		}


		if (flooping && snd_showstart.GetInt() == 7) {
			float drop = (count - 1) * snd_gain_db;

			if (drop != g_drop_prev) {
				DevMsg($"dB drop: {drop:F4} \n");
				g_drop_prev = drop;
			}
		}

		// crossfade to new gain

		gain = SND_FadeToNewGain(ch, gain);

		return gain;
	}

	// convert sound db level to approximate sound source radius,
	// used only for determining how much of sound is obscured by world

	const float SND_RADIUS_MAX = 20.0F * 12.0F; // max sound source radius
	const float SND_RADIUS_MIN = 2.0F * 12.0F;  // min sound source radius

	static float dB_To_Radius(float db) {
		float radius = SND_RADIUS_MIN + (SND_RADIUS_MAX - SND_RADIUS_MIN) * (db - SND_DB_MIN) / (SND_DB_MAX - SND_DB_MIN);

		return radius;
	}

	[InlineArray(5 * 3)]
	struct SndSpatialDist
	{
		int element;
	}

	[InlineArray(5)]
	struct SndSpatialValuePrev
	{
		float element;
	}

	struct SndSpatial
	{
		public int chan;            // 0..4 cycles through up to 5 channels
		public int cycle;           // 0..2 cycles through 3 vectors per channel
		public SndSpatialDist dist;         // stores last 3 channel distance values [channel][cycle]

		public SndSpatialValuePrev value_prev;  // previous value per channel

		public double last_change;
	}

	static bool g_ssp_init = false;
	static SndSpatial g_ssp;

	// return 0..1 percent difference between a & b

	static float PercentDifference(float a, float b) {
		float vp;

		if ((int)a == 0 && (int)b == 0)
			return 0.0F;

		if ((int)a == 0 || (int)b == 0)
			return 1.0F;

		if (a > b)
			vp = b / a;
		else
			vp = a / b;

		return 1.0F - vp;
	}

	// NOTE: Do not change SND_WALL_TRACE_LEN without also changing PRC_MDY6 delay value in SndDsp!

	const float SND_WALL_TRACE_LEN = 100.0F * 12.0F;        // trace max of 100' = max of 100 milliseconds of linear delay
	const float SND_SPATIAL_WAIT = 0.25F;           // seconds to wait between traces

	// change mod delay value on chan 0..3 to v (inches)

	static void DSP_SetSpatialDelay(int chan, float v) {
		// remap delay value 0..1200 to 1.0 to -1.0 for modulation

		float value = (v / SND_WALL_TRACE_LEN) - 1.0F;                  // -1.0...0
		value = value * 2.0F;                                           // -2.0...0
		value += 1.0F;                                                  // -1.0...1.0 (0...1200)
		value *= -1.0F;                                                 // 1.0...-1.0 (0...1200)

		// assume first processor in dsp_spatial is the modulating delay unit for DSP_ChangePresetValue

		int iproc = 0;

		DSP_ChangePresetValue(idsp_spatial, chan, iproc, value);
		/*

			if (chan & 0x01)
				DevMsg("RDly: %3.0f \n", v/12 );
			else
				DevMsg("LDly: %3.0f \n", v/12 );
		*/
	}

	// use non-feedback delay to stereoize (or make quad, or quad + center) the mono dsp_room fx,
	// This simulates the average sum of delays caused by reflections
	// from the left and right walls relative to the player.  The average delay
	// difference between left & right wall is (l + r)/2.  This becomes the average
	// delay difference between left & right ear.
	// call at most once per frame to update player->wall spatial delays

	static void SND_SetSpatialDelays() {
		float dist, v, vp;
		Vector3 v_dir, v_dir2;
		int chan_max = (g_AudioDevice!.IsSurround() ? 4 : 2) + (g_AudioDevice.IsSurroundCenter() ? 1 : 0);  // 2, 4, 5 channels

		// use listener_forward2d, which doesn't change when player looks up/down.

		Vector3 listener_forward2d;

		ConvertListenerVectorTo2D(out listener_forward2d, in listener_right);

		// init struct if 1st time through

		if (!g_ssp_init) {
			g_ssp = default;
			g_ssp_init = true;
		}

		// return if dsp_spatial is 0

		if (dsp_spatial.GetInt() == 0)
			return;

		// if listener has not been updated, do nothing

		if ((listener_origin == vec3_origin) &&
			(listener_forward == vec3_origin) &&
			(listener_right == vec3_origin) &&
			(listener_up == vec3_origin))
			return;

		if (!SND_IsInGame())
			return;

		// get time

		double dtime = soundServices.GetHostTime();

		// compare to previous time - if starting new check - don't check for new room until timer expires

		if (g_ssp.chan == 0 && g_ssp.cycle == 0) {
			if (Math.Abs(dtime - g_ssp.last_change) < SND_SPATIAL_WAIT)
				return;
		}

		// cycle through forward, left, rearward vectors, averaging to get left/right delay
		// count[chan][cycle] 0,1 0,2 0,3   1,1 1,2 1,3    2,1 2,2 2,3 ...

		g_ssp.cycle++;

		if (g_ssp.cycle == 3) {
			g_ssp.cycle = 0;

			// cycle through front left, front right, rear left, rear right, front center delays

			g_ssp.chan++;

			if (g_ssp.chan >= chan_max)
				g_ssp.chan = 0;
		}

		// set up traceline from player eyes to surrounding walls

		switch (g_ssp.chan) {
			default:
			case 0: // front left: trace max 100' 'cone' to player's left
				if (g_AudioDevice.IsSurround()) {
					// 4-5 speaker case - front left
					v_dir = (-listener_right + listener_forward2d) / 2.0f;
					v_dir = g_ssp.cycle != 0 ? (g_ssp.cycle == 1 ? -listener_right * 0.5f : listener_forward2d * 0.5f) : v_dir;
				}
				else {
					// 2 speaker case - left
					v_dir = listener_right * -1.0f;
					v_dir2 = g_ssp.cycle != 0 ? (g_ssp.cycle == 1 ? listener_forward2d * 0.5f : -listener_forward2d * 0.5f) : v_dir;
					v_dir = (v_dir + v_dir2) / 2.0f;
				}
				break;

			case 1: // front right: trace max 100' 'cone' to player's right
				if (g_AudioDevice.IsSurround()) {
					// 4-5 speaker case - front right
					v_dir = (listener_right + listener_forward2d) / 2.0f;
					v_dir = g_ssp.cycle != 0 ? (g_ssp.cycle == 1 ? listener_right * 0.5f : listener_forward2d * 0.5f) : v_dir;
				}
				else {
					// 2 speaker case - right
					v_dir = listener_right;
					v_dir2 = g_ssp.cycle != 0 ? (g_ssp.cycle == 1 ? listener_forward2d * 0.5f : -listener_forward2d * 0.5f) : v_dir;
					v_dir = (v_dir + v_dir2) / 2.0f;
				}
				break;

			case 2: // rear left: trace max 100' 'cone' to player's rear left
				v_dir = (listener_right + listener_forward2d) / -2.0f;
				v_dir = g_ssp.cycle != 0 ? (g_ssp.cycle == 1 ? -listener_right * 0.5f : -listener_forward2d * 0.5f) : v_dir;
				break;

			case 3: // rear right: trace max 100' 'cone' to player's rear right
				v_dir = (listener_right - listener_forward2d) / 2.0f;
				v_dir = g_ssp.cycle != 0 ? (g_ssp.cycle == 1 ? listener_right * 0.5f : -listener_forward2d * 0.5f) : v_dir;
				break;

			case 4: // front center: trace max 100' 'cone' to player's front
				v_dir = listener_forward2d;
				v_dir2 = g_ssp.cycle != 0 ? (g_ssp.cycle == 1 ? listener_right * 0.15f : -listener_right * 0.15f) : v_dir;
				v_dir = (v_dir + v_dir2);
				break;
		}

		Vector3 endpoint;
		TraceFilterWorldOnly filter = new();

		Vector3 mainViewOrigin = soundServices.MainViewOrigin();
		endpoint = mainViewOrigin + v_dir * SND_WALL_TRACE_LEN;
		Ray ray = default;
		ray.Init(mainViewOrigin, endpoint);
		g_pEngineTraceClient.TraceRay(in ray, MASK_BLOCK_AUDIO, ref filter, out Trace tr);

		dist = SND_WALL_TRACE_LEN;

		if (tr.DidHit())
			dist = (tr.EndPos - mainViewOrigin).Length();

		g_ssp.dist[g_ssp.chan * 3 + g_ssp.cycle] = (int)dist;

		// set new result in dsp_spatial delay params when all delay values have been filled in

		if (g_ssp.cycle == 0 && g_ssp.chan == 0) {
			// update delay for each channel

			for (int chan = 0; chan < chan_max; chan++) {
				// compute average of 3 traces per channel

				v = (g_ssp.dist[chan * 3 + 0] + g_ssp.dist[chan * 3 + 1] + g_ssp.dist[chan * 3 + 2]) / 3.0F;
				vp = g_ssp.value_prev[chan];

				// only change if 10% difference from previous

				if ((vp != v) && (int)v != 0 && (PercentDifference(v, vp) >= 0.1)) {
					// update when we have data for all L/R && RL/RR channels...

					if ((chan & 0x1) != 0) {
						float vr = MathF.Min(v, (50 * 12.0f));
						float vl = MathF.Min(g_ssp.value_prev[chan - 1], (50 * 12.0f));

						/* UNDONE: not needed, now that this applies only to dsp 'room' buffer

											// ensure minimum separation = average distance to walls

											float dmin = (vl + vr) / 2.0;		// average distance to walls
											float d = vl - vr;					// l/r separation

											// if separation is less than average, increase min

											if (abs(d) < dmin/2)
											{
												if (vl > vr)
													vl += dmin/2 - d;
												else
													vr += dmin/2 - d;
											}
						*/
						DSP_SetSpatialDelay(chan - 1, vl);
						DSP_SetSpatialDelay(chan, vr);
					}

					// update center chan

					if (chan == 4) {
						float vl = MathF.Min(v, (50 * 12.0f));
						DSP_SetSpatialDelay(chan, vl);
					}
				}

				g_ssp.value_prev[chan] = v;

			}

			// update wait timer now that all values have been checked

			g_ssp.last_change = dtime;
		}
	}
}
