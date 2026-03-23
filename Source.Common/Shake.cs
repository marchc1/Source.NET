namespace Source.Common;

public enum ShakeCommand : int {
	Start,
	Stop,
	Amplitude,
	Frequency,
	StartRumbleOnly,
	StartNoRumble
}

public struct ScreenShake {
	public ShakeCommand Command;
	public float Amplitude;
	public float Frequency;
	public float Duration; // TODO: Does network_test change the precision of this?
}

public enum FadeFlags : short {
	In = 0x0001,
	Out = 0x0002,
	Modulate = 0x0004,
	StayOut = 0x0008,
	Purge = 0x0010,
}

public struct ScreenFade {
	public const int SCREENFADE_FRACBITS = 9;

	public ushort Duration;
	public ushort HoldTime;
	public FadeFlags FadeFlags;
	public byte R, G, B, A;
}
