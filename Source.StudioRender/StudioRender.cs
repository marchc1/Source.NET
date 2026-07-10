using Source.Common;
using Source.Common.DataCache;
using Source.Common.Engine;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;

using System;
using System.Buffers;
using System.Drawing.Drawing2D;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Source.StudioRender;

public struct BodyPartInfo
{
	public int SubModelIndex;
	public MStudioModel? SubModel;
}
public enum StudioModelLighting
{
	Hardware,
	Software,
	Mouth
}

public struct LightPos
{
	public Vector3 Delta;
	public float Falloff;
	public float Dot;
}

[EngineComponent]
public unsafe class StudioRender
{
	IMaterialSystem materialSystem = Singleton<IMaterialSystem>();
	IStudioDataCache studioDataCache = Singleton<IStudioDataCache>();

	StudioRenderCtx? pRC;
	Matrix3x4* pBoneToWorld;
	int nBoneToWorld;
	StudioHeader? StudioHdr;
	StudioMeshData[]? StudioMeshes;

	public readonly Matrix3x4[] PoseToWorld = new Matrix3x4[Studio.MAXSTUDIOBONES];
	public readonly Matrix3x4[] PoseToDecal = new Matrix3x4[Studio.MAXSTUDIOBONES];

	internal const int MAXLOCALLIGHTS = 4;

	internal void DrawModel(ref DrawModelInfo info, StudioRenderCtx RC, Span<Matrix3x4> boneToWorld, StudioRenderFlags flags) {
		// TODO: a better way to do this that doesnt require unsafe
		// TODO: flex
		nBoneToWorld = boneToWorld.Length;
		fixed (Matrix3x4* pBtW = boneToWorld) {
			pRC = RC;
			pBoneToWorld = pBtW;

			using MatRenderContextPtr pRenderContext = new(materialSystem);

			// TODO: Disable flex if we're told to...
			// TODO: Enable wireframe if we're told to...
			int boneMask = Studio.BONE_USED_BY_VERTEX_AT_LOD(info.Lod);

			// Preserve the matrices if we're skinning
			pRenderContext.MatrixMode(MaterialMatrixMode.Model);
			pRenderContext.PushMatrix();
			pRenderContext.LoadIdentity();

			StudioHdr = info.StudioHdr;
			if (info.HardwareData.LODs == null) {
				Msg($"Missing LODs for {StudioHdr.GetName()}, lod index is {info.Lod}.\n");
				return;
			}
			StudioMeshes = info.HardwareData.LODs[info.Lod].MeshData;

			// Bone to world must be set before calling drawmodel; it uses that here
			ComputePoseToWorld(PoseToWorld, StudioHdr, boneMask, in pRC.ViewOrigin, pBoneToWorld);

			R_StudioRenderModel(pRenderContext, info.Skin, info.Body, info.HitboxSet, info.ClientEntity,
				info.HardwareData.LODs[info.Lod].Materials,
				info.HardwareData.LODs[info.Lod].MaterialFlags, flags, boneMask, info.Lod, info.ColorMeshes);

			// TODO: decals

			// Restore the matrices if we're skinning
			pRenderContext.MatrixMode(MaterialMatrixMode.Model);
			pRenderContext.PopMatrix();

			// TODO: Restore the configs


			pRenderContext.SetNumBoneWeights(0);
			pRC = null;
			StudioMeshes = null;
			StudioHdr = null;
			pBoneToWorld = null;
		}
	}

	private void ComputePoseToWorld(Span<Matrix3x4> poseToWorld, StudioHeader studioHdr, int boneMask, in Vector3 viewOrigin, Matrix3x4* pBoneToWorld) {
		if ((studioHdr.Flags & StudioHdrFlags.StaticProp) != 0) {
			poseToWorld[0] = pBoneToWorld[0];
			return;
		}

		if (studioHdr.LinearBones() == null) {
			for (int i = 0; i < studioHdr.NumBones; i++) {
				MStudioBone pCurBone = studioHdr.Bone(i);
				if ((pCurBone.Flags & boneMask) == 0)
					continue;

				Matrix3x4 poseToBone = pCurBone.PoseToBone;
				MathLib.ConcatTransforms(in pBoneToWorld[i], in poseToBone, out poseToWorld[i]);
			}
		}
		else {
			MStudioLinearBone linearBones = studioHdr.LinearBones()!;

			for (int i = 0; i < studioHdr.NumBones; i++) {
				if ((linearBones.Flags(i) & boneMask) == 0)
					continue;

				Matrix3x4 poseToBone = linearBones.PoseToBone(i);
				MathLib.ConcatTransforms(in pBoneToWorld[i], in poseToBone, out poseToWorld[i]);
			}
		}
	}

	bool SkippedMeshes;
	bool DrawTranslucentSubModels;

	public int R_StudioRenderModel(IMatRenderContext renderContext, int skin, int body, int hitboxset, object? entity,
									Span<IMaterial> materials, Span<int> materialFlags, StudioRenderFlags flags, int boneMask, int lod, Span<ColorMeshInfo> colorMeshes) {
		StudioRenderFlags nDrawGroup = flags & StudioRenderFlags.DrawGroupMask;

		// TODO: Draw modes for entities/bones stuff

		int numTrianglesRendered = 0;

		// Build list of submodels
		BodyPartInfo[] pBodyPartInfo = ArrayPool<BodyPartInfo>.Shared.Rent(StudioHdr!.NumBodyParts);
		for (int i = 0; i < StudioHdr.NumBodyParts; ++i)
			pBodyPartInfo[i].SubModelIndex = R_StudioSetupModel(i, body, out pBodyPartInfo[i].SubModel, StudioHdr);

		if (nDrawGroup != StudioRenderFlags.DrawTranslucentOnly) {
			SkippedMeshes = false;
			DrawTranslucentSubModels = false;
			numTrianglesRendered += R_StudioRenderFinal(renderContext, skin, StudioHdr.NumBodyParts, pBodyPartInfo,
				entity, materials, materialFlags, boneMask, lod, colorMeshes);
		}
		else {
			SkippedMeshes = true;
		}

		if (SkippedMeshes && nDrawGroup != StudioRenderFlags.DrawOpaqueOnly) {
			DrawTranslucentSubModels = true;
			numTrianglesRendered += R_StudioRenderFinal(renderContext, skin, StudioHdr.NumBodyParts, pBodyPartInfo,
				entity, materials, materialFlags, boneMask, lod, colorMeshes);
		}
		ArrayPool<BodyPartInfo>.Shared.Return(pBodyPartInfo, true);
		return numTrianglesRendered;
	}

	MStudioModel? SubModel;

	private int R_StudioRenderFinal(IMatRenderContext renderContext, int skin, int bodyPartCount, BodyPartInfo[] pBodyPartInfo, object? clientEntity, Span<IMaterial> materials, Span<int> materialFlags, int boneMask, int lod, Span<ColorMeshInfo> colorMeshes) {
		int numTrianglesRendered = 0;

		for (int i = 0; i < bodyPartCount; i++) {
			SubModel = pBodyPartInfo[i].SubModel;

			// TODO: Flex controller stuff
			numTrianglesRendered += R_StudioDrawPoints(renderContext, skin, clientEntity, materials, materialFlags, boneMask, lod, colorMeshes);
		}
		return numTrianglesRendered;
	}

	private int R_StudioDrawPoints(IMatRenderContext renderContext, int skin, object? clientEntity, Span<IMaterial> materials, Span<int> materialFlagsSpan, int boneMask, int lod, Span<ColorMeshInfo> colorMeshes) {
		int numTrianglesRendered = 0;

		// happens when there's a model load failure
		if (StudioMeshes == null)
			return 0;

		// todo: wireframe translucent thing

		if (pRC!.Config.Skin != 0) {
			skin = pRC.Config.Skin;
			if (skin >= StudioHdr!.NumSkinFamilies)
				skin = 0;
		}

		Span<short> pskinref = StudioHdr!.SkinRef(0);
		if (skin > 0 && skin < StudioHdr!.NumSkinFamilies)
			pskinref = pskinref[(skin * StudioHdr!.NumSkinRef)..];

		for (int i = 0; i < SubModel!.NumMeshes; ++i) {
			MStudioMesh pmesh = SubModel.Mesh(i);
			StudioMeshData pMeshData = StudioMeshes[pmesh.MeshID];
			Assert(pMeshData != null);

			if (pMeshData.NumGroup == 0)
				continue;

			StudioModelLighting lighting = StudioModelLighting.Hardware;
			int materialFlags = materialFlagsSpan[pskinref[pmesh.Material]];

			IMaterial? pMaterial = R_StudioSetupSkinAndLighting(renderContext, pskinref[pmesh.Material], materials, materialFlags, clientEntity, colorMeshes, ref lighting);
			if (pMaterial == null)
				continue;

			// VertexCache.SetMesh(i);

			// The following are special cases that can't be covered with
			// the normal static/dynamic methods due to optimization reasons
			switch (pmesh.MaterialType) {
				case 1:
					// numTrianglesRendered += R_StudioDrawEyeball(renderContext, pmesh, pMeshData, lighting, pMaterial, lod);
					break;
				default:
					numTrianglesRendered += R_StudioDrawMesh(renderContext, pmesh, pMeshData, lighting, pMaterial, colorMeshes, lod);
					break;
			}
		}

		// Reset this state so it doesn't hose other parts of rendering
		renderContext.SetNumBoneWeights(0);

		return numTrianglesRendered;
	}

	private int R_StudioDrawMesh(IMatRenderContext renderContext, MStudioMesh pmesh, StudioMeshData pMeshData, StudioModelLighting lighting, IMaterial pMaterial, Span<ColorMeshInfo> colorMeshes, int lod) {
		int numTrianglesRendered = 0;

		for (int j = 0; j < pMeshData.NumGroup; ++j) {
			StudioMeshGroup pGroup = pMeshData.MeshGroup![j];

			bool bIsFlexed = (pGroup.Flags & StudioMeshGroupFlags.IsFlexed) != 0;
			bool bIsDeltaFlexed = (pGroup.Flags & StudioMeshGroupFlags.IsDeltaFlexed) != 0;

			bool bFlexStatic = bIsDeltaFlexed; // && g_pMaterialSystemHardwareConfig->SupportsStreamOffset()); << todo: research

			bool bIsHardwareSkinnedData = (pGroup.Flags & StudioMeshGroupFlags.IsHWSkinned) != 0;
			bool bShouldHardwareSkin = bIsHardwareSkinnedData && (!bIsFlexed || bFlexStatic) && (lighting != StudioModelLighting.Software);

			if (bShouldHardwareSkin && !pRC!.Config.DrawNormals && !pRC!.Config.DrawTangentFrame && !pRC!.Config.Wireframe) {
				if (!pRC!.Config.NoHardware)
					numTrianglesRendered += R_StudioDrawStaticMesh(renderContext, pmesh, pGroup, lighting, pRC.AlphaMod, pMaterial, lod, colorMeshes);
			}
			else {
				if (!pRC!.Config.NoSoftware)
					numTrianglesRendered += R_StudioDrawDynamicMesh(renderContext, pmesh, pGroup, lighting, pRC.AlphaMod, pMaterial, lod);
			}
		}
		return numTrianglesRendered;
	}

	private int R_StudioDrawStaticMesh(IMatRenderContext renderContext, MStudioMesh pmesh, StudioMeshGroup pGroup, StudioModelLighting lighting, float alphaMod, IMaterial pMaterial, int lod, Span<ColorMeshInfo> colorMeshes) {
		int numTrianglesRendered = 0;

		bool bDoSoftwareLighting = colorMeshes.IsEmpty &&
			((pRC!.Config.SoftwareSkin) || pRC.Config.DrawNormals || pRC.Config.DrawTangentFrame ||
			// (pMaterial != null ? pMaterial.NeedsSoftwareSkinning() : false) ||
			(pRC.Config.SoftwareLighting) ||
			((lighting != StudioModelLighting.Hardware) && (lighting != StudioModelLighting.Mouth)));

		if (bDoSoftwareLighting) {
			if (pRC.Config.NoSoftware)
				return 0;

			bool needsTangentSpace = pMaterial != null ? pMaterial.NeedsTangentSpace() : false;
			renderContext.MatrixMode(MaterialMatrixMode.Model);
			renderContext.LoadIdentity();

			VertexFormat fmt = ComputeSWSkinVertexFormat(pMaterial!);
			bool dx8Vertex = fmt.GetUserDataSize() != 0;

			IMesh mesh = renderContext.GetDynamicMesh(false, null, pGroup.Mesh);

			MeshBuilder meshBuilder = new();
			meshBuilder.Begin(mesh, MaterialPrimitiveType.Heterogenous, pGroup.NumVertices, 0);

			R_StudioSoftwareProcessMesh(pmesh, ref meshBuilder, pGroup.NumVertices, pGroup.GroupIndexToMeshIndex!, lighting, false, alphaMod, needsTangentSpace, dx8Vertex, pMaterial!);

			meshBuilder.End();

			return R_StudioDrawGroupSWSkin(renderContext, pGroup, mesh);
		}

		// Needed when we switch back and forth between hardware + software lighting
		// TODO ^^^^^^^^^^^^^^^^^^

		// Build separate flex stream containing deltas, which will get copied into another vertex stream
		// TODO ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

		if (!colorMeshes.IsEmpty && (pGroup.ColorMeshID != -1))
			numTrianglesRendered = R_StudioDrawGroupHWSkin(renderContext, pGroup, pGroup.Mesh, ref colorMeshes[pGroup.ColorMeshID]);
		else
			numTrianglesRendered = R_StudioDrawGroupHWSkin(renderContext, pGroup, pGroup.Mesh, ref Unsafe.NullRef<ColorMeshInfo>());

		// TODO: Morph/flex

		return numTrianglesRendered;
	}

	private int R_StudioDrawGroupHWSkin(IMatRenderContext renderContext, StudioMeshGroup pGroup, IMesh? mesh, ref ColorMeshInfo colorMeshInfo) {
		int numTrianglesRendered = 0;

		if (StudioHdr!.NumBones == 1) {
			renderContext.MatrixMode(MaterialMatrixMode.Model);
			renderContext.LoadMatrix(PoseToWorld[0]);

			renderContext.SetNumBoneWeights(0);
		}

		if (!Unsafe.IsNullRef(ref colorMeshInfo))
			mesh!.SetColorMesh(colorMeshInfo.Mesh!, colorMeshInfo.VertOffsetInBytes);
		else
			mesh!.SetColorMesh(null!, 0);

		for (int j = 0; j < pGroup.NumStrips; ++j) {
			OptimizedModel.StripHeader pStrip = pGroup.StripData![j];

			if (StudioHdr.NumBones > 1) {
				renderContext.SetNumBoneWeights(pStrip.NumBones);

				for (int k = 0; k < pStrip.NumBoneStateChanges; ++k) {
					ref OptimizedModel.BoneStateChangeHeader pStateChange = ref pStrip.BoneStateChange(k);
					if (pStateChange.NewBoneID < 0)
						break;

					renderContext.LoadBoneMatrix(pStateChange.HardwareID, in PoseToWorld[pStateChange.NewBoneID]);
				}
			}

			mesh.SetPrimitiveType((pStrip.Flags & OptimizedModel.StripHeaderFlags.IsTriStrip) != 0 ? MaterialPrimitiveType.TriangleStrip : MaterialPrimitiveType.Triangles);

			mesh.Draw(pStrip.IndexOffset, pStrip.NumIndices);
			numTrianglesRendered += 0; // TODO: uniquetris
		}
		mesh.SetColorMesh(null, 0);

		return numTrianglesRendered;
	}

	private int R_StudioDrawDynamicMesh(IMatRenderContext renderContext, MStudioMesh pmesh, StudioMeshGroup pGroup, StudioModelLighting lighting, float alphaMod, IMaterial pMaterial, int lod) {
		bool doFlex = ((pGroup.Flags & StudioMeshGroupFlags.IsFlexed) != 0) && pRC!.Config.Flex;

		bool doSoftwareLighting = (pRC!.Config.SoftwareLighting) ||
			((lighting != StudioModelLighting.Hardware) && (lighting != StudioModelLighting.Mouth));

		bool swSkin = doSoftwareLighting || pRC.Config.DrawNormals || pRC.Config.DrawTangentFrame ||
			((pGroup.Flags & StudioMeshGroupFlags.IsHWSkinned) == 0) ||
			pRC.Config.SoftwareSkin ||
			(pMaterial != null ? pMaterial.NeedsSoftwareSkinning() : false);

		if (!doFlex && !swSkin)
			return R_StudioDrawStaticMesh(renderContext, pmesh, pGroup, lighting, alphaMod, pMaterial, lod, default);

		MStudioMeshVertexData? vertData = GetFatVertexData(pmesh, StudioHdr!);
		if (vertData == null)
			return 0;

		int numTrianglesRendered = 0;

		renderContext.MatrixMode(MaterialMatrixMode.Model);
		renderContext.LoadIdentity();

		if (doFlex) {
			// todo
		}

		bool needsTangentSpace = pMaterial != null ? pMaterial.NeedsTangentSpace() : false;

		VertexFormat fmt = ComputeSWSkinVertexFormat(pMaterial!);
		bool dx8Vertex = fmt.GetUserDataSize() != 0;

		IMesh mesh = renderContext.GetDynamicMesh(false, null, pGroup.Mesh);

		MeshBuilder meshBuilder = new();
		meshBuilder.Begin(mesh, MaterialPrimitiveType.Heterogenous, pGroup.NumVertices, 0);

		if (swSkin)
			R_StudioSoftwareProcessMesh(pmesh, ref meshBuilder, pGroup.NumVertices, pGroup.GroupIndexToMeshIndex!, lighting, doFlex, alphaMod, needsTangentSpace, dx8Vertex, pMaterial!);
		else if (doFlex) {
			// todo
		}

		meshBuilder.End();

		if (!swSkin)
			numTrianglesRendered = R_StudioDrawGroupHWSkin(renderContext, pGroup, mesh, ref Unsafe.NullRef<ColorMeshInfo>());
		else
			numTrianglesRendered = R_StudioDrawGroupSWSkin(renderContext, pGroup, mesh);

		// todo

		return numTrianglesRendered;
	}

	private int R_StudioDrawGroupSWSkin(IMatRenderContext renderContext, StudioMeshGroup pGroup, IMesh mesh) {
		int numTrianglesRendered = 0;

		renderContext.SetNumBoneWeights(0);

		for (int j = 0; j < pGroup.NumStrips; ++j) {
			OptimizedModel.StripHeader strip = pGroup.StripData![j];

			mesh.SetPrimitiveType((strip.Flags & OptimizedModel.StripHeaderFlags.IsTriStrip) != 0 ? MaterialPrimitiveType.TriangleStrip : MaterialPrimitiveType.Triangles);

			mesh.Draw(strip.IndexOffset, strip.NumIndices);
			numTrianglesRendered += 0; // TODO: uniquetris
		}

		return numTrianglesRendered;
	}

	private static Matrix3x4 ComputeSkinMatrix(in MStudioBoneWeight boneWeights, Span<Matrix3x4> poseToWorld) {
		Matrix3x4 result;
		switch (boneWeights.NumBones) {
			default:
			case 1:
				return poseToWorld[boneWeights.Bone[0]];

			case 2: {
					ref Matrix3x4 boneMat0 = ref poseToWorld[boneWeights.Bone[0]];
					ref Matrix3x4 boneMat1 = ref poseToWorld[boneWeights.Bone[1]];
					float weight0 = boneWeights.Weight[0];
					float weight1 = boneWeights.Weight[1];

					result = default;
					for (int r = 0; r < 3; ++r)
						for (int c = 0; c < 4; ++c)
							result[r, c] = boneMat0[r, c] * weight0 + boneMat1[r, c] * weight1;
					return result;
				}

			case 3: {
					ref Matrix3x4 boneMat0 = ref poseToWorld[boneWeights.Bone[0]];
					ref Matrix3x4 boneMat1 = ref poseToWorld[boneWeights.Bone[1]];
					ref Matrix3x4 boneMat2 = ref poseToWorld[boneWeights.Bone[2]];
					float weight0 = boneWeights.Weight[0];
					float weight1 = boneWeights.Weight[1];
					float weight2 = boneWeights.Weight[2];

					result = default;
					for (int r = 0; r < 3; ++r)
						for (int c = 0; c < 4; ++c)
							result[r, c] = boneMat0[r, c] * weight0 + boneMat1[r, c] * weight1 + boneMat2[r, c] * weight2;
					return result;
				}
		}
	}

	private void R_PerformLighting(in Vector3 forward, float illum, in Vector3 pos, in Vector3 norm, uint alphaMask, out uint color, StudioModelLighting lighting) {
		if (lighting == StudioModelLighting.Software) {
			R_ComputeLightAtPoint3(in pos, in norm, out Vector3 lightColor);

			byte r = MathLib.LinearToLightmap(lightColor.X);
			byte g = MathLib.LinearToLightmap(lightColor.Y);
			byte b = MathLib.LinearToLightmap(lightColor.Z);

			color = (uint)(b | (g << 8) | (r << 16)) | alphaMask;
		}
		else if (lighting == StudioModelLighting.Mouth) {
			if (illum != 0.0f) {
				R_ComputeLightAtPoint3(in pos, in norm, out Vector3 lightColor);
				// todo R_MouthLighting

				byte r = MathLib.LinearToLightmap(lightColor.X);
				byte g = MathLib.LinearToLightmap(lightColor.Y);
				byte b = MathLib.LinearToLightmap(lightColor.Z);

				color = (uint)(b | (g << 8) | (r << 16)) | alphaMask;
			}
			else
				color = alphaMask;
		}
		else
			color = alphaMask;
	}

	private static void R_TransformVert(in Vector3 srcPos, in Vector3 srcNorm, in Matrix3x4 skinMat, out Vector3 pos, out Vector3 norm) {
		MathLib.VectorTransform(in srcPos, in skinMat, out pos);
		MathLib.VectorRotate(in srcNorm, in skinMat, out norm);
	}

	private void R_StudioSoftwareProcessMesh(MStudioMesh mesh, ref MeshBuilder meshBuilder, int numVertices, ushort[] groupToMesh, StudioModelLighting lighting, bool doFlex, float blend, bool needsTangentSpace, bool dx8Vertex, IMaterial material) {
		uint alphaMask = (uint)MathLib.RoundFloatToInt(blend * 255.0f);
		alphaMask = Math.Clamp(alphaMask, 0u, 255u);
		alphaMask <<= 24;

		MStudioMeshVertexData? vertData = GetFatVertexData(mesh, StudioHdr!);
		if (vertData != null)
			R_StudioSoftwareProcessMesh(vertData, PoseToWorld, ref meshBuilder, numVertices, groupToMesh, alphaMask, lighting, material);
	}

	private void R_StudioSoftwareProcessMesh(MStudioMeshVertexData vertData, Span<Matrix3x4> poseToWorld, ref MeshBuilder meshBuilder, int numVertices, ushort[] groupToMesh, uint alphaMask, StudioModelLighting lighting, IMaterial material) {
		Assert(numVertices > 0);

		float illum = 1.0f;
		Vector3 forward = default;
		// todo

		for (int j = 0; j < numVertices; ++j) {
			int n = groupToMesh[j];
			ref MStudioVertex vert = ref vertData.Vertex(n);

			Matrix3x4 skinMat = ComputeSkinMatrix(in vert.BoneWeights, poseToWorld);

			// todo: flex
			R_TransformVert(in vert.Position, in vert.Normal, in skinMat, out Vector3 pos, out Vector3 norm);

			R_PerformLighting(in forward, illum, in pos, in norm, alphaMask, out uint color, lighting);

			meshBuilder.Position3fv(in pos);
			meshBuilder.Normal3fv(in norm);
			meshBuilder.Color4ubv([(byte)(color >> 16), (byte)(color >> 8), (byte)color, (byte)(color >> 24)]);
			meshBuilder.TexCoord2fv(0, in vert.TexCoord);
			meshBuilder.AdvanceVertex();
		}
	}

	uint fatVertexWarnCount = 0;

	private MStudioMeshVertexData? GetFatVertexData(MStudioMesh mesh, StudioHeader studioHdr) {
		if (mesh.Model.CacheVertexData(studioDataCache, studioHdr) == null)
			return null;

		MStudioMeshVertexData? vertData = mesh.GetVertexData(studioDataCache, studioHdr);
		Assert(vertData != null);
		if (vertData == null) {
			if (fatVertexWarnCount++ < 20)
				Warning("ERROR: model verts have been compressed, cannot render! (use \"-no_compressed_vvds\")");
		}
		return vertData;
	}

	private void R_ComputeLightAtPoint3(in Vector3 pos, in Vector3 normal, out Vector3 color) {
		if (pRC!.Config.FullBright != 0) {
			color = new(1.0f, 1.0f, 1.0f);
			return;
		}

		Span<LightPos> lightpos = stackalloc LightPos[MAXLOCALLIGHTS];
		R_LightStrengthWorld(in pos, pRC.NumLocalLights, pRC.LocalLights, lightpos);

		R_LightAmbient_4D(in normal, pRC.LightBoxColors, out color);

		R_LightEffectsWorld3(pRC.LocalLights, lightpos, in normal, ref color, pRC.NumLocalLights);
	}

	internal static void R_LightStrengthWorld(in Vector3 vert, int lightcount, Span<LightDesc> desc, Span<LightPos> light) {
		for (int i = 0; i < lightcount; i++) {
			R_WorldLightDelta(in desc[i], in vert, out light[i].Delta);
			light[i].Falloff = R_WorldLightDistanceFalloff(in desc[i], in light[i].Delta);

			MathLib.VectorNormalizeFast(ref light[i].Delta);
			light[i].Dot = MathLib.DotProduct(light[i].Delta, desc[i].Direction);
		}
	}

	private static void R_WorldLightDelta(in LightDesc wl, in Vector3 org, out Vector3 delta) {
		switch (wl.Type) {
			case LightType.Point:
			case LightType.Spot:
				MathLib.VectorSubtract(wl.Position, org, out delta);
				break;

			case LightType.Directional:
				MathLib.VectorMultiply(wl.Direction, -1, out delta);
				break;

			default:
				Assert(false);
				delta = default;
				break;
		}
	}

	private static float R_WorldLightDistanceFalloff(in LightDesc wl, in Vector3 delta) {
		float dist2 = MathLib.DotProduct(delta, delta);

		if (wl.Range != 0.0f) {
			if (dist2 > wl.Range * wl.Range)
				return 0.0f;
		}

		float total = float.Epsilon;

		LightTypeOptimizationFlags flags = (LightTypeOptimizationFlags)wl.Flags;

		if ((flags & LightTypeOptimizationFlags.HasAttenuation0) != 0)
			total = wl.Attenuation0;

		if ((flags & LightTypeOptimizationFlags.HasAttenuation1) != 0)
			total += wl.Attenuation1 * MathF.Sqrt(dist2);

		if ((flags & LightTypeOptimizationFlags.HasAttenuation2) != 0)
			total += wl.Attenuation2 * dist2;

		return 1.0f / total;
	}

	private static float R_WorldLightAngle(in LightDesc wl, in Vector3 lnormal, in Vector3 snormal, in Vector3 delta) {
		float dot, dot2, ratio;

		switch (wl.Type) {
			case LightType.Point:
				dot = MathLib.DotProduct(snormal, delta);
				if (dot < 0.0f)
					return 0.0f;
				return dot;

			case LightType.Spot:
				dot = MathLib.DotProduct(snormal, delta);
				if (dot < 0.0f)
					return 0.0f;

				dot2 = -MathLib.DotProduct(delta, lnormal);
				if (dot2 <= wl.PhiDot)
					return 0.0f;

				ratio = dot;
				if (dot2 >= wl.ThetaDot)
					return ratio;

				if ((wl.Falloff == 1.0f) || (wl.Falloff == 0.0f))
					ratio *= (dot2 - wl.PhiDot) / (wl.ThetaDot - wl.PhiDot);
				else
					ratio *= MathF.Pow((dot2 - wl.PhiDot) / (wl.ThetaDot - wl.PhiDot), wl.Falloff);
				return ratio;

			case LightType.Directional:
				dot2 = -MathLib.DotProduct(snormal, lnormal);
				if (dot2 < 0.0f)
					return 0.0f;
				return dot2;

			case LightType.Disable:
				return 0.0f;

			default:
				Assert(false);
				return 0.0f;
		}
	}

	internal static void R_LightEffectsWorld3(Span<LightDesc> desc, Span<LightPos> light, in Vector3 normal, ref Vector3 dest, int numLights) {
		for (int i = 0; i < numLights; i++) {
			if (desc[i].Type == LightType.Disable)
				continue;

			float ratio = light[i].Falloff * R_WorldLightAngle(in desc[i], in desc[i].Direction, in normal, in light[i].Delta);
			if (ratio > 0)
				dest += desc[i].Color * ratio;
		}
	}

	private static void R_LightAmbient_4D(in Vector3 normal, InlineArray6<Vector4> pLightBoxColor, out Vector3 lv) {
		MathLib.VectorScale(normal.X > 0.0f ? pLightBoxColor[0].AsVector3D() : pLightBoxColor[1].AsVector3D(), normal.X * normal.X, out lv);
		MathLib.VectorMA(lv, normal.Y * normal.Y, normal.Y > 0.0f ? pLightBoxColor[2].AsVector3D() : pLightBoxColor[3].AsVector3D(), out lv);
		MathLib.VectorMA(lv, normal.Z * normal.Z, normal.Z > 0.0f ? pLightBoxColor[4].AsVector3D() : pLightBoxColor[5].AsVector3D(), out lv);
	}

	internal static void R_LightAmbient_3D(in Vector3 normal, ReadOnlySpan<Vector3> pLightBoxColor, out Vector3 lv) {
		MathLib.VectorScale(normal.X > 0.0f ? pLightBoxColor[0] : pLightBoxColor[1], normal.X * normal.X, out lv);
		MathLib.VectorMA(lv, normal.Y * normal.Y, normal.Y > 0.0f ? pLightBoxColor[2] : pLightBoxColor[3], out lv);
		MathLib.VectorMA(lv, normal.Z * normal.Z, normal.Z > 0.0f ? pLightBoxColor[4] : pLightBoxColor[5], out lv);
	}

	private static VertexFormat ComputeSWSkinVertexFormat(IMaterial pMaterial) {
		bool DX8OrHigherVertex = pMaterial.GetVertexFormat().GetUserDataSize() != 0;
		VertexFormat fmt = VertexFormat.Position | VertexFormat.Normal | VertexFormat.Color | VertexFormat.BoneIndex | VertexExts.GetBoneWeight(2) | VertexFormat.TexCoord2D_0;
		if (DX8OrHigherVertex)
			fmt |= VertexExts.GetUserDataSize(4);
		return fmt;
	}

	static uint translucentCache = 0;
	static uint originalTextureVarCache = 0;
	static uint lightmapVarCache = 0;

	private IMaterial? R_StudioSetupSkinAndLighting(IMatRenderContext renderContext, int index, Span<IMaterial> materials, int materialFlags, object? clientRenderable, Span<ColorMeshInfo> colorMeshes, ref StudioModelLighting lighting) {
		IMaterial? pMaterial = null;
		bool bCheckForConVarDrawTranslucentSubModels = false;
		// TODO: wireframe
		// todo: env cubemap only

		if (pRC!.ForcedMaterial == null /* TODO: Shadow/SSAO overrides here!! */) {
			pMaterial = materials[index];
			if (pMaterial == null) {
				Assert(false);
				return null;
			}
		}
		else {
			// TODO!!
			Assert(false);
			return null;
		}

		lighting = R_StudioComputeLighting(pMaterial, materialFlags, colorMeshes);
		if (lighting == StudioModelLighting.Mouth) {
			// TODO
			Assert(false);
			return null;
		}

		// todo: lightmap var
		renderContext.Bind(pMaterial, clientRenderable);

		if (bCheckForConVarDrawTranslucentSubModels) {
			bool translucent = pMaterial.IsTranslucent();

			if (DrawTranslucentSubModels != translucent) {
				SkippedMeshes = true;
				return null;
			}
		}

		return pMaterial;
	}

	private StudioModelLighting R_StudioComputeLighting(IMaterial pMaterial, int materialFlags, Span<ColorMeshInfo> colorMeshes) {
		Assert(pMaterial != null);
		bool doMouthLighting = materialFlags != 0 && (StudioHdr!.NumMouths >= 1);

		bool doSoftwareLighting = doMouthLighting || (pMaterial!.IsVertexLit() && pMaterial.NeedsSoftwareLighting());

		if (!pRC!.Config.SupportsVertexAndPixelShaders) {
			if (!doSoftwareLighting && !colorMeshes.IsEmpty)
				pMaterial!.SetUseFixedFunctionBakedLighting(true);
			else {
				doSoftwareLighting = true;
				pMaterial!.SetUseFixedFunctionBakedLighting(false);
			}
		}

		StudioModelLighting lighting = StudioModelLighting.Hardware;
		if (doMouthLighting)
			lighting = StudioModelLighting.Mouth;
		else if (doSoftwareLighting)
			lighting = StudioModelLighting.Software;

		return lighting;
	}

	public int R_StudioSetupModel(int bodypart, int entity_body, out MStudioModel? subModel, StudioHeader studioHdr) {
		int index;
		MStudioBodyParts pbodypart;

		if (bodypart > studioHdr.NumBodyParts) {
			ConDMsg($"R_StudioSetupModel: no such bodypart {bodypart}\n");
			bodypart = 0;
		}

		pbodypart = studioHdr.BodyPart(bodypart);

		if (pbodypart.Base == 0) {
			Warning($"Model has missing body part: {studioHdr.GetName()}\n");
			Assert(false);
		}
		index = entity_body / pbodypart.Base;
		index = index % pbodypart.NumModels;

		subModel = pbodypart.Model(index);
		return index;
	}
}
