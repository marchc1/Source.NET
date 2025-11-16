using Source.Common.MaterialSystem;

using System.Numerics;

namespace Source.Common;

public static class MaterialVertexFormat {
	public static readonly VertexFormat SkinnedModel = VertexFormat.Position | VertexFormat.Color | VertexFormat.Normal | VertexFormat.TexCoord2D_0 | VertexExts.GetBoneWeight(2) | VertexFormat.BoneIndex | VertexExts.GetUserDataSize(4);
	public static readonly VertexFormat Model = VertexFormat.Position | VertexFormat.Color | VertexFormat.Normal | VertexFormat.TexCoord2D_0 | VertexExts.GetUserDataSize(4);
	public static readonly VertexFormat Color = VertexFormat.Specular;
}

public interface IStudioRender {
	void BeginFrame();
	void EndFrame();
	bool LoadModel(StudioHeader studioHDR, Memory<byte> vtxData, StudioHWData hardwareData);
	void UnloadModel(StudioHWData hardwareData);

	int GetMaterialList(StudioHeader studioHDR, Span<IMaterial> materials);
	Span<Matrix4x4> LockBoneMatrices(int boneCount);
	void UnlockBoneMatrices();
}
