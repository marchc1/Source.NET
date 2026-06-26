using Source.Common.Formats.BSP;
using Source.Common.Mathematics;

namespace Source.Common;

public struct DispNodeInfo
{
	public const int CHILDREN_HAVE_TRIANGLES = 0x1;
	public ushort FirstTesselationIndex;
	public byte Count;
	public byte Flags;
}

public struct TesselateVert(VertIndex index, short node)
{
	public VertIndex Index = index;
	public short Node = node;
}

public struct TesselateWinding
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

	public VertInfo() {
		for (int i = 0; i < 2; i++) {
			Dependencies[i].Vert = new VertIndex(-1, -1);
			Dependencies[i].Neighbor = -1;
		}

		for (int i = 0; i < NUM_REVERSE_DEPENDENCIES; i++) {
			ReverseDependencies[i].Vert = new VertIndex(-1, -1);
			ReverseDependencies[i].Neighbor = -1;
		}

		Parent.X = Parent.Y = -1;
		NodeLevel = -1;
	}
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

public class PowerInfo(VertInfo[] vertInfo, FourVerts[] sideVerts, FourVerts[] childVerts, FourVerts[] sideVertCorners, TwoUShorts[] errorEdges, TriInfo[] triInfos)
{
	public VertInfo[] VertInfo = vertInfo;
	public FourVerts[] SideVerts = sideVerts;
	public FourVerts[] ChildVerts = childVerts;
	public FourVerts[] SideVertCorners = sideVertCorners;
	public TwoUShorts[] ErrorEdges = errorEdges;

	TriInfo[]? TriInfos = triInfos;
	int NTriInfos;

	int Power;

	public VertIndex RootNode;
	int SideLength;
	public int SideLengthM1;
	public int MidPoint;
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

	public const int NUM_POWERINFOS = BSPFileCommon.MAX_MAP_DISP_POWER + 1;

	static readonly short[,] g_SideVertMul = { { 1, 0 }, { 0, 1 }, { -1, 0 }, { 0, -1 } };

	static readonly VertCorners[] g_SideVertCorners =
	[
		new() { Corner1 = [1, -1],  Corner2 = [1, 1] },
		new() { Corner1 = [1, 1],   Corner2 = [-1, 1] },
		new() { Corner1 = [-1, 1],  Corner2 = [-1, -1] },
		new() { Corner1 = [-1, -1], Corner2 = [1, -1] }
	];

	static readonly VertIndex[] g_ChildNodeIndexMul =
	[
		new VertIndex(1, 1),
		new VertIndex(-1, 1),
		new VertIndex(-1, -1),
		new VertIndex(1, -1)
	];

	static readonly VertIndex[,] g_ChildNodeDependencies =
	{
		{ new VertIndex(1, 0),  new VertIndex(0, 1) },
		{ new VertIndex(0, 1),  new VertIndex(-1, 0) },
		{ new VertIndex(-1, 0), new VertIndex(0, -1) },
		{ new VertIndex(0, -1), new VertIndex(1, 0) }
	};

	static readonly int[,,] g_OrientationRotations =
	{
		{ { 1, 0 },  { 0, 1 } },
		{ { 0, 1 },  { -1, 0 } },
		{ { -1, 0 }, { 0, -1 } },
		{ { 0, -1 }, { 1, 0 } }
	};

	static VertIndex Transform2D(int[,] mat, in VertIndex vert, in VertIndex centerPoint) {
		VertIndex translated = vert - centerPoint;

		VertIndex transformed = new(
			(short)(translated.X * mat[0, 0] + translated.Y * mat[0, 1]),
			(short)(translated.X * mat[1, 0] + translated.Y * mat[1, 1]));

		return transformed + centerPoint;
	}

	static int[,] OrientationRotation(int orient) => new int[,] {
		{ g_OrientationRotations[orient, 0, 0], g_OrientationRotations[orient, 0, 1] },
		{ g_OrientationRotations[orient, 1, 0], g_OrientationRotations[orient, 1, 1] }
	};

	static void GetEdgeVertIndex(int sideLength, int edge, short vert, ref VertIndex output) {
		if (edge == BSPFileCommon.NEIGHBOREDGE_RIGHT) {
			output.X = (short)(sideLength - 1);
			output.Y = vert;
		}
		else if (edge == BSPFileCommon.NEIGHBOREDGE_TOP) {
			output.X = vert;
			output.Y = (short)(sideLength - 1);
		}
		else if (edge == BSPFileCommon.NEIGHBOREDGE_LEFT) {
			output.X = 0;
			output.Y = vert;
		}
		else {
			output.X = vert;
			output.Y = 0;
		}
	}

	static ushort VertToIndex(in VertIndex vert, int maxPower) => (ushort)(vert.Y * ((1 << maxPower) + 1) + vert.X);

	static VertIndex WrapVertIndex(in VertIndex input, int sideLength) {
		short[] output = new short[2];

		for (short i = 0; i < 2; i++) {
			if (input[i] < 0)
				output[i] = (short)(sideLength - 1 - (-input[i] % sideLength));
			else if (input[i] >= sideLength)
				output[i] = (short)(input[i] % sideLength);
			else
				output[i] = input[i];
		}

		return new VertIndex(output[0], output[1]);
	}

	static int GetFreeDependency(Span<VertDependency> dep, int elements) {
		for (int i = 0; i < elements; i++) {
			if (!dep[i].IsValid())
				return i;
		}

		Assert(false);
		return 0;
	}

	static void AddDependency(VertInfo[] dependencies, int sideLength, in VertIndex nodeIndex, in VertIndex dependency, int maxPower, bool checkNeighborDependency, bool addReverseDependency) {
		ushort nodeIndexInt = VertToIndex(nodeIndex, maxPower);
		ref VertInfo node = ref dependencies[nodeIndexInt];

		int dep = GetFreeDependency(node.Dependencies, 2);
		node.Dependencies[dep].Vert = dependency;
		node.Dependencies[dep].Neighbor = -1;

		if (addReverseDependency) {
			ref VertInfo depNode = ref dependencies[VertToIndex(dependency, maxPower)];
			dep = GetFreeDependency(depNode.ReverseDependencies, Common.VertInfo.NUM_REVERSE_DEPENDENCIES);
			depNode.ReverseDependencies[dep].Vert = nodeIndex;
			depNode.ReverseDependencies[dep].Neighbor = -1;
		}

		if (checkNeighborDependency) {
			short connection = DispUtilsHelper.GetEdgeIndexFromPoint(nodeIndex, maxPower);
			if (connection != -1) {
				Assert(!node.Dependencies[1].IsValid());

				VertIndex delta = new((short)(nodeIndex.X - dependency.X), (short)(nodeIndex.Y - dependency.Y));
				VertIndex newIndex = new((short)(nodeIndex.X + delta.X), (short)(nodeIndex.Y + delta.Y));

				int fullSideLength = (1 << maxPower) + 1;
				node.Dependencies[1].Vert = WrapVertIndex(new VertIndex(newIndex.X, newIndex.Y), fullSideLength);
				node.Dependencies[1].Neighbor = connection;
			}
		}
	}

	static readonly TesselateVert[] g_TesselateVerts =
	[
		new TesselateVert(new VertIndex(1, -1),  BSPFileCommon.CHILDNODE_LOWER_RIGHT),
		new TesselateVert(new VertIndex(0, -1),  -1),
		new TesselateVert(new VertIndex(-1, -1), BSPFileCommon.CHILDNODE_LOWER_LEFT),
		new TesselateVert(new VertIndex(-1, 0),  -1),
		new TesselateVert(new VertIndex(-1, 1),  BSPFileCommon.CHILDNODE_UPPER_LEFT),
		new TesselateVert(new VertIndex(0, 1),   -1),
		new TesselateVert(new VertIndex(1, 1),   BSPFileCommon.CHILDNODE_UPPER_RIGHT),
		new TesselateVert(new VertIndex(1, 0),   -1),
		new TesselateVert(new VertIndex(1, -1),  BSPFileCommon.CHILDNODE_LOWER_RIGHT)
	];

	public static TesselateWinding g_TWinding = new() {
		Verts = g_TesselateVerts,
		NVerts = (short)g_TesselateVerts.Length
	};

	static void InitPowerInfoTriInfos_R(PowerInfo info, in VertIndex nodeIndex, ref int triInfo, int maxPower, int level) {
		ushort nodeIndexInt = VertToIndex(nodeIndex, maxPower);

		if (level + 1 < maxPower) {
			for (int child = 0; child < 4; child++) {
				InitPowerInfoTriInfos_R(info, info.ChildVerts[nodeIndexInt].Verts[child], ref triInfo, maxPower, level + 1);
			}
		}
		else {
			ushort[] indices = new ushort[3];

			int vertInc = 1 << (maxPower - level - 1);

			ref TesselateWinding winding = ref g_TWinding;

			int curTriVert = 0;
			for (int vert = 0; vert < winding.NVerts; vert++) {
				VertIndex sideVert = VertIndex.BuildOffsetVertIndex(nodeIndex, winding.Verts![vert].Index, vertInc);

				if (curTriVert == 1) {
					info.TriInfos![triInfo].Indices[0] = indices[0];
					info.TriInfos![triInfo].Indices[1] = VertToIndex(sideVert, maxPower);
					info.TriInfos![triInfo].Indices[2] = nodeIndexInt;
					++triInfo;
				}

				indices[0] = VertToIndex(sideVert, maxPower);
				curTriVert = 1;
			}
		}
	}

	static void InitPowerInfo_R(PowerInfo powerInfo, int maxPower, in VertIndex nodeIndex, in VertIndex dependency1, in VertIndex dependency2, in VertIndex nodeEdge1, in VertIndex nodeEdge2, in VertIndex parent, int level) {
		int sideLength = (1 << maxPower) + 1;
		ushort nodeIndexInt = VertToIndex(nodeIndex, maxPower);

		powerInfo.VertInfo[nodeIndexInt].Parent = parent;
		powerInfo.VertInfo[nodeIndexInt].NodeLevel = (short)(level + 1);

		powerInfo.ErrorEdges[nodeIndexInt].Values[0] = VertToIndex(nodeEdge1, maxPower);
		powerInfo.ErrorEdges[nodeIndexInt].Values[1] = VertToIndex(nodeEdge2, maxPower);

		AddDependency(powerInfo.VertInfo, sideLength, nodeIndex, dependency1, maxPower, false, true);
		AddDependency(powerInfo.VertInfo, sideLength, nodeIndex, dependency2, maxPower, false, true);

		int power = maxPower - level;
		int vertInc = 1 << (power - 1);

		for (int side = 0; side < 4; side++) {
			VertIndex sideVert = new((short)(nodeIndex.X + g_SideVertMul[side, 0] * vertInc), (short)(nodeIndex.Y + g_SideVertMul[side, 1] * vertInc));
			ushort sideVertIndex = VertToIndex(sideVert, maxPower);

			powerInfo.SideVerts[nodeIndexInt].Verts[side] = sideVert;

			VertIndex sideVertCorner0 = new((short)(nodeIndex.X + g_SideVertCorners[side].Corner1[0] * vertInc), (short)(nodeIndex.Y + g_SideVertCorners[side].Corner1[1] * vertInc));
			VertIndex sideVertCorner1 = new((short)(nodeIndex.X + g_SideVertCorners[side].Corner2[0] * vertInc), (short)(nodeIndex.Y + g_SideVertCorners[side].Corner2[1] * vertInc));

			powerInfo.SideVertCorners[nodeIndexInt].Verts[side] = sideVertCorner0;

			powerInfo.ErrorEdges[sideVertIndex].Values[0] = VertToIndex(sideVertCorner0, maxPower);
			powerInfo.ErrorEdges[sideVertIndex].Values[1] = VertToIndex(sideVertCorner1, maxPower);

			AddDependency(powerInfo.VertInfo, sideLength, sideVert, nodeIndex, maxPower, true, true);
		}

		int nodeInc = vertInc >> 1;
		if (nodeInc != 0) {
			for (int child = 0; child < 4; child++) {
				VertIndex childVert = new((short)(nodeIndex.X + g_ChildNodeIndexMul[child].X * nodeInc), (short)(nodeIndex.Y + g_ChildNodeIndexMul[child].Y * nodeInc));

				powerInfo.ChildVerts[nodeIndexInt].Verts[child] = childVert;

				InitPowerInfo_R(powerInfo,
					maxPower,
					childVert,

					new VertIndex((short)(nodeIndex.X + g_ChildNodeDependencies[child, 0].X * vertInc), (short)(nodeIndex.Y + g_ChildNodeDependencies[child, 0].Y * vertInc)),
					new VertIndex((short)(nodeIndex.X + g_ChildNodeDependencies[child, 1].X * vertInc), (short)(nodeIndex.Y + g_ChildNodeDependencies[child, 1].Y * vertInc)),

					nodeIndex,
					new VertIndex((short)(nodeIndex.X + g_ChildNodeIndexMul[child].X * vertInc), (short)(nodeIndex.Y + g_ChildNodeIndexMul[child].Y * vertInc)),

					nodeIndex,
					level + 1);
			}
		}
	}

	public static void InitPowerInfo(PowerInfo info, int maxPower) {
		int sideLength = (1 << maxPower) + 1;

		VertIndex nodeDependency1 = new((short)(sideLength - 1), (short)(sideLength - 1));
		VertIndex nodeDependency2 = new(0, 0);

		info.RootNode = new VertIndex((short)(sideLength / 2), (short)(sideLength / 2));
		info.SideLength = sideLength;
		info.SideLengthM1 = sideLength - 1;
		info.MidPoint = sideLength / 2;
		info.MaxVerts = sideLength * sideLength;

		info.CornerPointIndices[BSPFileCommon.CORNER_LOWER_LEFT].Init(0, 0);
		info.CornerPointIndices[BSPFileCommon.CORNER_UPPER_LEFT].Init(0, (short)(sideLength - 1));
		info.CornerPointIndices[BSPFileCommon.CORNER_UPPER_RIGHT].Init((short)(sideLength - 1), (short)(sideLength - 1));
		info.CornerPointIndices[BSPFileCommon.CORNER_LOWER_RIGHT].Init((short)(sideLength - 1), 0);

		InitPowerInfo_R(info,
			maxPower,
			info.RootNode,

			nodeDependency1,
			nodeDependency2,

			new VertIndex(0, 0),
			new VertIndex((short)(sideLength - 1), (short)(sideLength - 1)),

			new VertIndex(-1, -1),
			0);

		info.Power = maxPower;

		int triInfo = 0;
		InitPowerInfoTriInfos_R(info, info.RootNode, ref triInfo, maxPower, 0);

		for (int edge = 0; edge < 4; edge++) {
			VertIndex nextVert = default;
			GetEdgeVertIndex(sideLength, edge, 0, ref info.EdgeStartVerts[edge]);
			GetEdgeVertIndex(sideLength, edge, 1, ref nextVert);
			info.EdgeIncrements[edge] = nextVert - info.EdgeStartVerts[edge];

			VertIndex nbStartVert = default, nbNextVert = default, nbDelta;
			GetEdgeVertIndex(sideLength, (edge + 2) & 3, 0, ref nbStartVert);
			GetEdgeVertIndex(sideLength, (edge + 2) & 3, 1, ref nbNextVert);
			nbDelta = nbNextVert - nbStartVert;

			for (int orient = 0; orient < 4; orient++) {
				info.NeighborStartVerts[edge, orient] = Transform2D(OrientationRotation(orient), nbStartVert, new VertIndex((short)(sideLength / 2), (short)(sideLength / 2)));
				info.NeighborIncrements[edge, orient] = Transform2D(OrientationRotation(orient), nbDelta, new VertIndex(0, 0));
			}
		}

		int curPowerOf4 = 1;
		int curTotal = 0;
		for (int i = 0; i < maxPower - 1; i++) {
			curTotal += curPowerOf4;

			info.NodeIndexIncrements[maxPower - i - 2] = curTotal;

			curPowerOf4 *= 4;
		}

		info.NodeCount = curTotal + curPowerOf4;

		info.NTriInfos = MathLib.Square(1 << maxPower) * 2;
	}

	static PowerInfo CreatePowerInfo(int size) {
		VertInfo[] vertInfo = new VertInfo[size * size];
		for (int i = 0; i < vertInfo.Length; i++)
			vertInfo[i] = new VertInfo();

		return new PowerInfo(
			vertInfo,
			new FourVerts[size * size],
			new FourVerts[size * size],
			new FourVerts[size * size],
			new TwoUShorts[size * size],
			new TriInfo[(size - 1) * (size - 1) * 2]);
	}

	static readonly PowerInfo?[] g_PowerInfos = new PowerInfo?[NUM_POWERINFOS];

	static PowerInfo() {
		g_PowerInfos[2] = CreatePowerInfo(5);
		g_PowerInfos[3] = CreatePowerInfo(9);
		g_PowerInfos[4] = CreatePowerInfo(17);

		for (int i = 0; i <= BSPFileCommon.MAX_MAP_DISP_POWER; i++) {
			if (g_PowerInfos[i] != null)
				InitPowerInfo(g_PowerInfos[i]!, i);
		}
	}

	public static PowerInfo GetPowerInfo(int power) {
		Assert(power >= 0 && power < g_PowerInfos.Length);
		Assert(g_PowerInfos[power] != null);
		return g_PowerInfos[power]!;
	}
}