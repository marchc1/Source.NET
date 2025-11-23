using Source.Common;
using Source.Common.Audio;
using Source.Common.Client;
using Source.Common.Commands;
using Source.Common.Filesystem;
using Source.Common.Mathematics;
using Source.Common.Networking;
using Source.Engine.Client;
using Source.Engine.Server;

using System;
using System.Diagnostics.Tracing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

using static Source.Common.Networking.svc_ClassInfo;

using static Source.Engine.SoundGlobals;
namespace Source.Engine;

[EngineComponent]
public static class SoundGlobals
{

	public static readonly Dictionary<FileNameHandle_t, SfxTable> Sounds = [];
	[Dependency] public static IFileSystem filesystem = null!;

	[Dependency] public static GameServer sv = null!;
	[Dependency] public static ClientState cl = null!;
}

public class SfxTable : ISfxTable
{
	public AudioSource? Source { get; set; }
	public bool UseErrorFilename { get; set; }
	public bool IsUISound { get; set; }
	public bool IsLateLoad { get; set; }
	public bool MixGroupsCached { get; set; }
	public byte MixGroupCount { get; set; }

	public FileNameHandle_t NamePoolIndex;
	public void SetNamePoolIndex(FileNameHandle_t handle) {
		NamePoolIndex = handle;
		if (NamePoolIndex != FileNameHandle_t.MaxValue) {
			// on name changed todo
		}
	}
	public ReadOnlySpan<char> GetName() {
		if (Sounds.ContainsKey(NamePoolIndex)) {
			ReadOnlySpan<char> str = filesystem.String(NamePoolIndex);
			return str;
		}
		return null;
	}
	public ReadOnlySpan<char> GetFileName() {
		ReadOnlySpan<char> name = GetName();
		return !name.IsEmpty ? SoundCharsUtils.SkipSoundChars(name) : null;
	}

	public bool IsPrecachedSound() {
		ReadOnlySpan<char> name = GetName();
		if (sv.IsActive())
			return false; // Todo

		return cl.LookupSoundIndex(name) != -1 ? true : false;
	}
}

public partial class Sound
{
	readonly ConVar snd_surround = new("snd_surround_speakers", "-1", FCvar.InternalUse);
	readonly ConVar snd_legacy_surround = new("snd_legacy_surround", "0", FCvar.Archive);
	readonly ConVar snd_noextraupdate = new("snd_noextraupdate", "0", 0);
	readonly ConVar snd_show = new("snd_show", "0", FCvar.Cheat, "Show sounds info");
	readonly ConVar snd_visualize = new("snd_visualize", "0", FCvar.Cheat, "Show sounds location in world");
	readonly ConVar snd_pitchquality = new("snd_pitchquality", "1", FCvar.Archive);      // 1) use high quality pitch shifters

	readonly static ConVar volume = new("volume", "1.0", FCvar.Archive, "Sound volume", 0.0, 1.0);
	readonly ConVar snd_musicvolume = new("snd_musicvolume", "1.0", FCvar.Archive, "Music volume", 0.0, 1.0);

	readonly ConVar snd_mixahead = new("snd_mixahead", "0.1", FCvar.Archive);
	readonly ConVar snd_mix_async = new("snd_mix_async", "0", 0);

	public bool Initialized;
	public IAudioDevice? AudioDevice;

	public void Init() {
		if (sv.IsDedicated() && !CommandLine.CheckParm("-forcesound"))
			return;

		DevMsg("Sound Initialization: Start\n");
		// TODO: Vox

		if (CommandLine.CheckParm("-nosound")) {
			AudioDevice = Audio.GetNullDevice();
			return;
		}

		Initialized = true;
		ActiveChannels.Init();
		Startup();

		StopAllSounds(true);
		// AllocDsps?
		DevMsg($"Sound Initialization: Finish, Sampling Rate: {AudioDevice!.DeviceDmaSpeed()} Hz\n");
	}

	readonly IFileSystem fileSystem;
	readonly ICommandLine CommandLine;
	readonly ISoundServices soundServices;

	public Sound(IFileSystem fileSystem, ISoundServices soundServices, ICommandLine commandLine) {
		ActiveChannels = new(this);
		this.fileSystem = fileSystem;
		this.soundServices = soundServices;
		this.CommandLine = commandLine;
	}

	public void Startup() {
		if (AudioDevice == null || AudioDevice == Audio.GetNullDevice()) {
			AudioDevice = Audio.AutoDetectInit(false);
			if (AudioDevice == null) {
				Error("Unable to init audio");
			}
		}
	}
	public SfxTable? PrecacheSound(ReadOnlySpan<char> name) {
		if (AudioDevice == null)
			return null;

		if (!AudioDevice.IsActive())
			return null;

		SfxTable? sfx = FindName(name, out _);
		if (sfx != null)
			LoadSound(sfx);
		else
			AssertMsg(false, "Sound.PrecacheSound: Failed to create sfx");

		return sfx;
	}

	private void LoadSound(SfxTable sfx) {
		LoadSound(sfx, null);
	}

	public static readonly ClassMemoryPool<SfxTable> SoundPool = new();
	double accumulatedSoundLoadTime = 0;

	private AudioSource? LoadSound(SfxTable sfx, AudioChannel? ch) {
		if (sfx.Source == null) {
			double st = Platform.Time;

			bool bUserVox = false;

			bool bStream = SoundCharsUtils.TestSoundChar(sfx.GetName(), SoundChars.Stream);
			if (!bStream)
				bUserVox = SoundCharsUtils.TestSoundChar(sfx.GetName(), SoundChars.UserVox);

			//  if (bStream)
			//  	sfx.Source = Audio_CreateStreamedWave(sfx);
			//  else {
			//  	if (bUserVox)
			//  		sfx.Source = Voice_SetupAudioSource(ch!.SoundSource, ch.EntChannel);
			//  	else // load all into memory directly
			//  		sfx.Source = Audio_CreateMemoryWave(sfx);
			//  }

			double ed = Platform.Time;
			accumulatedSoundLoadTime += (ed - st);
		}
		else {
			sfx.Source.CheckAudioSourceCache();
		}

		if (sfx.Source == null)
			return null;


		// first time to load?  Create the mixer
		if (ch != null && ch.Mixer == null) {
			ch.Mixer = sfx.Source.CreateMixer(ch.InitialStreamPosition);
			if (ch.Mixer == null) {
				return null;
			}
		}

		return sfx.Source;
	}


	private SfxTable? FindName(ReadOnlySpan<char> inName, out bool inCache) {
		SfxTable? sfx = null;

		if (inName.IsEmpty)
			Error("Sound.FindName: NULL\n");

		ReadOnlySpan<char> name = inName;

		// see if already loaded
		FileNameHandle_t fnHandle = fileSystem.FindOrAddFileName(name);

		if (Sounds.TryGetValue(fnHandle, out sfx)) {
			inCache = (sfx.Source != null && sfx.Source.IsCached()) ? true : false;
			return sfx;
		}
		else {
			Sounds[fnHandle] = SoundPool.Alloc();
			sfx = Sounds[fnHandle];

			sfx.SetNamePoolIndex(fnHandle);
			sfx.Source = null;

			inCache = false;
		}

		return sfx;
	}

	public void MarkUISound(SfxTable sound) {
		sound.IsUISound = true;
	}

	internal long StartSound(in StartSoundParams parms) {
		if (parms.Sfx == null)
			return 0;

		if (parms.StaticSound)
			return StartStaticSound(parms);
		else
			return StartDynamicSound(parms);
	}
	public readonly ActiveChannels ActiveChannels;
	public readonly AudioChannel[] Channels = new AudioChannel[AudioChannel.MAX_CHANNELS].InstantiateArray();

	readonly ConVar snd_showstart = new("snd_showstart", "0", FCvar.Cheat);  // showstart always skips info on player footsteps!
																			 // 1 - show sound name, channel, volume, time 
																			 // 2 - show dspmix, distmix, dspface, l/r/f/r vols
																			 // 3 - show sound origin coords
																			 // 4 - show gain of dsp_room
																			 // 5 - show dB loss due to obscured sound
																			 // 6 - reserved
																			 // 7 - show 2 and total gain & dist in ft. to sound source

	static bool IsValidSampleRate(int rate) => rate == IAudioDevice.SOUND_11k || rate == IAudioDevice.SOUND_22k || rate == IAudioDevice.SOUND_44k;

	private long StartDynamicSound(StartSoundParams parms) {
		return 0;
	}

	private long StartStaticSound(in StartSoundParams parms) {
		return 0;
	}

	internal void Shutdown() {

	}

	Vector3 ListenerOrigin;
	Vector3 ListenerForward;
	Vector3 ListenerRight;
	Vector3 ListenerUp;
	bool IsListenerUnderwater;

	internal void Update() {
		if (!AudioDevice!.IsActive())
			return;

		UpdateSoundFade();

		ListenerOrigin = vec3_origin;
		ListenerForward = vec3_origin;
		ListenerRight = vec3_origin;
		ListenerUp = vec3_origin;
		IsListenerUnderwater = false;

		PerformUpdate();
	}

	internal void Update(in AudioState audioState) {
		if (!AudioDevice!.IsActive())
			return;

		UpdateSoundFade();

		ListenerOrigin = audioState.Origin;
		MathLib.AngleVectors(in audioState.Angles, out ListenerForward, out ListenerRight, out ListenerUp);
		IsListenerUnderwater = audioState.IsUnderwater;

		PerformUpdate();
	}

	TimeUnit_t LastSoundFrame;
	TimeUnit_t LastMixTime;
	TimeUnit_t EstFrameTime;

	private void PerformUpdate() {
		// Something should've set up the ListenerOrigin/ListenerDirection/IsListenerUnderwater variables before calling this method!

		AudioDevice!.UpdateListener(ListenerOrigin, ListenerForward, ListenerRight, ListenerUp);
		int voiceChannelCount = 0;
		int voiceChannelMaxVolume = 0;

		TimeUnit_t now = Platform.Time;
		LastSoundFrame = now;
		LastMixTime = now;
		EstFrameTime = (EstFrameTime * 0.9f) + (soundServices.GetHostFrametime() * 0.1f);
		Update_(EstFrameTime + snd_mixahead.GetDouble());
	}

	private void Update_(double mixAheadTime) {
		// TODO: snd_mix_async?

		ShutdownMixThread();
		UpdateGuts(mixAheadTime);
	}

	private void ShutdownMixThread() { }

	int oldSampleOutCount;
	int buffers;
	int paintedtime;
	int soundtime;
	void GetSoundTime() {
		int fullsamples = AudioDevice!.DeviceSampleCount() / AudioDevice.DeviceChannels();
		int sampleOutCount = AudioDevice.GetOutputPosition();
		if (sampleOutCount < oldSampleOutCount) {
			// buffer wrapped
			buffers++;
			if (paintedtime > 0x70000000) {
				// time to chop things off to avoid 32 bit limits
				buffers = 0;
				paintedtime = fullsamples;
				StopAllSounds(true);
			}
		}

		oldSampleOutCount = sampleOutCount;

		// No cl_movieinfo/replay rn
		soundtime = buffers * fullsamples + sampleOutCount;
	}

	private void StopAllSounds(bool clear) {
		if (AudioDevice == null)
			return;

		if (!AudioDevice.IsActive())
			return;

		// todo
	}

	private void UpdateGuts(double mixAheadTime) {
		GetSoundTime();

		uint endtime = (uint)AudioDevice!.PaintBegin(mixAheadTime, soundtime, paintedtime);
		int samples = (int)(endtime - paintedtime);
		samples = samples < 0 ? 0 : samples;
		if (samples != 0) {
			// MIX_PaintChannels
		}

		AudioDevice!.PaintEnd();
	}

	private void UpdateSoundFade() {

	}

	// todo: everything else here
	// Further research is needed on audio systems
}
