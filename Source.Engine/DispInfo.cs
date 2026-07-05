using CommunityToolkit.HighPerformance;

using Source.Common;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Formats.BSP;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;

using System.Numerics;
using System.Runtime.InteropServices;

namespace Source.Engine;

public class DispInfo : DispUtilsHelper, IDispInfo
{
	public Vector3 BBoxMin;
	public Vector3 BBoxMax;

	public int NumIndices;
	public int IndexOffset;
	public GroupMesh? Mesh;
	public int VertOffset;
	public float BumpSTexCoordOffset;

	public readonly MeshReader MeshReader = new();

	public readonly List<ushort> Indices = [];
	public readonly List<DispRenderVert> Verts = [];

	public MaxDispVertsBitVec ActiveVerts;
	public MaxDispVertsBitVec AllowedVerts;

	int LMPageID;

	nint ParentSurfPointer;
	readonly WeakReference<WorldBrushData> brushData = new(null!);
	public ref BSPMSurface2 ParentSurfID => ref GetParent();

	public int PointStart;

	int LightmapAlphaStart;

	int Contents;

	bool Touched;

	int SurfProp;

	int Power;

	ushort[]? Tags;

	Vector3[] BaseSurfacePositions = new Vector3[4];
	Vector2[] BaseSurfaceTexCoords = new Vector2[4];

	public PowerInfo? PowerInfo;

	DispNeighbor[] EdgeNeighbors = new DispNeighbor[4];
	DispCornerNeighbors[] CornerNeighbors = new DispCornerNeighbors[4];

	public int LightmapSamplePositionStart;

	public int WalkIndexCount;
	public ushort[]? WalkIndices;

	public int BuildIndexCount;
	public ushort[]? BuildIndices;

	DispNodeInfo[]? NodeInfo;

	Vector3 ViewerSphereCenter;

	bool InUse;

	DispDecalHandle FirstDecal;
	DispShadowHandle FirstShadowDecal;

	public ushort Index;

	ushort Tag;

	public DispArray? DispArray;

	static readonly MatSysInterface MatSys = Singleton<MatSysInterface>();

	public void GetIntersectingSurfaces(GetIntersectingSurfaces_Struct pStruct) => throw new NotImplementedException();
	public void RenderWireframeInLightmapPage(int pageId) => throw new NotImplementedException();

	public void GetBoundingBox(out Vector3 bbMin, out Vector3 bbMax) => throw new NotImplementedException();

	internal void SetParent(ref BSPMSurface2 surfID, WorldBrushData brushData) {
		this.brushData.SetTarget(brushData);
		ParentSurfPointer = surfID.SurfNum;
	}

	public ref BSPMSurface2 GetParent() {
		if (!brushData.TryGetTarget(out WorldBrushData? brush))
			throw new NullReferenceException("The world brush data has gone out of GC scope, and the pointer to a BSPMSurface2 cannot be retrieved.");
		return ref brush.Surfaces2![ParentSurfPointer];
	}

	// public void AddDynamicLights(DLight[] lights, uint lightMask) => throw new NotImplementedException();
	// public uint ComputeDynamicLightMask(DLight[] lights) => throw new NotImplementedException();

	// public DispDecalHandle NotifyAddDecal(Decal decal, float flSize) => throw new NotImplementedException();
	public void NotifyRemoveDecal(DispDecalHandle h) => throw new NotImplementedException();
	public DispShadowHandle AddShadowDecal(ShadowHandle_t shadowHandle) => throw new NotImplementedException();
	public void RemoveShadowDecal(DispShadowHandle handle) => throw new NotImplementedException();

	public bool ComputeShadowFragments(DispShadowHandle h, out int vertexCount, out int indexCount) => throw new NotImplementedException();

	public bool GetTag() => throw new NotImplementedException();
	public void SetTag() => throw new NotImplementedException();

	public DispInfo? GetDispByIndex(int index) => index == 0xFFFF ? null : DispArray!.DispInfos[index];

	public nint GetDispIndex() => throw new NotImplementedException();

	public void SetTouched(bool touched) => Touched = touched;
	public bool IsTouched() => Touched;

	public void ClearLOD() => throw new NotImplementedException();

	public void DrawDispAxes() => throw new NotImplementedException();
	public bool Render(GroupMesh group, bool allowDebugModes) {
#if !SWDS
		if (Mesh == null) {
			AssertMsg(false, "CDispInfo::Render: Mesh == null");
			return false;
		}

		// todo
		// if (R_CullBox(BBoxMin, BBoxMax, g_Frustum))
		//	return false;

		bool normalRender = true;
		if (allowDebugModes) {
			// todo
		}

		if (normalRender) {
			if (group.NumVisible < group.Visible.Count) {
				if (NumIndices != 0) {
					group.Visible[group.NumVisible] = new PrimList(IndexOffset, NumIndices);
					group.VisibleDisps[group.NumVisible] = this;
					group.NumVisible++;
					group.Group!.Visible++;
				}
			}
			else
				AssertMsg(false, "Overflowed visible mesh list");
		}
#endif

		return true;
	}

	// public void AddSingleDynamicLight(ref DLight dl) => throw new NotImplementedException();
	// public void AddSingleDynamicLightBumped(ref DLight dl) => throw new NotImplementedException();

	// public void AddSingleDynamicAlphaLight(ref DLight dl) => throw new NotImplementedException();

	public bool TestRay(in Ray ray, float start, float end, ref float dist, Vector2[]? lightmapUV, Vector2[]? textureUV) => throw new NotImplementedException();

	public override PowerInfo GetPowerInfo() => PowerInfo!;
	public override DispNeighbor GetEdgeNeighbor(int index) => throw new NotImplementedException();
	public override DispCornerNeighbors GetCornerNeighbors(int index) => throw new NotImplementedException();
	public override DispUtilsHelper GetDispUtilsByIndex(int index) => GetDispByIndex(index)!;

	public int VertIndex(int x, int y) {
		Assert(x >= 0 && x < GetSideLength() && y >= 0 && y < GetSideLength());
		return y * GetSideLength() + x;
	}
	public int VertIndex(in VertIndex vert) {
		Assert(vert.X >= 0 && vert.X < GetSideLength() && vert.Y >= 0 && vert.Y < GetSideLength());
		return vert.Y * GetSideLength() + vert.X;
	}
	public VertIndex IndexToVert(int index) => throw new NotImplementedException();

	public void SetNodeIntersectsDecal(DispDecal dispDecal, in VertIndex nodeIndex) => throw new NotImplementedException();
	public int GetNodeIntersectsDecal(DispDecal dispDecal, in VertIndex nodeIndex) => throw new NotImplementedException();

	public void CopyMapDispData(in BSPDDispInfo buildDisp) {
		LightmapAlphaStart = buildDisp.LightmapAlphaStart;
		Power = buildDisp.Power;

		Assert(Power >= 2 && Power <= BSPFileCommon.MAX_MAP_DISP_POWER + 1);
		PowerInfo = PowerInfo.GetPowerInfo(Power);

		int size = GetSideLength();
		Indices.SetSize(6 * (size - 1) * (size - 1));

		NodeInfo = new DispNodeInfo[PowerInfo.NodeCount];
	}

	public bool CopyCoreDispData(Model world, MaterialSystem_SortInfo[] sortInfos, CoreDispInfo coreDisp, bool restoring) {
		LMPageID = sortInfos[ModelLoader.MSurf_MaterialSortID(ref GetParent())].LightmapPageID;

#if !SWDS
		MatSysInterface.SurfaceCtx ctx = default;
		MatSys.SurfSetupSurfaceContext(ref ctx, ref GetParent());
#endif

		if (IsPC() && restoring) {
#if !SWDS
			if (NumLightmaps() > 1)
				BumpSTexCoordOffset = ctx.BumpSTexCoordOffset;
			else
				BumpSTexCoordOffset = 0.0f;

			for (int i = 0; i < NumVerts(); i++)
				coreDisp.GetLuxelCoord(0, i, out GetVertex(i).LMCoords);
#endif
			return true;
		}

		CoreDispSurface surface = coreDisp.GetSurface();
		for (int index = 0; index < 4; index++) {
			surface.GetTexCoord(index, out BaseSurfaceTexCoords[index]);
			BaseSurfacePositions[index] = surface.GetPoint(index);
		}

#if !SWDS
		CopyCoreDispVertData(coreDisp, ctx.BumpSTexCoordOffset);
#endif

		for (int iEdge = 0; iEdge < 4; iEdge++) {
			EdgeNeighbors[iEdge] = coreDisp.GetEdgeNeighbor(iEdge);
			CornerNeighbors[iEdge] = coreDisp.GetCornerNeighbors(iEdge);
		}

		AllowedVerts = coreDisp.GetAllowedVerts();

		NumIndices = 0;
		return true;
	}

	internal void CopyCoreDispVertData(CoreDispInfo coreDisp, float bumpSTexCoordOffset) {
#if !SWDS
		if (NumLightmaps() <= 1)
			bumpSTexCoordOffset = 0.0f;

		Verts.SetSizeInitialized(PowerInfo!.MaxVerts);
		BumpSTexCoordOffset = bumpSTexCoordOffset;
		for (int i = 0; i < NumVerts(); i++) {
			ref DispRenderVert vert = ref GetVertex(i);

			coreDisp.GetVert(i, out vert.Pos);

			coreDisp.GetTexCoord(i, out vert.TexCoord);
			coreDisp.GetLuxelCoord(0, i, out vert.LMCoords);

			coreDisp.GetNormal(i, out vert.Normal);
			coreDisp.GetTangentS(i, out vert.SVector);
			coreDisp.GetTangentT(i, out vert.TVector);
		}
#endif
	}

	public int NumLightmaps() => (ModelLoader.MSurf_Flags(ref ParentSurfID) & SurfDraw.BumpLight) != 0 ? Constants.NUM_BUMP_VECTS + 1 : 1;

	public Vector3 GetFlatVert(int vertex) => throw new NotImplementedException();

	internal void UpdateBoundingBox() {
		BBoxMin.Init(1e24f, 1e24f, 1e24f);
		BBoxMax.Init(-1e24f, -1e24f, -1e24f);
	}

	public new int GetSideLength() => PowerInfo!.GetSideLength();

	public int NumVerts() => PowerInfo!.GetNumVerts();

	public void DecalProjectVert(in Vector3 pos, DispDecalBase dispDecal, in ShadowInfo_t info, out Vector3 outVec) => throw new NotImplementedException();

	public void CullDecals(int nodeBit, DispDecal[] decals, int nDecals, DispDecal[] childDecals, ref int nChildDecals) => throw new NotImplementedException();

	internal ref DispNodeInfo GetNodeInfoRef(int nodeBit) => ref NodeInfo![nodeBit];

	internal void TesselateDisplacement() {
		// ClearAllDecalFragments();

		// ClearAllShadowDecalFragments();

		int maxIndices = MathLib.Square(GetSideLength() - 1) * 6;

		EngineTesselateHelper helper = new();
		helper.Disp = this;
		helper.IndexMesh.BeginModify(Mesh!.Mesh!, 0, 0, IndexOffset, maxIndices);
		helper.ActiveVerts = ActiveVerts;
		helper.PowerInfo = GetPowerInfo();

		DispTesselate.TesselateDisplacement(helper);

		helper.IndexMesh.EndModify();
		NumIndices = helper.NIndices;
	}

	public void SpecifyDynamicMesh() => throw new NotImplementedException();
	public void SpecifyWalkableDynamicMesh() => throw new NotImplementedException();
	public void SpecifyBuildableDynamicMesh() => throw new NotImplementedException();

	public void InitializeActiveVerts() {
		ActiveVerts.ClearAll();

		ActiveVerts.Set(VertIndex(0, 0));
		ActiveVerts.Set(VertIndex(GetSideLength() - 1, 0));
		ActiveVerts.Set(VertIndex(GetSideLength() - 1, GetSideLength() - 1));
		ActiveVerts.Set(VertIndex(0, GetSideLength() - 1));
		ActiveVerts.Set(VertIndex(PowerInfo!.RootNode));

		for (int side = 0; side < 4; side++) {
			ref DispNeighbor pSide = ref EdgeNeighbors[side];

			if ((pSide.SubNeighbors[0].IsValid() && pSide.SubNeighbors[0].GetSpan() != NeighborSpan.CORNER_TO_CORNER) ||
				(pSide.SubNeighbors[1].IsValid() && pSide.SubNeighbors[1].GetSpan() != NeighborSpan.CORNER_TO_CORNER)) {
				int edgeDim = g_EdgeDims[side];

				VertIndex nodeIndex = new();
				nodeIndex[edgeDim] = (short)(g_EdgeSideLenMul[side] * PowerInfo.SideLengthM1);
				nodeIndex[edgeDim ^ 1] = (short)PowerInfo.MidPoint;
				ActiveVerts.Set(VertIndex(nodeIndex));
			}
		}
	}

	public ref DispRenderVert GetVertex(int i) {
		Assert(i < NumVerts());
		return ref Verts.AsSpan()[i];
	}

	// public void ComputeLightmapAndTextureCoordinate(in RayDispOutput output, Vector2[]? luv, Vector2[]? tuv) => throw new NotImplementedException();

	public void GenerateDecalFragments(in VertIndex nodeIndex, int nodeBitIndex, ushort decalHandle, DispDecalBase dispDecal) => throw new NotImplementedException();

	void TestAddDecalTri(int indexStart, ushort decalHandle, DispDecal dispDecal) => throw new NotImplementedException();
	void TestAddDecalTri(int indexStart, ushort decalHandle, DispShadowDecal dispDecal) => throw new NotImplementedException();

	// DispDecalFragment AllocateDispDecalFragment(DispDecalHandle h, int nVerts = 6) => throw new NotImplementedException();

	// void ClearDecalFragments(DispDecalHandle h) => throw new NotImplementedException();
	void ClearAllDecalFragments() => throw new NotImplementedException();

	// DispShadowFragment AllocateShadowDecalFragment(DispShadowHandle h, int nCount) => throw new NotImplementedException();

	// void ClearShadowDecalFragments(DispShadowHandle h) => throw new NotImplementedException();
	void ClearAllShadowDecalFragments() => throw new NotImplementedException();

	void GenerateDecalFragments_R(in VertIndex nodeIndex, int nodeBitIndex, ushort decalHandle, DispDecalBase dispDecal, int level) => throw new NotImplementedException();

	void SetupDecalNodeIntersect(in VertIndex nodeIndex, int nodeBitIndex, DispDecalBase dispDecal, in ShadowInfo_t info) => throw new NotImplementedException();

	// bool SetupDecalNodeIntersect_R(in VertIndex nodeIndex, int nodeBitIndex, DispDecalBase dispDecal, in ShadowInfo_t info, int level, DecalNodeSetupCache cache) => throw new NotImplementedException();

	public static DispInfo? GetModelDisp(Model world, int i) {
		return DispInfo_IndexArray(world.Brush.Shared!.DispInfos, i);
	}

	static readonly ConVar r_DrawDisp = new("r_DrawDisp", "1", FCvar.Cheat, "Toggles rendering of displacment maps");
	public static void DispInfo_RenderList(int sortGroup, Span<SurfaceHandle_t> list, int listCount, bool ortho, uint flags, RenderDepthMode depthMode) {
		if (r_DrawDisp.GetInt() == 0 || listCount == 0)
			return;

		DispInfo[] visibleDisps = new DispInfo[BSPFileCommon.MAX_MAP_DISPINFO];

		DispInfo_BuildPrimLists(sortGroup, list, listCount, depthMode != RenderDepthMode.Normal, visibleDisps, out int visibleDispCount);

		DispInfo_DrawPrimLists(depthMode);

		if (depthMode != RenderDepthMode.Normal)
			return;

		for (int i = 0; i < listCount; i++) {
			SurfaceHandle_t cur = list[i];
			ref BSPMSurface2 surf = ref ModelLoader.SurfaceHandleFromIndex(cur);
			ShadowDecalHandle_t decalHandle = ModelLoader.MSurf_ShadowDecals(ref surf);
			if (decalHandle != SHADOW_DECAL_HANDLE_INVALID) {
				// g_pShadowMgr.AddShadowsOnSurfaceToRenderList(decalHandle) // todo
			}
		}

		bool flashlightMask = !(((DrawWorldListFlags)flags & DrawWorldListFlags.Refraction) != 0 || ((DrawWorldListFlags)flags & DrawWorldListFlags.Reflection) != 0);

		// todo

		// g_pShadowMgr.RenderFlashlights(flashlightMask)
		// OverlayMgr().RenderOverlays(sortGroup)
		// g_pShadowMgr.DrawFlashlightOverlays(sortGroup, flashlightMask
		// OverlayMgr().ClearRenderLists(sortGroup)

		// DispInfo_BatchDecals(visibleDisps, visibleDispCount);
		// DispInfo_DrawDecals(visibleDisps, visibleDispCount);

		// g_pShadowMgr.DrawFlashlightDecalsOnDisplacements(sortGroup, visibleDisps, visibleDispCount, flashlightMask)
		// g_pShadowMgr.RenderShadows()
		// g_pShadowMgr.ClearShadowRenderList()

		DispInfo_DrawDebugInformation(list, listCount);
	}

	static void DispInfo_BuildPrimLists(int sortGroup, Span<SurfaceHandle_t> list, int listCount, bool depthOnly, DispInfo[] visibleDisps, out int visibleDispCount) {
		visibleDispCount = 0;
		bool debugConvars = false; // !depthOnly ? DispInfoRenderDebugModes() : false // todo
		for (int i = 0; i < listCount; i++) {
			DispInfo disp = (DispInfo)ModelLoader.SurfaceHandleFromIndex(list[i]).DispInfo!;
			if (!disp.Render(disp.Mesh!, debugConvars))
				continue;

			if (visibleDispCount < BSPFileCommon.MAX_MAP_DISPINFO)
				visibleDisps[visibleDispCount++] = disp;

			if (depthOnly)
				continue;
#if !SWDS
			// OverlayMgr().AddFragmentListToRenderList(sortGroup, MSurf_OverlayFragmentList(list[i]), true) // todo
#endif
		}
	}

	static readonly ConVar disp_dynamic = new("disp_dynamic", "0", 0);

	static void DispInfo_DrawPrimLists(RenderDepthMode depthMode) {
		int dispGroupsSize = g_DispGroups.Count;

		int fullbright = MatSysInterface.MaterialSystemConfig.Fullbright;

		using MatRenderContextPtr renderContext = new(materials);

		for (int iGroup = 0; iGroup < dispGroupsSize; iGroup++) {
			DispGroup group = g_DispGroups[iGroup];
			if (group.Visible == 0)
				continue;

			if (depthMode != RenderDepthMode.Normal) {
				// todo!!!!!!
			}
			else
				renderContext.Bind(group.Material!, null);

			if (fullbright != 1 && depthMode == RenderDepthMode.Normal)
				renderContext.BindLightmapPage(group.LightmapPageID);
			else {
				if (group.Material!.GetPropertyFlag(MaterialPropertyTypes.NeedsBumpedLightmaps))
					renderContext.BindLightmapPage(StandardLightmap.WhiteBump);
				else
					renderContext.BindLightmapPage(StandardLightmap.White);
			}

			int meshesSize = group.Meshes.Count;

			for (int iMesh = 0; iMesh < meshesSize; iMesh++) {
				GroupMesh mesh = group.Meshes[iMesh];
				if (mesh.NumVisible == 0)
					continue;

				if (disp_dynamic.GetInt() != 0) {
					for (int iVisible = 0; iVisible < mesh.NumVisible; iVisible++)
						mesh.VisibleDisps[iVisible]!.SpecifyDynamicMesh();
				}
				else
					mesh.Mesh!.Draw(CollectionsMarshal.AsSpan(mesh.Visible), mesh.NumVisible);

				mesh.NumVisible = 0;
			}
		}
	}
	static void DispInfo_BatchDecals(DispInfo[] visibleDisps, int visibleDispCount) => throw new NotImplementedException();
	static void DispInfo_DrawDecals(DispInfo[] visibleDisps, int visibleDispCount) => throw new NotImplementedException();
	static void DispInfo_DrawDebugInformation(Span<SurfaceHandle_t> list, int listCount) {
		// => throw new NotImplementedException();
	}

	public static IDispInfo? MLeaf_Disaplcement(BSPMLeaf leaf, int index, WorldBrushData? data = null) {
		data ??= host_state.WorldBrush;
		Assert(index < leaf.DispCount);
		int dispIndex = data!.DispInfoReferences![leaf.DispListStart + index];
		return DispInfo_IndexArray(data.DispInfos, dispIndex);
	}

	private static DispInfo? DispInfo_IndexArray(object? oArray, int i) {
		DispArray? array = (DispArray?)oArray;
		if (array == null)
			return null;
		return array.DispInfos[i];
	}
}

public class GetIntersectingSurfaces_Struct
{
	public Model? Model;
	public Vector3 Center;
	public byte[]? CenterPVS;
	public float Radius;
	public bool OnlyVisible;
	public SurfInfo[]? Infos;
	public int MaxInfos;
	public int SetInfos;
}
