using ManagedBass;

using Source.Common.Audio;
using Source.Common.Filesystem;

using System.Numerics;

using static Source.AudioSystem.AudioGlobals;
using static Source.AudioSystem.BassAudioFileSource;
namespace Source.AudioSystem;

[EngineComponent]
public static class AudioGlobals
{
	[Dependency] public static IFileSystem filesystem = null!;
}


public class BassAudioSource : AudioSource
{
	public int BassStream = 0;
}


public class BassAudioFileSource : BassAudioSource, IDisposable
{
	public BassAudioFileSource(ReadOnlySpan<char> file) {
		Span<char> fileSearch = stackalloc char[file.Length + 6];
		"sound/".CopyTo(fileSearch);
		file.CopyTo(fileSearch[6..]);

		if (!filesystem.FileExists(fileSearch, "game"))
			return;

		IFileHandle? fh = filesystem.Open(fileSearch, FileOpenOptions.Read | FileOpenOptions.Binary, "game");
		if (fh == null)
			return;

		byte[] data = new byte[fh.Stream.Length];
		fh.Stream.ReadExactly(data);
		fh.Dispose();

		BassStream = Bass.CreateStream(data, 0, data.Length, BassFlags.Default);
		if(BassStream == 0) {
			Dbg.Msg($"BASS: {Bass.LastError}\n");
		} 
	}

	public void Dispose() {
		Bass.StreamFree(BassStream);
		BassStream = 0;
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
		BassAudioSource src = PreloadSound(parms.Sfx);
		Bass.ChannelPlay(src.BassStream);
		return 0;
	}

	private BassAudioSource PreloadSound(SfxTable? sfx) {
		if (sfx == null)
			return null!;

		if (sfx.Source != null)
			return (BassAudioSource)sfx.Source;

		var src = new BassAudioFileSource(sfx.GetFileName());
		sfx.Source = src;
		return src;
	}

	public long StartStaticSound(in StartSoundParams parms) {
		throw new NotImplementedException();
	}

	public void Update(double v) {
		Bass.Update((int)(float)(v * 1000));
	}

	public void UpdateListener(in Vector3 listenerOrigin, in Vector3 listenerForward, in Vector3 listenerRight, in Vector3 listenerUp, bool isListenerUnderwater) {

	}
}
