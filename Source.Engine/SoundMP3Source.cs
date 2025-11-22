using Source.Common.Audio;

namespace Source.Engine;

public class AudioSourceMP3 : AudioSource
{
	public override void CacheLoad() {
		throw new NotImplementedException();
	}

	public override void CacheUnload() {
		throw new NotImplementedException();
	}

	public override AudioStatus GetCacheStatus() {
		throw new NotImplementedException();
	}

	public override ReadOnlySpan<char> GetSentence() {
		throw new NotImplementedException();
	}

	public override bool IsLooped() {
		throw new NotImplementedException();
	}

	public override bool IsStereoWav() {
		throw new NotImplementedException();
	}

	public override bool IsStreaming() {
		throw new NotImplementedException();
	}

	public override bool IsVoiceSource() {
		throw new NotImplementedException();
	}

	public override int SampleCount() {
		throw new NotImplementedException();
	}

	public override int SampleRate() {
		throw new NotImplementedException();
	}

	public override int SampleSize() {
		throw new NotImplementedException();
	}

}

public partial class Sound
{
	public static bool Audio_IsMP3(ReadOnlySpan<char> name) {
		int len = name.Length;
		if (len > 4)
			if (name[(len - 4)..].Equals(".mp3", StringComparison.InvariantCultureIgnoreCase))
				return true;
		return false;
	}
	public static AudioSource Audio_CreateMemoryMP3(SfxTable sfx) {
		throw new NotImplementedException();
	}
	public static AudioSource Audio_CreateStreamedMP3(SfxTable sfx) {
		throw new NotImplementedException();
	}
}
