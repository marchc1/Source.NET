using Source.Common;
using Source.Common.Engine;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;

using System.Buffers;
using System.Numerics;

namespace Source.StudioRender;

public struct BodyPartInfo
{
	public int SubModelIndex;
	public MStudioModel? SubModel;
}

[EngineComponent]
public unsafe class StudioRender
{
	IMaterialSystem materialSystem = Singleton<IMaterialSystem>();

	StudioRenderCtx* pRC;
	Matrix4x4* pBoneToWorld;
	int nBoneToWorld;
	StudioHeader? StudioHdr;
	StudioMeshData[]? StudioMeshes;

	public readonly Matrix4x4[] PoseToWorld = new Matrix4x4[Studio.MAXSTUDIOBONES];
	public readonly Matrix4x4[] PoseToDecal = new Matrix4x4[Studio.MAXSTUDIOBONES];

	internal void DrawModel(ref DrawModelInfo info, ref StudioRenderCtx RC, Span<Matrix4x4> boneToWorld, StudioRenderFlags flags) {
		// TODO: a better way to do this that doesnt require unsafe
		// TODO: flex
		nBoneToWorld = boneToWorld.Length;
		fixed (StudioRenderCtx* pRCtx = &RC)
		fixed (Matrix4x4* pBtW = boneToWorld) {
			pRC = pRCtx;
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
			ComputePoseToWorld(PoseToWorld, StudioHdr, boneMask, in pRC->ViewOrigin, pBoneToWorld);

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

	private int R_StudioRenderFinal(IMatRenderContext renderContext, int skin, int numBodyParts, BodyPartInfo[] pBodyPartInfo, object? entity, Span<IMaterial> materials, Span<int> materialFlags, int boneMask, int lod, Span<ColorMeshInfo> colorMeshes) {
		throw new NotImplementedException();
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
