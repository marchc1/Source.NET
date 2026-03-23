using CommunityToolkit.HighPerformance;

using Source.Common;
using Source.Common.Formats.BSP;
using Source.Common.Mathematics;

using System.Numerics;
using System.Runtime.CompilerServices;

namespace Source.Engine;

public static partial class CM
{
	public static int PointLeafnum(in Vector3 point) {
		CollisionBSPData bspData = GetCollisionBSPData();
		if (bspData.NumPlanes == 0)
			return 0;
		return PointLeafnum_r(bspData, in point, 0);
	}

	public static int PointLeafnum_r(CollisionBSPData bspData, in Vector3 point, int num) {
		float d;
		ref CollisionNode node = ref Unsafe.NullRef<CollisionNode>();
		ref CollisionPlane plane = ref Unsafe.NullRef<CollisionPlane>();

		while (num >= 0) {
			node = ref bspData.MapNodes.AsSpan()[bspData.MapRootNode + num];
			plane = ref bspData.MapPlanes.AsSpan()[node.CollisionPlaneIdx];

			if ((int)plane.Type < 3)
				d = point[(int)plane.Type] - plane.Dist;
			else
				d = Vector3.Dot(plane.Normal, point) - plane.Dist;

			if (d < 0)
				num = node.Children[1];
			else
				num = node.Children[0];
		}

		return -1 - num;
	}

	public static void BoxTrace(in Ray ray, int headnode, Mask brushmask, bool computeEndPt, ref Trace trace) {
		// todo
	}
}
