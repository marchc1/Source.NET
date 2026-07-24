using Source.Common;
using Source.Common.Audio;

using System.Numerics;

namespace Source.AudioSystem;

public enum ChanVolume
{
	FrontLeft = 0,
	FrontRight = 1,
	RearLeft = 2,
	RearRight = 3,
	FrontCenter = 4,
	FrontCenter0 = 5,
	FrontLeftD = 6,
	FrontRightD = 7,
	RearLeftD = 8,
	RearRightD = 9,
	FrontCenterD = 10,
	FrontCenterD0 = 11,
	Count = 12
}

public static class SndChannels
{
	public static InlineArrayMaxChannels<Channel> Channels;
	public static int TotalChannels;

	public static readonly ActiveChannels g_ActiveChannels = new();
}

public struct ChannelFlags
{
	public bool UpdatePositions;
	public bool IsSentence;
	public bool Dry;
	public bool Speaker;
	public bool StereoWav;
	public bool DelayedStart;
	public bool FromServer;
	public bool FirstPass;
	public bool Traced;
	public bool FastPitch;
	public bool IsFreeingChannel;
	public bool CompatibilityAttenuation;
	public bool ShouldPause;
	public bool IgnorePhonemes;
}

public struct Channel
{
	public int Guid;
	public int Userdata;

	public SfxTable? Sfx;
	// public AudioMixer? Mixer;
	public int BassChannel;

	public InlineArray12<float> Volume;
	public InlineArray12<float> VolumeTarget;
	public InlineArray12<float> VolumeInc;
	public uint FreeChannelAtSampleTime;

	public SoundSource SoundSource;
	public int EntChannel;
	public int SpeakerEntity;
	public short MasterVol;
	public short BasePitch;
	public float Pitch;
	public InlineArray8<int> MixGroups;
	public int LastMixGroupId;
	public float LastVol;

	public Vector3 Origin;
	public Vector3 Direction;
	public float DistMult;

	public float DspMix;
	public float DspFace;
	public float DistMix;
	public float DspMixMin;
	public float DspMixMax;

	public float Radius;

	public float ObGain;
	public float ObGainTarget;
	public float ObGainInc;

	public short ActiveIndex;
	public short Index;
	public byte WavType;
	public byte Pad;

	public InlineArray8<byte> SamplePrev;

	public int InitialStreamPosition;

	public int SpecialDsp;

	public ChannelFlags Flags;
}

public class ChannelList
{
	public int iCount;
	public InlineArrayMaxChannels<short> List;
	public InlineArrayMaxChannels<bool> Quashed;
	public List<int> SpecialDSPs = [];
	public bool HasSpeakerChannels;
	public bool HasDryChannels;
	public bool Has11kChannels;
	public bool Has22kChannels;
	public bool Has44kChannels;

	public int Count() => iCount;

	public int GetChannelIndex(int listIndex) => List[listIndex];

	public ref Channel GetChannel(int listIndex) => ref SndChannels.Channels[GetChannelIndex(listIndex)];

	public bool IsQuashed(int listIndex) => Quashed[listIndex];

	public void RemoveChannelFromList(int listIndex) {
		iCount--;
		if (iCount > 0 && listIndex != iCount) {
			List[listIndex] = List[iCount];
			Quashed[listIndex] = Quashed[iCount];
		}
	}
}

public class ActiveChannels
{

	int Count;
	InlineArrayMaxChannels<short> List;

	public void Add(ref Channel channel) {
		Assert(channel.ActiveIndex == 0);
		List[Count] = channel.Index;
		Count++;
		channel.ActiveIndex = (short)Count;
	}

	public void Remove(ref Channel channel) {
		if (channel.ActiveIndex == 0)
			return;

		int activeIndex = channel.ActiveIndex - 1;
		Assert(activeIndex >= 0 && activeIndex < Count);
		Assert(List[activeIndex] == channel.Index);
		Count--;

		if (activeIndex < Count) {
			List[activeIndex] = List[Count];
			SndChannels.Channels[List[activeIndex]].ActiveIndex = (short)(activeIndex + 1);
		}
		channel.ActiveIndex = 0;
	}

	public void GetActiveChannels(ChannelList list) {
		list.iCount = Count;
		if (Count != 0)
			List[..Count].CopyTo(list.List);

		// for (int i = SOUND_BUFFER_SPECIAL_START; i < g_paintBuffers.Count(); ++i) {
		// 	paintbuffer_t* pSpecialBuffer = MIX_GetPPaintFromIPaint(i);
		// 	if (pSpecialBuffer->nSpecialDSP != 0)
		// 		list.SpecialDSPs.AddToTail(pSpecialBuffer->nSpecialDSP);
		// }

		list.HasSpeakerChannels = true;
		list.Has11kChannels = true;
		list.Has22kChannels = true;
		list.Has44kChannels = true;
		list.HasDryChannels = true;
	}

	public void Init() => Count = 0;
	public int GetActiveCount() => Count;
}
