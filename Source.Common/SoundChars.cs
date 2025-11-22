using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace Source.Common;

public static class SoundCharsUtils {

	public static ReadOnlySpan<char> SkipSoundChars(ReadOnlySpan<char> name) {
		int i = 0;
		for (i = 0; i < name.Length; i++) {
			if (!IsSoundChar(name[i]))
				break;
		}

		return name[i..];
	}
	public static bool IsSoundChar(char ic) {
		bool b = false;
		SoundChars c = (SoundChars)ic;
		b |= c == SoundChars.Stream || c == SoundChars.UserVox || c == SoundChars.Sentence || c == SoundChars.DryMix || c == SoundChars.Omni;
		b |= c == SoundChars.Doppler || c == SoundChars.Directional || c == SoundChars.DistVariant || c == SoundChars.SpatialStereo || c == SoundChars.FastPitch;
		return b;
	}
	public static bool TestSoundChar(ReadOnlySpan<char> name, SoundChars c) {
		for (int i = 0; i < name.Length; i++) {
			if (!IsSoundChar(name[i]))
				break;
			if (name[i] == (char)c)
				return true;
		}

		return false;
	}
}
public enum SoundChars : int
{
	Stream = '*',
	UserVox = '?',
	Sentence = '!',
	DryMix = '#',
	Doppler = '>',
	Directional = '<',
	DistVariant = '^',
	Omni = '@',
	SpatialStereo = ')',
	FastPitch = '}'
}
