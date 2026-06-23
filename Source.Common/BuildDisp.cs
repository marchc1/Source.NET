using Source.Common.Formats.BSP;
using Source.Common.Mathematics;

using System.Globalization;
using System.Numerics;

namespace Source.Common;

struct CoreDispBBox
{
	public Vector3 VMin, VMax;
}

public class CoreDispSurface
{
	public const int QUAD_POINT_COUNT = 4;
	public const int MAX_CORNER_NEIGHBOR_COUNT = 16;

	int Index;

	int PointCount;
	Vector3[] Points = new Vector3[QUAD_POINT_COUNT];
	Vector3[] Normals = new Vector3[QUAD_POINT_COUNT];
	Vector2[] TexCoords = new Vector2[QUAD_POINT_COUNT];
	Vector2[,] LuxelCoords = new Vector2[Constants.NUM_BUMP_VECTS + 1, QUAD_POINT_COUNT];
	float[] Alphas = new float[QUAD_POINT_COUNT];

	int LuxelU;
	int LuxelV;

	DispNeighbor[] EdgeNeighbors = new DispNeighbor[4];
	DispCornerNeighbors[] CornerNeighbors = new DispCornerNeighbors[4];

	int Flags;
	int Contents;

	Vector3 SAxis;
	Vector3 TAxis;
	int PointStartIndex;
	Vector3 PointStart;

	public void Init() {
		Index = -1;
		PointCount = 0;

		int i;
		for (i = 0; i < QUAD_POINT_COUNT; i++) {
			MathLib.VectorClear(out Points[i]);
			MathLib.VectorClear(out Normals[i]);
			MathLib.Vector2DClear(out TexCoords[i]);

			for (int j = 0; j < Constants.NUM_BUMP_VECTS; j++)
				MathLib.Vector2DClear(out LuxelCoords[i, j]);

			Alphas[i] = 1;
		}

		PointStartIndex = -1;
		MathLib.VectorClear(out PointStart);
		MathLib.VectorClear(out SAxis);
		MathLib.VectorClear(out TAxis);

		for (i = 0; i < 4; i++) {
			EdgeNeighbors[i].SetInvalid();
			CornerNeighbors[i].SetInvalid();
		}

		Flags = 0;
		Contents = 0;
	}

	public void SetHandle(int handle) => Index = handle;
	public int GetHandle() => Index;

	public void SetPointCount(int count) {
		if (count != 4)
			return;

		PointCount = count;
	}
	public int GetPointCount() => PointCount;

	public void SetPoint(int index, in Vector3 pt) {
		Assert(index >= 0);
		Assert(index < QUAD_POINT_COUNT);
		MathLib.VectorCopy(pt, out Points[index]);
	}
	public void GetPoint(int index, out Vector3 pt) {
		Assert(index >= 0);
		Assert(index < QUAD_POINT_COUNT);
		MathLib.VectorCopy(Points[index], out pt);
	}
	public Vector3 GetPoint(int index) {
		Assert(index >= 0);
		Assert(index < QUAD_POINT_COUNT);
		return Points[index];
	}

	public void SetPointNormal(int index, in Vector3 normal) {
		Assert(index >= 0);
		Assert(index < QUAD_POINT_COUNT);
		MathLib.VectorCopy(normal, out Normals[index]);
	}
	public void GetPointNormal(int index, out Vector3 normal) {
		Assert(index >= 0);
		Assert(index < QUAD_POINT_COUNT);
		MathLib.VectorCopy(Normals[index], out normal);
	}
	public void SetTexCoord(int index, in Vector2 texCoord) {
		Assert(index >= 0);
		Assert(index < QUAD_POINT_COUNT);
		MathLib.Vector2DCopy(texCoord, out TexCoords[index]);
	}
	public void GetTexCoord(int index, out Vector2 texCoord) {
		Assert(index >= 0);
		Assert(index < QUAD_POINT_COUNT);
		MathLib.Vector2DCopy(TexCoords[index], out texCoord);
	}

	public void SetLuxelCoord(int bumpIndex, int index, in Vector2 luxelCoord) {
		Assert(index >= 0);
		Assert(index < QUAD_POINT_COUNT);
		Assert(bumpIndex >= 0);
		Assert(bumpIndex < Constants.NUM_BUMP_VECTS + 1);
		MathLib.Vector2DCopy(luxelCoord, out LuxelCoords[bumpIndex, index]);
	}
	public void GetLuxelCoord(int bumpIndex, int index, out Vector2 luxelCoord) {
		Assert(index >= 0);
		Assert(index < QUAD_POINT_COUNT);
		Assert(bumpIndex >= 0);
		Assert(bumpIndex < Constants.NUM_BUMP_VECTS + 1);
		MathLib.Vector2DCopy(LuxelCoords[bumpIndex, index], out luxelCoord);
	}
	public void SetLuxelCoords(int bumpIndex, Vector2[] coords) {
		Assert(bumpIndex >= 0);
		Assert(bumpIndex < Constants.NUM_BUMP_VECTS + 1);
		for (int i = 0; i < 4; i++)
			MathLib.Vector2DCopy(coords[i], out LuxelCoords[bumpIndex, i]);
	}
	public void GetLuxelCoords(int bumpIndex, Vector2[] coords) {
		Assert(bumpIndex >= 0);
		Assert(bumpIndex < Constants.NUM_BUMP_VECTS + 1);
		for (int i = 0; i < 4; i++)
			MathLib.Vector2DCopy(LuxelCoords[bumpIndex, i], out coords[i]);
	}


	public void SetLuxelU(int u) => LuxelU = u;
	public int GetLuxelU() => LuxelU;
	public void SetLuxelV(int v) => LuxelV = v;
	public int GetLuxelV() => LuxelV;
	public bool CalcLuxelCoords(int luxels, bool adjust, in Vector3 u, in Vector3 v) {
		if (luxels <= 0.0f)
			return false;

		int offset = 0;
		if (adjust)
			offset = GetPointStartIndex();

		bool longU = LongestInU(u, v);

		float lengthTemp;
		float uLength = (Points[(3 + offset) % 4] - Points[(0 + offset) % 4]).Length();
		lengthTemp = (Points[(2 + offset) % 4] - Points[(1 + offset) % 4]).Length();
		if (lengthTemp > uLength)
			uLength = lengthTemp;

		float vLength = (Points[(1 + offset) % 4] - Points[(0 + offset) % 4]).Length();
		lengthTemp = (Points[(2 + offset) % 4] - Points[(3 + offset) % 4]).Length();
		if (lengthTemp > vLength)
			vLength = lengthTemp;

		float flOOLuxelScale = 1.0f / luxels;
		float uValue = (int)(uLength * flOOLuxelScale) + 1;
		if (uValue > BSPFileCommon.MAX_DISP_LIGHTMAP_DIM_WITHOUT_BORDER)
			uValue = BSPFileCommon.MAX_DISP_LIGHTMAP_DIM_WITHOUT_BORDER;

		float vValue = (int)(vLength * flOOLuxelScale) + 1;
		if (vValue > BSPFileCommon.MAX_DISP_LIGHTMAP_DIM_WITHOUT_BORDER)
			vValue = BSPFileCommon.MAX_DISP_LIGHTMAP_DIM_WITHOUT_BORDER;

		bool swapped = false;
		if (longU) {
			if (vValue > uValue)
				swapped = true;
		}
		else {
			if (uValue > vValue)
				swapped = true;
		}

		LuxelU = (int)uValue;
		LuxelV = (int)vValue;

		for (int iBump = 0; iBump < Constants.NUM_BUMP_VECTS + 1; ++iBump) {
			LuxelCoords[iBump, (0 + offset) % 4].Init(0.5f, 0.5f);
			LuxelCoords[iBump, (1 + offset) % 4].Init(0.5f, vValue + 0.5f);
			LuxelCoords[iBump, (2 + offset) % 4].Init(uValue + 0.5f, vValue + 0.5f);
			LuxelCoords[iBump, (3 + offset) % 4].Init(uValue + 0.5f, 0.5f);
		}

		return swapped;
	}
	public void SetAlpha(int index, float alpha) {
		Assert(index >= 0);
		Assert(index < QUAD_POINT_COUNT);
		Alphas[index] = alpha;
	}
	public float GetAlpha(int index) {
		Assert(index >= 0);
		Assert(index < QUAD_POINT_COUNT);
		return Alphas[index];
	}

	public void GetNormal(out Vector3 normal) {
		Vector3[] tmp = new Vector3[2];
		MathLib.VectorSubtract(Points[1], Points[0], out tmp[0]);
		MathLib.VectorSubtract(Points[3], Points[0], out tmp[1]);
		MathLib.CrossProduct(tmp[1], tmp[0], out normal);
		MathLib.VectorNormalize(ref normal);
	}
	public void SetFlags(int flag) => Flags = flag;
	public int GetFlags() => Flags;
	public void SetContents(int contents) => Contents = contents;
	public int GetContents() => Contents;

	public void SetSAxis(in Vector3 axis) => MathLib.VectorCopy(axis, out SAxis);
	public void GetSAxis(out Vector3 axis) => MathLib.VectorCopy(SAxis, out axis);
	public void SetTAxis(in Vector3 axis) => MathLib.VectorCopy(axis, out TAxis);
	public void GetTAxis(out Vector3 axis) => MathLib.VectorCopy(TAxis, out axis);

	public void SetPointStartIndex(int index) {
		Assert(index >= 0);
		Assert(index < QUAD_POINT_COUNT);
		PointStartIndex = index;
	}
	public int GetPointStartIndex() => PointStartIndex;
	public void SetPointStart(in Vector3 pt) => MathLib.VectorCopy(pt, out PointStart);
	public void GetPointStart(out Vector3 pt) => MathLib.VectorCopy(PointStart, out pt);

	public void SetNeighborData(in InlineArray4<DispNeighbor> edgeNeighbors, in InlineArray4<DispCornerNeighbors> cornerNeighbors) {
		for (int i = 0; i < 4; i++) {
			EdgeNeighbors[i] = edgeNeighbors[i];
			CornerNeighbors[i] = cornerNeighbors[i];
		}
	}

	public void GeneratePointStartIndexFromMappingAxes(in Vector3 sAxis, in Vector3 tAxis) {
		if (PointStartIndex != -1) return;

		int numIndices = 0;
		int[] indices = new int[4];
		int offsetIndex;

		float minValue = MathLib.DotProduct(tAxis, Points[0]);
		indices[numIndices] = 0;
		numIndices++;

		int i;
		for (i = 1; i < PointCount; i++) {
			float value = MathLib.DotProduct(tAxis, Points[i]);
			float delta = value - minValue;
			delta = Math.Abs(delta);
			if (delta < 0.1) {
				indices[numIndices] = i;
				numIndices++;
			}
			else if (value < minValue) {
				minValue = value;
				indices[0] = i;
				numIndices = 1;
			}
		}

		minValue = MathLib.DotProduct(sAxis, Points[indices[0]]);
		offsetIndex = indices[0];

		for (i = 1; i < numIndices; i++) {
			float value = MathLib.DotProduct(sAxis, Points[indices[i]]);
			if (value < minValue) {
				minValue = value;
				offsetIndex = indices[i];
			}
		}

		PointStartIndex = offsetIndex;
	}
	public int GenerateSurfPointStartIndex() => throw new NotImplementedException();
	public int FindSurfPointStartIndex() {
		if (PointStartIndex != -1) return PointStartIndex;

		int minIndex = -1;
		float minDistance = float.MaxValue;

		for (int i = 0; i < QUAD_POINT_COUNT; i++) {
			MathLib.VectorSubtract(PointStart, Points[i], out Vector3 segment);
			float distanceSq = segment.LengthSqr();
			if (distanceSq < minDistance) {
				minDistance = distanceSq;
				minIndex = i;
			}
		}

		PointStartIndex = minIndex;

		return minIndex;
	}
	public void AdjustSurfPointData() {
		Vector3[] tmpPoints = new Vector3[4];
		Vector3[] tmpNormals = new Vector3[4];
		Vector2[] tmpTexCoords = new Vector2[4];
		float[] tmpAlphas = new float[4];

		int i;
		for (i = 0; i < QUAD_POINT_COUNT; i++) {
			MathLib.VectorCopy(Points[i], out tmpPoints[i]);
			MathLib.VectorCopy(Normals[i], out tmpNormals[i]);
			MathLib.Vector2DCopy(TexCoords[i], out tmpTexCoords[i]);

			tmpAlphas[i] = Alphas[i];
		}

		for (i = 0; i < QUAD_POINT_COUNT; i++) {
			MathLib.VectorCopy(tmpPoints[(i + PointStartIndex) % 4], out Points[i]);
			MathLib.VectorCopy(tmpNormals[(i + PointStartIndex) % 4], out Normals[i]);
			MathLib.Vector2DCopy(tmpTexCoords[(i + PointStartIndex) % 4], out TexCoords[i]);

			Alphas[i] = tmpAlphas[i];
		}
	}

	public ref DispCornerNeighbors GetCornerNeighbors(int corner) {
		Assert(corner >= 0 && corner < CornerNeighbors.Length);
		return ref CornerNeighbors[corner];
	}

	public int GetCornerNeighborCount(int corner) => GetCornerNeighbors(corner).NumNeighbors;
	public int GetCornerNeighbor(int corner, int neighbor) {
		Assert(neighbor >= 0 && neighbor < GetCornerNeighbors(corner).NumNeighbors);
		return GetCornerNeighbors(corner).Neighbors[neighbor];
	}

	public ref DispNeighbor GetEdgeNeighbor(int edge) {
		Assert(edge >= 0 && edge < EdgeNeighbors.Length);
		return ref EdgeNeighbors[edge];
	}

	bool LongestInU(in Vector3 u, in Vector3 v) {
		Vector3 normU = u;
		Vector3 normV = v;
		MathLib.VectorNormalize(ref normU);
		MathLib.VectorNormalize(ref normV);

		float[] distU = new float[4];
		float[] distV = new float[4];
		for (int point = 0; point < 4; ++point) {
			distU[point] = normU.Dot(Points[point]);
			distV[point] = normV.Dot(Points[point]);
		}

		float uLength = 0.0f;
		float vLength = 0.0f;
		for (int point = 0; point < 4; ++point) {
			float testDist = Math.Abs(distU[(point + 1) % 4] - distU[point]);
			if (testDist > uLength)
				uLength = testDist;

			testDist = Math.Abs(distV[(point + 1) % 4] - distV[point]);
			if (testDist > vLength)
				vLength = testDist;
		}

		if (uLength < vLength)
			return false;

		return true;
	}
}

public class CoreDispNode
{
	Vector3[] BBox = new Vector3[2];
	float ErrorTerm;
	int VertIndex;
	int[] NeighborVertIndices = new int[MAX_NEIGHBOR_VERT_COUNT];
	Vector3[,] SurfBBoxes = new Vector3[MAX_SURF_AT_NODE_COUNT, 2];
	CollisionPlane[] SurfPlanes = new CollisionPlane[MAX_SURF_AT_NODE_COUNT];
	Vector3[,] RayBBoxes = new Vector3[4, 2];

	public const int MAX_NEIGHBOR_NODE_COUNT = 4;
	public const int MAX_NEIGHBOR_VERT_COUNT = 8;
	public const int MAX_SURF_AT_NODE_COUNT = 8;

	public void Init() {
		MathLib.VectorClear(out BBox[0]);
		MathLib.VectorClear(out BBox[1]);

		ErrorTerm = 0;
		VertIndex = -1;

		int i;
		for (i = 0; i < MAX_NEIGHBOR_NODE_COUNT; i++)
			NeighborVertIndices[i] = -1;

		for (i = 0; i < MAX_SURF_AT_NODE_COUNT; i++) {
			MathLib.VectorClear(out SurfBBoxes[i, 0]);
			MathLib.VectorClear(out SurfBBoxes[i, 1]);
			MathLib.VectorClear(out SurfPlanes[i].Normal);
			SurfPlanes[i].Dist = 0;
		}
	}

	public void SetBoundingBox(in Vector3 min, in Vector3 max) {
		MathLib.VectorCopy(min, out BBox[0]);
		MathLib.VectorCopy(max, out BBox[1]);
	}
	public void GetBoundingBox(out Vector3 min, out Vector3 max) {
		MathLib.VectorCopy(BBox[0], out min);
		MathLib.VectorCopy(BBox[1], out max);
	}

	public void SetErrorTerm(float errorTerm) => ErrorTerm = errorTerm;
	public float GetErrorTerm() => ErrorTerm;

	public void SetNeighborNodeIndex(int dir, int index) => throw new NotImplementedException();
	public int GetNeighborNodeIndex(int dir) => throw new NotImplementedException();

	public void SetCenterVertIndex(int index) => VertIndex = index;
	public int GetCenterVertIndex() => VertIndex;
	public void SetNeighborVertIndex(int dir, int index) {
		Assert(dir >= 0);
		Assert(dir < MAX_NEIGHBOR_VERT_COUNT);
		NeighborVertIndices[dir] = index;
	}
	public int GetNeighborVertIndex(int dir) {
		Assert(dir >= 0);
		Assert(dir < MAX_NEIGHBOR_VERT_COUNT);
		return NeighborVertIndices[dir];
	}

	public void SetTriBoundingBox(int index, in Vector3 min, in Vector3 max) {
		Assert(index >= 0);
		Assert(index < MAX_SURF_AT_NODE_COUNT);
		MathLib.VectorCopy(min, out SurfBBoxes[index, 0]);
		MathLib.VectorCopy(max, out SurfBBoxes[index, 1]);
	}
	public void GetTriBoundingBox(int index, out Vector3 min, out Vector3 max) {
		Assert(index >= 0);
		Assert(index < MAX_SURF_AT_NODE_COUNT);
		MathLib.VectorCopy(SurfBBoxes[index, 0], out min);
		MathLib.VectorCopy(SurfBBoxes[index, 1], out max);
	}
	public void SetTriPlane(int index, in Vector3 normal, float dist) {
		Assert(index >= 0);
		Assert(index < MAX_SURF_AT_NODE_COUNT);
		MathLib.VectorCopy(normal, out SurfPlanes[index].Normal);
		SurfPlanes[index].Dist = dist;
	}
	public void GetTriPlane(int index, ref CollisionPlane plane) {
		Assert(index >= 0);
		Assert(index < MAX_SURF_AT_NODE_COUNT);
		MathLib.VectorCopy(SurfPlanes[index].Normal, out plane.Normal);
		plane.Dist = SurfPlanes[index].Dist;
	}

	public void SetRayBoundingBox(int index, in Vector3 min, in Vector3 max) {
		Assert(index >= 0);
		Assert(index < 4);
		MathLib.VectorCopy(min, out RayBBoxes[index, 0]);
		MathLib.VectorCopy(max, out RayBBoxes[index, 1]);
	}
	public void GetRayBoundingBox(int index, out Vector3 min, out Vector3 max) {
		Assert(index >= 0);
		Assert(index < 4);
		MathLib.VectorCopy(RayBBoxes[index, 0], out min);
		MathLib.VectorCopy(RayBBoxes[index, 1], out max);
	}
}

public struct CoreDispVert
{
	public Vector3 FieldVector;
	public float FieldDistance;

	public Vector3 SubdivNormal;
	public Vector3 SubdivPos;

	public Vector3 Vert;
	public Vector3 FlatVert;
	public Vector3 Normal;
	public Vector3 TangentS;
	public Vector3 TangentT;
	public Vector2 TexCoord;
	public InlineArray4<Vector2> LuxelCoords;

	public float Alpha;
}

struct CoreDispTri
{
	public InlineArray3<ushort> Index;
	public DispTriTags Tags;
}

public class CoreDispInfo : DispUtilsHelper
{
	public const int WEST = 0;
	public const int NORTH = 1;
	public const int EAST = 2;
	public const int SOUTH = 3;
	public const int SOUTHWEST = 4;
	public const int SOUTHEAST = 5;
	public const int NORTHWEST = 6;
	public const int NORTHEAST = 7;

	public const int SURF_BUMPED = 0x1;
	public const int SURF_NOPHYSICS_COLL = 0x2;
	public const int SURF_NOHULL_COLL = 0x4;
	public const int SURF_NORAY_COLL = 0x8;

	public const int MAX_DISP_POWER = BSPFileCommon.MAX_MAP_DISP_POWER;
	public const int MAX_VERT_COUNT = BSPFileCommon.MAX_DISPVERTS;
	public const int MAX_NODE_COUNT = 85;

	CoreDispNode[]? Nodes;

	float Elevation;

	int Power;

	CoreDispSurface Surf;

	CoreDispVert[]? Verts;

	CoreDispTri[]? Tris;

	int RenderIndexCount;
	ushort[]? RenderIndices;
	int RenderCounter;

	bool Touched;
	CoreDispInfo? Next;

	CoreDispInfo[]? ListBase;
	nint ListSize;

	MaxDispVertsBitVec AllowedVerts;

	nint ListIndex;

	public CoreDispInfo() {
		Verts = null;
		RenderIndices = null;
		Nodes = null;
		Tris = null;

		Surf = new();
		Surf.Init();

		Power = 0;
		Elevation = 0;
		RenderIndexCount = 0;
		RenderCounter = 0;
		Touched = false;

		Next = null;

		ListBase = null;
		ListSize = 0;
		ListIndex = -1;
	}

	public static CoreDispInfo FromDispUtils(DispUtilsHelper p) => throw new NotImplementedException();

	public override DispNeighbor GetEdgeNeighbor(int index) => GetSurface().GetEdgeNeighbor(index);
	public override DispCornerNeighbors GetCornerNeighbors(int index) => GetSurface().GetCornerNeighbors(index);
	public override PowerInfo GetPowerInfo() => PowerInfo.GetPowerInfo(Power);
	public override DispUtilsHelper GetDispUtilsByIndex(int index) {
		Assert(ListBase != null);
		return index == 0xFFFF ? null! : ListBase![index];
	}

	public void InitSurf(int parentIndex, Vector3[] points, Vector3[] normals, Vector2[] texCoords, Vector2[,] lightCoords, int contents, int flags, bool generateSurfPointStart, ref Vector3 startPoint, bool hasMappingAxes, ref Vector3 uAxis, ref Vector3 vAxis) => throw new NotImplementedException();

	public void InitDispInfo(int power, int minTess, float smoothingAngle, float[]? alphas, Vector3[]? dispVectorField, float[]? dispDistances) {
		Assert(power >= BSPFileCommon.MIN_MAP_DISP_POWER && power <= BSPFileCommon.MAX_MAP_DISP_POWER);

		Power = power;

		if ((minTess & 0x80000000) != 0) {
			int nFlags = minTess;
			nFlags &= ~unchecked((int)0x80000000);
			GetSurface().SetFlags(nFlags);
		}

		int size = GetSize();
		Verts = new CoreDispVert[size];

		int indexCount = size * 2 * 3;
		RenderIndices = new ushort[indexCount];

		int nodeCount = GetNodeCount(power);
		Nodes = new CoreDispNode[nodeCount];

		int i;
		for (i = 0; i < size; i++) {
			Verts[i].FieldVector.Init();
			Verts[i].SubdivPos.Init();
			Verts[i].SubdivNormal.Init();
			Verts[i].FieldDistance = 0.0f;
			Verts[i].Vert.Init();
			Verts[i].FlatVert.Init();
			Verts[i].Normal.Init();
			Verts[i].TangentS.Init();
			Verts[i].TangentT.Init();
			Verts[i].TexCoord.Init();

			for (int j = 0; j < Constants.NUM_BUMP_VECTS + 1; j++)
				Verts[i].LuxelCoords[j].Init();

			Verts[i].Alpha = 0.0f;
		}

		for (i = 0; i < indexCount; i++)
			RenderIndices[i] = 0;

		for (i = 0; i < nodeCount; i++) {
			Nodes[i] = new CoreDispNode();
			Nodes[i].Init();
		}

		if (alphas != null && dispVectorField != null && dispDistances != null) {
			for (i = 0; i < size; i++) {
				Verts[i].FieldVector = dispVectorField[i];
				Verts[i].FieldDistance = dispDistances[i];
				Verts[i].Alpha = alphas[i];
			}
		}

		int triCount = GetTriCount();
		if (triCount != 0) {
			Tris = new CoreDispTri[triCount];
			InitTris();
		}
	}

	public void InitDispInfo(int power, int minTess, float smoothingAngle, Span<DispVert> verts, Span<DispTri> tris) {
		Vector3[] vectors = new Vector3[BSPFileCommon.MAX_DISPVERTS];
		float[] dists = new float[BSPFileCommon.MAX_DISPVERTS];
		float[] alphas = new float[BSPFileCommon.MAX_DISPVERTS];

		int vertCount = BSPFileCommon.NUM_DISP_POWER_VERTS(power);
		for (int i = 0; i < vertCount; i++) {
			vectors[i] = verts[i].Vector;
			dists[i] = verts[i].Dist;
			alphas[i] = verts[i].Alpha;
		}

		InitDispInfo(power, minTess, smoothingAngle, alphas, vectors, dists);

		int triCount = BSPFileCommon.NUM_DISP_POWER_TRIS(power);
		for (int tri = 0; tri < triCount; ++tri)
			Tris![tri].Tags = tris[tri].Tags;
	}

	public bool Create() {
		CoreDispSurface surf = GetSurface();
		if (surf.GetPointCount() != 4) return false;

		GenerateDispSurf();

		GenerateDispSurfNormals();

		GenerateDispSurfTangentSpaces();

		CalcDispSurfCoords(false, 0);

		for (int bumpID = 0; bumpID < (Constants.NUM_BUMP_VECTS + 1); bumpID++)
			CalcDispSurfCoords(true, bumpID);

		GenerateLODTree();

		GenerateCollisionData();

		CreateTris();

		return true;
	}
	public bool CreateWithoutLOD() {
		CoreDispSurface surf = GetSurface();
		if (surf.GetPointCount() != 4) return false;

		GenerateDispSurf();

		GenerateDispSurfNormals();

		GenerateDispSurfTangentSpaces();

		CalcDispSurfCoords(false, 0);

		for (int bumpID = 0; bumpID < (Constants.NUM_BUMP_VECTS + 1); bumpID++)
			CalcDispSurfCoords(true, bumpID);

		GenerateCollisionData();

		CreateTris();

		return true;
	}

	public CoreDispSurface GetSurface() => Surf;
	public CoreDispNode GetNode(int index) {
		Assert(index >= 0);
		Assert(index < MAX_NODE_COUNT);
		return Nodes![index];
	}

	public void SetPower(int power) => Power = power;
	public int GetPostSpacing() => (1 << Power) + 1;
	public int GetWidth() => (1 << Power) + 1;
	public int GetHeight() => (1 << Power) + 1;
	public int GetSize() => ((1 << Power) + 1) * ((1 << Power) + 1);

	public void SetDispUtilsHelperInfo(CoreDispInfo[] listBase, nint listSize) {
		ListBase = listBase;
		ListSize = listSize;
	}

	public void SetNeighborData(in InlineArray4<DispNeighbor> edgeNeighbors, in InlineArray4<DispCornerNeighbors> cornerNeighbors) => GetSurface().SetNeighborData(edgeNeighbors, cornerNeighbors);

	public Vector3 GetCornerPoint(int index) => GetVert(VertIndexToInt(GetCornerPointIndex(index)));

	public void SetVert(int index, in Vector3 vert) {
		Assert(index >= 0);
		Assert(index < MAX_VERT_COUNT);
		MathLib.VectorCopy(vert, out Verts![index].Vert);
	}
	public void GetVert(int index, out Vector3 vert) {
		Assert(index >= 0);
		Assert(index < MAX_VERT_COUNT);
		MathLib.VectorCopy(Verts![index].Vert, out vert);
	}
	public Vector3 GetVert(int index) {
		Assert(index >= 0);
		Assert(index < MAX_VERT_COUNT);
		return Verts![index].Vert;
	}
	public Vector3 GetVert(in VertIndex index) => throw new NotImplementedException();

	public void GetFlatVert(int index, out Vector3 vert) => throw new NotImplementedException();
	public void SetFlatVert(int index, in Vector3 vert) => throw new NotImplementedException();

	public void GetNormal(int index, out Vector3 normal) {
		Assert(index >= 0);
		Assert(index < MAX_VERT_COUNT);
		MathLib.VectorCopy(Verts![index].Normal, out normal);
	}
	public Vector3 GetNormal(int index) {
		Assert(index >= 0);
		Assert(index < MAX_VERT_COUNT);
		return Verts![index].Normal;
	}
	public Vector3 GetNormal(in VertIndex index) => GetNormal(VertIndexToInt(index));
	public void SetNormal(int index, in Vector3 normal) {
		Assert(index >= 0);
		Assert(index < MAX_VERT_COUNT);
		MathLib.VectorCopy(normal, out Verts![index].Normal);
	}
	public void SetNormal(in VertIndex index, in Vector3 normal) => throw new NotImplementedException();

	public void GetTangentS(int index, out Vector3 tangentS) {
		Assert(index >= 0);
		Assert(index < GetSize());
		MathLib.VectorCopy(Verts![index].TangentS, out tangentS);
	}
	public Vector3 GetTangentS(int index) {
		Assert(index >= 0);
		Assert(index < GetSize());
		return Verts![index].TangentS;
	}
	public Vector3 GetTangentS(in VertIndex index) => GetTangentS(VertIndexToInt(index));
	public void GetTangentT(int index, out Vector3 tangentT) {
		Assert(index >= 0);
		Assert(index < GetSize());
		MathLib.VectorCopy(Verts![index].TangentT, out tangentT);
	}
	public void SetTangentS(int index, in Vector3 tangentS) => Verts![index].TangentS = tangentS;
	public void SetTangentT(int index, in Vector3 tangentT) => Verts![index].TangentT = tangentT;

	public void SetTexCoord(int index, in Vector2 texCoord) => throw new NotImplementedException();
	public void GetTexCoord(int index, out Vector2 texCoord) {
		Assert(index >= 0);
		Assert(index < GetSize());
		MathLib.Vector2DCopy(Verts![index].TexCoord, out texCoord);
	}

	public void SetLuxelCoord(int bumpIndex, int index, in Vector2 luxelCoord) => throw new NotImplementedException();
	public void GetLuxelCoord(int bumpIndex, int index, out Vector2 luxelCoord) {
		Assert(index < MAX_VERT_COUNT);
		Assert(bumpIndex >= 0);
		Assert(bumpIndex < Constants.NUM_BUMP_VECTS + 1);
		MathLib.Vector2DCopy(Verts![index].LuxelCoords[bumpIndex], out luxelCoord);
	}

	public void SetAlpha(int index, float alpha) => throw new NotImplementedException();
	public float GetAlpha(int index) {
		Assert(index >= 0);
		Assert(index < MAX_VERT_COUNT);
		return Verts![index].Alpha;
	}

	public int GetTriCount() => (GetHeight() - 1) * (GetWidth() - 1) * 2;
	public void GetTriIndices(int tri, out ushort v1, out ushort v2, out ushort v3) {
		if (Tris == null || (tri < 0) || (tri >= GetTriCount())) {
			Assert(tri >= 0);
			Assert(tri < GetTriCount());
			Assert(Tris == null);
			v1 = v2 = v3 = 0;
			return;
		}

		CoreDispTri pTri = Tris[tri];
		v1 = pTri.Index[0];
		v2 = pTri.Index[1];
		v3 = pTri.Index[2];
	}
	public void SetTriIndices(int tri, ushort v1, ushort v2, ushort v3) => throw new NotImplementedException();
	public void GetTriPos(int tri, out Vector3 v1, out Vector3 v2, out Vector3 v3) => throw new NotImplementedException();
	public void SetTriTag(int tri, DispTriTags tag) => throw new NotImplementedException();
	public void ResetTriTag(int tri, DispTriTags tag) => throw new NotImplementedException();
	public void ToggleTriTag(int tri, DispTriTags tag) => throw new NotImplementedException();
	public bool IsTriTag(int tri, DispTriTags tag) => (Tris![tri].Tags & tag) != 0;
	public ushort GetTriTagValue(int tri) => throw new NotImplementedException();
	public void SetTriTagValue(int tri, ushort val) => throw new NotImplementedException();

	public bool IsTriWalkable(int tri) => throw new NotImplementedException();
	public bool IsTriBuildable(int tri) => throw new NotImplementedException();
	public bool IsTriRemove(int tri) => throw new NotImplementedException();

	public void SetElevation(float elevation) => throw new NotImplementedException();
	public float GetElevation() => throw new NotImplementedException();

	public void ResetFieldVectors() => throw new NotImplementedException();
	public void SetFieldVector(int index, in Vector3 v) => throw new NotImplementedException();
	public void GetFieldVector(int index, out Vector3 v) => throw new NotImplementedException();
	public void ResetFieldDistances() => throw new NotImplementedException();
	public void SetFieldDistance(int index, float dist) => throw new NotImplementedException();
	public float GetFieldDistance(int index) => throw new NotImplementedException();

	public void ResetSubdivPositions() => throw new NotImplementedException();
	public void SetSubdivPosition(int ndx, in Vector3 v) => throw new NotImplementedException();
	public void GetSubdivPosition(int ndx, out Vector3 v) => throw new NotImplementedException();

	public void ResetSubdivNormals() => throw new NotImplementedException();
	public void SetSubdivNormal(int ndx, in Vector3 v) => throw new NotImplementedException();
	public void GetSubdivNormal(int ndx, out Vector3 v) => throw new NotImplementedException();

	public void SetRenderIndexCount(int count) => throw new NotImplementedException();
	public int GetRenderIndexCount() => throw new NotImplementedException();
	public void SetRenderIndex(int index, ushort triIndex) => throw new NotImplementedException();
	public ushort GetRenderIndex(int index) => throw new NotImplementedException();

	public CoreDispVert GetDispVert(int vert) => throw new NotImplementedException();
	public CoreDispVert[] GetDispVertList() => throw new NotImplementedException();
	public ushort[] GetRenderIndexList() => throw new NotImplementedException();

	public void SetTouched(bool touched) => throw new NotImplementedException();
	public bool IsTouched() => throw new NotImplementedException();

	public void CalcDispSurfCoords(bool lightmap, int lightmapID) {
		Vector2[] texCoords = new Vector2[4];
		Vector2[] luxelCoords = new Vector2[4];
		CoreDispSurface surf = GetSurface();

		int i;
		for (i = 0; i < 4; i++) {
			surf.GetTexCoord(i, out texCoords[i]);
			surf.GetLuxelCoord(lightmapID, i, out luxelCoords[i]);
		}

		int postSpacing = GetPostSpacing();
		float ooInt = 1.0f / (postSpacing - 1);

		Vector2[] edgeInt = new Vector2[2];
		if (!lightmap) {
			MathLib.Vector2DSubtract(texCoords[1], texCoords[0], out edgeInt[0]);
			MathLib.Vector2DSubtract(texCoords[2], texCoords[3], out edgeInt[1]);
		}
		else {
			MathLib.Vector2DSubtract(luxelCoords[1], luxelCoords[0], out edgeInt[0]);
			MathLib.Vector2DSubtract(luxelCoords[2], luxelCoords[3], out edgeInt[1]);
		}
		MathLib.Vector2DMultiply(edgeInt[0], ooInt, out edgeInt[0]);
		MathLib.Vector2DMultiply(edgeInt[1], ooInt, out edgeInt[1]);

		for (i = 0; i < postSpacing; i++) {
			Vector2[] endPts = new Vector2[2];
			MathLib.Vector2DMultiply(edgeInt[0], i, out endPts[0]);
			MathLib.Vector2DMultiply(edgeInt[1], i, out endPts[1]);
			if (!lightmap) {
				MathLib.Vector2DAdd(endPts[0], texCoords[0], out endPts[0]);
				MathLib.Vector2DAdd(endPts[1], texCoords[3], out endPts[1]);
			}
			else {
				MathLib.Vector2DAdd(endPts[0], luxelCoords[0], out endPts[0]);
				MathLib.Vector2DAdd(endPts[1], luxelCoords[3], out endPts[1]);
			}

			MathLib.Vector2DSubtract(endPts[1], endPts[0], out Vector2 seg);
			MathLib.Vector2DMultiply(seg, ooInt, out Vector2 segInt);

			for (int j = 0; j < postSpacing; j++) {
				MathLib.Vector2DMultiply(segInt, j, out seg);

				if (!lightmap)
					MathLib.Vector2DAdd(endPts[0], seg, out Verts![i * postSpacing + j].TexCoord);
				else
					MathLib.Vector2DAdd(endPts[0], seg, out Verts![i * postSpacing + j].LuxelCoords[lightmapID]);
			}
		}
	}
	public void GetPositionOnSurface(float u, float v, out Vector3 vPos, ref Vector3 normal, ref float alpha) => throw new NotImplementedException();

	public void DispUVToSurf(in Vector2 dispUV, out Vector3 point, ref Vector3 normal, ref float alpha) => throw new NotImplementedException();
	public void BaseFacePlaneToDispUV(in Vector3 planePt, out Vector2 dispUV) => throw new NotImplementedException();
	public bool SurfToBaseFacePlane(in Vector3 surfPt, out Vector3 planePt) => throw new NotImplementedException();

	public void SetListIndex(nint index) => throw new NotImplementedException();
	public nint GetListIndex() => throw new NotImplementedException();

	public ref MaxDispVertsBitVec GetAllowedVerts() => ref AllowedVerts;
	public void AllowedVerts_Clear() => throw new NotImplementedException();
	public int AllowedVerts_GetNumDWords() => throw new NotImplementedException();
	public uint AllowedVerts_GetDWord(int i) => throw new NotImplementedException();
	public void AllowedVerts_SetDWord(int i, uint val) => throw new NotImplementedException();

	public void Position_Update(int vert, Vector3 pos) => throw new NotImplementedException();

	void GenerateDispSurf() {
		int i;
		CoreDispSurface surf = GetSurface();
		Vector3[] points = new Vector3[4];
		for (i = 0; i < 4; i++)
			surf.GetPoint(i, out points[i]);

		int postSpacing = GetPostSpacing();
		float ooInt = 1.0f / (postSpacing - 1);

		Vector3[] edgeInt = new Vector3[2];
		MathLib.VectorSubtract(points[1], points[0], out edgeInt[0]);
		MathLib.VectorScale(edgeInt[0], ooInt, out edgeInt[0]);
		MathLib.VectorSubtract(points[2], points[3], out edgeInt[1]);
		MathLib.VectorScale(edgeInt[1], ooInt, out edgeInt[1]);

		Vector3 elevNormal = default;
		if (Elevation != 0.0f) {
			surf.GetNormal(out elevNormal);
			MathLib.VectorScale(elevNormal, Elevation, out elevNormal);
		}

		for (i = 0; i < postSpacing; i++) {
			Vector3[] endPts = new Vector3[2];
			MathLib.VectorScale(edgeInt[0], i, out endPts[0]);
			MathLib.VectorAdd(endPts[0], points[0], out endPts[0]);
			MathLib.VectorScale(edgeInt[1], i, out endPts[1]);
			MathLib.VectorAdd(endPts[1], points[3], out endPts[1]);

			Vector3 seg, segInt;
			MathLib.VectorSubtract(endPts[1], endPts[0], out seg);
			MathLib.VectorScale(seg, ooInt, out segInt);

			for (int j = 0; j < postSpacing; j++) {
				int ndx = i * postSpacing + j;

				ref CoreDispVert vert = ref Verts![ndx];

				vert.FlatVert = endPts[0] + (segInt * j);
				vert.Vert = vert.FlatVert;

				if (Elevation != 0.0f)
					vert.Vert += elevNormal;

				vert.Vert += vert.SubdivPos;

				vert.Vert += vert.FieldVector * vert.FieldDistance;
			}
		}
	}
	void GenerateDispSurfNormals() {
		int postSpacing = GetPostSpacing();

		for (int i = 0; i < postSpacing; i++) {
			for (int j = 0; j < postSpacing; j++) {
				bool[] isEdge = new bool[4];

				for (int k = 0; k < 4; k++)
					isEdge[k] = DoesEdgeExist(j, i, k, postSpacing);

				CalcNormalFromEdges(j, i, isEdge, out Vector3 normal);

				MathLib.VectorCopy(normal, out Verts![i * postSpacing + j].Normal);
			}
		}
	}
	void GenerateDispSurfTangentSpaces() {
		CoreDispSurface surf = GetSurface();
		surf.GetSAxis(out Vector3 sAxis);
		surf.GetTAxis(out Vector3 tAxis);

		int size = GetSize();
		for (int i = 0; i < size; i++) {
			MathLib.VectorCopy(tAxis, out Verts![i].TangentT);
			MathLib.VectorNormalize(ref Verts[i].TangentT);
			MathLib.CrossProduct(Verts[i].Normal, Verts[i].TangentT, out Verts[i].TangentS);
			MathLib.VectorNormalize(ref Verts[i].TangentS);
			MathLib.CrossProduct(Verts[i].TangentS, Verts[i].Normal, out Verts[i].TangentT);
			MathLib.VectorNormalize(ref Verts[i].TangentT);

			surf.GetNormal(out Vector3 planeNormal);
			MathLib.CrossProduct(sAxis, tAxis, out Vector3 tmpVect);
			if (MathLib.DotProduct(planeNormal, tmpVect) > 0.0f)
				MathLib.VectorScale(Verts[i].TangentS, -1.0f, out Verts[i].TangentS);
		}
	}
	static bool DoesEdgeExist(int indexRow, int indexCol, int direction, int postSpacing) {
		switch (direction) {
			case 0:
				if ((indexRow - 1) < 0) return false;
				return true;
			case 1:
				if ((indexCol + 1) > (postSpacing - 1)) return false;
				return true;
			case 2:
				if ((indexRow + 1) > (postSpacing - 1)) return false;
				return true;
			case 3:
				if ((indexCol - 1) < 0) return false;
				return true;
			default:
				return false;
		}
	}
	void CalcNormalFromEdges(int indexRow, int indexCol, bool[] isEdge, out Vector3 normal) {
		int postSpacing = (1 << Power) + 1;

		int normalCount = 0;

		MathLib.VectorClear(out Vector3 accumNormal);

		Vector3[] tmpVect = new Vector3[2];
		Vector3 tmpNormal;

		if (isEdge[1] && isEdge[2]) {
			MathLib.VectorSubtract(Verts![(indexCol + 1) * postSpacing + indexRow].Vert, Verts[indexCol * postSpacing + indexRow].Vert, out tmpVect[0]);
			MathLib.VectorSubtract(Verts[indexCol * postSpacing + indexRow + 1].Vert, Verts[indexCol * postSpacing + indexRow].Vert, out tmpVect[1]);
			MathLib.CrossProduct(tmpVect[1], tmpVect[0], out tmpNormal);
			MathLib.VectorNormalize(ref tmpNormal);
			MathLib.VectorAdd(accumNormal, tmpNormal, out accumNormal);
			normalCount++;

			MathLib.VectorSubtract(Verts[(indexCol + 1) * postSpacing + indexRow].Vert, Verts[indexCol * postSpacing + indexRow + 1].Vert, out tmpVect[0]);
			MathLib.VectorSubtract(Verts[(indexCol + 1) * postSpacing + indexRow + 1].Vert, Verts[indexCol * postSpacing + indexRow + 1].Vert, out tmpVect[1]);
			MathLib.CrossProduct(tmpVect[1], tmpVect[0], out tmpNormal);
			MathLib.VectorNormalize(ref tmpNormal);
			MathLib.VectorAdd(accumNormal, tmpNormal, out accumNormal);
			normalCount++;
		}

		if (isEdge[0] && isEdge[1]) {
			MathLib.VectorSubtract(Verts![(indexCol + 1) * postSpacing + (indexRow - 1)].Vert, Verts[indexCol * postSpacing + (indexRow - 1)].Vert, out tmpVect[0]);
			MathLib.VectorSubtract(Verts[indexCol * postSpacing + indexRow].Vert, Verts[indexCol * postSpacing + (indexRow - 1)].Vert, out tmpVect[1]);
			MathLib.CrossProduct(tmpVect[1], tmpVect[0], out tmpNormal);
			MathLib.VectorNormalize(ref tmpNormal);
			MathLib.VectorAdd(accumNormal, tmpNormal, out accumNormal);
			normalCount++;

			MathLib.VectorSubtract(Verts[(indexCol + 1) * postSpacing + (indexRow - 1)].Vert, Verts[indexCol * postSpacing + indexRow].Vert, out tmpVect[0]);
			MathLib.VectorSubtract(Verts[(indexCol + 1) * postSpacing + indexRow].Vert, Verts[indexCol * postSpacing + indexRow].Vert, out tmpVect[1]);
			MathLib.CrossProduct(tmpVect[1], tmpVect[0], out tmpNormal);
			MathLib.VectorNormalize(ref tmpNormal);
			MathLib.VectorAdd(accumNormal, tmpNormal, out accumNormal);
			normalCount++;
		}

		if (isEdge[0] && isEdge[3]) {
			MathLib.VectorSubtract(Verts![indexCol * postSpacing + (indexRow - 1)].Vert, Verts[(indexCol - 1) * postSpacing + (indexRow - 1)].Vert, out tmpVect[0]);
			MathLib.VectorSubtract(Verts[(indexCol - 1) * postSpacing + indexRow].Vert, Verts[(indexCol - 1) * postSpacing + (indexRow - 1)].Vert, out tmpVect[1]);
			MathLib.CrossProduct(tmpVect[1], tmpVect[0], out tmpNormal);
			MathLib.VectorNormalize(ref tmpNormal);
			MathLib.VectorAdd(accumNormal, tmpNormal, out accumNormal);
			normalCount++;

			MathLib.VectorSubtract(Verts[indexCol * postSpacing + (indexRow - 1)].Vert, Verts[(indexCol - 1) * postSpacing + indexRow].Vert, out tmpVect[0]);
			MathLib.VectorSubtract(Verts[indexCol * postSpacing + indexRow].Vert, Verts[(indexCol - 1) * postSpacing + indexRow].Vert, out tmpVect[1]);
			MathLib.CrossProduct(tmpVect[1], tmpVect[0], out tmpNormal);
			MathLib.VectorNormalize(ref tmpNormal);
			MathLib.VectorAdd(accumNormal, tmpNormal, out accumNormal);
			normalCount++;
		}

		if (isEdge[2] && isEdge[3]) {
			MathLib.VectorSubtract(Verts![indexCol * postSpacing + indexRow].Vert, Verts[(indexCol - 1) * postSpacing + indexRow].Vert, out tmpVect[0]);
			MathLib.VectorSubtract(Verts[(indexCol - 1) * postSpacing + indexRow + 1].Vert, Verts[(indexCol - 1) * postSpacing + indexRow].Vert, out tmpVect[1]);
			MathLib.CrossProduct(tmpVect[1], tmpVect[0], out tmpNormal);
			MathLib.VectorNormalize(ref tmpNormal);
			MathLib.VectorAdd(accumNormal, tmpNormal, out accumNormal);
			normalCount++;

			MathLib.VectorSubtract(Verts[indexCol * postSpacing + indexRow].Vert, Verts[(indexCol - 1) * postSpacing + indexRow + 1].Vert, out tmpVect[0]);
			MathLib.VectorSubtract(Verts[indexCol * postSpacing + indexRow + 1].Vert, Verts[(indexCol - 1) * postSpacing + indexRow + 1].Vert, out tmpVect[1]);
			MathLib.CrossProduct(tmpVect[1], tmpVect[0], out tmpNormal);
			MathLib.VectorNormalize(ref tmpNormal);
			MathLib.VectorAdd(accumNormal, tmpNormal, out accumNormal);
			normalCount++;
		}

		MathLib.VectorScale(accumNormal, 1.0f / normalCount, out normal);
	}
	void CalcDispSurfAlphas() => throw new NotImplementedException();
	void GenerateLODTree() {
		int size = GetSize();
		int initialIndex = (size - 1) >> 1;
		Nodes![0].SetCenterVertIndex(initialIndex);
		CalcVertIndicesAtNodes(0);

		for (int i = Power; i > 0; i--)
			CalcNodeInfo(0, i);
	}
	void CalcVertIndicesAtNodes(int nodeIndex) {
		int level = GetNodeLevel(nodeIndex);
		if (level == Power) return;

		int[] childIndices = new int[4];
		int i, j;
		for (i = 0, j = 4; i < 4; i++, j++) {
			childIndices[i] = GetNodeChild(Power, nodeIndex, j);
			int centerIndex = GetNodeVertIndexFromParentIndex(level, Nodes![nodeIndex].GetCenterVertIndex(), j);
			Nodes[childIndices[i]].SetCenterVertIndex(centerIndex);
		}

		for (i = 0; i < 4; i++)
			CalcVertIndicesAtNodes(childIndices[i]);
	}
	int GetNodeVertIndexFromParentIndex(int level, int parentVertIndex, int direction) {
		int shift = 1 << (Power - (level + 1));

		int extent = (1 << Power) + 1;

		int posX = parentVertIndex % extent;
		int posY = parentVertIndex / extent;

		switch (direction) {
			case SOUTHWEST: {
					posX -= shift;
					posY -= shift;
					break;
				}
			case SOUTHEAST: {
					posX += shift;
					posY -= shift;
					break;
				}
			case NORTHWEST: {
					posX -= shift;
					posY += shift;
					break;
				}
			case NORTHEAST: {
					posX += shift;
					posY += shift;
					break;
				}
			default:
				return -99999;
		}

		return (posY * extent) + posX;
	}
	void CalcNodeInfo(int nodeIndex, int terminationLevel) {
		int level = GetNodeLevel(nodeIndex);

		if (level == terminationLevel) {
			CalcNeighborVertIndicesAtNode(nodeIndex, level);
			CalcErrorTermAtNode(nodeIndex, level);
			CalcBoundingBoxAtNode(nodeIndex);
			CalcTriSurfInfoAtNode(nodeIndex);

			return;
		}

		for (int i = 4; i < 8; i++) {
			int childIndex = GetNodeChild(Power, nodeIndex, i);
			CalcNodeInfo(childIndex, terminationLevel);
		}
	}
	void CalcNeighborVertIndicesAtNode(int nodeIndex, int level) {
		int shift = 1 << (Power - level);
		int extent = (1 << Power) + 1;

		for (int direction = 0; direction < 8; direction++) {
			int posX = Nodes![nodeIndex].GetCenterVertIndex() % extent;
			int posY = Nodes[nodeIndex].GetCenterVertIndex() / extent;

			bool error = false;
			switch (direction) {
				case WEST: {
						posX -= shift;
						break;
					}
				case NORTH: {
						posY += shift;
						break;
					}
				case EAST: {
						posX += shift;
						break;
					}
				case SOUTH: {
						posY -= shift;
						break;
					}
				case SOUTHWEST: {
						posX -= shift;
						posY -= shift;
						break;
					}
				case SOUTHEAST: {
						posX += shift;
						posY -= shift;
						break;
					}
				case NORTHWEST: {
						posX -= shift;
						posY += shift;
						break;
					}
				case NORTHEAST: {
						posX += shift;
						posY += shift;
						break;
					}
				default: {
						error = true;
						break;
					}
			}

			if (error)
				Nodes![nodeIndex].SetNeighborVertIndex(direction, -99999);
			else
				Nodes![nodeIndex].SetNeighborVertIndex(direction, (posY * extent) + posX);
		}
	}
	void CalcNeighborNodeIndicesAtNode(int nodeIndex, int level) => throw new NotImplementedException();
	void CalcErrorTermAtNode(int nodeIndex, int level) {
		if (level == Power) return;

		int[] neighborVertIndices = new int[9];
		for (int i = 0; i < 8; i++)
			neighborVertIndices[i] = Nodes![nodeIndex].GetNeighborVertIndex(i);
		neighborVertIndices[8] = Nodes![nodeIndex].GetCenterVertIndex();

		MathLib.VectorAdd(Verts![neighborVertIndices[5]].Vert, Verts[neighborVertIndices[4]].Vert, out Vector3 v);
		MathLib.VectorScale(v, 0.5f, out v);
		MathLib.VectorSubtract(Verts[neighborVertIndices[0]].Vert, v, out Vector3 segment);
		float errorTerm = (float)MathLib.VectorLength(segment);

		MathLib.VectorAdd(Verts[neighborVertIndices[5]].Vert, Verts[neighborVertIndices[6]].Vert, out v);
		MathLib.VectorScale(v, 0.5f, out v);
		MathLib.VectorSubtract(Verts[neighborVertIndices[1]].Vert, v, out segment);
		if (errorTerm < (float)MathLib.VectorLength(segment))
			errorTerm = (float)MathLib.VectorLength(segment);

		MathLib.VectorAdd(Verts[neighborVertIndices[6]].Vert, Verts[neighborVertIndices[7]].Vert, out v);
		MathLib.VectorScale(v, 0.5f, out v);
		MathLib.VectorSubtract(Verts[neighborVertIndices[2]].Vert, v, out segment);
		if (errorTerm < (float)MathLib.VectorLength(segment))
			errorTerm = (float)MathLib.VectorLength(segment);

		MathLib.VectorAdd(Verts[neighborVertIndices[7]].Vert, Verts[neighborVertIndices[4]].Vert, out v);
		MathLib.VectorScale(v, 0.5f, out v);
		MathLib.VectorSubtract(Verts[neighborVertIndices[3]].Vert, v, out segment);
		if (errorTerm < (float)MathLib.VectorLength(segment))
			errorTerm = (float)MathLib.VectorLength(segment);

		MathLib.VectorAdd(Verts[neighborVertIndices[4]].Vert, Verts[neighborVertIndices[6]].Vert, out v);
		MathLib.VectorScale(v, 0.5f, out v);
		MathLib.VectorSubtract(Verts[neighborVertIndices[8]].Vert, v, out segment);
		if (errorTerm < (float)MathLib.VectorLength(segment))
			errorTerm = (float)MathLib.VectorLength(segment);

		MathLib.VectorAdd(Verts[neighborVertIndices[5]].Vert, Verts[neighborVertIndices[7]].Vert, out v);
		MathLib.VectorScale(v, 0.5f, out v);
		MathLib.VectorSubtract(Verts[neighborVertIndices[8]].Vert, v, out segment);
		if (errorTerm < (float)MathLib.VectorLength(segment))
			errorTerm = (float)MathLib.VectorLength(segment);

		errorTerm += GetMaxErrorFromChildren(nodeIndex, level);

		Nodes[nodeIndex].SetErrorTerm(errorTerm);
	}
	float GetMaxErrorFromChildren(int nodeIndex, int level) {
		if (level == Power) return 0.0f;

		float errorTerm = 0.0f;
		for (int i = 4; i < 8; i++) {
			int childIndex = GetNodeChild(Power, nodeIndex, i);

			float nodeErrorTerm = Nodes![childIndex].GetErrorTerm();
			if (errorTerm < nodeErrorTerm)
				errorTerm = nodeErrorTerm;
		}

		return errorTerm;
	}
	void CalcBoundingBoxAtNode(int nodeIndex) {
		Vector3 min, max;

		int level = GetNodeLevel(nodeIndex);

		int vertIndex = Nodes![nodeIndex].GetCenterVertIndex();
		if (level == Power) {
			MathLib.VectorCopy(Verts![vertIndex].Vert, out min);
			MathLib.VectorCopy(Verts[vertIndex].Vert, out max);
		}
		else {
			CalcMinMaxBoundingBoxAtNode(nodeIndex, out min, out max);

			if (min[0] > Verts![vertIndex].Vert[0])
				min[0] = Verts[vertIndex].Vert[0];

			if (min[1] > Verts[vertIndex].Vert[1])
				min[1] = Verts[vertIndex].Vert[1];

			if (min[2] > Verts[vertIndex].Vert[2])
				min[2] = Verts[vertIndex].Vert[2];

			if (max[0] < Verts[vertIndex].Vert[0])
				max[0] = Verts[vertIndex].Vert[0];

			if (max[1] < Verts[vertIndex].Vert[1])
				max[1] = Verts[vertIndex].Vert[1];

			if (max[2] < Verts[vertIndex].Vert[2])
				max[2] = Verts[vertIndex].Vert[2];
		}

		for (int i = 0; i < 8; i++) {
			int neighborVertIndex = Nodes[nodeIndex].GetNeighborVertIndex(i);

			if (min[0] > Verts[neighborVertIndex].Vert[0])
				min[0] = Verts[neighborVertIndex].Vert[0];

			if (min[1] > Verts[neighborVertIndex].Vert[1])
				min[1] = Verts[neighborVertIndex].Vert[1];

			if (min[2] > Verts[neighborVertIndex].Vert[2])
				min[2] = Verts[neighborVertIndex].Vert[2];

			if (max[0] < Verts[neighborVertIndex].Vert[0])
				max[0] = Verts[neighborVertIndex].Vert[0];

			if (max[1] < Verts[neighborVertIndex].Vert[1])
				max[1] = Verts[neighborVertIndex].Vert[1];

			if (max[2] < Verts[neighborVertIndex].Vert[2])
				max[2] = Verts[neighborVertIndex].Vert[2];
		}

		Nodes[nodeIndex].SetBoundingBox(min, max);
	}
	void CalcMinMaxBoundingBoxAtNode(int nodeIndex, out Vector3 min, out Vector3 max) {
		int childNodeIndex = GetNodeChild(Power, nodeIndex, 4);

		Nodes![childNodeIndex].GetBoundingBox(out min, out max);

		for (int i = 1, j = 5; i < 4; i++, j++) {
			childNodeIndex = GetNodeChild(Power, nodeIndex, j);
			Nodes[childNodeIndex].GetBoundingBox(out Vector3 nodeMin, out Vector3 nodeMax);

			if (min[0] > nodeMin[0]) min[0] = nodeMin[0];
			if (min[1] > nodeMin[1]) min[1] = nodeMin[1];
			if (min[2] > nodeMin[2]) min[2] = nodeMin[2];
			if (max[0] < nodeMax[0]) max[0] = nodeMax[0];
			if (max[1] < nodeMax[1]) max[1] = nodeMax[1];
			if (max[2] < nodeMax[2]) max[2] = nodeMax[2];
		}
	}
	void CalcTriSurfInfoAtNode(int nodeIndex) {
		int[,] indices = new int[8, 3];

		CalcTriSurfIndices(nodeIndex, indices);
		CalcTriSurfBoundingBoxes(nodeIndex, indices);
		CalcRayBoundingBoxes(nodeIndex, indices);
		CalcTriSurfPlanes(nodeIndex, indices);
	}
	void CalcTriSurfIndices(int nodeIndex, int[,] indices) {
		indices[0, 0] = Nodes![nodeIndex].GetNeighborVertIndex(4);
		indices[0, 1] = Nodes[nodeIndex].GetNeighborVertIndex(0);
		indices[0, 2] = Nodes[nodeIndex].GetNeighborVertIndex(3);

		indices[1, 0] = Nodes[nodeIndex].GetNeighborVertIndex(3);
		indices[1, 1] = Nodes[nodeIndex].GetNeighborVertIndex(0);
		indices[1, 2] = Nodes[nodeIndex].GetCenterVertIndex();

		indices[2, 0] = Nodes[nodeIndex].GetNeighborVertIndex(3);
		indices[2, 1] = Nodes[nodeIndex].GetCenterVertIndex();
		indices[2, 2] = Nodes[nodeIndex].GetNeighborVertIndex(5);

		indices[3, 0] = Nodes[nodeIndex].GetNeighborVertIndex(5);
		indices[3, 1] = Nodes[nodeIndex].GetCenterVertIndex();
		indices[3, 2] = Nodes[nodeIndex].GetNeighborVertIndex(2);

		indices[4, 0] = Nodes[nodeIndex].GetNeighborVertIndex(0);
		indices[4, 1] = Nodes[nodeIndex].GetNeighborVertIndex(6);
		indices[4, 2] = Nodes[nodeIndex].GetCenterVertIndex();

		indices[5, 0] = Nodes[nodeIndex].GetCenterVertIndex();
		indices[5, 1] = Nodes[nodeIndex].GetNeighborVertIndex(6);
		indices[5, 2] = Nodes[nodeIndex].GetNeighborVertIndex(1);

		indices[6, 0] = Nodes[nodeIndex].GetCenterVertIndex();
		indices[6, 1] = Nodes[nodeIndex].GetNeighborVertIndex(1);
		indices[6, 2] = Nodes[nodeIndex].GetNeighborVertIndex(2);

		indices[7, 0] = Nodes[nodeIndex].GetNeighborVertIndex(2);
		indices[7, 1] = Nodes[nodeIndex].GetNeighborVertIndex(1);
		indices[7, 2] = Nodes[nodeIndex].GetNeighborVertIndex(7);
	}
	void CalcTriSurfBoundingBoxes(int nodeIndex, int[,] indices) {
		Vector3 triMin, triMax;

		for (int i = 0; i < 8; i++) {
			Nodes![nodeIndex].GetTriBoundingBox(i, out triMin, out triMax);

			for (int j = 0; j < 3; j++) {
				if (triMin[0] > Verts![indices[i, j]].Vert[0])
					triMin[0] = Verts[indices[i, j]].Vert[0];

				if (triMin[1] > Verts[indices[i, j]].Vert[1])
					triMin[1] = Verts[indices[i, j]].Vert[1];

				if (triMin[2] > Verts[indices[i, j]].Vert[2])
					triMin[2] = Verts[indices[i, j]].Vert[2];

				if (triMax[0] < Verts[indices[i, j]].Vert[0])
					triMax[0] = Verts[indices[i, j]].Vert[0];

				if (triMax[1] < Verts[indices[i, j]].Vert[1])
					triMax[1] = Verts[indices[i, j]].Vert[1];

				if (triMax[2] < Verts[indices[i, j]].Vert[2])
					triMax[2] = Verts[indices[i, j]].Vert[2];
			}

			Nodes[nodeIndex].SetTriBoundingBox(i, triMin, triMax);
		}
	}
	void CalcRayBoundingBoxes(int nodeIndex, int[,] indices) {
		Vector3 triMin = default, triMax = default;

		for (int i = 0; i < 4; i++) {
			triMin[0] = triMax[0] = Verts![indices[i * 2, 0]].Vert[0];
			triMin[1] = triMax[1] = Verts[indices[i * 2, 0]].Vert[1];
			triMin[2] = triMax[2] = Verts[indices[i * 2, 0]].Vert[2];

			for (int j = 0; j < 3; j++) {
				if (triMin[0] > Verts[indices[i * 2, j]].Vert[0])
					triMin[0] = Verts[indices[i * 2, j]].Vert[0];
				if (triMin[0] > Verts[indices[i * 2 + 1, j]].Vert[0])
					triMin[0] = Verts[indices[i * 2 + 1, j]].Vert[0];

				if (triMin[1] > Verts[indices[i * 2, j]].Vert[1])
					triMin[1] = Verts[indices[i * 2, j]].Vert[1];
				if (triMin[1] > Verts[indices[i * 2 + 1, j]].Vert[1])
					triMin[1] = Verts[indices[i * 2 + 1, j]].Vert[1];

				if (triMin[2] > Verts[indices[i * 2, j]].Vert[2])
					triMin[2] = Verts[indices[i * 2, j]].Vert[2];
				if (triMin[2] > Verts[indices[i * 2 + 1, j]].Vert[2])
					triMin[2] = Verts[indices[i * 2 + 1, j]].Vert[2];

				if (triMax[0] < Verts[indices[i * 2, j]].Vert[0])
					triMax[0] = Verts[indices[i * 2, j]].Vert[0];
				if (triMax[0] < Verts[indices[i * 2 + 1, j]].Vert[0])
					triMax[0] = Verts[indices[i * 2 + 1, j]].Vert[0];

				if (triMax[1] < Verts[indices[i * 2, j]].Vert[1])
					triMax[1] = Verts[indices[i * 2, j]].Vert[1];
				if (triMax[1] < Verts[indices[i * 2 + 1, j]].Vert[1])
					triMax[1] = Verts[indices[i * 2 + 1, j]].Vert[1];

				if (triMax[2] < Verts[indices[i * 2, j]].Vert[2])
					triMax[2] = Verts[indices[i * 2, j]].Vert[2];
				if (triMax[2] < Verts[indices[i * 2 + 1, j]].Vert[2])
					triMax[2] = Verts[indices[i * 2 + 1, j]].Vert[2];
			}

			Nodes![nodeIndex].SetRayBoundingBox(i, triMin, triMax);
		}
	}
	void CalcTriSurfPlanes(int nodeIndex, int[,] indices) {
		for (int i = 0; i < 8; i++) {
			Vector3[] v = new Vector3[3];
			MathLib.VectorCopy(Verts![indices[i, 0]].Vert, out v[0]);
			MathLib.VectorCopy(Verts[indices[i, 1]].Vert, out v[1]);
			MathLib.VectorCopy(Verts[indices[i, 2]].Vert, out v[2]);

			Vector3[] seg = new Vector3[2];
			MathLib.VectorSubtract(v[1], v[0], out seg[0]);
			MathLib.VectorSubtract(v[2], v[0], out seg[1]);

			MathLib.CrossProduct(seg[1], seg[0], out Vector3 normal);
			MathLib.VectorNormalize(ref normal);
			float dist = MathLib.DotProduct(v[0], normal);

			Nodes![nodeIndex].SetTriPlane(i, normal, dist);
		}
	}
	void GenerateCollisionData() => GenerateCollisionSurface();
	void GenerateCollisionSurface() {
		int nWidth = (1 << Power) + 1;
		int nHeight = (1 << Power) + 1;

		RenderIndexCount = 0;
		for (int iV = 0; iV < (nHeight - 1); iV++) {
			for (int iU = 0; iU < (nWidth - 1); iU++) {
				int ndx = (iV * nWidth) + iU;

				bool odd = (ndx % 2) == 1;
				if (odd)
					BuildTriTLtoBR(ndx);
				else
					BuildTriBLtoTR(ndx);
			}
		}
	}

	void CreateBoundingBoxes(CoreDispBBox[] BBox, int count) => throw new NotImplementedException();

	void DispUVToSurf_TriTLToBR(out Vector3 point, ref Vector3 normal, ref float alpha, float u, float v, in Vector3 intersectPoint) => throw new NotImplementedException();
	void DispUVToSurf_TriBLToTR(out Vector3 point, ref Vector3 normal, ref float alpha, float u, float v, in Vector3 intersectPoint) => throw new NotImplementedException();
	void DispUVToSurf_TriTLToBR_1(in Vector3 intersectPoint, int snapU, int nextU, int snapV, int nNextV, out Vector3 point, ref Vector3 normal, ref float alpha, bool backup) => throw new NotImplementedException();
	void DispUVToSurf_TriTLToBR_2(in Vector3 intersectPoint, int snapU, int nextU, int snapV, int nNextV, out Vector3 point, ref Vector3 normal, ref float alpha, bool backup) => throw new NotImplementedException();
	void DispUVToSurf_TriBLToTR_1(in Vector3 intersectPoint, int snapU, int nextU, int snapV, int nNextV, out Vector3 point, ref Vector3 normal, ref float alpha, bool backup) => throw new NotImplementedException();
	void DispUVToSurf_TriBLToTR_2(in Vector3 intersectPoint, int snapU, int nextU, int snapV, int nNextV, out Vector3 point, ref Vector3 normal, ref float alpha, bool backup) => throw new NotImplementedException();

	void GetTriangleIndicesForDispBBox(int index, int[,] nTris) => throw new NotImplementedException();

	void BuildTriTLtoBR(int ndx) {
		int width = (1 << Power) + 1;

		RenderIndices![RenderIndexCount] = (ushort)ndx;
		RenderIndices[RenderIndexCount + 1] = (ushort)(ndx + width);
		RenderIndices[RenderIndexCount + 2] = (ushort)(ndx + 1);
		RenderIndexCount += 3;

		RenderIndices[RenderIndexCount] = (ushort)(ndx + 1);
		RenderIndices[RenderIndexCount + 1] = (ushort)(ndx + width);
		RenderIndices[RenderIndexCount + 2] = (ushort)(ndx + width + 1);
		RenderIndexCount += 3;
	}
	void BuildTriBLtoTR(int ndx) {
		int width = (1 << Power) + 1;

		RenderIndices![RenderIndexCount] = (ushort)ndx;
		RenderIndices[RenderIndexCount + 1] = (ushort)(ndx + width);
		RenderIndices[RenderIndexCount + 2] = (ushort)(ndx + width + 1);
		RenderIndexCount += 3;

		RenderIndices[RenderIndexCount] = (ushort)ndx;
		RenderIndices[RenderIndexCount + 1] = (ushort)(ndx + width + 1);
		RenderIndices[RenderIndexCount + 2] = (ushort)(ndx + 1);
		RenderIndexCount += 3;
	}

	void InitTris() {
		if (Tris == null) {
			Assert(false);
			return;
		}

		int triCount = GetTriCount();

		for (int i = 0; i < triCount; i++)
			Tris[i].Tags = 0;
	}
	void CreateTris() {
		if (Tris == null) {
			Assert(false);
			return;
		}

		Assert(GetTriCount() == (RenderIndexCount / 3));

		int triCount = GetTriCount();
		for (int tri = 0, render = 0; tri < triCount; ++tri, render += 3) {
			Tris[tri].Index[0] = RenderIndices![render];
			Tris[tri].Index[1] = RenderIndices[render + 1];
			Tris[tri].Index[2] = RenderIndices[render + 2];
		}
	}

	public static int GetNodeLevel(int index) {
		if (index == 0) return 1;
		if (index < 5) return 2;
		if (index < 21) return 3;
		if (index < 85) return 4;
		return -1;
	}

	public static int GetNodeCount(int power) => (1 << (power << 1)) / 3;

	public static int GetNodeParent(int index) => (index - 1) >> 2;

	public static int GetNodeChild(int power, int index, int direction) => (index << 2) + (direction - 3);

	public static int GetNodeMinNodeAtLevel(int level) {
		switch (level) {
			case 1: return 0;
			case 2: return 1;
			case 3: return 5;
			case 4: return 21;
			default: return -99999;
		}
	}

	public static void GetComponentsFromNodeIndex(int index, out int x, out int y) {
		x = 0;
		y = 0;

		for (int shift = 0; index != 0; shift++) {
			x |= (index & 1) << shift;
			index >>= 1;

			y |= (index & 1) << shift;
			index >>= 1;
		}
	}

	public static int GetNodeIndexFromComponents(int x, int y) {
		int index = 0;

		int shift;
		for (shift = 0; x != 0; shift += 2, x >>= 1)
			index |= (x & 1) << shift;

		for (shift = 1; y != 0; shift += 2, y >>= 1)
			index |= (y & 1) << shift;

		return index;
	}

	public static int GetNodeNeighborNode(int power, int index, int direction, int level) {
		int minNodeIndex = GetNodeMinNodeAtLevel(level);
		int nodeExtent = 1 << (level - 1);

		GetComponentsFromNodeIndex(index - minNodeIndex, out int posX, out int posY);

		switch (direction) {
			case WEST: {
					if ((posX - 1) < 0)
						return -(WEST + 1);
					else
						return GetNodeIndexFromComponents(posX - 1, posY) + minNodeIndex;
				}
			case NORTH: {
					if ((posY + 1) == nodeExtent)
						return -(NORTH + 1);
					else
						return GetNodeIndexFromComponents(posX, posY + 1) + minNodeIndex;
				}
			case EAST: {
					if ((posX + 1) == nodeExtent)
						return -(EAST + 1);
					else
						return GetNodeIndexFromComponents(posX + 1, posY) + minNodeIndex;
				}
			case SOUTH: {
					if ((posY - 1) < 0)
						return -(SOUTH + 1);
					else
						return GetNodeIndexFromComponents(posX, posY - 1) + minNodeIndex;
				}
			default: {
					return -99999;
				}
		}
	}

	public static int GetNodeNeighborNodeFromNeighborSurf(int power, int index, int direction, int level, int neighborOrient) {
		int minNodeIndex = GetNodeMinNodeAtLevel(level);
		int nodeExtent = 1 << (level - 1);

		GetComponentsFromNodeIndex(index - minNodeIndex, out int posX, out int posY);

		switch (direction) {
			case WEST: {
					return neighborOrient switch {
						WEST => -(GetNodeIndexFromComponents(posX, nodeExtent - 1 - posY) + minNodeIndex),
						NORTH => -(GetNodeIndexFromComponents(nodeExtent - 1 - posY, nodeExtent - 1) + minNodeIndex),
						EAST => -(GetNodeIndexFromComponents(nodeExtent - 1, posY) + minNodeIndex),
						SOUTH => -(GetNodeIndexFromComponents(posY, posX) + minNodeIndex),
						_ => -99999,
					};
				}
			case NORTH: {
					return neighborOrient switch {
						WEST => -(GetNodeIndexFromComponents(nodeExtent - 1 - posY, nodeExtent - 1 - posX) + minNodeIndex),
						NORTH => -(GetNodeIndexFromComponents(nodeExtent - 1 - posX, posY) + minNodeIndex),
						EAST => -(GetNodeIndexFromComponents(posY, posX) + minNodeIndex),
						SOUTH => -(GetNodeIndexFromComponents(posX, nodeExtent - 1 - posY) + minNodeIndex),
						_ => -99999,
					};
				}
			case EAST: {
					return neighborOrient switch {
						WEST => -(GetNodeIndexFromComponents(nodeExtent - 1 - posX, posY) + minNodeIndex),
						NORTH => -(GetNodeIndexFromComponents(posY, posX) + minNodeIndex),
						EAST => -(GetNodeIndexFromComponents(posX, nodeExtent - 1 - posY) + minNodeIndex),
						SOUTH => -(GetNodeIndexFromComponents(nodeExtent - 1 - posY, nodeExtent - 1 - posX) + minNodeIndex),
						_ => -99999,
					};
				}
			case SOUTH: {
					return neighborOrient switch {
						WEST => -(GetNodeIndexFromComponents(posY, posX) + minNodeIndex),
						NORTH => -(GetNodeIndexFromComponents(posX, nodeExtent - 1) + minNodeIndex),
						EAST => -(GetNodeIndexFromComponents(nodeExtent - 1, nodeExtent - 1 - posX) + minNodeIndex),
						SOUTH => -(GetNodeIndexFromComponents(nodeExtent - 1 - posX, posY) + minNodeIndex),
						_ => -99999,
					};
				}
			default: {
					return -99999;
				}
		}
	}
}
