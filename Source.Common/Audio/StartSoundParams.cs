using System.Numerics;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;

namespace Source.Common.Audio;

public static class AttenuationValues
{
	public const float VOL_NORM = 1.0f;

	public const float ATTN_NONE = 0.0f;
	public const float ATTN_NORM = 0.8f;
	public const float ATTN_IDLE = 2.0f;
	public const float ATTN_STATIC = 1.25f;
	public const float ATTN_RICOCHET = 1.5f;
	public const float MAX_ATTENUATION = 3.98f;

	// HL2 world is 8x bigger now! We want to hear gunfire from farther.
	// Don't change this without consulting Kelly or Wedge (sjb).
	public const float ATTN_GUNFIRE = 0.27f;


	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static SoundLevel ATTN_TO_SNDLVL(float a) => (SoundLevel)(int)(a > 0.0f ? (50 + 20 / a) : 0);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static float SNDLVL_TO_ATTN(SoundLevel a) => (int)a > 50 ? (20.0f / (float)(a - 50)) : 4.0F;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static float SNDLVL_TO_ATTN(int a) => a > 50 ? (20.0f / (float)(a - 50)) : 4.0F;
}

public enum SoundLevel : uint
{
	LvlNone = 0,
	Lvl20dB = 20,
	Lvl25dB = 25,
	Lvl30dB = 30,
	Lvl35dB = 35,
	Lvl40dB = 40,
	Lvl45dB = 45,
	Lvl50dB = 50,
	Lvl55dB = 55,
	LvlIdle = 60,
	Lvl60dB = 60,
	Lvl65dB = 65,
	LvlStatic = 66,
	Lvl70dB = 70,
	LvlNorm = 75,
	Lvl75dB = 75,
	Lvl80dB = 80,
	LvlTalking = 80,
	Lvl85dB = 85,
	Lvl90dB = 90,
	Lvl95dB = 95,
	Lvl100dB = 100,
	Lvl105dB = 105,
	Lvl110dB = 110,
	Lvl120dB = 120,
	Lvl130dB = 130,
	LvlGunfire = 140,
	Lvl140dB = 140,
	Lvl150dB = 150,
	Lvl180dB = 180
}

public enum SoundFlags
{
	NoFlags = 0,
	ChangeVolume = 1 << 0,
	ChangePitch = 1 << 1,
	Stop = 1 << 2,
	Spawning = 1 << 3,
	Delay = 1 << 4,
	StopLooping = 1 << 5,
	Speaker = 1 << 6,
	ShouldPause = 1 << 7,
	IgnorePhonemes = 1 << 8,
	IgnoreName = 1 << 9,
	DoNotOverwriteExistingOnChannel = 1 << 10,
}

public enum SoundEntityChannel
{
	Replace = -1,

	Auto = 0,
	Weapon = 1,
	Voice = 2,
	Item = 3,
	Body = 4,
	Stream = 5,
	Static = 6,
	Voice2 = 7,
	VoiceBase = 8,

	UserBase = VoiceBase + 128
}

public class SfxTable
{
	// The audio system implements these (CSfxTable's name pool lives in snd_dma).
	public static class Impl
	{
		public delegate ReadOnlySpan<char> GetNameFn(SfxTable sfx);
		public delegate FileNameHandle_t GetFileNameHandleFn(SfxTable sfx);
		public delegate bool IsPrecachedSoundFn(SfxTable sfx);
		public delegate void OnNameChangedFn(SfxTable sfx, ReadOnlySpan<char> name);
		public delegate bool IsValidNamePoolIndexFn(FileNameHandle_t index);
		public static GetNameFn? GetName;
		public static GetFileNameHandleFn? GetFileNameHandle;
		public static IsPrecachedSoundFn? IsPrecachedSound;
		public static OnNameChangedFn? OnNameChanged;
		public static IsValidNamePoolIndexFn? IsValidNamePoolIndex;
	}

	public FileNameHandle_t NamePoolIndex;
	public AudioSource? Source;

	public bool UseErrorFilename;
	public bool IsUISound;
	public bool IsLateLoad;
	public bool MixGroupsCached;
	public byte MixGroupCount;
	// UNDONE: Use a fixed bit vec here?
	public readonly byte[] MixGroupList = new byte[8];

	// gets sound name, possible decoracted with prefixes
	public virtual ReadOnlySpan<char> GetName() => Impl.GetName != null ? Impl.GetName(this) : null;
	// gets the filename, the part after the optional prefixes
	public ReadOnlySpan<char> GetFileName() {
		ReadOnlySpan<char> name = GetName();
		return !name.IsEmpty ? SoundCharsUtils.SkipSoundChars(name) : null;
	}
	public FileNameHandle_t GetFileNameHandle() => Impl.GetFileNameHandle != null ? Impl.GetFileNameHandle(this) : 0;

	public void SetNamePoolIndex(FileNameHandle_t index) {
		NamePoolIndex = index;
		if (Impl.IsValidNamePoolIndex != null && Impl.IsValidNamePoolIndex(NamePoolIndex))
			OnNameChanged(GetName());
	}
	public bool IsPrecachedSound() => Impl.IsPrecachedSound != null && Impl.IsPrecachedSound(this);
	public void OnNameChanged(ReadOnlySpan<char> name) => Impl.OnNameChanged?.Invoke(this, name);
}

public struct StartSoundParams
{
	public bool StaticSound;
	public int UserData;
	public int SoundSource;
	public SoundEntityChannel EntChannel;
	public SfxTable? Sfx;
	public Vector3 Origin;
	public Vector3 Direction;
	public bool UpdatePositions;
	public float Volume;
	public SoundLevel SoundLevel;
	public SoundFlags Flags;
	public int Pitch;
	public int SpecialDSP;
	public bool FromServer;
	public TimeUnit_t Delay;
	public int SpeakerEntity;
	public bool SuppressRecording;
	public int InitialStreamPosition;

	public StartSoundParams() {
		UpdatePositions = true;
		Volume = 1;
		SoundLevel = SoundLevel.LvlNorm;
		Pitch = 100;
		SpeakerEntity = -1;
	}
}
