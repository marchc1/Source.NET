using CommunityToolkit.HighPerformance;

using DStruct.BinaryTrees;

using Source.Common;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Formats.BSP;
using Source.Common.Formats.Keyvalues;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;

using System.Numerics;
using System.Runtime.CompilerServices;

namespace Source.Engine;

public struct MaterialList
{
	public short NextBlock;
	public short Count;
	public InlineArray15<nint> Surfaces;
}

public struct SurfaceSortGroup
{
	public short ListHead;
	public short ListTail;
	public ushort VertexCount;
	public short GroupListIndex;
	public ushort VertexCountNoDetail;
	public ushort IndexCountNoDetail;
	public ushort TriangleCount;
	public ushort SurfaceCount;
}

public class MSurfaceSortList
{
	int MaxSortIDs;
	public void Init(int maxSortIDs, int minMaterialLists) {
		List.Clear();
		List.EnsureCapacity(minMaterialLists);

		MaxSortIDs = maxSortIDs;

		int groupMax = maxSortIDs * (int)MatSortGroup.Max;
		Groups.EnsureCount(groupMax);
		memreset(Groups.AsSpan());

		int groupBytes = (groupMax + 7) >> 3;
		GroupUsed.EnsureCount(groupBytes);
		memreset(GroupUsed.AsSpan());

		for (int i = 0; i < SortGroupLists.Length; i++) {
			ref List<SurfaceSortGroup> list = ref SortGroupLists.AsSpan()[i];
			list ??= [];
			if (i == 0) {
				list.Clear();
				list.EnsureCapacity(128);
				GroupOffset[0] = 0;
			}
			else {
				list.Clear();
				list.EnsureCapacity(16);
				GroupOffset[i] = maxSortIDs * i;
			}
		}

		InitGroup(ref EmptyGroup);
	}

	public void Shutdown() { }

	public void Reset() => Init(MaxSortIDs, List.Capacity);

	public void EnsureMaxSortIDs(int newMaxSortIDs) {
		if (newMaxSortIDs > MaxSortIDs) {
			int oldMax = MaxSortIDs;
			newMaxSortIDs += 255;
			newMaxSortIDs -= newMaxSortIDs & 255;
			int groupMax = newMaxSortIDs * (int)MatSortGroup.Max;
			int groupBytes = (groupMax + 7) >> 3;
			Groups.EnsureCount(groupMax);
			GroupUsed.EnsureCount(groupBytes);
			for (int i = (int)MatSortGroup.Max; --i >= 0;) {
				for (int j = newMaxSortIDs; --j >= 0;) {
					int newIndex = (i * newMaxSortIDs) + j;
					if (j < oldMax) {
						if (i != 0) {
							int oldIndex = (i * oldMax) + j;
							MarkGroupNotUsed(newIndex);
							if (IsGroupUsed(oldIndex)) {
								MarkGroupNotUsed(oldIndex);
								MarkGroupUsed(newIndex);
								Groups[newIndex] = Groups[oldIndex];
								SurfaceSortGroup oldGroup = Groups[oldIndex];
								InitGroup(ref oldGroup);
								Groups[oldIndex] = oldGroup;
							}
						}
						if (IsGroupUsed(newIndex) && Groups[newIndex].GroupListIndex >= 0)
							SortGroupLists[i][Groups[newIndex].GroupListIndex] = Groups[newIndex];
					}
					else
						MarkGroupNotUsed(newIndex);
				}
				GroupOffset[i] = i * newMaxSortIDs;
			}
			MaxSortIDs = newMaxSortIDs;
		}
	}

	readonly int[] GroupOffset = new int[(int)MatSortGroup.Max];
	readonly List<byte> GroupUsed = [];
	readonly List<MaterialList> List = [];
	readonly List<SurfaceSortGroup> Groups = [];
	readonly List<SurfaceSortGroup>[] SortGroupLists = new List<SurfaceSortGroup>[(int)MatSortGroup.Max];

	public void InitGroup(ref SurfaceSortGroup group) {
		group.ListHead = -1;
		group.ListTail = -1;
		group.VertexCount = 0;
		group.GroupListIndex = -1;
		group.VertexCountNoDetail = 0;
		group.IndexCountNoDetail = 0;
		group.TriangleCount = 0;
		group.SurfaceCount = 0;
	}
	public bool IsGroupUsed(int groupIndex) => (GroupUsed[(groupIndex >> 3)] & (1 << (groupIndex & 7))) != 0;
	public void MarkGroupUsed(int groupIndex) => GroupUsed[groupIndex >> 3] |= checked((byte)(1 << (groupIndex & 7)));
	public void MarkGroupNotUsed(int groupIndex) => GroupUsed[groupIndex >> 3] &= unchecked((byte)~(1 << (groupIndex & 7)));

	internal void AddSurfaceToTail(ref BSPMSurface2 surface, int sortGroup, short sortID) {
		Span<SurfaceSortGroup> groups = Groups.AsSpan();
		int index = GroupOffset[sortGroup] + sortID;
		ref SurfaceSortGroup group = ref groups[index];
		if (!IsGroupUsed(index)) {
			MarkGroupUsed(index);
			InitGroup(ref group);
		}
		ref MaterialList list = ref Unsafe.NullRef<MaterialList>();
		Span<MaterialList> m_list = List.AsSpan();
		short prevIndex = -1;
		int vertCount = ModelLoader.MSurf_VertCount(ref surface);
		int triangleCount = vertCount - 2;
		group.TriangleCount += (ushort)triangleCount;
		group.SurfaceCount++;
		group.VertexCount += (ushort)vertCount;
		if ((ModelLoader.MSurf_Flags(ref surface) & SurfDraw.Node) != 0) {
			group.VertexCountNoDetail += (ushort)vertCount;
			group.IndexCountNoDetail += (ushort)(triangleCount * 3);
		}
		if (group.ListTail != -1) {
			list = ref m_list[group.ListTail];
			if (list.Count >= 15 /* list.Surfaces length */) {
				prevIndex = group.ListTail;
				list = ref Unsafe.NullRef<MaterialList>();
			}
		}
		if (!Unsafe.IsNullRef(ref list)) {
			list.Surfaces[list.Count] = surface.SurfNum;
			list.Count++;
		}
		else {
			List.Add(default);
			short nextBlock = (short)(List.Count - 1);
			// m_list may be invalid now! remake the span
			m_list = List.AsSpan();

			if (prevIndex >= 0)
				m_list[prevIndex].NextBlock = nextBlock;

			group.ListTail = nextBlock;
			if (group.ListHead == -1) {
				SortGroupLists[sortGroup].Add(group);
				index = (short)(SortGroupLists[sortGroup].Count - 1);
				group.GroupListIndex = (short)index;
				group.ListHead = nextBlock;
			}

			list = ref m_list[nextBlock];
			list.NextBlock = -1;
			list.Count = 1;
			list.Surfaces[0] = surface.SurfNum;
		}

		if (group.GroupListIndex >= 0)
			SortGroupLists[sortGroup][group.GroupListIndex] = group;
	}

	public List<SurfaceSortGroup> GetSortList(int sortGroup) => SortGroupLists[sortGroup];

	public void GetSurfaceListForGroup(List<SurfaceHandle_t> list, in SurfaceSortGroup group) {
		for (short blockIndex = group.ListHead; blockIndex != -1; blockIndex = GetSurfaceBlock(blockIndex).NextBlock) {
			ref MaterialList matList = ref GetSurfaceBlock(blockIndex);
			for (int index = 0; index < matList.Count; ++index)
				list.Add((SurfaceHandle_t)matList.Surfaces[index]);
		}
	}

	internal ref SurfaceSortGroup GetGroupForSortID(int sortGroup, int sortID) {
		return ref GetGroupByIndex(GetIndexForSortID(sortGroup, sortID));
	}

	internal ref MaterialList GetSurfaceBlock(short index) {
		return ref List.AsSpan()[index];
	}

	private int GetIndexForSortID(int sortGroup, int sortID) {
		return GroupOffset[sortGroup] + sortID;
	}

	SurfaceSortGroup EmptyGroup;

	private ref SurfaceSortGroup GetGroupByIndex(int groupIndex) {
		if (!IsGroupUsed(groupIndex))
			return ref EmptyGroup;
		return ref Groups.AsSpan()[groupIndex];
	}

	internal ref BSPMSurface2 GetSurfaceAtHead(in SurfaceSortGroup group) {
		if (group.ListHead == -1)
			return ref Unsafe.NullRef<BSPMSurface2>();
		return ref host_state.WorldBrush!.Surfaces2![List.AsSpan()[group.ListHead].Surfaces[0]];
	}
}
public class MatSysInterface(IMaterialSystem materials, IServiceProvider services)
{
	public readonly TextureReference FullFrameFBTexture0 = new();
	public readonly TextureReference FullFrameFBTexture1 = new();

	public int FrameCount = 1;
	public static readonly int[] LightStyleValue = new int[256];
	public static readonly int[] LightStyleNumFrames = new int[256];
	public static readonly int[] LightStyleFrame = new int[256];

	public void Init() {
		InitWellKnownRenderTargets();
		InitDebugMaterials();
	}

	private void InitDebugMaterials() {
		MaterialEmpty = GL_LoadMaterial("debug/debugempty", MaterialDefines.TEXTURE_GROUP_OTHER)!;
#if !SWDS
		// TODO: the rest of these important materials
#endif
	}

	private void InitWellKnownRenderTargets() {
#if !SWDS
		materials.BeginRenderTargetAllocation();
		FullFrameFBTexture0.Init(CreateFullFrameFBTexture(0));
		FullFrameFBTexture0.Init(CreateFullFrameFBTexture(1));
		materials.EndRenderTargetAllocation();
#endif
	}

	private ITexture CreateFullFrameFBTexture(int textureIndex, CreateRenderTargetFlags extraFlags = 0) {
		Span<char> textureName = stackalloc char[256];

		if (textureIndex > 0)
			sprintf(textureName, MaterialDefines.FULL_FRAME_FRAMEBUFFER_INDEXED).D(textureIndex);
		else
			strcpy(textureName, MaterialDefines.FULL_FRAME_FRAMEBUFFER);

		CreateRenderTargetFlags rtFlags = extraFlags | CreateRenderTargetFlags.HDR;
		return materials.CreateNamedRenderTargetTextureEx(
			textureName.SliceNullTerminatedString(),
			1, 1, RenderTargetSizeMode.FullFrameBuffer,
			materials.GetRenderContext().GetShaderAPI().GetBackBufferFormat(), MaterialRenderTargetDepth.Shared,
			TextureFlags.ClampS | TextureFlags.ClampT,
			rtFlags)!;
	}

	internal enum ToolTexture
	{
		/// <summary> Not a tool texture </summary>
		None = 0,
		/// <summary> A tool texture by means of starting with tools/, but otherwise an unimportant use case beyond not renderable </summary>
		Unknown = 1,
		/// <summary> The 2D skybox texture. </summary>
		Skybox2D = 2,
		/// <summary> The 3D skybox texture. </summary>
		Skybox3D = 3
	}
	static ToolTexture TryGetToolTexture(ReadOnlySpan<char> texture) => texture switch {
		"tools/toolsskybox2d" => ToolTexture.Skybox2D,
		"tools/toolsskybox" => ToolTexture.Skybox3D,
		_ => texture.StartsWith("tools/", StringComparison.InvariantCultureIgnoreCase) ? ToolTexture.Unknown : ToolTexture.None
	};
	internal struct MeshList
	{
		public IMesh Mesh;
		public IMaterial Material;
		public int VertCount;
		public int IndexCount;
		public int LightmapPageID;
		public VertexFormat VertexFormat;
		// TODO: Is there a better way to handle this? I can't figure out how Source does...
		public ToolTexture ToolTexture;
	}

	internal readonly List<MeshList> Meshes = [];

	// These are pointers into Meshes
	internal readonly List<int> SkyboxMeshesIndices = [];
	internal readonly List<int> Skybox2DMeshesIndices = [];
	internal readonly List<int> Skybox3DMeshesIndices = [];

	internal readonly List<IMesh?> WorldStaticMeshes = [];

	ConVar mat_max_worldmesh_vertices = new((32767 / 3).ToString(), 0);
	public static readonly ConVar r_drawbrushmodels = new("r_drawbrushmodels", "1", FCvar.Cheat, "Render brush models. 0=Off, 1=Normal, 2=Wireframe");


	public static void VertexCountForSurfaceList(MSurfaceSortList list, in SurfaceSortGroup group, out int vertexCount, out int indexCount) {
		vertexCount = indexCount = 0;
		for (short _blockIndex = group.ListHead; _blockIndex != -1; _blockIndex = list.GetSurfaceBlock(_blockIndex).NextBlock) {
			ref MaterialList matList = ref list.GetSurfaceBlock(_blockIndex);
			for (int _index = 0; _index < matList.Count; ++_index) {
				ref BSPMSurface2 surfID = ref host_state.WorldBrush!.Surfaces2![matList.Surfaces[_index]];
				int vertCount = ModelLoader.MSurf_VertCount(ref surfID);
				vertexCount += vertCount;

				int numPolygons = vertCount - 2;
				indexCount += 3 * numPolygons;
			}
		}
	}
	public const uint TEXINFO_USING_BASETEXTURE2 = 0x0001;
	public void WorldStaticMeshCreate() {
		FrameCount = 1;
		WorldStaticMeshDestroy();
		Meshes.Clear();
		SkyboxMeshesIndices.Clear();
		Skybox2DMeshesIndices.Clear();
		Skybox3DMeshesIndices.Clear();

		int sortIDs = materials.GetNumSortIDs();

		Assert(WorldStaticMeshes.Count == 0);
		WorldStaticMeshes.EnsureCountDefault(sortIDs);

		MSurfaceSortList matSortArray = new();
		matSortArray.Init(sortIDs, 512);
		Span<int> sortIndex = stackalloc int[WorldStaticMeshes.Count];

		for (int surfaceIndex = 0; surfaceIndex < host_state.WorldBrush!.NumSurfaces; surfaceIndex++) {
			ref BSPMSurface2 surfID = ref ModelLoader.SurfaceHandleFromIndex(surfaceIndex, host_state.WorldBrush);
			ModelLoader.MSurf_Flags(ref surfID) &= ~SurfDraw.TangentSpace;

			if (ModelLoader.SurfaceHasDispInfo(ref surfID)) {
				ModelLoader.MSurf_VertBufferIndex(ref surfID) = 0xFFFF;
				continue;
			}

			matSortArray.AddSurfaceToTail(ref surfID, 0, ModelLoader.MSurf_MaterialSortID(ref surfID));
		}

		for (int i = 0; i < WorldStaticMeshes.Count; i++) {
			ref readonly SurfaceSortGroup group = ref matSortArray.GetGroupForSortID(0, i);
			VertexCountForSurfaceList(matSortArray, group, out int vertexCount, out int indexCount);

			ref BSPMSurface2 surfID = ref matSortArray.GetSurfaceAtHead(in group);
			WorldStaticMeshes[i] = null;
			sortIndex[i] = !Unsafe.IsNullRef(ref surfID) ? FindOrAddMesh(ModelLoader.MSurf_TexInfo(ref surfID).Material, i, vertexCount, indexCount) : -1;
		}

		using MatRenderContextPtr renderContext = new(materials);
		var meshes = Meshes.AsSpan();
		for (int i = 0; i < Meshes.Count; i++) {
			VertexFormat format = meshes[i].Material.GetVertexFormat();
			meshes[i].Mesh = renderContext.CreateStaticMesh(format, MaterialDefines.TEXTURE_GROUP_STATIC_VERTEX_BUFFER_WORLD, meshes[i].Material);
			int vertBufferIndex = 0;

			// We precalculate the tool texture type as an enumeration and then
			// store indices into Meshes into lists where needed (mostly for skybox rendering).
			meshes[i].ToolTexture = TryGetToolTexture(meshes[i].Material.GetName());
			switch (meshes[i].ToolTexture) {
				case ToolTexture.Skybox2D: SkyboxMeshesIndices.Add(i); Skybox2DMeshesIndices.Add(i); break;
				case ToolTexture.Skybox3D: SkyboxMeshesIndices.Add(i); Skybox3DMeshesIndices.Add(i); break;
			}

			MeshBuilder meshBuilder = new();
			meshBuilder.Begin(meshes[i].Mesh, MaterialPrimitiveType.Triangles, meshes[i].VertCount, 0);
			for (int j = 0; j < WorldStaticMeshes.Count; j++) {
				int meshId = sortIndex[j];
				if (meshId == i) {
					WorldStaticMeshes[j] = Meshes[i].Mesh;
					ref readonly SurfaceSortGroup group = ref matSortArray.GetGroupForSortID(0, j);
					for (short _blockIndex = group.ListHead; _blockIndex != -1; _blockIndex = matSortArray.GetSurfaceBlock(_blockIndex).NextBlock) {
						ref MaterialList matList = ref matSortArray.GetSurfaceBlock(_blockIndex);
						for (int _index = 0; _index < matList.Count; ++_index) {
							ref BSPMSurface2 surfID = ref host_state.WorldBrush!.Surfaces2![matList.Surfaces[_index]];
							ModelLoader.MSurf_VertBufferIndex(ref surfID) = (ushort)vertBufferIndex;
							BuildMSurfaceVertexArrays(host_state.WorldBrush!, ref surfID, IMaterialSystem.OVERBRIGHT, ref meshBuilder);
							vertBufferIndex += ModelLoader.MSurf_VertCount(ref surfID);
						}
					}
				}
			}

			meshBuilder.End();
			Assert(vertBufferIndex == Meshes[i].VertCount);
			meshBuilder.Dispose();
		}

		// Msg($"Total {Meshes.Count} meshes, {WorldStaticMeshes.Count} before\n");
	}

	private void BuildMSurfacePrimVerts(BSPPrimType type, WorldBrushData brushData, ref BSPMPrimitive prim, ref MeshBuilder builder, ref BSPMSurface2 surfID) {
		bool negate = false;
		Vector3 vect = default;
		if ((ModelLoader.MSurf_Flags(ref surfID) & SurfDraw.TangentSpace) != 0)
			negate = TangentSpaceSurfaceSetup(ref surfID, out vect);

		for (int i = 0; i < prim.VertCount; i++) {
			ref BSPMPrimVert primVert = ref brushData.PrimVerts![prim.FirstVert + i];
			builder.Position3fv(primVert.Position);
			builder.Normal3fv(ModelLoader.MSurf_Plane(ref surfID).Normal);
			builder.TexCoord2fv(0, primVert.TexCoord);
			builder.TexCoord2fv(1, primVert.LightCoord);
			if ((ModelLoader.MSurf_Flags(ref surfID) & SurfDraw.TangentSpace) != 0) {
				TangentSpaceComputeBasis(out Vector3 tangentS, out Vector3 tangentT, ModelLoader.MSurf_Plane(ref surfID).Normal, ref vect, false);
				builder.TangentS3fv(tangentS);
				builder.TangentT3fv(tangentT);
			}
			builder.AdvanceVertex();
		}
	}

	private void BuildMSurfacePrimIndices(BSPPrimType type, WorldBrushData brushData, ref BSPMPrimitive prim, ref MeshBuilder builder) {
		for (int i = 0; i < prim.IndexCount; i++) {
			ushort primIndex = brushData.PrimIndices![prim.FirstIndex + i];
			builder.Index((ushort)(primIndex - prim.FirstVert));
			builder.AdvanceIndex();
		}
	}

	public struct SurfaceCtx
	{
		public InlineArray2<int> LightmapSize;
		public InlineArray2<int> LightmapPageSize;
		public float BumpSTexCoordOffset;
		public Vector2 Offset;
		public Vector2 Scale;
	}

	internal void BuildMSurfaceVerts(WorldBrushData brushData, ref BSPMSurface2 surfID, Vector3[]? verts, Vector2[]? texCoords, Vector2[,]? lightCoords) {
		SurfaceCtx ctx = default;
		SurfSetupSurfaceContext(ref ctx, ref surfID);

		int vertCount = ModelLoader.MSurf_VertCount(ref surfID);
		int vertFirstIndex = ModelLoader.MSurf_FirstVertIndex(ref surfID);
		for (int i = 0; i < vertCount; i++) {
			int vertIndex = brushData.VertIndices![vertFirstIndex + i];

			ref Vector3 vec = ref brushData.Vertexes![vertIndex].Position;

			if (verts != null)
				MathLib.VectorCopy(vec, out verts[i]);

			if (texCoords != null)
				SurfComputeTextureCoordinate(ref ctx, ref surfID, ref vec, ref texCoords[i]);

			if (lightCoords != null) {
				SurfComputeLightmapCoordinate(ref ctx, ref surfID, ref vec, ref lightCoords[i, 0]);

				if ((ModelLoader.MSurf_Flags(ref surfID) & SurfDraw.BumpLight) != 0) {
					for (int bumpID = 1; bumpID <= Constants.NUM_BUMP_VECTS; bumpID++) {
						lightCoords[i, bumpID].X = lightCoords[i, 0].X + (bumpID * ctx.BumpSTexCoordOffset);
						lightCoords[i, bumpID].Y = lightCoords[i, 0].Y;
					}
				}
			}
		}
	}

	private void BuildMSurfaceVertexArrays(WorldBrushData brushData, ref BSPMSurface2 surfID, float overbright, ref MeshBuilder builder) {
		SurfaceCtx ctx = default;
		SurfSetupSurfaceContext(ref ctx, ref surfID);

		Color flatColor = new(255, 255, 255, 255);

		Vector3 vect = default;
		bool negate = false;
		if ((ModelLoader.MSurf_Flags(ref surfID) & SurfDraw.TangentSpace) != 0)
			negate = TangentSpaceSurfaceSetup(ref surfID, out vect);

		CheckMSurfaceBaseTexture2(brushData, ref surfID);
		for (int i = 0; i < ModelLoader.MSurf_VertCount(ref surfID); i++) {
			int vertIndex = brushData.VertIndices![ModelLoader.MSurf_FirstVertIndex(ref surfID) + i];

			ref Vector3 vec = ref brushData.Vertexes![vertIndex].Position;
			builder.Position3fv(vec);

			Vector2 uv = default;

			SurfComputeTextureCoordinate(ref ctx, ref surfID, ref vec, ref uv);
			builder.TexCoord2fv(0, uv);

			SurfComputeLightmapCoordinate(ref ctx, ref surfID, ref vec, ref uv);
			builder.TexCoord2fv(1, uv);

			if ((ModelLoader.MSurf_Flags(ref surfID) & SurfDraw.BumpLight) != 0) {
				if (uv.X + ctx.BumpSTexCoordOffset * 3 > 1.00001f) {
					Assert(false);

					SurfComputeLightmapCoordinate(ref ctx, ref surfID, ref vec, ref uv);
				}
				builder.TexCoord2f(2, ctx.BumpSTexCoordOffset, 0.0f);
			}

			ref Vector3 normal = ref brushData.VertNormals![brushData.VertNormalIndices![ModelLoader.MSurf_FirstVertNormal(ref surfID) + i]];
			builder.Normal3fv(normal);
			if ((ModelLoader.MSurf_Flags(ref surfID) & SurfDraw.TangentSpace) != 0) {
				TangentSpaceComputeBasis(out Vector3 tangentS, out Vector3 tangentT, normal, ref vect, negate);
				builder.TangentS3fv(tangentS);
				builder.TangentT3fv(tangentT);
			}

			if (!ModelLoader.SurfaceHasDispInfo(ref surfID) && (ModelLoader.MSurf_TexInfo(ref surfID).TexInfoFlags & TEXINFO_USING_BASETEXTURE2) != 0) {
				bool warned = false;
				if (!warned) {
					ReadOnlySpan<char> materialName = ModelLoader.MSurf_TexInfo(ref surfID).Material!.GetName();
					warned = true;
					Warning($"Warning: WorldTwoTextureBlend found on a non-displacement surface (material: {materialName}). This wastes perf for no benefit.\n");
				}

				builder.Color4ub(255, 255, 255, 0);
			}
			else {
				builder.Color3ubv(flatColor);
			}

			builder.AdvanceVertex();
		}

	}

	private static bool TangentSpaceSurfaceSetup(ref BSPMSurface2 surfID, out Vector3 tVect) {
		MathLib.VectorCopy(ModelLoader.MSurf_TexInfo(ref surfID).TextureVecsTexelsPerWorldUnits[0].AsVector3D(), out Vector3 sVect);
		MathLib.VectorCopy(ModelLoader.MSurf_TexInfo(ref surfID).TextureVecsTexelsPerWorldUnits[1].AsVector3D(), out tVect);
		MathLib.VectorNormalize(ref sVect);
		MathLib.VectorNormalize(ref tVect);
		MathLib.CrossProduct(sVect, tVect, out Vector3 tmpVect);
		if (MathLib.DotProduct(ModelLoader.MSurf_Plane(ref surfID).Normal, tmpVect) > 0.0f)
			return true;
		return false;
	}

	private static void TangentSpaceComputeBasis(out Vector3 tangentS, out Vector3 tangentT, Vector3 normal, ref Vector3 vect, bool negate) {
		MathLib.CrossProduct(normal, vect, out tangentS);
		MathLib.VectorNormalize(ref tangentS);
		MathLib.CrossProduct(tangentS, normal, out tangentT);
		MathLib.VectorNormalize(ref tangentT);

		if (negate)
			MathLib.VectorScale(tangentS, -1.0f, out tangentS);
	}

	private bool CheckMSurfaceBaseTexture2(WorldBrushData brushData, ref BSPMSurface2 surfID) {
		if (!ModelLoader.SurfaceHasDispInfo(ref surfID) && 0 != (ModelLoader.MSurf_TexInfo(ref surfID).TexInfoFlags & TEXINFO_USING_BASETEXTURE2)) {
			ReadOnlySpan<char> materialName = ModelLoader.MSurf_TexInfo(ref surfID).Material!.GetName();
			if (!materialName.IsEmpty) {
				// Calculate the surface's centerpoint.
				Vector3 vCenter = new(0, 0, 0);
				for (int i = 0; i < ModelLoader.MSurf_VertCount(ref surfID); i++) {
					int vertIndex = brushData.VertIndices![ModelLoader.MSurf_FirstVertIndex(ref surfID) + i];
					vCenter += brushData.Vertexes![vertIndex].Position;
				}
				vCenter /= (float)ModelLoader.MSurf_VertCount(ref surfID);

				// Spit out the warning.				
				Warning("Warning: using WorldTwoTextureBlend on a non-displacement surface.\n" +
						 "Support for this will go away soon.\n" +
						 $"   - Material       : {materialName}\n" +
						 $"   - Surface center : {(int)vCenter.X} {(int)vCenter.Y} {(int)vCenter.Z}\n"
						 );
			}
			return true;
		}
		else {
			return false;
		}
	}

	internal MaterialSystem_SortInfo[]? MaterialSortInfoArray;
	private int SortInfoToLightmapPage(int sortID) => MaterialSortInfoArray![sortID].LightmapPageID;

	internal void SurfSetupSurfaceContext(ref SurfaceCtx ctx, ref BSPMSurface2 surfID) {
		materials.GetLightmapPageSize(SortInfoToLightmapPage(ModelLoader.MSurf_MaterialSortID(ref surfID)), out ctx.LightmapPageSize[0], out ctx.LightmapPageSize[1]);
		ctx.LightmapSize[0] = ModelLoader.MSurf_LightmapExtents(ref surfID)[0] + 1;
		ctx.LightmapSize[1] = ModelLoader.MSurf_LightmapExtents(ref surfID)[1] + 1;

		ctx.Scale.X = 1.0f / ctx.LightmapPageSize[0];
		ctx.Scale.Y = 1.0f / ctx.LightmapPageSize[1];

		ctx.Offset.X = (float)ModelLoader.MSurf_OffsetIntoLightmapPage(ref surfID)[0] * ctx.Scale.X;
		ctx.Offset.Y = (float)ModelLoader.MSurf_OffsetIntoLightmapPage(ref surfID)[1] * ctx.Scale.Y;

		if (ctx.LightmapPageSize[0] != 0.0f)
			ctx.BumpSTexCoordOffset = (float)ctx.LightmapSize[0] / ctx.LightmapPageSize[0];
		else
			ctx.BumpSTexCoordOffset = 0.0f;
	}

	internal void SurfComputeLightmapCoordinate(ref SurfaceCtx ctx, ref BSPMSurface2 surfID, ref Vector3 vec, ref Vector2 uv) {
		if ((ModelLoader.MSurf_Flags(ref surfID) & SurfDraw.NoLight) != 0)
			uv.X = uv.Y = 0.5f;

		else if (ModelLoader.MSurf_LightmapExtents(ref surfID)[0] == 0) {
			uv = (0.5f * ctx.Scale + ctx.Offset);
		}
		else {
			ref ModelTexInfo texInfo = ref ModelLoader.MSurf_TexInfo(ref surfID);

			uv.X = Vector3.Dot(vec, texInfo.LightmapVecsLuxelsPerWorldUnits[0].AsVector3()) + texInfo.LightmapVecsLuxelsPerWorldUnits[0][3];
			uv.X -= ModelLoader.MSurf_LightmapMins(ref surfID)[0];
			uv.X += 0.5f;

			uv.Y = Vector3.Dot(vec, texInfo.LightmapVecsLuxelsPerWorldUnits[1].AsVector3()) + texInfo.LightmapVecsLuxelsPerWorldUnits[1][3];
			uv.Y -= ModelLoader.MSurf_LightmapMins(ref surfID)[1];
			uv.Y += 0.5f;

			uv *= ctx.Scale;
			uv += ctx.Offset;

			Assert(uv.IsValid());
		}
		uv.X = Math.Clamp(uv.X, 0.0f, 1.0f);
		uv.Y = Math.Clamp(uv.Y, 0.0f, 1.0f);
	}

	public void SurfComputeTextureCoordinate(ref SurfaceCtx ctx, ref BSPMSurface2 surfID, ref Vector3 vec, ref Vector2 uv) {
		ref ModelTexInfo texInfo = ref ModelLoader.MSurf_TexInfo(ref surfID);

		// base texture coordinate
		uv.X = Vector3.Dot(vec, texInfo.TextureVecsTexelsPerWorldUnits[0].AsVector3()) + texInfo.TextureVecsTexelsPerWorldUnits[0][3];
		uv.X /= texInfo.Material!.GetMappingWidth();

		uv.Y = Vector3.Dot(vec, texInfo.TextureVecsTexelsPerWorldUnits[1].AsVector3()) + texInfo.TextureVecsTexelsPerWorldUnits[1][3];
		uv.Y /= texInfo.Material!.GetMappingHeight();
	}

	public static int CompareSurfID(ref BSPMSurface2 surfID1, ref BSPMSurface2 surfID2) {
		bool hasLightmap1 = (ModelLoader.MSurf_Flags(ref surfID1) & SurfDraw.NoLight) == 0;
		bool hasLightmap2 = (ModelLoader.MSurf_Flags(ref surfID2) & SurfDraw.NoLight) == 0;

		if (hasLightmap1 != hasLightmap2)
			return hasLightmap2.CompareTo(hasLightmap1);

		int enum1 = ModelLoader.MSurf_TexInfo(ref surfID1).Material!.GetEnumerationID();
		int enum2 = ModelLoader.MSurf_TexInfo(ref surfID2).Material!.GetEnumerationID();

		if (enum1 != enum2)
			return enum1.CompareTo(enum2);

		bool hasLightstyle1 = (ModelLoader.MSurf_Flags(ref surfID1) & SurfDraw.HasLightStyles) == 0;
		bool hasLightstyle2 = (ModelLoader.MSurf_Flags(ref surfID2) & SurfDraw.HasLightStyles) == 0;

		if (hasLightstyle1 != hasLightstyle2)
			return hasLightstyle2.CompareTo(hasLightstyle1);

		int area1 = ModelLoader.MSurf_LightmapExtents(ref surfID1)[0] * ModelLoader.MSurf_LightmapExtents(ref surfID1)[1];
		int area2 = ModelLoader.MSurf_LightmapExtents(ref surfID2)[0] * ModelLoader.MSurf_LightmapExtents(ref surfID2)[1];

		return area2.CompareTo(area1);
	}

	class RBComparer(BSPMSurface2[] surfaces) : IComparer<nint>
	{
		public int Compare(nint sn1, nint sn2) {
			ref BSPMSurface2 surfID1 = ref surfaces[sn1];
			ref BSPMSurface2 surfID2 = ref surfaces[sn2];
			return CompareSurfID(ref surfID1, ref surfID2);
		}
	}


	public const int NUM_BUMP_VECTS = 3;
	public static bool SurfNeedsBumpedLightmaps(ref BSPMSurface2 surfID) => ModelLoader.MSurf_TexInfo(ref surfID).Material!.GetPropertyFlag(MaterialPropertyTypes.NeedsBumpedLightmaps);
	public static bool SurfNeedsLightmap(ref BSPMSurface2 surfID) => ModelLoader.MSurf_TexInfo(ref surfID).Material!.GetPropertyFlag(MaterialPropertyTypes.NeedsLightmap);
	private void RegisterUnlightmappedSurface(ref BSPMSurface2 surfID) {
		ModelLoader.MSurf_MaterialSortID(ref surfID) = materials.AllocateWhiteLightmap(ModelLoader.MSurf_TexInfo(ref surfID).Material);
		ModelLoader.MSurf_OffsetIntoLightmapPage(ref surfID)[0] = 0;
		ModelLoader.MSurf_OffsetIntoLightmapPage(ref surfID)[1] = 0;
	}
	private void RegisterLightmappedSurface(ref BSPMSurface2 surfID) {
		Span<int> lightmapSize = stackalloc int[2];
		int allocationWidth, allocationHeight;
		bool needsBumpmap;

		lightmapSize[0] = ModelLoader.MSurf_LightmapExtents(ref surfID)[0] + 1;
		lightmapSize[1] = ModelLoader.MSurf_LightmapExtents(ref surfID)[1] + 1;

		needsBumpmap = SurfNeedsBumpedLightmaps(ref surfID);
		if (needsBumpmap) {
			ModelLoader.MSurf_Flags(ref surfID) |= SurfDraw.BumpLight;
			allocationWidth = lightmapSize[0] * (NUM_BUMP_VECTS + 1);
		}
		else {
			ModelLoader.MSurf_Flags(ref surfID) &= ~SurfDraw.BumpLight;
			allocationWidth = lightmapSize[0];
		}

		allocationHeight = lightmapSize[1];

		Span<int> offsetIntoLightmapPage = stackalloc int[2];
		ModelLoader.MSurf_MaterialSortID(ref surfID) = materials.AllocateLightmap(
			allocationWidth,
			allocationHeight,
			offsetIntoLightmapPage,
			ModelLoader.MSurf_TexInfo(ref surfID).Material);

		ModelLoader.MSurf_OffsetIntoLightmapPage(ref surfID)[0] = (short)offsetIntoLightmapPage[0];
		ModelLoader.MSurf_OffsetIntoLightmapPage(ref surfID)[1] = (short)offsetIntoLightmapPage[1];
	}
	internal void RegisterLightmapSurfaces() {
		if (host_state.WorldBrush == null || host_state.WorldModel == null)
			return;

		ref BSPMSurface2 surfID = ref Unsafe.NullRef<BSPMSurface2>();
		materials.BeginLightmapAllocation();

		RedBlackTree<nint> surfaces = new(new RBComparer(host_state.WorldBrush!.Surfaces2!));
		for (int surfaceIndex = 0; surfaceIndex < host_state.WorldBrush!.NumSurfaces; surfaceIndex++) {
			surfID = ref ModelLoader.SurfaceHandleFromIndex(surfaceIndex, host_state.WorldBrush);
			if ((ModelLoader.MSurf_TexInfo(ref surfID).Flags & Surf.NoLight) != 0 ||
				(ModelLoader.MSurf_Flags(ref surfID) & SurfDraw.NoLight) != 0) {
				ModelLoader.MSurf_Flags(ref surfID) |= SurfDraw.NoLight;
			}
			else
				ModelLoader.MSurf_Flags(ref surfID) &= ~SurfDraw.NoLight;

			surfaces.Insert(surfID.SurfNum);
		}

		foreach (var surfIDidx in surfaces.InOrderTraverse()) {
			surfID = ref host_state.WorldBrush!.Surfaces2![surfIDidx];
			// Msg($"Surf ID #{surfIDAddP++} == {surfIDidx}\n");
			bool hasLightmap = (ModelLoader.MSurf_Flags(ref surfID) & SurfDraw.NoLight) == 0;
			if (hasLightmap)
				RegisterLightmappedSurface(ref surfID);
			else
				RegisterUnlightmappedSurface(ref surfID);
		}

		materials.EndLightmapAllocation();
	}
	private int FindOrAddMesh(IMaterial? material, int sortID, int vertexCount, int indexCount) {
		VertexFormat format = material.GetVertexFormat();

		using MatRenderContextPtr renderContext = new(materials);

		int maxVertices = renderContext.GetMaxVerticesToRender(material);
		int maxIndices = renderContext.GetMaxIndicesToRender();

		int worldLimit = mat_max_worldmesh_vertices.GetInt();
		worldLimit = Math.Max(worldLimit, 1024);
		if (maxVertices > worldLimit)
			maxVertices = mat_max_worldmesh_vertices.GetInt();

		Span<MeshList> meshes = Meshes.AsSpan();

		int lightmapID = SortInfoToLightmapPage(sortID);

		for (int i = 0; i < meshes.Length; i++) {
			if (meshes[i].Material != material)
				continue;

			if (meshes[i].LightmapPageID != lightmapID)
				continue;

			if (meshes[i].VertCount + vertexCount > maxVertices)
				continue;

			if (meshes[i].IndexCount + indexCount > maxIndices)
				continue;


			meshes[i].VertCount += vertexCount;
			meshes[i].IndexCount += indexCount;
			return i;
		}

		Meshes.Add(new() {
			VertCount = vertexCount,
			IndexCount = indexCount,
			VertexFormat = format,
			Material = material,
			LightmapPageID = lightmapID
		});

		return Meshes.Count - 1;
	}

	public void WorldStaticMeshDestroy() {

	}

	public ConVar mat_loadtextures = new("1", 0);
	public IMaterial MaterialEmpty;

	public IMaterial GL_LoadMaterial(ReadOnlySpan<char> name, ReadOnlySpan<char> textureGroupName) {
		IMaterial? material = GL_LoadMaterialNoRef(name, textureGroupName);
		return material;
	}

	private IMaterial GL_LoadMaterialNoRef(ReadOnlySpan<char> name, ReadOnlySpan<char> textureGroupName) {
		if (mat_loadtextures.GetInt() != 0)
			return materials.FindMaterial(name, textureGroupName);
		else
			return MaterialEmpty;
	}

	internal void DestroySortInfo() {

	}

	internal void CreateSortInfo() {
		Assert(MaterialSortInfoArray == null);
		int sortIDs = materials.GetNumSortIDs();
		MaterialSortInfoArray = new MaterialSystem_SortInfo[sortIDs];
		materials.GetSortInfo(MaterialSortInfoArray);
		GenerateTexCoordsForPrimVerts();
		WorldStaticMeshCreate();
	}

	private void GenerateTexCoordsForPrimVerts() {
		WorldBrushData bsp = host_state.WorldBrush!;

		Span<int> lightmapSize = stackalloc int[2];
		Span<int> lightmapPageSize = stackalloc int[2];

		for (int surfaceIndex = 0, count = bsp.NumSurfaces; surfaceIndex < count; surfaceIndex++) {
			ref BSPMSurface2 surfID = ref ModelLoader.SurfaceHandleFromIndex(surfaceIndex, bsp);

			for (int j = 0; j < ModelLoader.MSurf_NumPrims(ref surfID, bsp); j++) {
				ref BSPMPrimitive prim = ref Unsafe.NullRef<BSPMPrimitive>();
				Assert(ModelLoader.MSurf_FirstPrimID(ref surfID, bsp) + j < bsp.NumPrimitives);
				prim = ref bsp.Primitives![ModelLoader.MSurf_FirstPrimID(ref surfID, bsp) + j];
				for (int k = 0; k < prim.VertCount; k++) {
					float sOffset, sScale, tOffset, tScale;

					materials.GetLightmapPageSize(SortInfoToLightmapPage(ModelLoader.MSurf_MaterialSortID(ref surfID)), out lightmapPageSize[0], out lightmapPageSize[1]);
					lightmapSize[0] = (ModelLoader.MSurf_LightmapExtents(ref surfID, bsp)[0]) + 1;
					lightmapSize[1] = (ModelLoader.MSurf_LightmapExtents(ref surfID, bsp)[1]) + 1;

					sScale = 1.0f / (float)lightmapPageSize[0];
					sOffset = (float)ModelLoader.MSurf_OffsetIntoLightmapPage(ref surfID)[0] * sScale;
					sScale = ModelLoader.MSurf_LightmapExtents(ref surfID)[0] * sScale;

					tScale = 1.0f / (float)lightmapPageSize[1];
					tOffset = (float)ModelLoader.MSurf_OffsetIntoLightmapPage(ref surfID)[1] * tScale;
					tScale = ModelLoader.MSurf_LightmapExtents(ref surfID)[1] * tScale;

					for (int l = 0; l < prim.VertCount; l++) {
						Assert(l + prim.FirstVert < bsp.NumPrimVerts);
						ref BSPMPrimVert vert = ref bsp.PrimVerts![l + prim.FirstVert];
						ref Vector3 vec = ref vert.Position;

						vert.TexCoord[0] = Vector3.Dot(vec, ModelLoader.MSurf_TexInfo(ref surfID, bsp).TextureVecsTexelsPerWorldUnits[0].AsVector3()) + ModelLoader.MSurf_TexInfo(ref surfID, bsp).TextureVecsTexelsPerWorldUnits[0][3];
						vert.TexCoord[0] /= ModelLoader.MSurf_TexInfo(ref surfID, bsp).Material!.GetMappingWidth();

						vert.TexCoord[1] = Vector3.Dot(vec, ModelLoader.MSurf_TexInfo(ref surfID).TextureVecsTexelsPerWorldUnits[1].AsVector3()) + ModelLoader.MSurf_TexInfo(ref surfID, bsp).TextureVecsTexelsPerWorldUnits[1][3];
						vert.TexCoord[1] /= ModelLoader.MSurf_TexInfo(ref surfID, bsp).Material!.GetMappingHeight();

						if ((ModelLoader.MSurf_Flags(ref surfID) & SurfDraw.NoLight) != 0) {
							vert.LightCoord[0] = 0.5f;
							vert.LightCoord[1] = 0.5f;
						}
						else if (ModelLoader.MSurf_LightmapExtents(ref surfID, bsp)[0] == 0) {
							vert.LightCoord[0] = sOffset;
							vert.LightCoord[1] = tOffset;
						}
						else {
							vert.LightCoord[0] = Vector3.Dot(vec, ModelLoader.MSurf_TexInfo(ref surfID, bsp).LightmapVecsLuxelsPerWorldUnits[0].AsVector3()) + ModelLoader.MSurf_TexInfo(ref surfID, bsp).LightmapVecsLuxelsPerWorldUnits[0][3];
							vert.LightCoord[0] -= ModelLoader.MSurf_LightmapMins(ref surfID, bsp)[0];
							vert.LightCoord[0] += 0.5f;
							vert.LightCoord[0] /= (float)ModelLoader.MSurf_LightmapExtents(ref surfID, bsp)[0];

							vert.LightCoord[1] = Vector3.Dot(vec, ModelLoader.MSurf_TexInfo(ref surfID, bsp).LightmapVecsLuxelsPerWorldUnits[1].AsVector3()) + ModelLoader.MSurf_TexInfo(ref surfID, bsp).LightmapVecsLuxelsPerWorldUnits[1][3];
							vert.LightCoord[1] -= ModelLoader.MSurf_LightmapMins(ref surfID, bsp)[1];
							vert.LightCoord[1] += 0.5f;
							vert.LightCoord[1] /= (float)ModelLoader.MSurf_LightmapExtents(ref surfID, bsp)[1];

							vert.LightCoord[0] = sOffset + vert.LightCoord[0] * sScale;
							vert.LightCoord[1] = tOffset + vert.LightCoord[1] * tScale;
						}
					}
				}
			}
		}
	}

	internal float GetScreenAspect() {
		// r_aspectratio todo
		IMatRenderContext renderContext = materials.GetRenderContext();

		renderContext.GetRenderTargetDimensions(out int width, out int height);
		return (height != 0) ? ((float)width / (float)height) : 1.0f;
	}

#if !SWDS
	[ConCommand("mat_setvideomode", "sets the width, height, windowed state of the material system")]
	static void mat_setvideomode(in TokenizedCommand args) {
		if (args.ArgC() < 4 || args.ArgC() > 5)
			return;

		int width = args.Arg(1, 0);
		int height = args.Arg(2, 0);
		bool windowed = args.Arg(3, 0) > 0;
		bool borderless = args.ArgC() == 5 && args.Arg(4, 0) > 0;

		Singleton<IVideoMode>().SetMode(new(width, height, windowed, borderless));
	}
#endif

	[ConCommand("mat_savechanges", "saves current video configuration to the registry")]
	static void mat_savechanges(in TokenizedCommand args) {
		commandLine.RemoveParm("-safe");
		UpdateMaterialSystemConfig();
		WriteMaterialSystemConfigToRegistry(MaterialSystemConfig);
	}

	static void UpdateMaterialSystemConfig() {
		// if (host_state.worldbrush && !host_state.worldbrush->lightdata) {
		// 	mat_fullbright.SetValue(1);
		// }

		bool lightmapsNeedReloading = materialSystem.UpdateConfig(false);
		if (lightmapsNeedReloading) {

		}
	}

	public static MaterialSystem_Config MaterialSystemConfig;
	public void InitMaterialSystemConfig(bool inEditMode) {
		MaterialSystemConfig = materials.GetCurrentConfigForVideoCard();

		if (inEditMode)
			return;

		MaterialSystem_Config config = MaterialSystemConfig;

#if !SWDS
		ReadMaterialSystemConfigFromRegistry(config);
#endif

		OverrideMaterialSystemConfigFromCommandLine(config);
		OverrideMaterialSystemConfig(config);

		WriteMaterialSystemConfigToRegistry(MaterialSystemConfig);

		materials.UpdateConfig(false);
	}

	static string[] RegistryConVars = [
		"mat_forceaniso",
		"mat_picmip",
		"mat_trilinear",
		"mat_vsync",
		"mat_forcehardwaresync",
		"mat_parallaxmap",
		"mat_reducefillrate",
		"r_shadowrendertotexture",
		"r_rootlod",
		"r_waterforceexpensive",
		"r_waterforcereflectentities",
		"mat_antialias",
		"mat_aaquality",
		"mat_specular",
		"mat_bumpmap",
		"mat_hdr_level",
		"mat_colorcorrection",
	];

	private static void WriteMaterialSystemConfigToRegistry(MaterialSystem_Config config) {
#if !SWDS
		WriteVideoConfigInt("ScreenWidth", config.VideoMode.Width);
		WriteVideoConfigInt("ScreenHeight", config.VideoMode.Height);
		WriteVideoConfigInt("ScreenWindowed", config.Windowed() ? 1 : 0);
		WriteVideoConfigInt("ScreenNoBorder", config.NoWindowBorder() ? 1 : 0);
		WriteVideoConfigInt("ScreenMSAA", config.AASamples);
		WriteVideoConfigInt("ScreenMSAAQuality", config.AAQuality);
		WriteVideoConfigInt("MotionBlur", config.MotionBlur ? 1 : 0);
		WriteVideoConfigInt("ShadowDepthTexture", config.ShadowDepthTexture ? 1 : 0);
		// WriteVideoConfigInt("VRModeAdapter", config.VRModeAdapter);

		// WriteVideoConfigString("ScreenMonitorGamma", mat_monitorgamma.GetString());

		foreach (string cvar in RegistryConVars) {
			ConVarRef var = new(cvar);

			if (!var.IsValid())
				continue;

			WriteVideoConfigInt(cvar, var.GetInt());
		}
#endif
	}


	private static void ReadMaterialSystemConfigFromRegistry(MaterialSystem_Config config) {
#if !SWDS
		ReadVideoConfigInt("ScreenWidth", ref config.VideoMode.Width);
		ReadVideoConfigInt("ScreenHeight", ref config.VideoMode.Height);
		config.SetFlag(MaterialSystem_Config_Flags.Windowed, ReadVideoConfigInt("ScreenWindowed", 0) != 0);
		config.SetFlag(MaterialSystem_Config_Flags.NoWindowBorder, ReadVideoConfigInt("ScreenNoBorder", 0) != 0);

		ReadOnlySpan<char> szMonitorGamma = ReadVideoConfigString("ScreenMonitorGamma", "2.2");
		if (!szMonitorGamma.IsEmpty) {
			// float monitorGamma = strtof(szMonitorGamma, nullptr);
			// if (monitorGamma > 3.0f)
			// 	monitorGamma = 2.2f;

			// monitorGamma = OverrideVideoConfigFromCommandLine("mat_monitorgamma", monitorGamma);

			// mat_monitorgamma.SetValue(monitorGamma);
			// config.MonitorGamma = mat_monitorgamma.GetFloat();
		}

		foreach (string cvar in RegistryConVars) {
			int value = ReadVideoConfigInt(cvar, -1);
			if (value == -1)
				continue;

			ConVarRef var = new(cvar);
			if (var.IsValid())
				var.SetValue(value);
		}

		int val = ReadVideoConfigInt("DXLevel_V1", -1);
		if (val != -1) {
			val = OverrideVideoConfigFromCommandLine("mat_dxlevel", val);

			ConVarRef conVar = new("mat_dxlevel");
			if (conVar.IsValid())
				conVar.SetValue(val);
		}

		val = ReadVideoConfigInt("MotionBlur", -1);
		if (val != -1) {
			val = OverrideVideoConfigFromCommandLine("mat_motion_blur_enabled", val);

			ConVarRef conVar = new("mat_motion_blur_enabled");
			if (conVar.IsValid()) {
				conVar.SetValue(val);
				config.MotionBlur = ReadVideoConfigInt("MotionBlur", 0) != 0;
			}
		}

		val = ReadVideoConfigInt("ShadowDepthTexture", -1);
		if (val != -1) {
			val = OverrideVideoConfigFromCommandLine("r_flashlightdepthtexture", val);

			ConVarRef conVar = new("r_flashlightdepthtexture");
			if (conVar.IsValid()) {
				conVar.SetValue(val);
				config.ShadowDepthTexture = ReadVideoConfigInt("ShadowDepthTexture", 0) != 0;
			}
		}
#endif
	}

	private static bool VideoConfigOverriddenFromCmdLine = false;

	private static int OverrideVideoConfigFromCommandLine(string cvarname, int curVal) {
		string szOption = $"+{cvarname}";
		if (commandLine.CheckParm(szOption)) {
			int newVal = commandLine.ParmValue(szOption, curVal);
			Warning($"Video configuration ignoring {cvarname} due to command line override\n");
			VideoConfigOverriddenFromCmdLine = true;
			return newVal;
		}
		return curVal;
	}

	public void OverrideMaterialSystemConfigFromCommandLine(MaterialSystem_Config config) {
		if (commandLine.FindParm("-sw") != 0 || commandLine.FindParm("-startwindowed") != 0 || commandLine.FindParm("-windowed") != 0 || commandLine.FindParm("-window") != 0)
			config.SetFlag(MaterialSystem_Config_Flags.Windowed, true);
		else if (commandLine.FindParm("-full") != 0 || commandLine.FindParm("-fullscreen") != 0)
			config.SetFlag(MaterialSystem_Config_Flags.Windowed, false);

		if (commandLine.FindParm("-noborder") != 0)
			config.SetFlag(MaterialSystem_Config_Flags.NoWindowBorder, true);

		if (commandLine.FindParm("-width") != 0 || commandLine.FindParm("-w") != 0) {
			config.VideoMode.Width = commandLine.ParmValue("-width", config.VideoMode.Width);
			config.VideoMode.Width = commandLine.ParmValue("-w", config.VideoMode.Width);

			if (!(commandLine.FindParm("-height") != 0 || commandLine.FindParm("-h") != 0))
				config.VideoMode.Height = config.VideoMode.Width * 3 / 4;
		}

		if (commandLine.FindParm("-height") != 0 || commandLine.FindParm("-h") != 0) {
			config.VideoMode.Height = commandLine.ParmValue("-height", config.VideoMode.Height);
			config.VideoMode.Height = commandLine.ParmValue("-h", config.VideoMode.Height);
		}

		if (commandLine.FindParm("-resizing") != 0)
			config.SetFlag(MaterialSystem_Config_Flags.Resizing, commandLine.CheckParm("-resizing") ? true : false);

		if (commandLine.FindParm("-mat_vsync") != 0)
			config.SetFlag(MaterialSystem_Config_Flags.NoWaitForVSync, commandLine.ParmValue("-mat_vsync", 1) == 0);

		config.AASamples = commandLine.ParmValue("-mat_antialias", config.AASamples);
		config.AAQuality = commandLine.ParmValue("-mat_aaquality", config.AAQuality);

		// Clamp the requested dimensions to the display resolution
		// TODO GetDisplayMode
		// MaterialVideoMode videoMode = default;
		// materials.GetDisplayMode(videoMode);
		// config.VideoMode.Width = Math.Min(videoMode.Width, config.VideoMode.Width);
		// config.VideoMode.Height = Math.Min(videoMode.Height, config.VideoMode.Height);

		// safe mode
		if (commandLine.FindParm("-safe") != 0) {
			config.SetFlag(MaterialSystem_Config_Flags.Windowed, true);
			config.VideoMode.Width = 640;//BASE_WIDTH;
			config.VideoMode.Height = 480;//BASE_HEIGHT;
			config.VideoMode.RefreshRate = 0;
			config.AASamples = 0;
			config.AAQuality = 0;
		}
	}

	public void OverrideMaterialSystemConfig(MaterialSystem_Config config) {
		bool lightmapsNeedReloading = materials.OverrideConfig(config, false);
		if (lightmapsNeedReloading) {

		}
	}

#if OSX
	const string MOD_VIDEO_CONFIG_SETTINGS = "videoconfig_mac.cfg";
	const bool USE_VIDEOCONFIG_FILE = true;
#elif LINUX
	const string MOD_VIDEO_CONFIG_SETTINGS = "videoconfig_linux.cfg";
	const bool USE_VIDEOCONFIG_FILE = true;
#elif WIN32
	const string MOD_VIDEO_CONFIG_SETTINGS = "videoconfig.cfg";
	const bool USE_VIDEOCONFIG_FILE = false;
#endif

#pragma warning disable CS0162 // Unreachable code
	static int ReadVideoConfigInt(ReadOnlySpan<char> name, int fallback) {
		if (USE_VIDEOCONFIG_FILE) {
			KeyValues videoConfig = new("videoconfig");
			bool exists = videoConfig.LoadFromFile(g_pFileSystem, MOD_VIDEO_CONFIG_SETTINGS, "MOD");

			if (!exists)
				return fallback;

			return videoConfig.GetInt(name, fallback);
		}
		else
			return registry.ReadInt(name, fallback);
	}

	static void ReadVideoConfigInt(ReadOnlySpan<char> name, ref int entry) {
		int value = ReadVideoConfigInt(name, -1);
		if (value != -1)
			entry = value;
	}

	static ReadOnlySpan<char> ReadVideoConfigString(ReadOnlySpan<char> name, ReadOnlySpan<char> fallback) {
		if (USE_VIDEOCONFIG_FILE) {
			KeyValues videoConfig = new("videoconfig");
			bool exists = videoConfig.LoadFromFile(g_pFileSystem, MOD_VIDEO_CONFIG_SETTINGS, "MOD");

			if (!exists)
				return fallback;

			return videoConfig.GetString(name, fallback);
		}
		else
			return registry.ReadString(name, fallback);
	}

	static void WriteVideoConfigInt(ReadOnlySpan<char> name, int value) {
		if (USE_VIDEOCONFIG_FILE) {
			KeyValues videoConfig = new("videoconfig");
			videoConfig.LoadFromFile(g_pFileSystem, MOD_VIDEO_CONFIG_SETTINGS, "MOD");
			videoConfig.SetInt(name, value);
			// videoConfig.SaveToFile(g_pFileSystem, MOD_VIDEO_CONFIG_SETTINGS, "MOD", false, false, true); TODO!!!!!
		}
		else
			registry.WriteInt(name, value);
	}

	static void WriteVideoConfigString(ReadOnlySpan<char> name, ReadOnlySpan<char> value) {
		if (USE_VIDEOCONFIG_FILE) {
			KeyValues videoConfig = new("videoconfig");
			videoConfig.LoadFromFile(g_pFileSystem, MOD_VIDEO_CONFIG_SETTINGS, "MOD");
			videoConfig.SetString(name, value);
			// videoConfig.SaveToFile(g_pFileSystem, MOD_VIDEO_CONFIG_SETTINGS, "MOD", false, false, true); TODO!!!!!
		}
		else
			registry.WriteString(name, value);
	}
#pragma warning restore CS0162
}

