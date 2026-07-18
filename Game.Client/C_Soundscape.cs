using Game.Shared;

using Source.Common;
using Source.Common.Audio;
using Source.Common.Commands;
using Source.Common.Formats.Keyvalues;

using System.Numerics;

namespace Game.Client;

public static class SoundscapeGlobals
{
	public static readonly C_SoundscapeSystem g_SoundscapeSystem = new();

	public static IGameSystem ClientSoundscapeSystem() => g_SoundscapeSystem;

	public static void Soundscape_OnStopAllSounds() => g_SoundscapeSystem.OnStopAllSounds();

	public static void Soundscape_Update(ref AudioParams audio) => g_SoundscapeSystem.UpdateAudioParams(ref audio);
}

public struct LoopingSound
{
	public Vector3 Position;
	public ReadOnlyMemory<char> WaveName;
	public float VolumeTarget;
	public float VolumeCurrent;
	public SoundLevel SoundLevel;
	public int Pitch;
	public int Id;
	public bool IsAmbient;
}

public struct RandomSound
{
	public Vector3 Position;
	public float NextPlayTime;
	public Interval Time;
	public Interval Volume;
	public Interval Pitch;
	public Interval SoundLevel;
	public float MasterVolume;
	public int WaveCount;
	public bool IsAmbient;
	public bool IsRandom;
	public KeyValues? Waves;

	public void Init() => this = default;
}

public struct SubSoundscapeParams
{
	public int RecurseLevel;
	public float MasterVolume;
	public int StartingPosition;
	public int PositionOverride;
	public int AmbientPositionOverride;
	public bool AllowDSP;
	public bool WroteSoundMixer;
	public bool WroteDSPVolume;
}

public class C_SoundscapeSystem : AutoGameSystemPerFrame
{
	public const int MAX_SOUNDSCAPE_RECURSION = 8;
	public const float DEFAULT_SOUND_RADIUS = 36.0f;

	int RestoreFrame;

	readonly List<KeyValues> SoundscapeScripts = [];
	readonly List<KeyValues> Soundscapes = [];
	AudioParams Params;
	readonly List<LoopingSound> LoopingSounds = [];
	readonly List<RandomSound> RandomSounds = [];
	float NextRandomTime;
	int LoopingSoundId;
	int ForcedSoundscapeIndex;
	float ForcedSoundscapeRadius;

	[CvarIgnore] static ConVar? DSPVolumeVar;
	[CvarIgnore] static ConVar? SoundMixerVar;

	public override ReadOnlySpan<char> Name() => "C_SoundScapeSystem";

	public C_SoundscapeSystem() : base("C_SoundScapeSystem") => RestoreFrame = -1;

	public void OnStopAllSounds() => throw new NotImplementedException();

	public override void LevelInitPreEntity() => throw new NotImplementedException();

	public override void LevelInitPostEntity() => throw new NotImplementedException();

	public override void LevelShutdownPreEntity() => throw new NotImplementedException();

	public override void LevelShutdownPostEntity() => throw new NotImplementedException();

	public override void OnSave() => throw new NotImplementedException();

	public override void OnRestore() => throw new NotImplementedException();

	public override void SafeRemoveIfDesired() => throw new NotImplementedException();

	public override void PreRender() => throw new NotImplementedException();

	public override void PostRender() => throw new NotImplementedException();

	public override bool Init() => throw new NotImplementedException();

	public override void Shutdown() => throw new NotImplementedException();

	public override void Update(TimeUnit_t frametime) => throw new NotImplementedException();

	public void PrintDebugInfo() => throw new NotImplementedException();

	public void UpdateAudioParams(ref AudioParams audio) => throw new NotImplementedException();

	public void GetAudioParams(out AudioParams outParams) => throw new NotImplementedException();

	public int GetCurrentSoundscape() => throw new NotImplementedException();

	public void DevReportSoundscapeName(int index) => throw new NotImplementedException();

	public void UpdateLoopingSounds(TimeUnit_t frametime) => throw new NotImplementedException();

	public int AddLoopingAmbient(ReadOnlySpan<char> soundName, float volume, int pitch) => throw new NotImplementedException();

	public void UpdateLoopingSound(ref LoopingSound loopSound) => throw new NotImplementedException();

	public void StopLoopingSound(ref LoopingSound loopSound) => throw new NotImplementedException();

	public int AddLoopingSound(ReadOnlySpan<char> soundName, bool isAmbient, float volume, SoundLevel soundLevel, int pitch, in Vector3 position) => throw new NotImplementedException();

	public int AddRandomSound(in RandomSound sound) => throw new NotImplementedException();

	public void PlayRandomSound(ref RandomSound sound) => throw new NotImplementedException();

	public void UpdateRandomSounds(float gameClock) => throw new NotImplementedException();

	public Vector3 GenerateRandomSoundPosition() => throw new NotImplementedException();

	public void ForceSoundscape(ReadOnlySpan<char> soundscapeName, float radius) => throw new NotImplementedException();

	public int FindSoundscapeByName(ReadOnlySpan<char> soundscapeName) => throw new NotImplementedException();

	public ReadOnlySpan<char> SoundscapeNameByIndex(int index) => throw new NotImplementedException();

	public KeyValues? SoundscapeByIndex(int index) => throw new NotImplementedException();

	public void StartNewSoundscape(KeyValues? soundscape) => throw new NotImplementedException();

	public void StartSubSoundscape(KeyValues? soundscape, ref SubSoundscapeParams parms) => throw new NotImplementedException();

	public void ProcessDSP(KeyValues? dsp) => throw new NotImplementedException();

	public void ProcessDSPPlayer(KeyValues? dspPlayer) => throw new NotImplementedException();

	public void ProcessPlayLooping(KeyValues? playLooping, in SubSoundscapeParams parms) => throw new NotImplementedException();

	public void ProcessPlayRandom(KeyValues? playRandom, in SubSoundscapeParams parms) => throw new NotImplementedException();

	public void ProcessPlaySoundscape(KeyValues? playSoundscape, ref SubSoundscapeParams parms) => throw new NotImplementedException();

	public void ProcessSoundMixer(KeyValues? soundMixer, ref SubSoundscapeParams parms) => throw new NotImplementedException();

	public void ProcessDSPVolume(KeyValues? key, ref SubSoundscapeParams parms) => throw new NotImplementedException();

	bool IsBeingRestored() => throw new NotImplementedException();

	void AddSoundScapeFile(ReadOnlySpan<char> filename) => throw new NotImplementedException();

	void TouchPlayLooping(KeyValues? ambient) => throw new NotImplementedException();

	void TouchPlayRandom(KeyValues? playRandom) => throw new NotImplementedException();

	void TouchWaveFiles(KeyValues? soundScape) => throw new NotImplementedException();

	void TouchSoundFile(ReadOnlySpan<char> wavefile) => throw new NotImplementedException();

	void TouchSoundFiles() => throw new NotImplementedException();
}
