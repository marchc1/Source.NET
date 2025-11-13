using Source.Common.MaterialSystem;

namespace Source.Common;

public interface IStudioRender {
	void BeginFrame();
	void EndFrame();
	void LoadModel(StudioHDR studioHDR, object vtxData, StudioHWData hardwareData);
	void UnloadModel(StudioHWData hardwareData);

	int GetMaterialList(StudioHDR studioHDR, Span<IMaterial> materials);
}
