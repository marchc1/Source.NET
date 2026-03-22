using CommunityToolkit.HighPerformance;

using SevenZip.CommandLineParser;

using Source.Common.Formats.Keyvalues;
using Source.Common.Physics;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Source.Physics;

public enum PhyModelType : short
{
	IVP_CompactSurface,
	IVP_MOPP,
	IVP_Ball,
	IVP_Virtual
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PhyHeader
{
	public int Size;
	public int ID;
	public int SolidCount;
	public long CheckSum;
}
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PhySurfaceHeader
{
	public int Size;
	public int VPhysicsID;
	public short Version;
	public PhyModelType ModelType;
}
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PhyCompactSurfaceHeader
{
	// Right after PhySurfaceHeader
	public int SurfaceSize;
	public Vector3 DragAxisAreas;
	public int AxisMapSize;
	public Vector3 MassCenter;
	public Vector3 RotationInertia;
	public float UpperLimitRadius;
	public uint BitwiseData1;
	public int OffsetLedgetreeRoot;
	public InlineArray2<int> Unused2;
	public int ID; // should be IVPS

	public readonly uint MaxDeviation => BitwiseData1 & 0xFF;              // low 8 bits
	public readonly int ByteSize => (int)(BitwiseData1 >> 8) & 0x00FFFFFF; // upper 24 bits
}
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PhyLedgeNode
{
	public int RightNodeOffset;
	public int CompactNodeOffset;
	public Vector3 Center;
	public float Radius;
	public InlineArray3<byte> BoxSizes;
	public byte Unused;
}
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PhyLedge
{
	public int PointOffset;
	public int BoneIndex;
	public uint BitwiseData1;
	public ushort TrianglesCount;
	public short Unknown;

	public readonly uint HasChildrenFlags => BitwiseData1 & 0x3;
	public readonly uint IsCompactFlag => (BitwiseData1 >> 2) & 0x3;
	public readonly uint SizeDiv16 => (BitwiseData1 >> 8) & 0x00FFFFFF;
}
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PhyEdge
{
	public uint Data;

	public readonly ushort StartPointIndex => (ushort)(Data & 0xFFFF);
	public readonly ushort OppositePointIndex => (ushort)((Data >> 16) & 0x7FFF);
	public readonly uint IsVirtual => (Data >> 31) & 1;
}
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PhyCompactTriangle
{
	public uint Data;
	public InlineArray3<PhyEdge> Edges;

	public readonly uint TriangleIndex => Data & 0xFFF;
	public readonly uint PierceIndex => (Data >> 12) & 0xFFF;
	public readonly uint MaterialIndex => (Data >> 24) & 0x7F;
	public readonly uint IsVirtual => (Data >> 31) & 1;
}


/// <summary>
/// The buffer argument is the start address of a PHY format block.
/// </summary>
public readonly ref struct PhyParser(ReadOnlySpan<byte> buffer)
{
	readonly ReadOnlySpan<byte> buffer = buffer;

	public readonly ReadOnlySpan<byte> GetData() => buffer;
	public readonly ref readonly PhyHeader ExtractPhyHeader()
		=> ref const_reinterpret<byte, PhyHeader>(buffer)[0];
	public readonly ref readonly PhySurfaceHeader ExtractPhySurfaceHeader()
		=> ref const_reinterpret<byte, PhySurfaceHeader>(buffer)[0];
	public readonly int ExtractSize()
		=> const_reinterpret<byte, PhyHeader>(buffer)[0].Size + sizeof(int);

	public unsafe void ParseSurfaces(List<Vector3[]> outConvexHulls) {
		ref readonly PhySurfaceHeader header = ref ExtractPhySurfaceHeader();

		ReadOnlySpan<byte> surfaceData = buffer[sizeof(PhySurfaceHeader)..];
		ref readonly PhyCompactSurfaceHeader compactHeader = ref const_reinterpret<byte, PhyCompactSurfaceHeader>(surfaceData)[0];

		int massCentreFieldOffset = Marshal.OffsetOf<PhyCompactSurfaceHeader>(nameof(PhyCompactSurfaceHeader.MassCenter)).ToInt32();

		int rootNodePos = massCentreFieldOffset + compactHeader.OffsetLedgetreeRoot;
		WalkLedgetree(surfaceData, rootNodePos, outConvexHulls);
	}

	private unsafe void WalkLedgetree(ReadOnlySpan<byte> surfaceData, int nodeOffset, List<Vector3[]> outConvexHulls) {
		ref readonly PhyLedgeNode node = ref const_reinterpret<byte, PhyLedgeNode>(surfaceData[nodeOffset..])[0];

		if (node.RightNodeOffset == 0) {
			int ledgeOffset = nodeOffset + node.CompactNodeOffset;
			ExtractLedgeVertices(surfaceData, ledgeOffset, outConvexHulls);
		}
		else {
			int leftOffset = nodeOffset + sizeof(PhyLedgeNode);
			int rightOffset = nodeOffset + node.RightNodeOffset;

			WalkLedgetree(surfaceData, leftOffset, outConvexHulls);
			WalkLedgetree(surfaceData, rightOffset, outConvexHulls);
		}
	}

	private unsafe void ExtractLedgeVertices(ReadOnlySpan<byte> surfaceData, int ledgeOffset, List<Vector3[]> outConvexHulls) {
		ref readonly PhyLedge ledge = ref const_reinterpret<byte, PhyLedge>(surfaceData[ledgeOffset..])[0];

		int triCount = ledge.TrianglesCount;

		int trianglesStart = ledgeOffset + sizeof(PhyLedge);

		int verticesStart = ledgeOffset + ledge.PointOffset;

		var vertexIndices = new HashSet<int>();
		for (int t = 0; t < triCount; t++) {
			int triOffset = trianglesStart + t * sizeof(PhyCompactTriangle);
			ref readonly PhyCompactTriangle tri = ref const_reinterpret<byte, PhyCompactTriangle>(surfaceData[triOffset..])[0];
			for (int e = 0; e < 3; e++)
				vertexIndices.Add(tri.Edges[e].StartPointIndex);
		}

		const int IVP_POINT_SIZE = 16;

		var hull = new Vector3[vertexIndices.Count];
		int i = 0;
		foreach (int idx in vertexIndices) {
			int vertAddr = verticesStart + idx * IVP_POINT_SIZE;
			ref readonly Vector3 point = ref const_reinterpret<byte, Vector3>(surfaceData[vertAddr..])[0];
			hull[i++] = point;
		}

		outConvexHulls.Add(hull);
	}
}

public static class PhysCollideParse
{
	/// <summary>
	/// We differ heavily here in that we expect a raw stream of bytes instead of post-size bytes
	/// </summary>
	public static PhysCollide? UnserializeFromBuffer(ReadOnlySpan<byte> buffer, int index, bool swap, out int readBytes) {
		PhyParser parser = new(buffer);
		ref readonly PhyHeader header = ref parser.ExtractPhyHeader();
		readBytes = parser.ExtractSize();

		if (header.ID == VPHYSICS_COLLISION_ID) {
			ref readonly PhySurfaceHeader surfaceHeader = ref const_reinterpret<PhyHeader, PhySurfaceHeader>(in header);
			Assert(surfaceHeader.Version == VPHYSICS_COLLISION_VERSION);
			switch (surfaceHeader.ModelType) {
				case PhyModelType.IVP_CompactSurface:
					return new PhysCollideCompactSurface(parser, index, swap);
				default:
					Assert(false);
					return null;
			}
		}

		return null;
	}
}
