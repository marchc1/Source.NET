using Source.Common;
using Source.Common.Engine;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;

using System;
using System.Buffers;
using System.Numerics;

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

[EngineComponent]
public unsafe class StudioRender
{
	IMaterialSystem materialSystem = Singleton<IMaterialSystem>();

	StudioRenderCtx? pRC;
	Matrix4x4* pBoneToWorld;
	int nBoneToWorld;
	StudioHeader? StudioHdr;
	StudioMeshData[]? StudioMeshes;

	public readonly Matrix4x4[] PoseToWorld = new Matrix4x4[Studio.MAXSTUDIOBONES];
	public readonly Matrix4x4[] PoseToDecal = new Matrix4x4[Studio.MAXSTUDIOBONES];

	internal void DrawModel(ref DrawModelInfo info, StudioRenderCtx RC, Span<Matrix4x4> boneToWorld, StudioRenderFlags flags) {
		// TODO: a better way to do this that doesnt require unsafe
		// TODO: flex
		nBoneToWorld = boneToWorld.Length;
		fixed (Matrix4x4* pBtW = boneToWorld) {
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

	private void ComputePoseToWorld(Span<Matrix4x4> poseToWorld, StudioHeader studioHdr, int boneMask, in Vector3 viewOrigin, Matrix4x4* pBoneToWorld) {
		if ((studioHdr.Flags & StudioHdrFlags.StaticProp) != 0) {
			poseToWorld[0] = pBoneToWorld[0];
			return;
		}

		if (studioHdr.LinearBones() == null) {
			for (int i = 0; i < studioHdr.NumBones; i++) {
				MStudioBone pCurBone = studioHdr.Bone(i);
				if ((pCurBone.Flags & boneMask) == 0)
					continue;

				Matrix4x4 poseToBone = pCurBone.PoseToBone.To4x4();
				MathLib.ConcatTransforms(in pBoneToWorld[i], in poseToBone, out poseToWorld[i]);
			}
		}
		else {
			MStudioLinearBone linearBones = studioHdr.LinearBones()!;

			for (int i = 0; i < studioHdr.NumBones; i++) {
				if ((linearBones.Flags(i) & boneMask) == 0)
					continue;

				Matrix4x4 poseToBone = linearBones.PoseToBone(i).To4x4();
				MathLib.ConcatTransforms(in pBoneToWorld[i], in poseToBone, out poseToWorld[i]);
			}
		}
	}

	bool SkippedMeshes;
	bool DrawTranslucentSubModels;

	private int R_StudioRenderModel(IMatRenderContext renderContext, int skin, int body, int hitboxset, object? entity,
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

			IMaterial? pMaterial = R_StudioSetupSkinAndLighting(renderContext, pskinref[pmesh.Material], materials, materialFlags, clientEntity, colorMeshes, lighting);
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
		throw new NotImplementedException();
	}

	static uint translucentCache = 0;
	static uint originalTextureVarCache = 0;
	static uint lightmapVarCache = 0;

	private IMaterial? R_StudioSetupSkinAndLighting(IMatRenderContext renderContext, int index, Span<IMaterial> materials, int materialFlags, object? clientRenderable, Span<ColorMeshInfo> colorMeshes, StudioModelLighting lighting) {
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
		bool doMouthLighting = materialFlags != 0 && (StudioHdr!.NumMouths >= 1);

		bool doSoftwareLighting = false; // TODO

		StudioModelLighting lighting = StudioModelLighting.Hardware;
		if (doMouthLighting)
			lighting = StudioModelLighting.Mouth;
		else if (doSoftwareLighting)
			lighting = StudioModelLighting.Software;

		return lighting;
	}

	private int R_StudioSetupModel(int bodypart, int entity_body, out MStudioModel? subModel, StudioHeader studioHdr) {
		int index;
		MStudioBodyParts pbodypart;

		if (bodypart > studioHdr.NumBodyParts) {
			ConDMsg($"R_StudioSetupModel: no such bodypart {bodypart}\n");
			bodypart = 0;
		}

		pbodypart = studioHdr.BodyPart(bodypart);

		if (pbodypart.Base == 0) {
			Warning($"Model has missing body part: {studioHdr.GetName()}\n");
			Assert(0);
		}
		index = entity_body / pbodypart.Base;
		index = index % pbodypart.NumModels;

		subModel = pbodypart.Model(index);
		return index;
	}
}
