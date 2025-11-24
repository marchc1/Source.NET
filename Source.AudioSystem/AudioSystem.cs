using ManagedBass;

using Source.Common.Audio;
using Source.Common.Commands;
using Source.Common.Filesystem;

using System.Numerics;

using static Source.AudioSystem.AudioGlobals;
using static Source.AudioSystem.BassAudioMemorySource;
namespace Source.AudioSystem;

[EngineComponent]
public static class AudioGlobals
{
	[Dependency] public static IFileSystem filesystem = null!;
	[Dependency] public static AudioCache audiocache = null!;
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

	public BassAudioMemorySource(ReadOnlySpan<char> file) {
		Info = null!;
		byte[]? data = audiocache.Lookup(file);
		if (data == null)
			return;

		BassHandle = Bass.SampleLoad(data, 0, data.Length, MAX_CHANNELS, BassFlags.Default);
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

	public override bool IsLooped() {
		return false; // todo
	}

	public override int PickDynamicChannel(int soundsource, SoundEntityChannel entchannel, in Vector3 origin, SfxTable sfx, double delay, bool doNotOverwriteExisting) {
		int ch = Bass.SampleGetChannel(BassHandle, OnlyNew: false);
		if (ch == 0)
			return 0;

		return ch;
	}

	public override int PickStaticChannel(int soundsource, SfxTable sfx) {
		return staticChannel;
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
		if (!Bass.Init(1))
			return false;

		return true;
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
		Bass.ChannelPlay(targetChannel);
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
		if (targetChannel == 0)
			return 0;

		Bass.ChannelPlay(targetChannel);
		Bass.ChannelSetAttribute(targetChannel, ChannelAttribute.Frequency, parms.Pitch / 100f);
		Bass.ChannelSetAttribute(targetChannel, ChannelAttribute.Volume, parms.Volume / 100f);

		return 0;
	}

	private bool AlterChannel(int soundSource, SoundEntityChannel entChannel, SfxTable? sfx, int vol, int pitch, SoundFlags flags) {
		return false;
	}

	public void Update(double v) {
		Bass.GlobalSampleVolume = (int)(volume.GetFloat() * 10000);
		Bass.GlobalMusicVolume = (int)(snd_musicvolume.GetFloat() * volume.GetFloat() * 10000);
		Bass.Update((int)(float)(v * 1000));
	}

	public void UpdateListener(in Vector3 listenerOrigin, in Vector3 listenerForward, in Vector3 listenerRight, in Vector3 listenerUp, bool isListenerUnderwater) {

	}

	readonly static ConVar volume = new("volume", "1.0", FCvar.Archive, "Sound volume", 0.0, 1.0);
	readonly ConVar snd_musicvolume = new("snd_musicvolume", "1.0", FCvar.Archive, "Music volume", 0.0, 1.0);

}
