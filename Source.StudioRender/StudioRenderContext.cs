using Source.Common;
using Source.Common.MaterialSystem;

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Source.StudioRender;

/// <summary>
/// Analog of StudioRenderContext_t
/// </summary>
public struct StudioRenderCtx
{

}

/// <summary>
/// Analog of CStudioRenderContext
/// </summary>
public class StudioRenderContext(IMaterialSystem materialSystem) : IStudioRender
{
	public void BeginFrame() {
		throw new NotImplementedException();
	}

	public void EndFrame() {
		throw new NotImplementedException();
	}

	public int GetMaterialList(StudioHeader studioHDR, Span<IMaterial> materials) {
		AssertMsg(studioHDR != null, "Don't ignore this assert! StudioRenderContext.GetMaterialList() has null studioHDR.");

		if (studioHDR == null)
			return 0;

		if (studioHDR.TextureIndex == 0)
			return 0;

		int i;
		int j;
		int found = 0;
		Span<char> path = stackalloc char[MAX_PATH];
		for (i = 0; i < studioHDR.NumTextures; i++) {
			path.Clear();
			IMaterial? material = null;

			for (j = 0; j < studioHDR.NumCDTextures && material.IsErrorMaterial(); j++) {
				// If we don't do this, we get filenames like "materials\\blah.vmt".
				ReadOnlySpan<char> textureName = studioHDR.Texture(i).Name();
				if (!textureName.IsEmpty && textureName[0].IsPathSeparator())
					textureName = textureName[1..];

				// This prevents filenames like /models/blah.vmt.
				ReadOnlySpan<char> pCdTexture = studioHDR.CDTexture(j);
				if (!textureName.IsEmpty && pCdTexture[0].IsPathSeparator())
					textureName = textureName[1..];

				StrTools.ComposeFileName(pCdTexture, textureName, path);

				if ((studioHDR.Flags & StudioHdrFlags.Obsolete) != 0)
					material = materialSystem.FindMaterialEx("models/obsolete/obsolete", TEXTURE_GROUP_MODEL, MaterialFindContext.IsOnAModel, false);
				else
					material = materialSystem.FindMaterialEx(path, TEXTURE_GROUP_MODEL, MaterialFindContext.IsOnAModel, false);
			}

			if (material == null)
				continue;

			if (found < materials.Length) {
				int k;
				
				for (k = 0; k < found; k++)
					if (materials[k] == material)
						break;

				if (k >= found)
					materials[found++] = material;
			}
			else
				break;
		}

		return found;
	}

	public bool LoadModel(StudioHeader studioHdr, Memory<byte> vtxBuffer, StudioHWData studioHWData) {
		int i;
		int j;

		if (studioHdr == null || vtxBuffer.IsEmpty || studioHWData == null)
			return false;

		// NOTE: This must be called *after* Mod_LoadStudioModel
		OptimizedModel.FileHeader vertexHdr = new(vtxBuffer);
		if (vertexHdr.Checksum != studioHdr.Checksum) {
			ConDMsg($"Error! Model {studioHdr.GetName()} .vtx file out of synch with .mdl\n");
			return false;
		}

		studioHWData.NumStudioMeshes = 0;
		for (i = 0; i < studioHdr.NumBodyParts; i++) {
			MStudioBodyParts bodyPart = studioHdr.BodyPart(i);
			for (j = 0; j < bodyPart.NumModels; j++) {
				studioHWData.NumStudioMeshes += bodyPart.Model(j).NumMeshes;
			}
		}

		// Create static meshes
		Assert(vertexHdr.NumLODs != 0);
		studioHWData.RootLOD = Math.Min((int)studioHdr.RootLOD, vertexHdr.NumLODs - 1);
		studioHWData.NumLODs = vertexHdr.NumLODs;
		studioHWData.LODs = new StudioLODData[vertexHdr.NumLODs];
		for (int k = 0; k < vertexHdr.NumLODs; k++) 
			studioHWData.LODs[k] = new();

		// reset the runtime flags
		studioHdr.Flags &= ~StudioHdrFlags.UsesEnvCubemap;
		studioHdr.Flags &= ~StudioHdrFlags.UsesFbTexture;
		studioHdr.Flags &= ~StudioHdrFlags.UsesBumpmapping;

		int nColorMeshID = 0;
		int nLodID;
		for (nLodID = studioHWData.RootLOD; nLodID < studioHWData.NumLODs; nLodID++) {
			LoadMaterials(studioHdr, vertexHdr, studioHWData.LODs[nLodID], nLodID);
			R_StudioCreateStaticMeshes(studioHdr, vertexHdr, studioHWData, nLodID, ref nColorMeshID);
			ComputeHWMorphDecalBoneRemap(studioHdr, vertexHdr, studioHWData, nLodID);
			studioHWData.LODs[nLodID].SwitchPoint = vertexHdr.BodyPart(0).Model(0).LOD(nLodID).SwitchPoint;
		}

		return true;
	}

	private void ComputeHWMorphDecalBoneRemap(StudioHeader studioHdr, OptimizedModel.FileHeader vertexHdr, StudioHWData studioHWData, int nLodID) {
		throw new NotImplementedException();
	}

	private void R_StudioCreateStaticMeshes(StudioHeader studioHdr, OptimizedModel.FileHeader vertexHdr, StudioHWData studioHWData, int nLodID, ref int nColorMeshID) {
		throw new NotImplementedException();
	}

	struct Threaded_LoadMaterials_Data {
		public StudioRenderContext Context;
		public int ID;
		public int LodID;
		public StudioHeader Hdr;
		public OptimizedModel.FileHeader VtxHeader;
		public StudioLODData LodData;
	}

	private void LoadMaterials(StudioHeader hdr, OptimizedModel.FileHeader vtxHeader, StudioLODData lodData, int lodID) {
		if (hdr.NumTextures == 0) {
			lodData.Materials = null;
			return;
		}
		lodData.Materials = new IMaterial[hdr.NumTextures]; 
		lodData.MaterialFlags = new int[hdr.NumTextures];

		if (hdr.TextureIndex == 0)
			return;

		for (int i = 0; i < hdr.NumTextures; i++) {
			Threaded_LoadMaterials_Data threadData = new Threaded_LoadMaterials_Data() {
				Context = this,
				ID = i,
				LodID = lodID,
				Hdr = hdr,
				VtxHeader = vtxHeader,
				LodData = lodData
			};
			Threaded_LoadMaterials(in threadData);
		}
	}

	private void Threaded_LoadMaterials(in Threaded_LoadMaterials_Data threadData) {
		StudioHeader hdr = threadData.Hdr;
		OptimizedModel.FileHeader vtxHeader = threadData.VtxHeader;
		StudioLODData lodData = threadData.LodData;
		int lodID = threadData.LodID;
		int i = threadData.ID;

		Span<char> path = stackalloc char[MAX_PATH];
		IMaterial? material = null;

		// search through all specified directories until a valid material is found
		for (int j = 0; j < hdr.NumCDTextures && material.IsErrorMaterial(); j++) {
			// If we don't do this, we get filenames like "materials\\blah.vmt".
			ReadOnlySpan<char> textureName = GetTextureName(hdr, vtxHeader, lodID, i);
			if (!textureName.IsEmpty && (textureName[0] == StrTools.CORRECT_PATH_SEPARATOR || textureName[0] == StrTools.INCORRECT_PATH_SEPARATOR))
				textureName = textureName[1..];

			// This prevents filenames like /models/blah.vmt.
			ReadOnlySpan<char> cdTexture = hdr.CDTexture(j);
			if (!cdTexture.IsEmpty && (cdTexture[0] == StrTools.CORRECT_PATH_SEPARATOR || cdTexture[0] == StrTools.INCORRECT_PATH_SEPARATOR))
				cdTexture = cdTexture[1..];

			StrTools.ComposeFileName(cdTexture, textureName, path);

			if ((hdr.Flags & StudioHdrFlags.Obsolete) != 0) {
				material = materialSystem.FindMaterial("models/obsolete/obsolete", TEXTURE_GROUP_MODEL, false);
				if (material.IsErrorMaterial())
					Warning("StudioRender: OBSOLETE material missing: \"models/obsolete/obsolete\"\n");
			}
			else 
				material = materialSystem.FindMaterial(path, TEXTURE_GROUP_MODEL, false);
		}
		if (material.IsErrorMaterial()) {
			// hack - if it isn't found, go through the motions of looking for it again
			// so that the materialsystem will give an error.
			Span<char> szPrefix = stackalloc char[256];
			strcpy(szPrefix, hdr.GetName());
			StrTools.StrConcat(szPrefix, " : ", StrTools.COPY_ALL_CHARACTERS);
			for (int j = 0; j < hdr.NumCDTextures; j++) {
				strcpy(path, hdr.CDTexture(j));
				ReadOnlySpan<char> textureName = GetTextureName(hdr, vtxHeader, lodID, i);
				StrTools.StrConcat(path, textureName, StrTools.COPY_ALL_CHARACTERS);
				StrTools.FixSlashes(path);
				materialSystem.FindMaterial(path, TEXTURE_GROUP_MODEL, true, szPrefix);
			}
		}

		lodData.Materials![i] = material!;
		if (material != null) {
			// Increment the reference count for the material.
			// material.IncrementReferenceCount();
			threadData.Context.ComputeMaterialFlags(hdr, lodData, material);
			// lodData.MaterialFlags[i] = UsesMouthShader(material) ? 1 : 0;
			// ^ todo: flex system...
		}
	}

	private ReadOnlySpan<char> GetTextureName(StudioHeader hdr, OptimizedModel.FileHeader vtxHeader, int lodID, int inMaterialID) {
		OptimizedModel.MaterialReplacementListHeader materialReplacementList = vtxHeader.MaterialReplacementList(lodID);
		int i;
		for (i = 0; i < materialReplacementList.NumReplacements; i++) {
			OptimizedModel.MaterialReplacementHeader materialReplacement = materialReplacementList.MaterialReplacement(i);
			if (materialReplacement.MaterialID == inMaterialID) {
				string str = materialReplacement.MaterialReplacementName();
				return str;
			}
		}
		return hdr.Texture(inMaterialID).Name();
	}

	private void ComputeMaterialFlags(StudioHeader hdr, StudioLODData lodData, IMaterial material) {
		throw new NotImplementedException();
	}

	public Span<Matrix4x4> LockBoneMatrices(int boneCount) {
		throw new NotImplementedException();
	}

	public void UnloadModel(StudioHWData hardwareData) {
		throw new NotImplementedException();
	}

	public void UnlockBoneMatrices() {
		throw new NotImplementedException();
	}
}
