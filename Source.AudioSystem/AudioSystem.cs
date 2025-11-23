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
public class AudioCache {
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


public class BassAudioSource : AudioSource
{
	public int BassHandle = 0;

	public virtual void Play() {

	}
}


public class BassAudioMemorySource : BassAudioSource, IDisposable
{
	public BassAudioMemorySource(ReadOnlySpan<char> file) {
		byte[]? data = audiocache.Lookup(file);
		if (data == null)
			return;

		BassHandle = Bass.SampleLoad(data, 0, data.Length, 128, BassFlags.Default);
		if(BassHandle == 0) {
			Dbg.Msg($"BASS: {Bass.LastError}\n");
		} 
	}

	public void Dispose() {
		Bass.StreamFree(BassHandle);
		BassHandle = 0;
	}

	public override void Play() {
		var ch = Bass.SampleGetChannel(BassHandle, false);
		if (ch != 0)
			Bass.ChannelPlay(ch, true);
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

		BassAudioSource? src = (BassAudioSource?)sfx.Source;
		if (src == null) {
			src = new BassAudioMemorySource(sfx.GetFileName());
			sfx.Source = src;
		}

		src.Play();
		return 0;
	}

	public long StartStaticSound(in StartSoundParams parms) {
		throw new NotImplementedException();
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
