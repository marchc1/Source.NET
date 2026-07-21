using ManagedBass;

using Source.Common.Audio;

using static Source.AudioSystem.AudioGlobals;
using static Source.AudioSystem.SndChannels;

namespace Source.AudioSystem;

public static class SndMix
{
	public static void FreeChannel(ref Channel ch) {
		if (ch.Flags.IsFreeingChannel)
			return;
		ch.Flags.IsFreeingChannel = true;

		// CloseMouth(ch);

		soundServices.OnSoundStopped(ch.Guid, ch.SoundSource, (SoundEntityChannel)ch.EntChannel, ch.Sfx!.GetName());

		ch.Flags.IsSentence = false;

		// ch.Mixer = null;
		Bass.ChannelStop(ch.BassChannel);
		ch.BassChannel = 0;
		ch.Sfx = null;

		g_ActiveChannels.Remove(ref ch);

		short index = ch.Index;
		ch = default;
		ch.Index = index;
	}
}
