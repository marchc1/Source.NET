using Source.Common;
using Source.Common.MaterialSystem;

using System;
using System.Collections.Generic;
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

	public int GetMaterialList(StudioHDR studioHDR, Span<IMaterial> materials) {
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

	public void LoadModel(StudioHDR studioHDR, object vtxData, StudioHWData hardwareData) {
		throw new NotImplementedException();
	}

	public void UnloadModel(StudioHWData hardwareData) {
		throw new NotImplementedException();
	}
}
