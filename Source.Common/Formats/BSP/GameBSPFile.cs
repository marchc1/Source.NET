using Source.Common.Mathematics;

using System.Numerics;
using System.Runtime.InteropServices;

namespace Source.Common.Formats.BSP;

public enum GameLump
{
	DetailProps = ('d' << 24) | ('p' << 16) | ('r' << 8) | 'p',
	DetailPropLighting = ('d' << 24) | ('p' << 16) | ('l' << 8) | 't',
	StaticProps = ('s' << 24) | ('p' << 16) | ('r' << 8) | 'p',
	DetailPropLightingHDR = ('d' << 24) | ('p' << 16) | ('l' << 8) | 'h'
}

public enum GameLumpVersion
{
	DetailProps = 4,
	DetailPropLighting = 0,
	StaticProps = 10,
	StaticPropLighting = 0,
	DetailPropLightingHDR = 0
}

public enum DetailPropOrientation
{
	Normal = 0,
	ScreenAligned,
	ScreenAlignedVertical
}

public enum DetailPropType
{
	Model = 0,
	Sprite,
	ShapeCross,
	ShapeTri
}

[Flags]
public enum StaticPropFlags
{
	Fades = 0x1,
	UseLightingOrigin = 0x2,
	NoDraw = 0x4,
	IgnoreNormals = 0x8,
	NoShadow = 0x10,
	ScreenSpaceFade = 0x20,
	NoPerVertexLighting = 0x40,
	NoSelfShadowing = 0x80,
	NoPerTexelLighting = 0x100,
	WCMask = 0x1d8
}

public struct DetailObjectDictLump
{
	public InlineArray128<byte> Name;
}

public struct DetailSpriteDictLump
{
	public Vector2 UL;
	public Vector2 LR;
	public Vector2 TexUL;
	public Vector2 TexLR;
}

public struct DetailObjectLump
{
	public Vector3 Origin;
	public QAngle Angles;
	public ushort DetailModel;
	public ushort Leaf;
	public ColorRGBExp32 Lighting;
	public uint LightStyles;
	public byte LightStyleCount;
	public byte SwayAmount;
	public byte ShapeAngle;
	public byte ShapeSize;
	public byte Orientation;
	public InlineArray3<byte> Padding2;
	public byte Type;
	public InlineArray3<byte> Padding3;
	public float Scale;
}

public struct DetailPropLightstylesLump
{
	public ColorRGBExp32 Lighting;
	public byte Style;
}

public struct StaticPropDictLump
{
	public InlineArray128<byte> Name;
}

public struct StaticPropLumpV4
{
	public Vector3 Origin;
	public QAngle Angles;
	public ushort PropType;
	public ushort FirstLeaf;
	public ushort LeafCount;
	public byte Solid;
	public byte Flags;
	public int Skin;
	public float FadeMinDist;
	public float FadeMaxDist;
	public Vector3 LightingOrigin;
}

public struct StaticPropLumpV5
{
	public Vector3 Origin;
	public QAngle Angles;
	public ushort PropType;
	public ushort FirstLeaf;
	public ushort LeafCount;
	public byte Solid;
	public byte Flags;
	public int Skin;
	public float FadeMinDist;
	public float FadeMaxDist;
	public Vector3 LightingOrigin;
	public float ForcedFadeScale;
}

public struct StaticPropLumpV6
{
	public Vector3 Origin;
	public QAngle Angles;
	public ushort PropType;
	public ushort FirstLeaf;
	public ushort LeafCount;
	public byte Solid;
	public byte Flags;
	public int Skin;
	public float FadeMinDist;
	public float FadeMaxDist;
	public Vector3 LightingOrigin;
	public float ForcedFadeScale;
	public ushort MinDXLevel;
	public ushort MaxDXLevel;
}

public struct StaticPropLump
{
	public Vector3 Origin;
	public QAngle Angles;
	public ushort PropType;
	public ushort FirstLeaf;
	public ushort LeafCount;
	public byte Solid;
	public int Skin;
	public float FadeMinDist;
	public float FadeMaxDist;
	public Vector3 LightingOrigin;
	public float ForcedFadeScale;
	public ushort MinDXLevel;
	public ushort MaxDXLevel;
	public uint Flags;
	public ushort LightmapResolutionX;
	public ushort LightmapResolutionY;

	public static implicit operator StaticPropLump(StaticPropLumpV4 rhs) {
		StaticPropLump lump = default;
		lump.Origin = rhs.Origin;
		lump.Angles = rhs.Angles;
		lump.PropType = rhs.PropType;
		lump.FirstLeaf = rhs.FirstLeaf;
		lump.LeafCount = rhs.LeafCount;
		lump.Solid = rhs.Solid;
		lump.Flags = rhs.Flags;
		lump.Skin = rhs.Skin;
		lump.FadeMinDist = rhs.FadeMinDist;
		lump.FadeMaxDist = rhs.FadeMaxDist;
		lump.LightingOrigin = rhs.LightingOrigin;

		lump.ForcedFadeScale = 1.0f;
		lump.MinDXLevel = 0;
		lump.MaxDXLevel = 0;
		lump.LightmapResolutionX = 0;
		lump.LightmapResolutionY = 0;

		lump.Flags |= (uint)StaticPropFlags.NoPerTexelLighting;
		return lump;
	}

	public static implicit operator StaticPropLump(StaticPropLumpV5 rhs) {
		StaticPropLump lump = MemoryMarshal.Cast<StaticPropLumpV5, StaticPropLumpV4>(MemoryMarshal.CreateSpan(ref rhs, 1))[0];
		lump.ForcedFadeScale = rhs.ForcedFadeScale;
		return lump;
	}

	public static implicit operator StaticPropLump(StaticPropLumpV6 rhs) {
		StaticPropLump lump = MemoryMarshal.Cast<StaticPropLumpV6, StaticPropLumpV5>(MemoryMarshal.CreateSpan(ref rhs, 1))[0];
		lump.MinDXLevel = rhs.MinDXLevel;
		lump.MaxDXLevel = rhs.MaxDXLevel;
		return lump;
	}
}

public struct StaticPropLeafLump
{
	public ushort Leaf;
}

public struct StaticPropLightstylesLump
{
	public ColorRGBExp32 Lighting;
}
