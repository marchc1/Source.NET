using static Source.Engine.ModelLoader;
using static Source.Engine.DispHelpers;

using Source.Common;
using Source.Common.Formats.BSP;
using Source.Common.MaterialSystem;

using System.Numerics;
using System.Runtime.CompilerServices;
using Source.Common.Engine;
using CommunityToolkit.HighPerformance;

namespace Source.Engine;

static class DispHelpers
{
	public static void CalcMaxNumVertsAndIndices(int power, out int nVerts, out int nIndices) {
		int sideLength = (1 << power) + 1;
		nVerts = sideLength * sideLength;
		nIndices = (sideLength - 1) * (sideLength - 1) * 2 * 3;
	}
}

public static class DispMapload
{
	static void BuildDispGetSurfNormals(Vector3[] points, Vector3[] normals) => throw new NotImplementedException();

	static DispGroup? FindCombo(List<DispGroup> combos, int idLMPage, IMaterial? material) {
		foreach (var c in combos)
			if (c.LightmapPageID == idLMPage && c.Material == material)
				return c;

		return null;
	}

	static DispGroup AddCombo(List<DispGroup> combos, int idLMPage, IMaterial? material) {
		DispGroup combo = new DispGroup {
			LightmapPageID = idLMPage,
			Material = material,
			Visible = 0
		};
		combos.Add(combo);
		return combo;
	}

	static void BuildDispSurfInit(Model world, CoreDispInfo buildDisp, ref BSPMSurface2 worldSurfID) => throw new NotImplementedException();

	static VertexFormat ComputeDisplacementStaticMeshVertexFormat(IMaterial? material, DispGroup combo, Span<BSPDDispInfo> mapDisps) {
		VertexFormat vertexFormat = material!.GetVertexFormat();
		return vertexFormat;
	}

	static void AddEmptyMesh(Model world, DispGroup combo, Span<BSPDDispInfo> mapDisps, Span<int> dispInfos, int nDisps, int totalVerts, int totalIndices) {
		MatRenderContextPtr pRenderContext = new(SourceDllMain.materials);

		GroupMesh pMesh = new GroupMesh();
		combo.Meshes.Add(pMesh);

		VertexFormat vertexFormat = ComputeDisplacementStaticMeshVertexFormat(combo.Material, combo, mapDisps);
		pMesh.Mesh = pRenderContext.CreateStaticMesh(vertexFormat, MaterialDefines.TEXTURE_GROUP_STATIC_VERTEX_BUFFER_DISP);
		pMesh.Group = combo;
		pMesh.NumVisible = 0;

		using MeshBuilder builder = new();
		builder.Begin(pMesh.Mesh, MaterialPrimitiveType.Triangles, totalVerts, totalIndices);

		builder.AdvanceIndices(totalIndices);
		builder.AdvanceVertices(totalVerts);

		builder.End();

		pMesh.DispInfos.SetSize(nDisps);
		pMesh.Visible.SetSize(nDisps);
		pMesh.VisibleDisps.SetSize(nDisps);

		int iVertOffset = 0;
		int iIndexOffset = 0;
		for (int disp = 0; disp < nDisps; disp++) {
			DispInfo pDisp = DispInfo.GetModelDisp(world, dispInfos[disp])!;
			ref BSPDDispInfo mapDisp = ref mapDisps[dispInfos[disp]];

			pDisp.Mesh = pMesh;
			pDisp.VertOffset = iVertOffset;
			pDisp.IndexOffset = iIndexOffset;

			CalcMaxNumVertsAndIndices(mapDisp.Power, out int nVerts, out int nIndices);
			iVertOffset += nVerts;
			iIndexOffset += nIndices;

			pMesh.DispInfos[disp] = pDisp;
		}

		Assert(iVertOffset == totalVerts);
		Assert(iIndexOffset == totalIndices);
	}

	static void FillStaticBuffer(GroupMesh mesh, DispInfo disp, CoreDispInfo coreDisp, Span<DispVert> verts, int lightmaps) => throw new NotImplementedException();

	static void DispInfo_CreateMaterialGroups(Model world, MaterialSystem_SortInfo[] sortInfos) {
		for (int disp = 0; disp < world.Brush.Shared!.NumDispInfos; disp++) {
			DispInfo pDisp = DispInfo.GetModelDisp(world, disp)!;

			int idLMPage = sortInfos[MSurf_MaterialSortID(ref pDisp.ParentSurfID)].LightmapPageID;

			DispGroup? pCombo = FindCombo(g_DispGroups, idLMPage, MSurf_TexInfo(ref pDisp.ParentSurfID).Material);
			pCombo ??= AddCombo(g_DispGroups, idLMPage, MSurf_TexInfo(ref pDisp.ParentSurfID).Material);

			pCombo.DispInfos.Add(disp);
		}
	}

	static void DispInfo_LinkToParentFaces(Model world, Span<BSPDDispInfo> mapDisps, nint numDisplacements) {
		for (int disp = 0; disp < numDisplacements; disp++) {
			ref readonly BSPDDispInfo mapDisp = ref mapDisps[disp];
			DispInfo pDisp = DispInfo.GetModelDisp(world, disp)!;

			ref BSPMSurface2 surfID = ref SurfaceHandleFromIndex(mapDisp.MapFace);
			Assert(mapDisp.MapFace >= 0 && mapDisp.MapFace < world.Brush.Shared.NumSurfaces);
			Assert(MSurf_Flags(ref surfID) & SurfDraw.HasDisp);
			surfID.DispInfo = pDisp;
			pDisp.SetParent(ref surfID, world.Brush.Shared);
		}
	}

	static void DispInfo_CreateEmptyStaticBuffers(Model world, Span<BSPDDispInfo> mapDisps) {
		foreach (var combo in g_DispGroups) {
			int totalVerts = 0, totalIndices = 0;
			int iStart = 0;
			for (int disp = 0; disp < combo.DispInfos.Count; disp++) {
				ref BSPDDispInfo mapDisp = ref mapDisps[combo.DispInfos[disp]];

				CalcMaxNumVertsAndIndices(mapDisp.Power, out int nVerts, out int nIndices);

				// If we're going to pass our vertex buffer limit, or we're at the last one,
				// make a static buffer and fill it up.
				if ((totalVerts + nVerts) > MAX_STATIC_BUFFER_VERTS || (totalIndices + nIndices) > MAX_STATIC_BUFFER_INDICES) {
					AddEmptyMesh(world, combo, mapDisps, combo.DispInfos.AsSpan()[iStart..], disp - iStart, totalVerts, totalIndices);
					Assert(totalVerts > 0 && totalIndices > 0);

					totalVerts = totalIndices = 0;
					iStart = disp;
					--disp;
				}
				else if (disp == combo.DispInfos.Count - 1) {
					AddEmptyMesh(world, combo, mapDisps, combo.DispInfos.AsSpan()[iStart..], disp - iStart + 1, totalVerts + nVerts, totalIndices + nIndices);
					break;
				}
				else {
					totalVerts += nVerts;
					totalIndices += nIndices;
				}
			}
		}
	}

	public static bool DispInfo_CreateFromMapDisp(Model world, int disp, ref BSPDDispInfo mapDisp, CoreDispInfo coreDisp, Span<DispVert> verts, Span<DispTri> tris) {
		return true;
	}

	static void DispInfo_CreateStaticBuffersAndTags(Model world, int disp, CoreDispInfo coreDispInfo, Span<DispVert> tempVerts) {

	}

	static unsafe void SetupMeshReaders(Model world, nint numDisplacements) {
		for (int iDisp = 0; iDisp < numDisplacements; iDisp++) {
			DispInfo pDisp = DispInfo.GetModelDisp(world, iDisp)!;

			MeshDesc desc = default;

			desc.Vertex.PositionSize = sizeof(DispRenderVert);
			desc.Vertex.TexCoordSize[0] = sizeof(DispRenderVert);
			desc.Vertex.TexCoordSize[DISP_LMCOORDS_STAGE] = sizeof(DispRenderVert);
			desc.Vertex.NormalSize = sizeof(DispRenderVert);
			desc.Vertex.TangentSSize = sizeof(DispRenderVert);
			desc.Vertex.TangentTSize = sizeof(DispRenderVert);

			DispRenderVert[] pBaseVert = pDisp.Verts.Base();

			desc.Index.IndexSize = 1;

			pDisp.MeshReader.BeginRead_Direct(desc, pDisp.NumVerts(), pDisp.NumIndices);
		}
	}

	static void UpdateDispBBoxes(Model world, nint numDisplacements) {
		for (int iDisp = 0; iDisp < numDisplacements; iDisp++) {
			DispInfo pDisp = DispInfo.GetModelDisp(world, iDisp)!;
			pDisp.UpdateBoundingBox();
		}
	}

	public static bool DispInfo_LoadDisplacements(Model world, MaterialSystem_SortInfo[] sortInfos) {
		nint numDisplacements = MapLoadHelper.GetLumpSize(LumpIndex.DispInfo) / Unsafe.SizeOf<BSPDDispInfo>();
		nint numLuxels = MapLoadHelper.GetLumpSize(LumpIndex.DispLightmapAlphas);
		nint numSamplePositionBytes = MapLoadHelper.GetLumpSize(LumpIndex.DispLightmapSamplePositions);

		world.Brush.Shared!.NumDispInfos = (int)numDisplacements;
		world.Brush.Shared!.DispInfos = DispInfo_CreateArray(numDisplacements);

		MapLoadHelper dispInfos = new(LumpIndex.DispInfo);

		g_DispLMAlpha.Clear(); g_DispLMAlpha.SetSize((int)numLuxels);
		MapLoadHelper dispLMAlphas = new(LumpIndex.DispLightmapAlphas);
		dispLMAlphas.LoadLumpData(g_DispLMAlpha.AsSpan());

		g_DispLightmapSamplePositions.Clear(); g_DispLightmapSamplePositions.SetSize((int)numLuxels);
		MapLoadHelper dispLMPositions = new(LumpIndex.DispLightmapSamplePositions);
		dispLMAlphas.LoadLumpData(g_DispLightmapSamplePositions.AsSpan());

		Span<BSPDDispInfo> tempDisps = stackalloc BSPDDispInfo[BSPFileCommon.MAX_MAP_DISPINFO];
		dispInfos.LoadLumpData(tempDisps);

		DispInfo_LinkToParentFaces(world, tempDisps, numDisplacements);
		DispInfo_CreateMaterialGroups(world, sortInfos);
		DispInfo_CreateEmptyStaticBuffers(world, tempDisps);

		Span<DispVert> tempVerts = stackalloc DispVert[BSPFileCommon.MAX_DISPVERTS];
		Span<DispTri> tempTris = stackalloc DispTri[BSPFileCommon.MAX_DISPTRIS];

		MapLoadHelper dispVerts = new(LumpIndex.DispVerts);
		MapLoadHelper dispTris = new(LumpIndex.DispTris);

		int curVert = 0;
		int curTri = 0;

		List<CoreDispInfo> coreDisps = [];
		int disp = 0;
		for (disp = 0; disp < numDisplacements; disp++)
			coreDisps.Add(new());

		for (disp = 0; disp < numDisplacements; ++disp) {
			ref BSPDDispInfo mapDisp = ref tempDisps[disp];

			int numVerts = BSPFileCommon.NUM_DISP_POWER_VERTS(mapDisp.Power);
			ErrorIfNot(numVerts <= BSPFileCommon.MAX_DISPVERTS, $"DispInfo_LoadDisplacements: invalid vertex count ({numVerts})");
			dispVerts.LoadLumpData(curVert * Unsafe.SizeOf<DispVert>(), numVerts * Unsafe.SizeOf<DispVert>(), tempVerts);
			curVert += numVerts;

			int numTris = BSPFileCommon.NUM_DISP_POWER_TRIS(mapDisp.Power);
			ErrorIfNot(numTris <= BSPFileCommon.MAX_DISPTRIS, $"DispInfo_LoadDisplacements: invalid tri count ({numTris})");
			dispTris.LoadLumpData(curTri * Unsafe.SizeOf<DispTri>(), numTris * Unsafe.SizeOf<DispTri>(), tempTris);
			curTri += numTris;

			if (!DispInfo_CreateFromMapDisp(world, disp, ref mapDisp, coreDisps[disp], tempVerts, tempTris))
				return false;
		}

		SmoothDispSurfNormals(coreDisps.Base(), numDisplacements);

		for (disp = 0; disp < numDisplacements; ++disp) {
			DispInfo_CreateStaticBuffersAndTags(world, disp, coreDisps[disp], tempVerts);

			DispInfo pDisp = DispInfo.GetModelDisp(world, disp)!;
			pDisp.CopyCoreDispVertData(coreDisps[disp], pDisp.BumpSTexCoordOffset);

		}
		for (disp = 0; disp < numDisplacements; disp++) {
			DispInfo pDisp = DispInfo.GetModelDisp(world, disp)!;
			pDisp.ActiveVerts = pDisp.AllowedVerts;
		}

		for (disp = 0; disp < numDisplacements; disp++) {
			DispInfo pDisp = DispInfo.GetModelDisp(world, disp)!;
			pDisp.TesselateDisplacement();
		}

		SetupMeshReaders(world, numDisplacements);
		UpdateDispBBoxes(world, numDisplacements);

		return true;
	}

	static void DispInfo_ReleaseMaterialSystemObjects(Model world) => throw new NotImplementedException();

	static void BuildTagData(CoreDispInfo coreDisp, DispInfo disp) => throw new NotImplementedException();

	static int FindNeighborCornerVert(CoreDispInfo disp, in Vector3 point) => throw new NotImplementedException();

	static void UpdateTangentSpace(CoreDispInfo disp, int vert, in Vector3 normal, in Vector3 tanS) => throw new NotImplementedException();
	static void UpdateTangentSpace(CoreDispInfo disp, in VertIndex index, in Vector3 normal, in Vector3 tanS) => throw new NotImplementedException();

	static void BlendSubNeighbors(CoreDispInfo[] listBase, nint listSize) {

	}

	static int GetAllNeighbors(CoreDispInfo disp, int[] neighbors) => throw new NotImplementedException();

	static void BlendCorners(CoreDispInfo[] listBase, nint listSize) {

	}

	static void BlendEdges(CoreDispInfo[] listBase, nint listSize) {

	}

	static void SmoothDispSurfNormals(CoreDispInfo[] listBase, nint listSize) {
		for (int iDisp = 0; iDisp < listSize; ++iDisp)
			listBase[iDisp].SetDispUtilsHelperInfo(listBase, listSize);

		BlendSubNeighbors(listBase, listSize);
		BlendCorners(listBase, listSize);
		BlendEdges(listBase, listSize);
	}

	public static object? DispInfo_CreateArray(nint numDisplacements) {
		DispArray ret = new DispArray(numDisplacements);
		ret.CurTag = 1;
		for (nint i = 0; i < numDisplacements; i++) {
			ret.DispInfos[i] = new() {
				DispArray = ret
			};
		}
		return ret;
	}
}
