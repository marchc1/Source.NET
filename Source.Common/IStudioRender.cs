using Source.Common.Engine;
using Source.Common.MaterialSystem;

using System.Numerics;

namespace Source.Common;

public static class MaterialVertexFormat {
	public static readonly VertexFormat SkinnedModel = VertexFormat.Position | VertexFormat.Color | VertexFormat.Normal | VertexFormat.TexCoord2D_0 | VertexExts.GetBoneWeight(2) | VertexFormat.BoneIndex | VertexExts.GetUserDataSize(4);
	public static readonly VertexFormat Model = VertexFormat.Position | VertexFormat.Color | VertexFormat.Normal | VertexFormat.TexCoord2D_0 | VertexExts.GetUserDataSize(4);
	public static readonly VertexFormat Color = VertexFormat.Specular;
}

public struct ColorMeshInfo {
	public IMesh? Mesh;
	public int VertOffsetInBytes;
	public int NumVerts;
	public ITexture? Lightmap;
}

public struct DrawModelInfo {
	public StudioHeader StudioHdr;
	public StudioHWData HardwareData;
	public int Skin;
	public int Body;
	public int HitboxSet;
	public object? ClientEntity;
	public int Lod;
	public ColorMeshInfo[]? ColorMeshes;
	public bool StaticLighting;
	public InlineArray6<Vector3> AmbientCube;
	// todo: lights
}

public struct DrawModelResults {
	public int ActualTriCount;
	public int TextureMemoryBytes;
	public int NumHardwareBones;
	public int NumBatches;
	public int NumMaterials;
	public int LODUsed;
	public float LODMetric;
}

public interface IStudioRender {
	void BeginFrame();
	void EndFrame();
	bool LoadModel(StudioHeader studioHDR, Memory<byte> vtxData, StudioHWData hardwareData);
	void UnloadModel(StudioHWData hardwareData);

	int GetMaterialList(StudioHeader studioHDR, Span<IMaterial> materials);
	Span<Matrix4x4> LockBoneMatrices(int boneCount);
	void UnlockBoneMatrices();
	void DrawModel(ref DrawModelResults results, ref DrawModelInfo info, Span<Matrix4x4> boneToWorld, Span<byte> flexWeights, Span<byte> flexDelayedWeights, in Vector3 modelOrigin, StudioRenderFlags flags = StudioRenderFlags.DrawEntireModel);
	void SetViewState(in Vector3 currentViewOrigin, in Vector3 currentViewRight, in Vector3 currentViewUp, in Vector3 currentViewForward);
	void SetColorModulation(Vector3 r_colormod);
	void SetAlphaModulation(float r_blend);
}
