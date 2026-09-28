global using static Source.AudioSystem.SndChannels;

using Source.Common.Audio;

using System.Numerics;

namespace Source.AudioSystem;

public static class SndChannels
{
	// DO NOT REORDER: indices to fvolume arrays in channel_t

	public const int IFRONT_LEFT = 0;           // NOTE: must correspond to order of fvolume array below!
	public const int IFRONT_RIGHT = 1;
	public const int IREAR_LEFT = 2;
	public const int IREAR_RIGHT = 3;
	public const int IFRONT_CENTER = 4;
	public const int IFRONT_CENTER0 = 5;        // dummy slot - center channel is mono, but mixers reference volume[1] slot

	public const int IFRONT_LEFTD = 6;          // start of doppler right array
	public const int IFRONT_RIGHTD = 7;
	public const int IREAR_LEFTD = 8;
	public const int IREAR_RIGHTD = 9;
	public const int IFRONT_CENTERD = 10;
	public const int IFRONT_CENTERD0 = 11;      // dummy slot - center channel is mono, but mixers reference volume[1] slot

	public const int CCHANVOLUMES = 12;

	public static readonly Channel[] channels = CreateChannels();
	// 0 to MAX_DYNAMIC_CHANNELS-1	= normal entity sounds
	// MAX_DYNAMIC_CHANNELS to total_channels = static sounds

	public static int total_channels;

	public static readonly ActiveChannels g_ActiveChannels = new();

	static Channel[] CreateChannels() {
		Channel[] result = new Channel[MAX_CHANNELS];
		for (int i = 0; i < result.Length; i++)
			result[i] = new Channel(i);
		return result;
	}
}

//-----------------------------------------------------------------------------
// Purpose: Each currently playing wave is stored in a channel
//-----------------------------------------------------------------------------
// NOTE: 128bytes.  These are memset to zero at some points.  Do not add virtuals without changing that pattern.
// UNDONE: now 300 bytes...
public class Channel
{
	public readonly int Index;

	public int Guid;            // incremented each time a channel is allocated (to match with channel free in tools, etc.)
	public int UserData;        // user specified data for syncing to tools

	public SfxTable? Sfx;       // the actual sound
	public AudioMixer? Mixer;   // The sound's instance data for this channel

	// speaker channel volumes, indexed using IFRONT_LEFT to IFRONT_CENTER.
	// NOTE: never access these fvolume[] elements directly! Use channel helpers in SndDma.

	public readonly float[] FVolume = new float[CCHANVOLUMES];          // 0.0-255.0 current output volumes
	public readonly float[] FVolumeTarget = new float[CCHANVOLUMES];    // 0.0-255.0 target output volumes
	public readonly float[] FVolumeInc = new float[CCHANVOLUMES];       // volume increment, per frame, moves volume[i] to vol_target[i] (per spatialization)
	public uint FreeChannelAtSampleTime;

	public SoundSource SoundSource; // see EngineSoundGlobals for description.
	public int EntChannel;          // sound channel (CHAN_STREAM, CHAN_VOICE, etc.)
	public int SpeakerEntity;       // if a sound is being played through a speaker entity (e.g., on a monitor,), this is the
									//  entity upon which to show the lips moving, if the sound has sentence data
	public short MasterVol;         // 0-255 master volume
	public short BasePitch;         // base pitch percent (100% is normal pitch playback)
	public float Pitch;             // real-time pitch after any modulation or shift by dynamic data
	public readonly int[] MixGroups = new int[8];   // sound belongs to these mixgroups: world, actor, player weapon, explosion etc.
	public int LastMixGroupId;      // last mixgroupid selected
	public float LastVol;           // last volume after spatialization

	public Vector3 Origin;          // origin of sound effect
	public Vector3 Direction;       // direction of the sound
	public float DistMult;          // distance multiplier (attenuation/clipK)


	public float DspMix;            // 0 - 1.0 proportion of dsp to mix with original sound, based on distance
	public float DspFace;           // -1.0 - 1.0 (1.0 = facing listener)
	public float DistMix;           // 0 - 1.0 proportion based on distance from listner (1.0 - 100% wav right - far)
	public float DspMixMin;         // for dspmix calculation - set by current preset in SND_GetDspMix
	public float DspMixMax;         // for dspmix calculation - set by current preset in SND_GetDspMix

	public float Radius;            // Radius of this sound effect (spatialization is different within the radius)

	public float ObGain;            // gain drop if sound source obscured from listener
	public float ObGainTarget;      // target gain while crossfading between ob_gain & ob_gain_target
	public float ObGainInc;         // crossfade increment

	public short ActiveIndex;
	public char WavType;            // 0 default, CHAR_DOPPLER, CHAR_DIRECTIONAL, CHAR_DISTVARIANT
	public byte Pad;

	public readonly byte[] SamplePrev = new byte[8];    // last sample(s) in previous input data buffer - space for 2, 16 bit, stereo samples

	public int InitialStreamPosition;

	public int SpecialDsp;

	public ChannelFlags Flags;

	public Channel(int index) {
		Index = index;
	}

	public void Clear() {
		Guid = 0;
		UserData = 0;
		Sfx = null;
		Mixer = null;
		Array.Clear(FVolume);
		Array.Clear(FVolumeTarget);
		Array.Clear(FVolumeInc);
		FreeChannelAtSampleTime = 0;
		SoundSource = 0;
		EntChannel = 0;
		SpeakerEntity = 0;
		MasterVol = 0;
		BasePitch = 0;
		Pitch = 0;
		Array.Clear(MixGroups);
		LastMixGroupId = 0;
		LastVol = 0;
		Origin = default;
		Direction = default;
		DistMult = 0;
		DspMix = 0;
		DspFace = 0;
		DistMix = 0;
		DspMixMin = 0;
		DspMixMax = 0;
		Radius = 0;
		ObGain = 0;
		ObGainTarget = 0;
		ObGainInc = 0;
		ActiveIndex = 0;
		WavType = '\0';
		Pad = 0;
		Array.Clear(SamplePrev);
		InitialStreamPosition = 0;
		SpecialDsp = 0;
		Flags = default;
	}
}

public struct ChannelFlags
{
	public bool UpdatePositions;            // if true, assume sound source can move and update according to entity
	public bool IsSentence;                 // true if playing linked sentence
	public bool Dry;                        // if true, bypass all dsp processing for this sound (ie: music)
	public bool Speaker;                    // true if sound is playing through in-game speaker entity.
	public bool StereoWav;                  // if true, a stereo .wav file is the sample data source

	public bool DelayedStart;               // If true, sound had a delay and so same sound on same channel won't channel steal from it
	public bool FromServer;                 // for snd_show, networked sounds get colored differently than local sounds

	public bool FirstPass;                  // true if this is first time sound is spatialized
	public bool Traced;                     // true if channel was already checked this frame for obscuring
	public bool FastPitch;                  // true if using low quality pitch (fast, but no interpolation)

	public bool IsFreeingChannel;           // true when inside S_FreeChannel - prevents reentrance
	public bool CompatibilityAttenuation;   // True when we want to use goldsrc compatibility mode for the attenuation
											// In that case, dist_mul is set to a relatively meaningful value in StartDynamic/StartStaticSound,
											// but we interpret it totally differently in SND_GetGain.
	public bool ShouldPause;                // if true, sound should pause when the game is paused
	public bool IgnorePhonemes;             // if true, we don't want to drive animation w/ phoneme data
}

public class ChannelList
{
	public int Count;
	public readonly short[] List = new short[MAX_CHANNELS];
	public readonly bool[] Quashed = new bool[MAX_CHANNELS]; // if true, the channel should be advanced, but not mixed, because it's been heuristically suppressed

	public readonly List<int> SpecialDSPs = [];

	public bool HasSpeakerChannels;
	public bool HasDryChannels;
	public bool Has11kChannels;
	public bool Has22kChannels;
	public bool Has44kChannels;

	public int GetChannelIndex(int listIndex) => List[listIndex];
	public Channel GetChannel(int listIndex) => channels[GetChannelIndex(listIndex)];
	public bool IsQuashed(int listIndex) => Quashed[listIndex];

	public void RemoveChannelFromList(int listIndex) {
		// decrease the count by one, and swap the deleted channel with
		// the last one.
		Count--;
		if (Count > 0 && listIndex != Count) {
			List[listIndex] = List[Count];
			Quashed[listIndex] = Quashed[Count];
		}
	}
}

public class ActiveChannels
{
	int count;
	readonly short[] list = new short[MAX_CHANNELS];

	public void Add(Channel channel) {
		Assert(channel.ActiveIndex == 0);
		list[count] = (short)channel.Index;
		count++;
		channel.ActiveIndex = (short)count;
	}

	public void Remove(Channel channel) {
		if (channel.ActiveIndex == 0)
			return;
		int activeIndex = channel.ActiveIndex - 1;
		Assert(activeIndex >= 0 && activeIndex < count);
		Assert(channel == channels[list[activeIndex]]);
		count--;
		// Not the last one?  Swap the last one with this one and fix its index
		if (activeIndex < count) {
			list[activeIndex] = list[count];
			channels[list[activeIndex]].ActiveIndex = (short)(activeIndex + 1);
		}
		channel.ActiveIndex = 0;
	}

	public void GetActiveChannels(ChannelList channelList) {
		channelList.Count = count;
		if (count != 0)
			Array.Copy(list, channelList.List, count);

		channelList.SpecialDSPs.Clear();
		for (int i = SOUND_BUFFER_SPECIAL_START; i < g_paintBuffers.Count; ++i) {
			PaintBuffer specialBuffer = MIX_GetPPaintFromIPaint(i);
			if (specialBuffer.SpecialDSP != 0)
				channelList.SpecialDSPs.Add(specialBuffer.SpecialDSP);
		}

		channelList.HasSpeakerChannels = true;
		channelList.Has11kChannels = true;
		channelList.Has22kChannels = true;
		channelList.Has44kChannels = true;
		channelList.HasDryChannels = true;
	}

	public void Init() {
		count = 0;
	}

	public int GetActiveCount() => count;
}
