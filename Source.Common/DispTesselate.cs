namespace Source.Common;

public abstract class BaseTesselateHelper
{
	public MaxDispVertsBitVec ActiveVerts;
	public PowerInfo PowerInfo = null!;

	public int NIndices;
	public ushort[] TempIndices = new ushort[6];

	public abstract void EndTriangle();
	public abstract ref DispNodeInfo GetNodeInfo(int nodeBit);
}

public static class DispTesselate
{
	static int InternalVertIndex(PowerInfo info, in VertIndex vert) => vert.Y * info.GetSideLength() + vert.X;

	static void InternalEndTriangle(BaseTesselateHelper helper, in VertIndex nodeIndex, ref int curTriVert) {
		Assert(curTriVert == 2);

		helper.TempIndices[2] = (ushort)InternalVertIndex(helper.PowerInfo, nodeIndex);

		helper.EndTriangle();

		helper.TempIndices[0] = helper.TempIndices[1];
		curTriVert = 1;
	}

	static void TesselateDisplacementNode(BaseTesselateHelper helper, in VertIndex nodeIndex, int level, int[] activeChildren) {
		int power = helper.PowerInfo.GetPower() - level;
		int vertInc = 1 << (power - 1);

		ref TesselateWinding winding = ref PowerInfo.g_TWinding;

		int curTriVert = 0;
		for (int vert = 0; vert < winding.NVerts; vert++) {
			VertIndex sideVert = VertIndex.BuildOffsetVertIndex(nodeIndex, winding.Verts![vert].Index, vertInc);

			int vertNode = winding.Verts[vert].Node;
			bool node = (vertNode != -1) && activeChildren[vertNode] != 0;
			if (node) {
				if (curTriVert == 2)
					InternalEndTriangle(helper, nodeIndex, ref curTriVert);

				curTriVert = 0;
			}
			else {
				int vertBit = InternalVertIndex(helper.PowerInfo, sideVert);
				if (helper.ActiveVerts.IsBitSet(vertBit)) {
					helper.TempIndices[curTriVert] = (ushort)InternalVertIndex(helper.PowerInfo, sideVert);
					curTriVert++;
					if (curTriVert == 2)
						InternalEndTriangle(helper, nodeIndex, ref curTriVert);
				}
			}
		}
	}

	static void TesselateDisplacement_R(BaseTesselateHelper helper, in VertIndex nodeIndex, int nodeBitIndex, int level) {
		Assert(nodeBitIndex < helper.PowerInfo.NodeCount);
		ref DispNodeInfo nodeInfo = ref helper.GetNodeInfo(nodeBitIndex);

		int oldIndexCount = helper.NIndices;

		int[] activeChildren = new int[4];
		if (level >= helper.PowerInfo.GetPower() - 1) {
			activeChildren[0] = activeChildren[1] = activeChildren[2] = activeChildren[3] = 0;
		}
		else {
			int nodeIndexInt = InternalVertIndex(helper.PowerInfo, nodeIndex);

			int childNodeBit = nodeBitIndex + 1;
			for (int child = 0; child < 4; child++) {
				ref VertIndex childNode = ref helper.PowerInfo.ChildVerts[nodeIndexInt].Verts[child];

				int vertBit = InternalVertIndex(helper.PowerInfo, childNode);
				activeChildren[child] = helper.ActiveVerts.IsBitSet(vertBit) ? 1 : 0;

				if (activeChildren[child] != 0) {
					TesselateDisplacement_R(helper, childNode, childNodeBit, level + 1);
				}
				else {
					ref DispNodeInfo childInfo = ref helper.GetNodeInfo(childNodeBit);
					childInfo.Count = 0;
					childInfo.Flags = 0;
				}

				childNodeBit += helper.PowerInfo.NodeIndexIncrements[level];
			}
		}

		if (helper.NIndices != oldIndexCount) {
			nodeInfo.Flags = DispNodeInfo.CHILDREN_HAVE_TRIANGLES;
			oldIndexCount = helper.NIndices;
		}
		else {
			nodeInfo.Flags = 0;
		}

		TesselateDisplacementNode(helper, nodeIndex, level, activeChildren);

		nodeInfo.Count = (byte)(helper.NIndices - oldIndexCount);
		nodeInfo.FirstTesselationIndex = (ushort)oldIndexCount;
		Assert(nodeInfo.Count % 3 == 0);
	}

	public static void TesselateDisplacement(BaseTesselateHelper helper) {
		helper.NIndices = 0;

		TesselateDisplacement_R(helper, helper.PowerInfo.RootNode, 0, 0);
	}
}
