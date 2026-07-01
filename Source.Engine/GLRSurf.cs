global using static Source.Engine.GLRSurfGlobals;

using Source.Common;
using Source.Common.Formats.BSP;

using System.Numerics;

namespace Source.Engine;

public static class GLRSurfGlobals
{
	public static readonly EngineBSPTree g_ToolBSPTree = new();
}

public class EngineBSPTree : ISpatialQuery
{
	const int ENUM_SPHERE_TEST_X = 0x1;
	const int ENUM_SPHERE_TEST_Y = 0x2;
	const int ENUM_SPHERE_TEST_Z = 0x4;
	const int ENUM_SPHERE_TEST_ALL = 0x7;

	struct EnumLeafSphereInfo
	{
		public Vector3 Center;
		public float Radius;
		public Vector3 BoxCenter;
		public Vector3 BoxHalfDiagonal;
		public ISpatialLeafEnumerator Iterator;
		public nint Context;
	}

	static CommonHostState host_state => field ??= Singleton<CommonHostState>();

	public int LeafCount() => host_state.WorldBrush!.NumLeafs;

	public bool EnumerateLeavesAtPoint(in Vector3 pt, ISpatialLeafEnumerator pEnum, nint context) => pEnum.EnumerateLeaf(CM.PointLeafnum(pt), context);

	public bool EnumerateLeavesInBox(in Vector3 mins, in Vector3 maxs, ISpatialLeafEnumerator pEnum, nint context) => throw new NotImplementedException();

	public bool EnumerateLeavesInSphere(in Vector3 center, float radius, ISpatialLeafEnumerator pEnum, nint context) {
		EnumLeafSphereInfo info = default;
		info.Center = center;
		info.Radius = radius;
		info.Iterator = pEnum;
		info.Context = context;
		info.BoxCenter = center;
		info.BoxHalfDiagonal = new(radius, radius, radius);

		return EnumerateLeafInSphere_R(host_state.WorldBrush!.Nodes![0], ref info, ENUM_SPHERE_TEST_ALL);
	}

	public bool EnumerateLeavesAlongRay(in Ray ray, ISpatialLeafEnumerator pEnum, nint context) => throw new NotImplementedException();

	static bool EnumerateLeafInSphere_R(BSPMNode node, ref EnumLeafSphereInfo info, int testFlags) {
		while (true) {
			if (node.Contents == (int)Contents.Solid)
				return true;

			if (node.Contents >= 0) {
				if (testFlags != 0) {
					if (!CollisionUtils.IsBoxIntersectingSphereExtents(node.Center, node.HalfDiagonal, info.Center, info.Radius))
						return true;
				}

				return info.Iterator.EnumerateLeaf(((BSPMLeaf)node).Index, info.Context);
			}
			else if (testFlags != 0) {
				if (node.Contents == -1) {
					if ((testFlags & ENUM_SPHERE_TEST_X) != 0) {
						float delta = MathF.Abs(node.Center.X - info.BoxCenter.X);
						float size = node.HalfDiagonal.X + info.BoxHalfDiagonal.X;
						if (delta > size)
							return true;

						if (delta + node.HalfDiagonal.X < info.BoxHalfDiagonal.X)
							testFlags &= ~ENUM_SPHERE_TEST_X;
					}

					if ((testFlags & ENUM_SPHERE_TEST_Y) != 0) {
						float delta = MathF.Abs(node.Center.Y - info.BoxCenter.Y);
						float size = node.HalfDiagonal.Y + info.BoxHalfDiagonal.Y;
						if (delta > size)
							return true;

						if (delta + node.HalfDiagonal.Y < info.BoxHalfDiagonal.Y)
							testFlags &= ~ENUM_SPHERE_TEST_Y;
					}

					if ((testFlags & ENUM_SPHERE_TEST_Z) != 0) {
						float delta = MathF.Abs(node.Center.Z - info.BoxCenter.Z);
						float size = node.HalfDiagonal.Z + info.BoxHalfDiagonal.Z;
						if (delta > size)
							return true;

						if (delta + node.HalfDiagonal.Z < info.BoxHalfDiagonal.Z)
							testFlags &= ~ENUM_SPHERE_TEST_Z;
					}
				}
				else if (node.Contents == -2)
					testFlags = 0;
			}

			float normalDotCenter = Vector3.Dot(node.Plane.Normal, info.Center);

			if (normalDotCenter + info.Radius <= node.Plane.Dist)
				node = node.Children[1]!;
			else if (normalDotCenter - info.Radius >= node.Plane.Dist)
				node = node.Children[0]!;
			else {
				if (!EnumerateLeafInSphere_R(node.Children[0]!, ref info, testFlags))
					return false;

				node = node.Children[1]!;
			}
		}
	}
}
