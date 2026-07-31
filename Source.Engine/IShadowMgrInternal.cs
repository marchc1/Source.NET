using Source.Common.Engine;

using System.Numerics;

namespace Source.Engine;

public struct ShadowVertex
{
	public Vector3 Position;
	public Vector3 ShadowSpaceTexCoord;
}

public struct ShadowDecalRenderInfo
{
	public Vector2 TexOrigin;
	public Vector2 TexSize;
	public float FalloffOffset;
	public float OOZFalloffDist;
	public float FalloffAmount;
	public float FalloffBias;
}

public interface IShadowMgrInternal : IShadowMgr
{
	void LevelInit(int surfCount);
	void LevelShutdown();

	void AddShadowsOnSurfaceToRenderList(ShadowDecalHandle_t decalHandle);

	void RenderProjectedTextures(Matrix4x4? modelToWorld = null);

	void RenderShadows(Matrix4x4? modelToWorld = null);

	void RenderFlashlights(bool doMasking, Matrix4x4? modelToWorld = null);

	void ClearShadowRenderList();

	int ProjectAndClipVertices(ShadowHandle_t handle, ReadOnlySpan<Vector3> position, out ShadowVertex[]? outVertex);

	void ComputeRenderInfo(ref ShadowDecalRenderInfo info, ShadowHandle_t handle);

	int InvalidShadowIndex();
	void SetModelShadowState(ModelInstanceHandle_t instance);

	void SetNumWorldMaterialBuckets(int numMaterialSortBins);

	void DrawFlashlightDecals(int sortGroup, bool doMasking);

	void DrawFlashlightDecalsOnSingleSurface(SurfaceHandle_t surfID, bool doMasking);

	void DrawFlashlightOverlays(int sortGroup, bool doMasking);

	void DrawFlashlightDecalsOnDisplacements(int sortGroup, ReadOnlySpan<DispInfo?> visibleDisps, int visibleDispCount, bool doMasking);

	void SetFlashlightStencilMasks(bool doMasking);
	bool ModelHasShadows(ModelInstanceHandle_t instance);

	byte ComputeDarkness(float z, in ShadowDecalRenderInfo info) {
		z = (z - info.FalloffOffset) * info.OOZFalloffDist;
		z = z >= 0 ? z : 0.0f;
		z = info.FalloffBias + z * info.FalloffAmount;
		z = (z - 255.0f) >= 0 ? 255.0f : z;
		return (byte)z;
	}
}
