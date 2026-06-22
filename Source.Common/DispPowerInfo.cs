using Source.Common.Formats.BSP;

namespace Source.Common;

struct DispNodeInfo
{
	public const int CHILDREN_HAVE_TRIANGLES = 0x1;
	public ushort FirstTesselationIndex;
	public byte Count;
	public byte Flags;
}

struct TesselateVert(VertIndex index, short node)
{
	public VertIndex Index = index;
	public short Node = node;
}

struct TesselateWinding
{
	public TesselateVert[]? Verts;
	public short NVerts;
}

public struct VertDependency
{
	public VertIndex Vert;
	public short Neighbor;
	public readonly bool IsValid() => Vert.X != -1;
}

public struct VertInfo
{
	public const int NUM_REVERSE_DEPENDENCIES = 4;

	public InlineArray2<VertDependency> Dependencies;
	public InlineArray4<VertDependency> ReverseDependencies;

	public short NodeLevel;
	public VertIndex Parent;
}

public struct TwoUShorts
{
	public InlineArray2<ushort> Values;
}

public struct FourVerts
{
	public InlineArray4<VertIndex> Verts;
}

public struct TriInfo
{
	public InlineArray3<ushort> Indices;
}

struct VertCorners()
{
	public short[] Corner1 = new short[2];
	public short[] Corner2 = new short[2];
}

public class PowerInfo(VertInfo vertInfo, FourVerts sideVerts, FourVerts childVerts, FourVerts sideVertCorners, TwoUShorts errorEdges, TriInfo[] triInfos)
{
	public VertInfo VertInfo = vertInfo;
	public FourVerts SideVerts = sideVerts;
	public FourVerts ChildVerts = childVerts;
	public FourVerts SideVertCorners = sideVertCorners;
	public TwoUShorts ErrorEdges = errorEdges;

	TriInfo[]? TriInfos = triInfos;
	int NTriInfos;

	int Power;

	VertIndex RootNode;
	int SideLength;
	public int SideLengthM1;
	int MidPoint;
	public int MaxVerts;
	public int NodeCount;

	public int[] NodeIndexIncrements = new int[BSPFileCommon.MAX_MAP_DISP_POWER];

	public VertIndex[] EdgeStartVerts = new VertIndex[4];
	public VertIndex[] EdgeIncrements = new VertIndex[4];

	public VertIndex[,] NeighborStartVerts = new VertIndex[4, 4];
	public VertIndex[,] NeighborIncrements = new VertIndex[4, 4];

	VertIndex[] CornerPointIndices = new VertIndex[4];

	public int GetPower() => Power;

	public int GetSideLength() => SideLength;

	public ref VertIndex GetRootNode() => ref RootNode;

	public int GetMidPoint() => MidPoint;

	public int GetNumTriInfos() => NTriInfos;

	public ref TriInfo GetTriInfo(int i) => ref TriInfos![i];

	public int GetNumVerts() => MaxVerts;

	public ref VertIndex GetCornerPointIndex(int corner) {
		Assert(corner >= 0 && corner < 4);
		return ref CornerPointIndices[corner];
	}
}