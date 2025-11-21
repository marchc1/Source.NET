using Source.Common.Audio;
using Source.Common.Client;
using Source.Common.Filesystem;
using Source.Common.Mathematics;
using Source.Common.Networking;

using System.Numerics;
using System.Runtime.CompilerServices;

namespace Source.Engine;



public class Sound(IFileSystem fileSystem)
{
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
	}

	internal void Update(in AudioState audioState) {
		if (!AudioDevice!.IsActive())
			return;

		UpdateSoundFade();

		ListenerOrigin = audioState.Origin;
		MathLib.AngleVectors(in audioState.Angles, out ListenerForward, out ListenerRight, out ListenerUp);
	}

	private void _Update() {

	}

	private void UpdateSoundFade() {

	}

	// todo: everything else here
	// Further research is needed on audio systems
}
