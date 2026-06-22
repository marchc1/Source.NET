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

public class DispDecalBase
{

}

public class DispDecal : DispDecalBase
{

}

class DispShadowDecal : DispDecalBase
{

}

class DispShadowFragment
{

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
