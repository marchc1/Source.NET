using Source.Common.Formats.BSP;
using Source.Common.Mathematics;

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

	public void Init() => throw new NotImplementedException();

	public void SetHandle(int handle) => throw new NotImplementedException();
	public int GetHandle() => throw new NotImplementedException();

	public void SetPointCount(int count) => throw new NotImplementedException();
	public int GetPointCount() => throw new NotImplementedException();

	public void SetPoint(int index, in Vector3 pt) => throw new NotImplementedException();
	public void GetPoint(int index, out Vector3 pt) => throw new NotImplementedException();
	public Vector3 GetPoint(int index) => throw new NotImplementedException();

	public void SetPointNormal(int index, in Vector3 normal) => throw new NotImplementedException();
	public void GetPointNormal(int index, out Vector3 normal) => throw new NotImplementedException();
	public void SetTexCoord(int index, in Vector2 texCoord) => throw new NotImplementedException();
	public void GetTexCoord(int index, out Vector2 texCoord) => throw new NotImplementedException();

	public void SetLuxelCoord(int bumpIndex, int index, in Vector2 luxelCoord) => throw new NotImplementedException();
	public void GetLuxelCoord(int bumpIndex, int index, out Vector2 luxelCoord) => throw new NotImplementedException();
	public void SetLuxelCoords(int bumpIndex, Vector2[] coords) => throw new NotImplementedException();
	public void GetLuxelCoords(int bumpIndex, Vector2[] coords) => throw new NotImplementedException();

	public void SetLuxelU(int u) => LuxelU = u;
	public int GetLuxelU() => LuxelU;
	public void SetLuxelV(int v) => LuxelV = v;
	public int GetLuxelV() => LuxelV;
	public bool CalcLuxelCoords(int luxels, bool adjust, in Vector3 u, in Vector3 v) => throw new NotImplementedException();

	public void SetAlpha(int index, float alpha) => throw new NotImplementedException();
	public float GetAlpha(int index) => throw new NotImplementedException();

	public void GetNormal(out Vector3 normal) => throw new NotImplementedException();
	public void SetFlags(int flag) => throw new NotImplementedException();
	public int GetFlags() => throw new NotImplementedException();
	public void SetContents(int contents) => throw new NotImplementedException();
	public int GetContents() => throw new NotImplementedException();

	public void SetSAxis(in Vector3 axis) => throw new NotImplementedException();
	public void GetSAxis(out Vector3 axis) => throw new NotImplementedException();
	public void SetTAxis(in Vector3 axis) => throw new NotImplementedException();
	public void GetTAxis(out Vector3 axis) => throw new NotImplementedException();

	public void SetPointStartIndex(int index) => throw new NotImplementedException();
	public int GetPointStartIndex() => throw new NotImplementedException();
	public void SetPointStart(in Vector3 pt) => throw new NotImplementedException();
	public void GetPointStart(out Vector3 pt) => throw new NotImplementedException();

	public void SetNeighborData(DispNeighbor[] edgeNeighbors, DispCornerNeighbors[] cornerNeighbors) => throw new NotImplementedException();

	public void GeneratePointStartIndexFromMappingAxes(in Vector3 sAxis, in Vector3 tAxis) => throw new NotImplementedException();
	public int GenerateSurfPointStartIndex() => throw new NotImplementedException();
	public int FindSurfPointStartIndex() => throw new NotImplementedException();
	public void AdjustSurfPointData() => throw new NotImplementedException();

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

	bool LongestInU(in Vector3 u, in Vector3 v) => throw new NotImplementedException();
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

	public void Init() => throw new NotImplementedException();

	public void SetBoundingBox(in Vector3 min, in Vector3 max) => throw new NotImplementedException();
	public void GetBoundingBox(out Vector3 min, out Vector3 max) => throw new NotImplementedException();

	public void SetErrorTerm(float errorTerm) => throw new NotImplementedException();
	public float GetErrorTerm() => throw new NotImplementedException();

	public void SetNeighborNodeIndex(int dir, int index) => throw new NotImplementedException();
	public int GetNeighborNodeIndex(int dir) => throw new NotImplementedException();

	public void SetCenterVertIndex(int index) => throw new NotImplementedException();
	public int GetCenterVertIndex() => throw new NotImplementedException();
	public void SetNeighborVertIndex(int dir, int index) => throw new NotImplementedException();
	public int GetNeighborVertIndex(int dir) => throw new NotImplementedException();

	public void SetTriBoundingBox(int index, in Vector3 min, in Vector3 max) => throw new NotImplementedException();
	public void GetTriBoundingBox(int index, out Vector3 min, out Vector3 max) => throw new NotImplementedException();
	public void SetTriPlane(int index, in Vector3 normal, float dist) => throw new NotImplementedException();
	public void GetTriPlane(int index, ref CollisionPlane plane) => throw new NotImplementedException();

	public void SetRayBoundingBox(int index, in Vector3 min, in Vector3 max) => throw new NotImplementedException();
	public void GetRayBoundingBox(int index, out Vector3 min, out Vector3 max) => throw new NotImplementedException();
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
	public ushort Tags;
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

	public static CoreDispInfo FromDispUtils(DispUtilsHelper p) => throw new NotImplementedException();

	public override DispNeighbor GetEdgeNeighbor(int index) => throw new NotImplementedException();
	public override DispCornerNeighbors GetCornerNeighbors(int index) => throw new NotImplementedException();
	public override PowerInfo GetPowerInfo() => throw new NotImplementedException();
	public override DispUtilsHelper GetDispUtilsByIndex(int index) => throw new NotImplementedException();

	public void InitSurf(int parentIndex, Vector3[] points, Vector3[] normals, Vector2[] texCoords, Vector2[,] lightCoords, int contents, int flags, bool generateSurfPointStart, ref Vector3 startPoint, bool hasMappingAxes, ref Vector3 uAxis, ref Vector3 vAxis) => throw new NotImplementedException();

	public void InitDispInfo(int power, int minTess, float smoothingAngle, float[] alphas, Vector3[] dispVectorField, float[] dispDistances) => throw new NotImplementedException();
	public void InitDispInfo(int power, int minTess, float smoothingAngle, DispVert[] verts, DispTri[] tris) => throw new NotImplementedException();

	public bool Create() => throw new NotImplementedException();
	public bool CreateWithoutLOD() => throw new NotImplementedException();

	public CoreDispSurface GetSurface() => throw new NotImplementedException();
	public CoreDispNode GetNode(int index) => throw new NotImplementedException();

	public void SetPower(int power) => throw new NotImplementedException();
	public int GetPower() => throw new NotImplementedException();
	public int GetPostSpacing() => throw new NotImplementedException();
	public int GetWidth() => throw new NotImplementedException();
	public int GetHeight() => throw new NotImplementedException();
	public int GetSize() => throw new NotImplementedException();

	public void SetDispUtilsHelperInfo(CoreDispInfo[] listBase, nint listSize) {
		ListBase = listBase;
		ListSize = listSize;
	}

	public void SetNeighborData(DispNeighbor[] edgeNeighbors, DispCornerNeighbors[] cornerNeighbors) => throw new NotImplementedException();

	public VertIndex GetCornerPointIndex(int index) => throw new NotImplementedException();
	public Vector3 GetCornerPoint(int index) => throw new NotImplementedException();

	public void SetVert(int index, in Vector3 vert) => throw new NotImplementedException();
	public void GetVert(int index, out Vector3 vert) => throw new NotImplementedException();
	public Vector3 GetVert(int index) => throw new NotImplementedException();
	public Vector3 GetVert(in VertIndex index) => throw new NotImplementedException();

	public void GetFlatVert(int index, out Vector3 vert) => throw new NotImplementedException();
	public void SetFlatVert(int index, in Vector3 vert) => throw new NotImplementedException();

	public void GetNormal(int index, out Vector3 normal) => throw new NotImplementedException();
	public Vector3 GetNormal(int index) => throw new NotImplementedException();
	public Vector3 GetNormal(in VertIndex index) => throw new NotImplementedException();
	public void SetNormal(int index, in Vector3 normal) => throw new NotImplementedException();
	public void SetNormal(in VertIndex index, in Vector3 normal) => throw new NotImplementedException();

	public void GetTangentS(int index, out Vector3 tangentS) => throw new NotImplementedException();
	public Vector3 GetTangentS(int index) => throw new NotImplementedException();
	public Vector3 GetTangentS(in VertIndex index) => throw new NotImplementedException();
	public void GetTangentT(int index, out Vector3 tangentT) => throw new NotImplementedException();
	public void SetTangentS(int index, in Vector3 tangentS) => throw new NotImplementedException();
	public void SetTangentT(int index, in Vector3 tangentT) => throw new NotImplementedException();

	public void SetTexCoord(int index, in Vector2 texCoord) => throw new NotImplementedException();
	public void GetTexCoord(int index, out Vector2 texCoord) => throw new NotImplementedException();

	public void SetLuxelCoord(int bumpIndex, int index, in Vector2 luxelCoord) => throw new NotImplementedException();
	public void GetLuxelCoord(int bumpIndex, int index, out Vector2 luxelCoord) => throw new NotImplementedException();

	public void SetAlpha(int index, float alpha) => throw new NotImplementedException();
	public float GetAlpha(int index) => throw new NotImplementedException();

	public int GetTriCount() => throw new NotImplementedException();
	public void GetTriIndices(int tri, out ushort v1, out ushort v2, out ushort v3) => throw new NotImplementedException();
	public void SetTriIndices(int tri, ushort v1, ushort v2, ushort v3) => throw new NotImplementedException();
	public void GetTriPos(int tri, out Vector3 v1, out Vector3 v2, out Vector3 v3) => throw new NotImplementedException();
	public void SetTriTag(int tri, ushort tag) => throw new NotImplementedException();
	public void ResetTriTag(int tri, ushort tag) => throw new NotImplementedException();
	public void ToggleTriTag(int tri, ushort tag) => throw new NotImplementedException();
	public bool IsTriTag(int tri, ushort tag) => throw new NotImplementedException();
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

	public void CalcDispSurfCoords(bool lightmap, int lightmapID) => throw new NotImplementedException();
	public void GetPositionOnSurface(float u, float v, out Vector3 vPos, ref Vector3 normal, ref float alpha) => throw new NotImplementedException();

	public void DispUVToSurf(in Vector2 dispUV, out Vector3 point, ref Vector3 normal, ref float alpha) => throw new NotImplementedException();
	public void BaseFacePlaneToDispUV(in Vector3 planePt, out Vector2 dispUV) => throw new NotImplementedException();
	public bool SurfToBaseFacePlane(in Vector3 surfPt, out Vector3 planePt) => throw new NotImplementedException();

	public void SetListIndex(nint index) => throw new NotImplementedException();
	public nint GetListIndex() => throw new NotImplementedException();

	public MaxDispVertsBitVec GetAllowedVerts() => throw new NotImplementedException();
	public void AllowedVerts_Clear() => throw new NotImplementedException();
	public int AllowedVerts_GetNumDWords() => throw new NotImplementedException();
	public uint AllowedVerts_GetDWord(int i) => throw new NotImplementedException();
	public void AllowedVerts_SetDWord(int i, uint val) => throw new NotImplementedException();

	public void Position_Update(int vert, Vector3 pos) => throw new NotImplementedException();

	void GenerateDispSurf() => throw new NotImplementedException();
	void GenerateDispSurfNormals() => throw new NotImplementedException();
	void GenerateDispSurfTangentSpaces() => throw new NotImplementedException();
	bool DoesEdgeExist(int indexRow, int indexCol, int direction, int postSpacing) => throw new NotImplementedException();
	void CalcNormalFromEdges(int indexRow, int indexCol, bool[] isEdge, out Vector3 normal) => throw new NotImplementedException();
	void CalcDispSurfAlphas() => throw new NotImplementedException();
	void GenerateLODTree() => throw new NotImplementedException();
	void CalcVertIndicesAtNodes(int nodeIndex) => throw new NotImplementedException();
	int GetNodeVertIndexFromParentIndex(int level, int parentVertIndex, int direction) => throw new NotImplementedException();
	void CalcNodeInfo(int nodeIndex, int terminationLevel) => throw new NotImplementedException();
	void CalcNeighborVertIndicesAtNode(int nodeIndex, int level) => throw new NotImplementedException();
	void CalcNeighborNodeIndicesAtNode(int nodeIndex, int level) => throw new NotImplementedException();
	void CalcErrorTermAtNode(int nodeIndex, int level) => throw new NotImplementedException();
	float GetMaxErrorFromChildren(int nodeIndex, int level) => throw new NotImplementedException();
	void CalcBoundingBoxAtNode(int nodeIndex) => throw new NotImplementedException();
	void CalcMinMaxBoundingBoxAtNode(int nodeIndex, out Vector3 bMin, out Vector3 bMax) => throw new NotImplementedException();
	void CalcTriSurfInfoAtNode(int nodeIndex) => throw new NotImplementedException();
	void CalcTriSurfIndices(int nodeIndex, int[,] indices) => throw new NotImplementedException();
	void CalcTriSurfBoundingBoxes(int nodeIndex, int[,] indices) => throw new NotImplementedException();
	void CalcRayBoundingBoxes(int nodeIndex, int[,] indices) => throw new NotImplementedException();
	void CalcTriSurfPlanes(int nodeIndex, int[,] indices) => throw new NotImplementedException();
	void GenerateCollisionData() => throw new NotImplementedException();
	void GenerateCollisionSurface() => throw new NotImplementedException();

	void CreateBoundingBoxes(CoreDispBBox[] BBox, int count) => throw new NotImplementedException();

	void DispUVToSurf_TriTLToBR(out Vector3 point, ref Vector3 normal, ref float alpha, float u, float v, in Vector3 intersectPoint) => throw new NotImplementedException();
	void DispUVToSurf_TriBLToTR(out Vector3 point, ref Vector3 normal, ref float alpha, float u, float v, in Vector3 intersectPoint) => throw new NotImplementedException();
	void DispUVToSurf_TriTLToBR_1(in Vector3 intersectPoint, int snapU, int nextU, int snapV, int nNextV, out Vector3 point, ref Vector3 normal, ref float alpha, bool backup) => throw new NotImplementedException();
	void DispUVToSurf_TriTLToBR_2(in Vector3 intersectPoint, int snapU, int nextU, int snapV, int nNextV, out Vector3 point, ref Vector3 normal, ref float alpha, bool backup) => throw new NotImplementedException();
	void DispUVToSurf_TriBLToTR_1(in Vector3 intersectPoint, int snapU, int nextU, int snapV, int nNextV, out Vector3 point, ref Vector3 normal, ref float alpha, bool backup) => throw new NotImplementedException();
	void DispUVToSurf_TriBLToTR_2(in Vector3 intersectPoint, int snapU, int nextU, int snapV, int nNextV, out Vector3 point, ref Vector3 normal, ref float alpha, bool backup) => throw new NotImplementedException();

	void GetTriangleIndicesForDispBBox(int index, int[,] nTris) => throw new NotImplementedException();

	void BuildTriTLtoBR(int ndx) => throw new NotImplementedException();
	void BuildTriBLtoTR(int ndx) => throw new NotImplementedException();

	void InitTris() => throw new NotImplementedException();
	void CreateTris() => throw new NotImplementedException();
}
