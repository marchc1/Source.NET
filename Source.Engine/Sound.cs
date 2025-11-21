using Source.Common.Audio;
using Source.Common.Client;
using Source.Common.Commands;
using Source.Common.Filesystem;
using Source.Common.Mathematics;
using Source.Common.Networking;

using System.Numerics;
using System.Runtime.CompilerServices;

namespace Source.Engine;



public partial class Sound(IFileSystem fileSystem, SoundServices soundServices)
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
		if (AudioDevice == null || AudioDevice == Audio.GetNullDevice()) {
			AudioDevice = Audio.AutoDetectInit(false);
			if (AudioDevice == null) {
				Error("Unable to init audio");
			}
		}
	}

	public void Startup() {

	}

	ClassMemoryPool<SfxTable> SoundPool = new();

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
		LoadSound(sfx, ref Unsafe.NullRef<AudioChannel>());
	}
	private void LoadSound(SfxTable sfx, ref AudioChannel ch) {

	}

	readonly Dictionary<FileNameHandle_t, SfxTable> Sounds = [];

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

	internal void StartSound(in StartSoundParams parms) {

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

	private void ShutdownMixThread() {}

	int oldSampleOutCount;
	int buffers;
	int paintedtime;
	int soundtime;
	void GetSoundTime() {
		int fullsamples = AudioDevice!.DeviceSampleCount() / AudioDevice.DeviceChannels();
		int sampleOutCount = AudioDevice.GetOutputPosition();
		if(sampleOutCount < oldSampleOutCount) {
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
			MIX_PaintChannels(endtime, IsListenerUnderwater);
			MXR_DebugShowMixVolumes();
			MXR_UpdateAllDuckerVolumes();
		}

		AudioDevice!.PaintEnd();
	}

	private void UpdateSoundFade() {

	}

	// todo: everything else here
	// Further research is needed on audio systems
}
