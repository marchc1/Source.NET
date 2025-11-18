namespace Source.Common;

public enum StudioFlags : uint
{
	None = 0x00000000,
	Render = 0x00000001,
	ViewXFormAttachments = 0x00000002,
	DrawTranslucentSubmodels = 0x00000004,
	TwoPass = 0x00000008,
	StaticLighting = 0x00000010,
	Wireframe = 0x00000020,
	ItemBlink = 0x00000040,
	NoShadows = 0x00000080,
	WireframeVCollide = 0x00000100,
	NoOverrideForAttach = 0x00000200,
	GenerateStats = 0x01000000,
	SSAODepthTexture = 0x08000000,
	ShadowDepthTexture = 0x40000000,
	Transparency = 0x80000000
}

public enum ModelType
{
	Invalid,
	Brush,
	Sprite,
	Studio
}
