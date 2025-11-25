namespace Game.Shared;

[Flags]
public enum BeamFlags
{
	StartEntity = 0x00000001,
	EndEntity = 0x00000002,
	FadeIn = 0x00000004,
	FadeOut = 0x00000008,
	SineNoise = 0x00000010,
	Solid = 0x00000020,
	ShadeIn = 0x00000040,
	ShadeOut = 0x00000080,
	OnlyNoiseOnce = 0x00000100,
	NoTile = 0x00000200,
	UseHitboxes = 0x00000400,
	StartVisible = 0x00000800,
	EndVisible = 0x00001000,
	IsACtive = 0x00002000,
	Forever = 0x00004000,
	HaloBeam = 0x00008000,
	Reversed = 0x00010000,
	NumFlags = 17
}
