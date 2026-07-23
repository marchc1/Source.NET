using Source.Common.Audio;

namespace Source.Engine;

public partial class Sound
{
	public static AudioSource Voice_SetupAudioSource(int soudnsource, SoundEntityChannel entchannel) {
		throw new NotImplementedException();
	}

	internal bool IsInitted() {
		return Initialized;
	}

	internal void StopSound(int soundsource, int entchannel) {
		// todo
	}
}
