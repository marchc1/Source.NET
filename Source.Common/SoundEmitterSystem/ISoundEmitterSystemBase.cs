using SharpCompress.Common;

using Source.Common.Audio;
using Source.Common.SoundEmitterSystem;
using Source.Common.Utilities;

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;
using System.Xml.Linq;

using PitchInterval = Source.Common.SoundEmitterSystem.SoundInterval<System.Byte>;
using SoundLevelInterval = Source.Common.SoundEmitterSystem.SoundInterval<Source.Common.Audio.SoundLevel>;
using VolumeInterval = Source.Common.SoundEmitterSystem.SoundInterval<System.Half>;

namespace Source.Common.SoundEmitterSystem;

public struct Interval
{
	public float Start;
	public float Range;
	public static bool Compare(in Interval i1, in Interval i2) {
		return memcmp(in i1, in i2) == 0;
	}

	public static bool Compare<T>(in SoundInterval<T> i1, in SoundInterval<T> i2) where T : unmanaged {
		return memcmp(in i1, in i2) == 0;
	}

	public static Interval Read(ReadOnlySpan<char> str) {
		Interval tmp;
		tmp.Start = 0;
		tmp.Range = 0;

		int comma = str.IndexOf(',');
		if (comma >= 0) {
			tmp.Start = float.Parse(str.Slice(0, comma));
			tmp.Range = float.Parse(str.Slice(comma + 1)) - tmp.Start;
		}
		else if (str.Length > 0) {
			tmp.Start = float.Parse(str);
		}

		return tmp;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SoundFile
{
	public SoundFile() {
		Symbol = new(UTL_INVAL_SYMBOL);
		Gender = Gender.None;
		Available = true;
	}
	public UtlSymbol Symbol;
	public Gender Gender;
	public bool Available;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SoundInterval<T>
{
	public T Start;
	public T Range;
	public readonly ref Interval ToInterval(ref Interval dest) {
		dest.Start = (float?)(object?)Start ?? 0;
		dest.Range = (float?)(object?)Range ?? 0;
		return ref dest;
	}
	public void FromInterval(in Interval from) {
		Start = (T)(object)from.Start;
		Range = (T)(object)from.Range;
	}
	public readonly float Random() => RandomFloat((float?)(object?)Start ?? 0, ((float?)(object?)(Start) ?? 0) + ((float?)(object?)(Range) ?? 0));
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public ref struct SoundParametersInternal : IEquatable<SoundParametersInternal>
{

	public SoundParametersInternal() {

	}

	public void CopyFrom(ref SoundParametersInternal src) {

	}

	public bool Equals(SoundParametersInternal other) {
		if (Unsafe.AreSame(in this, in other))
			return true;

		if (Channel != other.Channel)
			return false;
		if (!Interval.Compare(Volume, other.Volume))
			return false;
		if (!Interval.Compare(Pitch, other.Pitch))
			return false;
		if (!Interval.Compare(SoundLevel, other.SoundLevel))
			return false;
		if (DelayMsec != other.DelayMsec)
			return false;
		if (bPlayToOwnerOnly != other.bPlayToOwnerOnly)
			return false;

		if (SoundNames.Length != other.SoundNames.Length)
			return false;

		// Compare items
		int c = SoundNames.Length;
		for (int i = 0; i < c; i++) {
			if (GetSoundNames()[i].Symbol != other.GetSoundNames()[i].Symbol)
				return false;
		}

		return true;
	}


	// NOTE:  Needs to reflect the soundlevel_t enum defined in soundflags.h
	static readonly FrozenDictionary<SoundLevel, string> g_pSoundLevels = (new Dictionary<SoundLevel, string> {
		{  Audio.SoundLevel.LvlNone, "SNDLVL_NONE" },
		{ Audio.SoundLevel.Lvl20dB, "SNDLVL_20dB" },
		{ Audio.SoundLevel.Lvl25dB, "SNDLVL_25dB" },
		{ Audio.SoundLevel.Lvl30dB, "SNDLVL_30dB" },
		{ Audio.SoundLevel.Lvl35dB, "SNDLVL_35dB" },
		{ Audio.SoundLevel.Lvl40dB, "SNDLVL_40dB" },
		{ Audio.SoundLevel.Lvl45dB, "SNDLVL_45dB" },
		{ Audio.SoundLevel.Lvl50dB, "SNDLVL_50dB" },
		{ Audio.SoundLevel.Lvl55dB, "SNDLVL_55dB" },
		{ Audio.SoundLevel.LvlIdle, "SNDLVL_IDLE" },
		{ Audio.SoundLevel.LvlTalking, "SNDLVL_TALKING" },
		{ Audio.SoundLevel.Lvl60dB, "SNDLVL_60dB" },
		{ Audio.SoundLevel.Lvl65dB, "SNDLVL_65dB" },
		{ Audio.SoundLevel.LvlStatic, "SNDLVL_STATIC"  },
		{ Audio.SoundLevel.Lvl70dB, "SNDLVL_70dB"  }   ,
		{ Audio.SoundLevel.LvlNorm, "SNDLVL_NORM"  }   ,
		{ Audio.SoundLevel.Lvl75dB, "SNDLVL_75dB"  }   ,
		{ Audio.SoundLevel.Lvl80dB, "SNDLVL_80dB"  }   ,
		{ Audio.SoundLevel.Lvl85dB, "SNDLVL_85dB"  }   ,
		{ Audio.SoundLevel.Lvl90dB, "SNDLVL_90dB"  }   ,
		{ Audio.SoundLevel.Lvl95dB, "SNDLVL_95dB"  }   ,
		{ Audio.SoundLevel.Lvl100dB, "SNDLVL_100dB"  },
		{ Audio.SoundLevel.Lvl105dB, "SNDLVL_105dB"  },
		{ Audio.SoundLevel.Lvl110dB, "SNDLVL_110dB"  },
		{ Audio.SoundLevel.Lvl120dB, "SNDLVL_120dB"  },
		{ Audio.SoundLevel.Lvl130dB, "SNDLVL_130dB"  },
		{ Audio.SoundLevel.LvlGunfire, "SNDLVL_GUNFIRE" },
		{ Audio.SoundLevel.Lvl140dB, "SNDLVL_140dB" },
		{ Audio.SoundLevel.Lvl150dB, "SNDLVL_150dB" },
		{ Audio.SoundLevel.Lvl180dB, "SNDLVL_180dB"  },
	}).ToFrozenDictionary();

	static readonly FrozenDictionary<ulong, SoundLevel> g_pSoundLevelsRev =
		g_pSoundLevels
		.Select(x => new KeyValuePair<ulong, SoundLevel>(x.Value.Hash(invariant: false), x.Key))
		.ToFrozenDictionary();

	static readonly FrozenDictionary<SoundEntityChannel, string> g_pChannelNames = (new Dictionary<SoundEntityChannel, string> {
		{ SoundEntityChannel.Auto, "CHAN_AUTO"  },
		{ SoundEntityChannel.Weapon, "CHAN_WEAPON"  },
		{ SoundEntityChannel.Voice, "CHAN_VOICE"  },
		{ SoundEntityChannel.Item, "CHAN_ITEM"  },
		{ SoundEntityChannel.Body, "CHAN_BODY"  },
		{ SoundEntityChannel.Stream, "CHAN_STREAM"  },
		{ SoundEntityChannel.Static, "CHAN_STATIC"  },
		{ SoundEntityChannel.Voice2, "CHAN_VOICE2"  },
	}).ToFrozenDictionary();

	static readonly FrozenDictionary<ulong, SoundEntityChannel> g_pChannelNamesRev =
		g_pChannelNames
		.Select(x => new KeyValuePair<ulong, SoundEntityChannel>(x.Value.Hash(invariant: false), x.Key))
		.ToFrozenDictionary();

	static readonly FrozenDictionary<float, string> g_pVolumeLevels = (new Dictionary<float, string> {
		{ VOL_NORM, "VOL_NORM"  },
	}).ToFrozenDictionary();

	static readonly FrozenDictionary<ulong, float> g_pVolumeLevelsRev =
		g_pVolumeLevels
		.Select(x => new KeyValuePair<ulong, float>(x.Value.Hash(invariant: false), x.Key))
		.ToFrozenDictionary();

	static readonly FrozenDictionary<byte, string> g_pPitchLookup = (new Dictionary<byte, string> {
		{ PITCH_NORM, "PITCH_NORM"  },
		{ PITCH_LOW, "PITCH_LOW"  },
		{ PITCH_HIGH, "PITCH_HIGH"  },
	}).ToFrozenDictionary();

	static readonly FrozenDictionary<ulong, byte> g_pPitchLookupRev =
		g_pPitchLookup
		.Select(x => new KeyValuePair<ulong, byte>(x.Value.Hash(invariant: false), x.Key))
		.ToFrozenDictionary();

	public static ReadOnlySpan<char> _SoundLevelToString(SoundLevel level) {
		if (g_pSoundLevels.TryGetValue(level, out string? v))
			return v;

		return $"{level}";
	}

	public static ReadOnlySpan<char> _ChannelToString(int channel) {
		if (g_pChannelNames.TryGetValue((SoundEntityChannel)channel, out string? v))
			return v;

		return $"{channel}";
	}

	public static ReadOnlySpan<char> _ChannelToString(SoundEntityChannel channel) {
		if (g_pChannelNames.TryGetValue(channel, out string? v))
			return v;

		return $"{channel}";
	}

	public static ReadOnlySpan<char> _VolumeToString(float volume) {
		if (g_pVolumeLevels.TryGetValue(volume, out string? v))
			return v;

		return $"{volume}";
	}

	public static ReadOnlySpan<char> _PitchToString(float pitch) {
		if (g_pPitchLookup.TryGetValue((byte)pitch, out string? v))
			return v;

		return $"{pitch}";
	}

	public static ReadOnlySpan<char> _PitchToString(byte pitch) {
		if (g_pPitchLookup.TryGetValue(pitch, out string? v))
			return v;

		return $"{pitch}";
	}

	public ReadOnlySpan<char> VolumeToString() {
		if (Volume.Range == (Half)0)
			return _VolumeToString((float)Volume.Start);

		return $"{Volume.Start}, {Volume.Start + Volume.Range}";
	}
	public ReadOnlySpan<char> ChannelToString() => _ChannelToString(Channel);
	public ReadOnlySpan<char> SoundLevelToString() {
		if (SoundLevel.Range == 0)
			return _SoundLevelToString((SoundLevel)(int)SoundLevel.Start);

		return $"{SoundLevel.Start}, {(SoundLevel)((int)SoundLevel.Start + (int)SoundLevel.Range)}";
	}
	public ReadOnlySpan<char> PitchToString() {
		if (Pitch.Range == 0)
			return _PitchToString((int)Pitch.Start);

		return $"{Pitch.Start}, {Pitch.Start + Pitch.Range}";
	}

	public void VolumeFromString(ReadOnlySpan<char> sz) {
		if (0 == strcmp(sz, "VOL_NORM")) {
			Volume.Start = (Half)VOL_NORM;
			Volume.Range = (Half)0.0f;
		}
		else {
			Volume.FromInterval(Interval.Read(sz));
		}
	}
	public SoundEntityChannel TextToChannel(ReadOnlySpan<char> name) {
		if (name.IsEmpty) {
			Assert(false);
			return SoundEntityChannel.Auto;
		}

		if (strcmp(name[..Math.Max(name.Length, "chan_".Length - 1)], "chan_") != 0)
			return (SoundEntityChannel)(int.TryParse(name, out int i) ? i : 0);

		if (g_pChannelNamesRev.TryGetValue(name.Hash(invariant: false), out SoundEntityChannel chan))
			return chan;

		// At this point, it starts with chan_ but is not recognized
		// atoi would return 0, so just do chan auto
		DevMsg($"SoundEmitterSystem:  Warning, unknown channel type in sounds.txt ({name})\n");

		return SoundEntityChannel.Auto;
	}

	public void ChannelFromString(ReadOnlySpan<char> sz) {
		Channel = TextToChannel(sz);
	}
	public const string SNDLVL_PREFIX = "SNDLVL_";

	public SoundLevel TextToSoundLevel(ReadOnlySpan<char> key) {
		if (key.IsEmpty) {
			Assert(0);
			return Audio.SoundLevel.LvlNorm;
		}

		if (g_pSoundLevelsRev.TryGetValue(key.Hash(invariant: false), out SoundLevel level))
			return level;

		if (0 == stricmp(key[..Math.Min((int)strlen(SNDLVL_PREFIX), key.Length)], SNDLVL_PREFIX)) {
			ReadOnlySpan<char> val = key[(int)strlen(SNDLVL_PREFIX)..];
			int.TryParse(val, out int sndlvl);
			if (sndlvl > 0 && sndlvl <= 180) 
				return (SoundLevel)sndlvl;
		}

		DevMsg($"SoundEmitterSystem:  Unknown sound level {key}\n");

		return Audio.SoundLevel.LvlNorm;
	}

	public void PitchFromString(ReadOnlySpan<char> sz) {
		if (0 == strcmp(sz, "PITCH_NORM")) {
			Pitch.Start = PITCH_NORM;
			Pitch.Range = 0;
		}
		else if (0 == strcmp(sz, "PITCH_LOW")) {
			Pitch.Start = PITCH_LOW;
			Pitch.Range = 0;
		}
		else if (0 == strcmp(sz, "PITCH_HIGH")) {
			Pitch.Start = PITCH_HIGH;
			Pitch.Range = 0;
		}
		else
			Pitch.FromInterval(Interval.Read(sz));
	}
	public void SoundLevelFromString(ReadOnlySpan<char> sz) {
		if (0 == strcmp(sz[..Math.Max(sz.Length, "SNDLVL_".Length - 1)], "SNDLVL_")) {
			SoundLevel.Start = TextToSoundLevel(sz);
			SoundLevel.Range = 0;
		}
		else
			SoundLevel.FromInterval(Interval.Read(sz));
	}

	public readonly SoundEntityChannel GetChannel() => Channel;

	public unsafe ref readonly VolumeInterval GetVolume() => ref Volume;
	public unsafe ref readonly PitchInterval GetPitch() => ref Pitch;
	public unsafe ref readonly SoundLevelInterval GetSoundLevel() => ref SoundLevel;
	public int GetDelayMsec() => DelayMsec;
	public bool OnlyPlayToOwner() => bPlayToOwnerOnly;
	public bool HadMissingWaveFiles() => bHadMissingWaveFiles;
	public bool UsesGenderToken() => bUsesGenderToken;
	public bool ShouldPreload() => bShouldPreload;

	public void SetChannel(SoundEntityChannel newChannel) => Channel = newChannel;
	public void SetChannel(int newChannel) => Channel = (SoundEntityChannel)newChannel;
	public void SetVolume(float start, float range = 0.0f) { Volume.Start = (Half)start; Volume.Range = (Half)range; }
	public void SetPitch(float start, float range = 0.0f) { Pitch.Start = (byte)start; Pitch.Range = (byte)range; }
	public void SetSoundLevel(float start, float range = 0.0f) { SoundLevel.Start = (SoundLevel)start; SoundLevel.Range = (SoundLevel)range; }
	public void SetDelayMsec(int delay) => DelayMsec = (ushort)delay;
	public void SetShouldPreload(bool bShouldPreload) => this.bShouldPreload = bShouldPreload;
	public void SetOnlyPlayToOwner(bool b) => bPlayToOwnerOnly = b;
	public void SetHadMissingWaveFiles(bool b) => bHadMissingWaveFiles = b;
	public void SetUsesGenderToken(bool b) => bUsesGenderToken = b;

	public void AddSoundName(in SoundFile soundFile) => AddToTail(ref SoundNames, ref SoundNamesCount, soundFile);
	public int NumSoundNames() => SoundNamesCount;
	public Span<SoundFile> GetSoundNamesForEdit() => SoundNames;
	public ReadOnlySpan<SoundFile> GetSoundNames() => SoundNames;

	public void AddConvertedName(in SoundFile soundFile) => AddToTail(ref ConvertedNames, ref ConvertedNamesCount, soundFile);
	int NumConvertedNames() => ConvertedNamesCount;
	Span<SoundFile> GetConvertedNamesForEdit() => ConvertedNames;
	ReadOnlySpan<SoundFile> GetConvertedNames() => ConvertedNames;

	void AddToTail(ref Span<SoundFile> dest, ref ushort destCount, in SoundFile source) {

	}

	Span<SoundFile> SoundNames;           // 4
	Span<SoundFile> ConvertedNames;       // 8

	ushort SoundNamesCount;           // 4
	ushort ConvertedNamesCount;       // 8

	VolumeInterval Volume;                   // 16
	SoundLevelInterval SoundLevel;               // 20
	PitchInterval Pitch;                 // 22
	SoundEntityChannel Channel;             // 24
	ushort DelayMsec;              // 26

	bool bPlayToOwnerOnly; // For weapon sounds...	// 27
						   // Internal use, for warning about missing .wav files
	bool bHadMissingWaveFiles;
	bool bUsesGenderToken;
	bool bShouldPreload;
}
public struct SoundParameters
{
	public SoundParameters() {
		Channel = SoundEntityChannel.Auto; // 0
		Volume = VOL_NORM;  // 1.0f
		Pitch = PITCH_NORM; // 100

		PitchLow = PITCH_NORM;
		PitchHigh = PITCH_NORM;

		SoundLevel = SoundLevel.LvlNorm; // 75dB
		SoundName[0] = '\0';
		PlayToOwnerOnly = false;
		Count = 0;

		DelayMsec = 0;
	}

	public SoundEntityChannel Channel;
	public float Volume;
	public int Pitch;
	public int PitchLow, PitchHigh;
	public SoundLevel SoundLevel;
	public bool PlayToOwnerOnly;
	public int Count;
	public InlineArray128<char> SoundName;
	public int DelayMsec;
}

public enum Gender : sbyte
{
	None,
	Male,
	Female
}

public interface ISoundEmitterSystemBase
{
	bool ModInit();
	void ModShutdown();

	int GetSoundIndex(ReadOnlySpan<char> name);
	bool IsValidIndex(int index);
	int GetSoundCount();

	ReadOnlySpan<char> GetSoundName(int index);
	bool GetParametersForSound(ReadOnlySpan<char> soundname, SoundParameters parms, Gender gender, bool isbeingemitted = false);

	ReadOnlySpan<char> GetWaveName(out UtlSymbol sym);
	UtlSymbol AddWaveName(ReadOnlySpan<char> name);

	SoundLevel LookupSoundLevel(ReadOnlySpan<char> soundname);
	ReadOnlySpan<char> GetWavFileForSound(ReadOnlySpan<char> soundname, ReadOnlySpan<char> actormodel);
	ReadOnlySpan<char> GetWavFileForSound(ReadOnlySpan<char> soundname, Gender gender);
	int CheckForMissingWavFiles(bool verbose);
	ReadOnlySpan<char> GetSourceFileForSound(int index);

	// Iteration methods
	int First();
	int Next(int i);
	int InvalidIndex();

	ref SoundParametersInternal InternalGetParametersForSound(int index);

	// The host application is responsible for dealing with dirty sound scripts, etc.
	bool AddSound(ReadOnlySpan<char> soundname, ReadOnlySpan<char> scriptfile, in SoundParametersInternal parms);
	void RemoveSound(ReadOnlySpan<char> soundname);
	void MoveSound(ReadOnlySpan<char> soundname, ReadOnlySpan<char> newscript);
	void RenameSound(ReadOnlySpan<char> soundname, ReadOnlySpan<char> newname);

	void UpdateSoundParameters(ReadOnlySpan<char> soundname, in SoundParametersInternal parms);

	int GetNumSoundScripts();
	ReadOnlySpan<char> GetSoundScriptName(int index);
	bool IsSoundScriptDirty(int index);
	int FindSoundScript(ReadOnlySpan<char> name);
	void SaveChangesToSoundScript(int scriptindex);

	void ExpandSoundNameMacros(in SoundParametersInternal parms, ReadOnlySpan<char> wavename);
	Gender GetActorGender(ReadOnlySpan<char> actormodel);
	void GenderExpandString(ReadOnlySpan<char> actormodel, ReadOnlySpan<char> inText, Span<char> outText);
	void GenderExpandString(Gender gender, ReadOnlySpan<char> inText, Span<char> outText);
	bool IsUsingGenderToken(ReadOnlySpan<char> soundname);

	// For blowing away caches based on filetimstamps of the manifest, or of any of the
	//  .txt files that are read into the sound emitter system
	uint GetManifestFileTimeChecksum();

	// Called from both client and server (single player) or just one (server only in dedicated server and client only if connected to a remote server)
	// Called by LevelInitPreEntity to override sound scripts for the mod with level specific overrides based on custom mapnames, etc.
	void AddSoundOverrides(ReadOnlySpan<char> scriptfile, bool preload = false);

	// Called by either client or server in LevelShutdown to clear out custom overrides
	void ClearSoundOverrides();

	bool GetParametersForSoundEx(ReadOnlySpan<char> soundname, ref HSOUNDSCRIPTHANDLE handle, ref SoundParameters parms, Gender gender, bool isbeingemitted = false);
	SoundLevel LookupSoundLevelByHandle(ReadOnlySpan<char> soundname, ref HSOUNDSCRIPTHANDLE handle);

	// TODO: void ReloadSoundEntriesInList(IFileList filesToReload);

	// Called by either client or server to force ModShutdown and ModInit
	void Flush();
}
