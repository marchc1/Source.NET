using Source.Common;
using Source.Common.Audio;
using Source.Common.Client;
using Source.Common.Commands;
using Source.Common.Filesystem;
using Source.Common.Mathematics;
using Source.Common.Networking;
using Source.Engine.Client;
using Source.Engine.Server;

using System.Numerics;
namespace Source.Engine;

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
		DevMsg("Sound Initialization: Start\n");
		// TODO: Vox

		if (CommandLine.CheckParm("-nosound")) {
			AudioDevice = Audio.GetNullDevice();
			return;
		}

		Initialized = true;
		Startup();

		StopAllSounds(true);
		// AllocDsps?
		DevMsg($"Sound Initialization: Finish, Sampling Rate: {AudioDevice!.DeviceDmaSpeed()} Hz\n");
	}

	readonly IFileSystem fileSystem;
	readonly ICommandLine CommandLine;
	readonly ISoundServices soundServices;

	public Sound(IFileSystem fileSystem, ISoundServices soundServices, ICommandLine commandLine) {
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
		return null;
	}

	public static readonly ClassMemoryPool<SfxTable> SoundPool = new();
	double accumulatedSoundLoadTime = 0;

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

	private long StartDynamicSound(StartSoundParams parms) => 0;
	private long StartStaticSound(in StartSoundParams parms) => 0;
	internal void Shutdown() { }

	Vector3 ListenerOrigin;
	Vector3 ListenerForward;
	Vector3 ListenerRight;
	Vector3 ListenerUp;
	bool IsListenerUnderwater;

	internal void Update() {
		if (!AudioDevice!.IsActive())
			return;

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

		AudioDevice!.UpdateListener(in ListenerOrigin, in ListenerForward, in ListenerRight, in ListenerUp, IsListenerUnderwater);
		int voiceChannelCount = 0;
		int voiceChannelMaxVolume = 0;

		TimeUnit_t now = Platform.Time;
		LastSoundFrame = now;
		LastMixTime = now;
		EstFrameTime = (EstFrameTime * 0.9f) + (soundServices.GetHostFrametime() * 0.1f);
		AudioDevice.Update(EstFrameTime + snd_mixahead.GetDouble());
	}

	private void StopAllSounds(bool clear) {
		if (AudioDevice == null)
			return;

		if (!AudioDevice.IsActive())
			return;
	}
}
