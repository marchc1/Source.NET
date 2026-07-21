using ManagedBass;

using Source.Common;
using Source.Common.Audio;
using Source.Common.Commands;
using Source.Common.Filesystem;

using System.Buffers.Binary;
using System.Numerics;

using static Source.AudioSystem.AudioGlobals;
using static Source.AudioSystem.BassAudioMemorySource;
using static Source.AudioSystem.SndChannels;
namespace Source.AudioSystem;

[EngineComponent]
public static class AudioGlobals
{
	[Dependency] public static IFileSystem filesystem = null!;
	[Dependency] public static AudioCache audiocache = null!;
	[Dependency] public static ISoundServices soundServices = null!;
}


[EngineComponent]
public class AudioCache
{
	public readonly Dictionary<FileNameHandle_t, byte[]> MemoryCache = [];

	public byte[]? Lookup(ReadOnlySpan<char> file) {
		FileNameHandle_t handle = filesystem.FindOrAddFileName(file);
		if (MemoryCache.TryGetValue(handle, out byte[]? data))
			return data;

		Span<char> fileSearch = stackalloc char[file.Length + 6];
		"sound/".CopyTo(fileSearch);
		file.CopyTo(fileSearch[6..]);

		if (!filesystem.FileExists(fileSearch, "game"))
			return null;

		IFileHandle? fh = filesystem.Open(fileSearch, FileOpenOptions.Read | FileOpenOptions.Binary, "game");
		if (fh == null)
			return null;

		data = new byte[fh.Stream.Length];
		fh.Stream.ReadExactly(data);
		fh.Dispose();
		MemoryCache[handle] = data;
		return data;
	}
}


public abstract class BassAudioSource : AudioSource
{
	public int BassHandle = 0;
	public virtual int PickDynamicChannel(SoundSource soundsource, SoundEntityChannel entchannel, in Vector3 origin, SfxTable sfx, TimeUnit_t delay, bool doNotOverwriteExisting) {
		return 0;
	}

	public virtual int PickStaticChannel(SoundSource soundsource, SfxTable sfx) {
		return 0;
	}
	public abstract bool IsLooped();
}


public class BassAudioMemorySource : BassAudioSource, IDisposable
{
	int staticChannel;
	private SampleInfo Info;
	int LoopStart;

	public BassAudioMemorySource(ReadOnlySpan<char> file) {
		Info = null!;
		LoopStart = -1;
		byte[]? data = audiocache.Lookup(file);
		if (data == null)
			return;

		ParseChunks(data, file);

		BassFlags flags = BassFlags.Bass3D | BassFlags.Mono;
		if (IsLooped())
			flags |= BassFlags.Loop;

		BassHandle = Bass.SampleLoad(data, 0, data.Length, MAX_CHANNELS, flags);
		if (BassHandle == 0) {
			Dbg.Msg($"BASS: {Bass.LastError}\n");
		}
		staticChannel = Bass.SampleGetChannel(BassHandle, OnlyNew: true);
		Bass.ChannelSetAttribute(staticChannel, ChannelAttribute.NoRamp, 1);
		// Make it so further dynamic attempts dont work
		Info = Bass.SampleGetInfo(BassHandle);
	}

	public void Dispose() {
		Bass.StreamFree(BassHandle);
		BassHandle = 0;
		Info = null!;
	}

	const uint WAVE_CUE = 'c' | ('u' << 8) | ('e' << 16) | (' ' << 24);
	const uint WAVE_SAMPLER = 's' | ('m' << 8) | ('p' << 16) | ('l' << 24);

	private void ParseChunks(ReadOnlySpan<byte> data, ReadOnlySpan<char> file) {
		if (data.Length < 12)
			return;

		int walk = 12;
		while (walk + 8 <= data.Length) {
			uint chunkName = BinaryPrimitives.ReadUInt32LittleEndian(data[walk..]);
			int chunkSize = BinaryPrimitives.ReadInt32LittleEndian(data[(walk + 4)..]);
			walk += 8;
			if (chunkSize < 0 || walk + chunkSize > data.Length)
				break;

			switch (chunkName) {
				case WAVE_CUE:
					ParseCueChunk(data.Slice(walk, chunkSize));
					break;
				case WAVE_SAMPLER:
					ParseSamplerChunk(data.Slice(walk, chunkSize), file);
					break;
			}

			walk += chunkSize + (chunkSize & 1);
		}
	}

	private void ParseCueChunk(ReadOnlySpan<byte> walk) {
		if (walk.Length < 4)
			return;

		int cueCount = BinaryPrimitives.ReadInt32LittleEndian(walk);
		if (cueCount > 0 && walk.Length >= 4 + 24)
			LoopStart = BinaryPrimitives.ReadInt32LittleEndian(walk[(4 + 20)..]);
	}

	private void ParseSamplerChunk(ReadOnlySpan<byte> walk, ReadOnlySpan<char> file) {
		if (walk.Length < 36 + 24)
			return;

		if (BinaryPrimitives.ReadUInt32LittleEndian(walk[28..]) > 0) {
			if (BinaryPrimitives.ReadUInt32LittleEndian(walk[(36 + 4)..]) == 0)
				LoopStart = BinaryPrimitives.ReadInt32LittleEndian(walk[(36 + 8)..]);
#if DEBUG
			else
				Msg($"Unknown sampler chunk type {BinaryPrimitives.ReadUInt32LittleEndian(walk[(36 + 4)..])} on {file}\n");
#endif
		}
	}

	public override bool IsLooped() {
		return LoopStart >= 0;
	}

	public override int PickDynamicChannel(int soundsource, SoundEntityChannel entchannel, in Vector3 origin, SfxTable sfx, double delay, bool doNotOverwriteExisting) {
		int ch = Bass.SampleGetChannel(BassHandle, OnlyNew: false);
		if (ch == 0)
			return 0;

		return ch;
	}

	public override int PickStaticChannel(int soundsource, SfxTable sfx) {
		int i;

		for (i = MAX_DYNAMIC_CHANNELS; i < TotalChannels; i++)
			if (Channels[i].Sfx == null)
				break;

		if (i < TotalChannels)
			return i;
		else {
			if (TotalChannels == MAX_CHANNELS) {
				DevMsg("total_channels == MAX_CHANNELS\n");
				return -1;
			}

			return TotalChannels++;
		}
	}
}


public class AudioSystem : IAudioSystem
{
	public int DeviceChannels() {
		return 0;
	}

	public int DeviceDmaSpeed() {
		return 0;
	}

	public ReadOnlySpan<char> DeviceName() {
		return "";
	}

	public int DeviceSampleBits() {
		return 0;
	}

	public int DeviceSampleBytes() {
		return 0;
	}

	public int DeviceSampleCount() {
		return 0;
	}

	public bool Init() {
		if (!Bass.Init(1, 44100, DeviceInitFlags.Device3D))
			return false;

		for (int i = 0; i < MAX_CHANNELS; i++)
			Channels[i].Index = (short)i;

		TotalChannels = MAX_DYNAMIC_CHANNELS;
		g_ActiveChannels.Init();

		return true;
	}

	private static Vector3D ToBass(in Vector3 v) => new(-v.Y, v.Z, v.X);

	private static void Spatialize(int channel, in StartSoundParams parms) {
		if (parms.SoundLevel == SoundLevel.LvlNone) {
			Bass.ChannelSet3DAttributes(channel, Mode3D.Off, -1, -1, -1, -1, -1);
			return;
		}

		float minDist = 36.0f * MathF.Pow(10.0f, ((int)parms.SoundLevel - 60) / 20.0f);
		Bass.ChannelSet3DAttributes(channel, Mode3D.Normal, minDist, 10000000.0f, -1, -1, -1);
		Bass.ChannelSet3DPosition(channel, ToBass(parms.Origin), default, default);
	}

	public bool IsActive() {
		return true;
	}

	public long StartDynamicSound(in StartSoundParams parms) {
		SfxTable? sfx = parms.Sfx;
		if (sfx == null)
			return 0;

		ReadOnlySpan<char> sndname = sfx.GetName();

		int vol = (int)(parms.Volume * 255);
		if (vol > 255) {
			DevMsg($"StartStaticSound: {sndname} volume > 255\n");
			vol = 255;
		}

		if ((parms.Flags & SoundFlags.Stop) != 0 || (parms.Flags & SoundFlags.ChangeVolume) != 0 || (parms.Flags & SoundFlags.ChangePitch) != 0)
			if (AlterChannel(parms.SoundSource, parms.EntChannel, parms.Sfx, vol, parms.Pitch, parms.Flags) || (parms.Flags & SoundFlags.Stop) != 0)
				return 0;

		if (parms.Pitch == 0)
			return 0;

		int targetChannel = PickDynamicChannel(parms.SoundSource, parms.EntChannel, parms.Origin, parms.Sfx, parms.Delay, (parms.Flags & SoundFlags.DoNotOverwriteExistingOnChannel) != 0);
		if (targetChannel == 0)
			return 0;

		PlaySound(in parms, targetChannel);

		return 0;
	}

	private void PlaySound(in StartSoundParams parms, int targetChannel) {
		Spatialize(targetChannel, in parms);
		ChannelInfo info = Bass.ChannelGetInfo(targetChannel);
		Bass.ChannelSetAttribute(targetChannel, ChannelAttribute.Frequency, info.Frequency * parms.Pitch / 100f);
		Bass.ChannelSetAttribute(targetChannel, ChannelAttribute.Volume, parms.Volume);
		Bass.ChannelPlay(targetChannel);
		Bass.Apply3D();
	}

	private int PickDynamicChannel(int soundSource, SoundEntityChannel entChannel, Vector3 origin, SfxTable? sfx, float delay, bool doNotOverwriteExisting) {
		Precache(sfx!);
		BassAudioSource? source = sfx!.Source as BassAudioSource;
		if (source == null)
			return 0;

		return source.PickDynamicChannel(soundSource, entChannel, origin, sfx, delay, doNotOverwriteExisting);
	}

	private int PickStaticChannel(int soundSource, SfxTable? sfx) {
		Precache(sfx!);
		BassAudioSource? source = sfx!.Source as BassAudioSource;
		if (source == null)
			return 0;

		return source.PickStaticChannel(soundSource, sfx);
	}

	private void Precache(SfxTable sfx) {
		BassAudioSource? src = (BassAudioSource?)sfx.Source;
		if (src == null) {
			src = new BassAudioMemorySource(sfx.GetFileName());
			sfx.Source = src;
		}
	}

	public long StartStaticSound(in StartSoundParams parms) {
		SfxTable? sfx = parms.Sfx;
		if (sfx == null)
			return 0;

		ReadOnlySpan<char> sndname = sfx.GetName();

		int vol = (int)(parms.Volume * 255);
		if (vol > 255) {
			DevMsg($"StartStaticSound: {sndname} volume > 255\n");
			vol = 255;
		}

		if ((parms.Flags & SoundFlags.Stop) != 0 || (parms.Flags & SoundFlags.ChangeVolume) != 0 || (parms.Flags & SoundFlags.ChangePitch) != 0)
			if (AlterChannel(parms.SoundSource, parms.EntChannel, parms.Sfx, vol, parms.Pitch, parms.Flags) || (parms.Flags & SoundFlags.Stop) != 0)
				return 0;

		if (parms.Pitch == 0)
			return 0;

		int targetChannel = PickStaticChannel(parms.SoundSource, sfx);
		if (targetChannel == -1)
			return 0;

		ref Channel ch = ref Channels[targetChannel];
		ch.Sfx = sfx;
		ch.SoundSource = parms.SoundSource;
		ch.EntChannel = (int)parms.EntChannel;
		ch.MasterVol = (short)vol;
		ch.BasePitch = (short)parms.Pitch;
		ch.Origin = parms.Origin;
		ch.BassChannel = Bass.SampleGetChannel(((BassAudioSource)sfx.Source!).BassHandle, OnlyNew: false);
		g_ActiveChannels.Add(ref ch);

		Spatialize(ch.BassChannel, in parms);
		ChannelInfo info = Bass.ChannelGetInfo(ch.BassChannel);
		Bass.ChannelSetAttribute(ch.BassChannel, ChannelAttribute.Frequency, info.Frequency * parms.Pitch / 100f);
		Bass.ChannelSetAttribute(ch.BassChannel, ChannelAttribute.Volume, parms.Volume);
		Bass.ChannelPlay(ch.BassChannel);
		Bass.Apply3D();

		return 0;
	}

	private bool AlterChannel(int soundSource, SoundEntityChannel entChannel, SfxTable? sfx, int vol, int pitch, SoundFlags flags) {
		ReadOnlySpan<char> name = sfx!.GetName();
		if (!name.IsEmpty && SoundCharsUtils.TestSoundChar(name, SoundChars.Sentence)) {
			ChannelList sentenceList = new();
			SndChannels.g_ActiveChannels.GetActiveChannels(sentenceList);
			for (int i = 0; i < sentenceList.Count(); i++) {
				ref Channel ch = ref sentenceList.GetChannel(i);
				if (ch.SoundSource == soundSource && ch.EntChannel == (int)entChannel && ch.Sfx != null) {
					if ((flags & SoundFlags.ChangePitch) != 0)
						ch.BasePitch = (short)pitch;

					if ((flags & SoundFlags.ChangeVolume) != 0)
						ch.MasterVol = (short)vol;

					if ((flags & SoundFlags.Stop) != 0)
						SndMix.FreeChannel(ref ch);

					return true;
				}
			}
			return false;
		}

		ChannelList list = new();
		g_ActiveChannels.GetActiveChannels(list);

		bool success = false;

		for (int i = 0; i < list.Count(); i++) {
			ref Channel ch = ref list.GetChannel(i);
			if (ch.SoundSource == soundSource &&
				((flags & SoundFlags.IgnoreName) != 0 ||
				 (ch.EntChannel == (int)entChannel && ch.Sfx == sfx))) {
				if ((flags & SoundFlags.ChangePitch) != 0) {
					ch.BasePitch = (short)pitch;
					ChannelInfo pitchInfo = Bass.ChannelGetInfo(ch.BassChannel);
					Bass.ChannelSetAttribute(ch.BassChannel, ChannelAttribute.Frequency, pitchInfo.Frequency * pitch / 100f);
				}

				if ((flags & SoundFlags.ChangeVolume) != 0) {
					ch.MasterVol = (short)vol;
					Bass.ChannelSetAttribute(ch.BassChannel, ChannelAttribute.Volume, vol / 255f);
				}

				if ((flags & SoundFlags.Stop) != 0)
					SndMix.FreeChannel(ref ch);

				if ((flags & SoundFlags.IgnoreName) == 0)
					return true;
				else
					success = true;
			}
		}

		return success;
	}

	public void Update(double v) {
		Bass.GlobalSampleVolume = (int)(volume_sfx.GetFloat() * volume.GetFloat() * 10000);
		Bass.GlobalMusicVolume = (int)(snd_musicvolume.GetFloat() * volume.GetFloat() * 10000);
		Bass.Update((int)(float)(v * 1000));
	}

	public void UpdateListener(in Vector3 listenerOrigin, in Vector3 listenerForward, in Vector3 listenerRight, in Vector3 listenerUp, bool isListenerUnderwater) {
		Bass.Set3DPosition(ToBass(listenerOrigin), default, ToBass(listenerForward), ToBass(listenerUp));
		Bass.Apply3D();
	}

	readonly static ConVar volume = new("volume", "1.0", FCvar.Archive, "Sound volume", 0.0, 1.0);
	readonly ConVar snd_musicvolume = new("snd_musicvolume", "1.0", FCvar.Archive, "Music volume", 0.0, 1.0);
	readonly static ConVar volume_sfx = new("volume_sfx", "1.0", FCvar.Archive, "Sound effects volume", 0.0, 1.0);

}
