using Source.Common.MaterialSystem;

using System.Numerics;

namespace Source.Common;

public interface IStudioRender {
	void BeginFrame();
	void EndFrame();
	void LoadModel(StudioHeader studioHDR, object vtxData, StudioHWData hardwareData);
	void UnloadModel(StudioHWData hardwareData);

	int GetMaterialList(StudioHeader studioHDR, Span<IMaterial> materials);
	Span<Matrix4x4> LockBoneMatrices(int boneCount);
	void UnlockBoneMatrices();
}
