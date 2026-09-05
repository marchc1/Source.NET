using ManagedBass;

using Source.Common;
using Source.Common.Audio;
using Source.Common.Commands;
using Source.Common.Filesystem;
using Source.Common.Mathematics;

using System.Buffers.Binary;
using System.Numerics;

using static Source.AudioSystem.AudioGlobals;
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

	public int LoopStartSample => LoopStart;

	public BassAudioMemorySource(SfxTable sfx) {
		Info = null!;
		LoopStart = -1;
		ReadOnlySpan<char> file = sfx.GetFileName();
		byte[]? data = audiocache.Lookup(file);
		if (data == null)
			return;

		ParseChunks(data, file);

		ReadOnlySpan<char> rawName = sfx.GetName();
		bool is2D = sfx.IsUISound
			|| (!rawName.IsEmpty && (SoundCharsUtils.TestSoundChar(rawName, SoundChars.Stream) || SoundCharsUtils.TestSoundChar(rawName, SoundChars.DryMix)));

		BassFlags flags = is2D ? BassFlags.Default : (BassFlags.Bass3D | BassFlags.Mono);
		if (IsLooped() && LoopStart <= 0)
			flags |= BassFlags.Loop;

		BassHandle = Bass.SampleLoad(data, 0, data.Length, MAX_CHANNELS, flags);
		if (BassHandle == 0) 
			Msg($"BASS: {Bass.LastError}\n");
		
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

	const float IN_TO_METERS = 0.0254f;

	public bool Init() {
		if (!Bass.Init(1, 44100, DeviceInitFlags.Device3D))
			return false;

		Bass.Set3DFactors(IN_TO_METERS, 0.0f, 1.0f);

		for (int i = 0; i < MAX_CHANNELS; i++)
			Channels[i].Index = (short)i;

		TotalChannels = MAX_DYNAMIC_CHANNELS;
		g_ActiveChannels.Init();

		return true;
	}

	Vector3 listener_origin;
	long s_SoundGuid;

	private Vector3D ToBass(in Vector3 v) => new(-v.Y, v.Z, v.X);

	private void SetupSpatialization(int channel, in StartSoundParams parms) {
		if (parms.SoundLevel == SoundLevel.LvlNone) {
			// 2D sound (UI/music/ambient): no positioning, plays centered.
			Bass.ChannelSet3DAttributes(channel, Mode3D.Off, -1, -1, -1, -1, -1);
			return;
		}

		Bass.ChannelSet3DAttributes(channel, Mode3D.Normal, -1, -1, -1, -1, -1);
		Bass.ChannelSet3DPosition(channel, ToBass(parms.Origin), default, default);
	}

	private void Spatialize(ref Channel ch) {
		// todo, this has a LOT more
		// TODO SoundMixer ^

		ch.DspFace = 1.0f;
		ch.DspMix = 0;
		ch.DistMix = 0;

		QAngle sourceAngles = new();
		sourceAngles.Init(0.0f, 0.0f, 0.0f);
		Vector3 entOrigin = ch.Origin;

		scoped SpatializationInfo si = default;

		si.Type = SpatializationType.InSpatialization;
		si.Origin = ref entOrigin;
		si.Angles = ref sourceAngles;
		if (ch.SoundSource != 0 && ch.Radius == 0)
			si.Radius = ref ch.Radius;

		soundServices.GetSoundSpatialization(ch.SoundSource, ref si);

		if (ch.Flags.UpdatePositions) {
			MathLib.AngleVectors(sourceAngles, out ch.Direction);
			ch.Origin = entOrigin;
		}
		else
			MathLib.VectorAngles(ch.Direction, out sourceAngles);

		Bass.ChannelSet3DPosition(ch.BassChannel, ToBass(ch.Origin), default, default);

		ApplyChannelVolume(ref ch);
	
		// TODO: Some sort of impl, maybe in engine API?
		// if (SND_IsInGame() || toolframework.InToolMode())
		ch.Flags.FirstPass = false;
	}

	private void ApplyChannelVolume(ref Channel ch) {
		if (ch.BassChannel == 0)
			return;

		bool music = ch.Flags.Music;
		bool player = ch.SoundSource != 0 && soundServices.IsPlayer(ch.SoundSource);
		bool looping = ch.Sfx?.Source is BassAudioSource bs && bs.IsLooped();

		float dist = Vector3.Distance(listener_origin, ch.Origin);
		float gain = ChannelGain.SND_GetGain(ref ch, player, music, looping, dist, ch.DistMult != 0.0f);

		float finalVol = Math.Clamp((ch.MasterVol / 255.0f) * gain, 0.0f, 1.0f);
		Bass.ChannelSetAttribute(ch.BassChannel, ChannelAttribute.Volume, finalVol);

		float vol255 = finalVol * 255.0f;
		ch.Volume[(int)ChanVolume.FrontLeft] = vol255;
		ch.Volume[(int)ChanVolume.FrontRight] = vol255;
		ch.LastVol = gain * (ch.MasterVol / 255.0f);
	}

	private static bool IsMusicSound(SfxTable sfx) {
		if (sfx.IsUISound)
			return true;
		ReadOnlySpan<char> name = sfx.GetName();
		return !name.IsEmpty && SoundCharsUtils.TestSoundChar(name, SoundChars.Stream);
	}

	static readonly Dictionary<int, SyncProcedure> LoopSyncProcs = [];
	private static void SetupLoop(ref Channel ch, int freq) {
		if (ch.Sfx?.Source is not BassAudioMemorySource src)
			return;
		if (!src.IsLooped() || src.LoopStartSample <= 0)
			return;

		int handle = ch.BassChannel;
		long loopBytes = Bass.ChannelSeconds2Bytes(handle, (double)src.LoopStartSample / freq);
		SyncProcedure proc = (h, chan, data, user) => Bass.ChannelSetPosition(chan, loopBytes, PositionFlags.Bytes);
		LoopSyncProcs[handle] = proc;
		Bass.ChannelSetSync(handle, SyncFlags.End | SyncFlags.Mixtime, 0, proc);
	}
	internal static void RemoveLoopSync(int handle) => LoopSyncProcs.Remove(handle);

	public void StopAllSounds(bool clear) {
		TotalChannels = MAX_DYNAMIC_CHANNELS;

		ChannelList list = new();
		g_ActiveChannels.GetActiveChannels(list);
		int i = 0;
		for (int listIndex = 0; listIndex < list.Count(); listIndex++) {
			ref Channel channel = ref list.GetChannel(listIndex);
			if (channel.Sfx != null)
				DevMsg(1, $"{i,2}:Stopped sound {channel.Sfx.GetName()}.\n");

			SndMix.FreeChannel(ref channel);
			++i;
		}

		Channels[..].Clear();
		for (int channelIndex = 0; channelIndex < MAX_CHANNELS; channelIndex++)
			Channels[channelIndex].Index = (short)channelIndex;

		// if (clear)
		// 	ClearBuffer();

		// soundfade = default;

		Assert(g_ActiveChannels.GetActiveCount() == 0);
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
		if (targetChannel == -1)
			return 0;

		ref Channel ch = ref Channels[targetChannel];
		ch.Sfx = sfx;
		ch.SoundSource = parms.SoundSource;
		ch.EntChannel = (int)parms.EntChannel;
		ch.MasterVol = (short)vol;
		ch.BasePitch = (short)parms.Pitch;
		ch.Origin = parms.Origin;
		ch.DistMult = SndGain.SNDLVL_TO_DIST_MULT((int)parms.SoundLevel);
		ch.Flags.UpdatePositions = parms.UpdatePositions && (parms.SoundSource != 0);
		ch.Flags.FirstPass = true;
		ch.Flags.Music = IsMusicSound(sfx);
		ch.BassChannel = Bass.SampleGetChannel(((BassAudioSource)sfx.Source!).BassHandle, OnlyNew: false);
		ch.Guid = (int)++s_SoundGuid;
		g_ActiveChannels.Add(ref ch);

		PlaySound(in parms, ref ch);

		StartSoundParams startedParms = parms;
		soundServices.OnSoundStarted(ch.Guid, ref startedParms, sndname);
		return ch.Guid;
	}

	private void PlaySound(in StartSoundParams parms, ref Channel ch) {
		int bassChannel = ch.BassChannel;
		SetupSpatialization(bassChannel, in parms);
		ChannelInfo info = Bass.ChannelGetInfo(bassChannel);
		Bass.ChannelSetAttribute(bassChannel, ChannelAttribute.Frequency, info.Frequency * parms.Pitch / 100f);
		SetupLoop(ref ch, info.Frequency);
		ApplyChannelVolume(ref ch);
		Bass.ChannelPlay(bassChannel);
		Bass.Apply3D();
	}

	private static int ChannelGetMaxVol(ref Channel ch) {
		float max = 0.0f;

		for (int i = 0; i < (int)ChanVolume.Count; i++)
			if (ch.Volume[i] > max)
				max = ch.Volume[i];

		return (int)max;
	}

	private static uint RemainingSamples(ref Channel channel) {
		if (channel.Sfx?.Source is not BassAudioSource source)
			return 0;

		int handle = channel.BassChannel;
		if (handle == 0)
			return 0;

		ChannelInfo info = Bass.ChannelGetInfo(handle);

		if (source.IsLooped())
			return (uint)info.Frequency;

		long lenBytes = Bass.ChannelGetLength(handle, PositionFlags.Bytes);
		long posBytes = Bass.ChannelGetPosition(handle, PositionFlags.Bytes);
		if (lenBytes <= 0 || posBytes < 0 || posBytes >= lenBytes)
			return 0;

		double secondsLeft = Bass.ChannelBytes2Seconds(handle, lenBytes - posBytes);
		return (uint)(secondsLeft * info.Frequency);
	}

	private static int StealDynamicChannel(SoundSource soundsource, int entchannel, in Vector3 origin, SfxTable? sfx, float delay, bool doNotOverwriteExisting) {
		Span<int> canSteal = stackalloc int[MAX_DYNAMIC_CHANNELS];
		int canStealCount = 0;

		int sameSoundCount = 0;
		uint sameSoundRemaining = 0xFFFFFFFF;
		int sameSoundIndex = -1;
		int sameVol = 0xFFFF;
		int availableChannel = -1;
		bool delaySame = false;

		Span<int> exactMatch = stackalloc int[MAX_DYNAMIC_CHANNELS];
		int exactCount = 0;

		for (int chIdx = 0; chIdx < MAX_DYNAMIC_CHANNELS; chIdx++) {
			ref Channel ch = ref Channels[chIdx];

			if (ch.ActiveIndex != 0) {
				if (entchannel != (int)SoundEntityChannel.Auto) {
					int checkChannel = entchannel;
					if (checkChannel == -1) {
						if (ch.EntChannel != (int)SoundEntityChannel.Stream && ch.EntChannel != (int)SoundEntityChannel.Voice && ch.EntChannel != (int)SoundEntityChannel.Voice2)
							checkChannel = ch.EntChannel;
					}
					if (ch.SoundSource == soundsource && (soundsource != -1) && ch.EntChannel == checkChannel) {
						if (doNotOverwriteExisting)
							return -1;

						if (ch.Flags.DelayedStart) {
							exactMatch[exactCount] = chIdx;
							exactCount++;
							continue;
						}
						return chIdx;
					}
				}

				if (ch.EntChannel == (int)SoundEntityChannel.Stream || ch.EntChannel == (int)SoundEntityChannel.Voice || ch.EntChannel == (int)SoundEntityChannel.Voice2)
					continue;

				if (soundServices.IsPlayer(ch.SoundSource) && !soundServices.IsPlayer(soundsource))
					continue;

				if (ch.Sfx == sfx) {
					delaySame = ch.Flags.DelayedStart || delaySame;
					sameSoundCount++;
					int maxVolume = ChannelGetMaxVol(ref ch);
					uint remaining = RemainingSamples(ref ch);
					if (maxVolume < sameVol || (maxVolume == sameVol && remaining < sameSoundRemaining)) {
						sameSoundIndex = chIdx;
						sameVol = maxVolume;
						sameSoundRemaining = remaining;
					}
				}
				canSteal[canStealCount++] = chIdx;
			}
			else {
				if (availableChannel < 0)
					availableChannel = chIdx;
			}
		}

		if (exactCount > 0) {
			// uint freeSampleTime = g_paintedtime + (uint)(delay * SOUND_DMA_SPEED);
			int returnChannel = exactMatch[0];
			uint minRemaining = RemainingSamples(ref Channels[returnChannel]);
			// if (Channels[returnChannel].FreeChannelAtSampleTime == 0 || Channels[returnChannel].FreeChannelAtSampleTime > freeSampleTime)
			// 	Channels[returnChannel].FreeChannelAtSampleTime = freeSampleTime;
			for (int i = 1; i < exactCount; i++) {
				int channel = exactMatch[i];
				// if (Channels[channel].FreeChannelAtSampleTime == 0 || Channels[channel].FreeChannelAtSampleTime > freeSampleTime)
				// 	Channels[channel].FreeChannelAtSampleTime = freeSampleTime;
				uint remain = RemainingSamples(ref Channels[channel]);
				if (remain < minRemaining) {
					returnChannel = channel;
					minRemaining = remain;
				}
			}

			if (exactCount > 1)
				return returnChannel;
		}

		if (voice_steal.GetInt() > 1 && sameSoundIndex >= 0) {
			int maxSameSounds = delaySame ? 5 : 4;
			// float distSqr = 0.0f;
			if (sfx!.Source != null) {
				// distSqr = origin.DistToSqr(listener_origin);
				if (sfx.Source is BassAudioSource source && source.IsLooped())
					maxSameSounds = 3;
			}

			if (sameSoundCount >= maxSameSounds) {
				// ref Channel ch = ref Channels[sameSoundIndex];
				// if (distSqr > 0.0f && ch.Origin.DistToSqr(listener_origin) < distSqr && entchannel != (int)SoundEntityChannel.Weapon)
				// 	return -1;

				return sameSoundIndex;
			}
		}

		if (availableChannel >= 0)
			return availableChannel;

		float lifeLeft = float.MaxValue;
		int firstToDie = -1;
		bool allowVoiceSteal = voice_steal.GetBool();

		for (int i = 0; i < canStealCount; i++) {
			int chIdx = canSteal[i];
			ref Channel ch = ref Channels[chIdx];
			float timeLeft = 0;
			if (allowVoiceSteal) {
				int maxVolume = ChannelGetMaxVol(ref ch);
				if (maxVolume < 5)
					return chIdx;

				if (ch.Sfx != null && ch.Sfx.Source != null)
					timeLeft = RemainingSamples(ref ch);
			}
			else {
				if (ch.Sfx != null)
					timeLeft = 1;
			}

			if (timeLeft < lifeLeft) {
				lifeLeft = timeLeft;
				firstToDie = chIdx;
			}
		}

		if (firstToDie >= 0)
			return firstToDie;

		return -1;
	}

	private int PickDynamicChannel(SoundSource soundsource, SoundEntityChannel entchannel, in Vector3 origin, SfxTable? sfx, float delay, bool doNotOverwriteExisting) {
		Precache(sfx!);

		int channel = StealDynamicChannel(soundsource, (int)entchannel, in origin, sfx, delay, doNotOverwriteExisting);
		if (channel == -1)
			return -1;

		ref Channel ch = ref Channels[channel];

		if (ch.Sfx != null) {
			AudioSource? source = ch.Sfx.Source;
			if (source != null) {
				if (source is BassAudioSource bassSource && bassSource.IsLooped()) {
					if (ch.SoundSource == soundsource && ch.EntChannel == (int)entchannel && ch.Sfx == sfx)
						return -1;
				}
			}

			SndMix.FreeChannel(ref ch);
		}

		return channel;
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
			src = new BassAudioMemorySource(sfx);
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
		ch.DistMult = SndGain.SNDLVL_TO_DIST_MULT((int)parms.SoundLevel);
		ch.Flags.UpdatePositions = parms.UpdatePositions && (parms.SoundSource != 0);
		ch.Flags.FirstPass = true;
		ch.Flags.Music = IsMusicSound(sfx);
		ch.BassChannel = Bass.SampleGetChannel(((BassAudioSource)sfx.Source!).BassHandle, OnlyNew: false);
		ch.Guid = (int)++s_SoundGuid;
		g_ActiveChannels.Add(ref ch);

		PlaySound(in parms, ref ch);

		StartSoundParams startedParms = parms;
		soundServices.OnSoundStarted(ch.Guid, ref startedParms, sndname);
		return ch.Guid;
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

	readonly ChannelList reap =new();
	public void Update(double v) {
		Bass.GlobalSampleVolume = (int)(volume.GetFloat() * 10000);

		reap.Clear();
		g_ActiveChannels.GetActiveChannels(reap);
		for (int i = 0; i < reap.Count(); i++) {
			ref Channel ch = ref reap.GetChannel(i);
			if (ch.Sfx == null || ch.Flags.FirstPass || ch.BassChannel == 0)
				continue;
			if (Bass.ChannelIsActive(ch.BassChannel) == PlaybackState.Stopped)
				SndMix.FreeChannel(ref ch);
		}

		// int voiceChannelCount = 0;
		// int voiceChannelMaxVolume = 0;

		// g_snd_trace_count = 0;

		// SND_SetSpatialDelays();

		// DAS_CheckNewRoomDSP();

		ChannelList list = new();
		g_ActiveChannels.GetActiveChannels(list);

		if (snd_spatialize_roundrobin.GetInt() == 0) {
			for (int i = 0; i < list.Count(); i++) {
				ref Channel ch = ref list.GetChannel(i);
				Assert(ch.Sfx);
				Assert(ch.ActiveIndex > 0);

				Spatialize(ref ch);

				// if (ch.Sfx.Source != null && ch.Sfx.Source.IsVoiceSource()) {
				// 	voiceChannelCount++;
				// 	voiceChannelMaxVolume = max(voiceChannelMaxVolume, ChannelGetMaxVol(ref ch));
				// }
			}
		}
		else {
			uint robinmask = (uint)((1 << snd_spatialize_roundrobin.GetInt()) - 1);
			uint i = 0;

			for (int listIndex = 0; listIndex < list.Count(); listIndex++) {
				ref Channel ch = ref list.GetChannel(listIndex);
				Assert(ch.Sfx);
				Assert(ch.ActiveIndex > 0);

				if (ch.Flags.FirstPass || (robinmask & s_roundrobin) == (i & robinmask))
					Spatialize(ref ch);

				// if (ch.Sfx.Source != null && ch.Sfx.Source.IsVoiceSource()) {
				// 	voiceChannelCount++;
				// 	voiceChannelMaxVolume = max(voiceChannelMaxVolume, ChannelGetMaxVol(ref ch));
				// }

				++i;
			}

			++s_roundrobin;
		}

		// SND_ChannelTraceReset();

		Bass.Apply3D();

		Bass.Update((int)(float)(v * 1000));
	}

	public void UpdateListener(in Vector3 listenerOrigin, in Vector3 listenerForward, in Vector3 listenerRight, in Vector3 listenerUp, bool isListenerUnderwater) {
		listener_origin = listenerOrigin;
		Bass.Set3DPosition(ToBass(listenerOrigin), default, ToBass(listenerForward), ToBass(listenerUp));
		Bass.Apply3D();
	}

	public IVoiceRecord? CreateVoiceRecord(int v) {
		return null;
	}

	public bool IsSoundStillPlaying(int guid) {
		ChannelList list = new();
		g_ActiveChannels.GetActiveChannels(list);
		for (int i = 0; i < list.Count(); i++) {
			ref Channel ch = ref list.GetChannel(i);
			if (ch.Guid == guid && ch.Sfx != null)
				return true;
		}
		return false;
	}

	public void StopSoundByGuid(int guid) {
		ChannelList list = new();
		g_ActiveChannels.GetActiveChannels(list);
		for (int i = 0; i < list.Count(); i++) {
			ref Channel ch = ref list.GetChannel(i);
			if (ch.Guid == guid) {
				SndMix.FreeChannel(ref ch);
				return;
			}
		}
	}

	public void SetVolumeByGuid(int guid, float fvol) {
		ChannelList list = new();
		g_ActiveChannels.GetActiveChannels(list);
		for (int i = 0; i < list.Count(); i++) {
			ref Channel ch = ref list.GetChannel(i);
			if (ch.Guid == guid) {
				ch.MasterVol = (short)(255.0f * Math.Clamp(fvol, 0.0f, 1.0f));
				ApplyChannelVolume(ref ch);
				return;
			}
		}
	}

	readonly static ConVar volume = new("volume", "1.0", FCvar.Archive, "Sound volume", 0.0, 1.0);
	readonly static ConVar snd_spatialize_roundrobin = new("snd_spatialize_roundrobin", "0", FCvar.None, "Lowend optimization: if nonzero, spatialize only a fraction of sound channels each frame. 1/2^x of channels will be spatialized per frame.");
	readonly static ConVar voice_steal = new("voice_steal", "2");
	static uint s_roundrobin = 0;

}
