using Source.Common.Audio;

using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Engine;

public struct ChannelList {
	public int Count() => count;
	public int GetChannelIndex(int listIndex) => List[listIndex];
	public AudioChannel? GetChannel(int listIndex) => Sound.Channels[GetChannelIndex(listIndex)];
	public void RemoveChannelFromList(int listIndex) {
		count--;
		if (count > 0 && listIndex != count) {
			List[listIndex] = List[count];
			Quashed[listIndex] = Quashed[count];
		}
	}
	public bool IsQuashed(int listIndex) => Quashed[listIndex];

	public int count;
	public InlineArrayMaxChannels<short> List;
	public InlineArrayMaxChannels<bool> Quashed;

	public bool HasSpeakerChannels;
	public bool HasDryChannels;
	public bool Has11kChannels;
	public bool Has22kChannels;
	public bool Has44kChannels;
	public Sound Sound;
}

public class ActiveChannels(Sound sound) {
	public void Add(AudioChannel channel) {

	}
	public void Remove(AudioChannel channel) {

	}
	public void GetActiveChannels(ref ChannelList list) {

	}
	public void Init() {

	}
	public int GetActiveCount() => count;

	int count;
	InlineArrayMaxChannels<short> list;
}
