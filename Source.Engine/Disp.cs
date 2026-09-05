global using static Source.Engine.Disp;

using Source.Common;
using Source.Common.MaterialSystem;

using System.Numerics;


namespace Source.Engine;

public static class Disp
{
	public const int DISP_LMCOORDS_STAGE = 1;
	public const int MAX_STATIC_BUFFER_VERTS = (8 * 1024);
	public const int MAX_STATIC_BUFFER_INDICES = (8 * 1024);
	public const int MAX_DISP_DECALS = 32;

	public const DispDecalHandle DISP_DECAL_HANDLE_INVALID = unchecked((DispDecalHandle)~0);
	public const DispShadowHandle DISP_SHADOW_HANDLE_INVALID = unchecked((DispShadowHandle)~0);
	public const DispDecalFragmentHandle DISP_DECAL_FRAGMENT_HANDLE_INVALID = unchecked((DispDecalFragmentHandle)~0);
	public const DispShadowFragmentHandle DISP_SHADOW_FRAGMENT_HANDLE_INVALID = unchecked((DispShadowFragmentHandle)~0);

	public static readonly List<byte> g_DispLMAlpha = [];
	public static readonly List<byte> g_DispLightmapSamplePositions = [];
	public static readonly List<DispGroup> g_DispGroups = [];
}

public class DispArray(nint elements)
{
	public DispInfo[] DispInfos = new DispInfo[elements];
	public int CurTag;
}

class EngineTesselateHelper : BaseTesselateHelper
{
	public MeshBuilder IndexMesh = new();
	public DispInfo Disp = null!;

	public override void EndTriangle() {
		int vertOffset = Disp.VertOffset;

		IndexMesh.Index((ushort)(TempIndices[0] + vertOffset));
		IndexMesh.AdvanceIndex();

		IndexMesh.Index((ushort)(TempIndices[1] + vertOffset));
		IndexMesh.AdvanceIndex();

		IndexMesh.Index((ushort)(TempIndices[2] + vertOffset));
		IndexMesh.AdvanceIndex();

		Disp.Indices[NIndices] = (ushort)(TempIndices[0] + vertOffset);
		Disp.Indices[NIndices + 1] = (ushort)(TempIndices[1] + vertOffset);
		Disp.Indices[NIndices + 2] = (ushort)(TempIndices[2] + vertOffset);

		NIndices += 3;
	}

	public override ref DispNodeInfo GetNodeInfo(int nodeBit) => ref Disp.GetNodeInfoRef(nodeBit);
}

public class GroupMesh
{
	public IMesh? Mesh;
	public readonly List<DispInfo?> DispInfos = [];
	public readonly List<DispInfo?> VisibleDisps = [];
	public readonly List<PrimList> Visible = [];
	public int NumVisible;
	public DispGroup? Group;
}

public class DispGroup
{
	public int LightmapPageID;
	public IMaterial? Material;
	public readonly List<GroupMesh> Meshes = [];
	public readonly List<int> DispInfos = [];
	public int Visible;
}

struct SideVertCorners
{
	public InlineArray2<FourVerts> Corners;
}

[Flags]
public enum DecalFlags : byte
{
	NodeBitfieldComputed = 0x1,
	DecalShadow = 0x2,
	NoIntersection = 0x4,
	FragmentsComputed = 0x8,
}

public struct DispDecalBase
{
	public DispNodeIntersectBitVec NodeIntersect;

	public DecalFlags Flags;
	public ushort NVerts;
	public ushort NTris;
}

public struct DispDecal
{
	public DispDecalBase Base;

	public Decal? Decal;
	public InlineArray2<float> DecalWorldScale;
	public InlineArray3<Vector3> TextureSpaceBasis;
	public float Size;
	public DispDecalFragmentHandle FirstFragment;
}

public struct DispShadowDecal
{
	public DispDecalBase Base;

	public ShadowHandle_t Shadow;
	public DispShadowFragmentHandle FirstFragment;
}

public struct DispShadowFragment
{
	public const int MAX_VERTS = 12;

	public int NVerts;
	public ShadowVertex[]? ShadowVerts;
}

public struct DispRenderVert
{
	public Vector3 Pos;
	public Vector3 Normal;
	public Vector3 SVector;
	public Vector3 TVector;
	public Vector2 TexCoord;
	public Vector2 LMCoords;
}
