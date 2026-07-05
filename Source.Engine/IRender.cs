using Source.Common;
using Source.Common.Engine;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;

using System.Drawing.Drawing2D;
using System.Numerics;

namespace Source.Engine;

public interface IRender
{
	void FrameBegin();
	void FrameEnd();

	void ViewSetupVis(bool novis, ReadOnlySpan<Vector3> origin);

	void ViewDrawFade(Span<byte> color, IMaterial? fadeMaterial);

	void DrawSceneBegin();
	void DrawSceneEnd();

	IWorldRenderList? CreateWorldList();
	void BuildWorldLists(IWorldRenderList? list, ref WorldListInfo info, int forceViewLeaf, ReadOnlySpan<VisOverrideData> visData, bool shadowDepth, Span<float> reflectionWaterHeight);
	void DrawWorldLists(IWorldRenderList? list, uint flags, float waterZAdjust);

	ref readonly Vector3 ViewOrigin();
	ref readonly QAngle ViewAngles();
	ref readonly ViewSetup ViewGetCurrent();
	ref readonly Matrix4x4 ViewMatrix();
	ref readonly Matrix4x4 WorldToScreenMatrix();
	float GetFramerate();
	float GetZNear();
	float GetZFar();
	void DrawSkybox(float zFar, int drawFlags = 0x3F);

	// Query current fov and view model fov
	float GetFov();
	float GetFovY();
	float GetFovViewmodel();


	// Compute the clip-space coordinates of a point in 3D
	// Clip-space is normalized screen coordinates (-1 to 1 in x and y)
	// Returns true if the point is behind the camera
	bool ClipTransformWithProjection(in Matrix4x4 worldToScreen, in Vector3 point, out Vector3 clip);
	// Same, using the current engine's matrices.
	bool ClipTransform(in Vector3 point, out Vector3 clip);

	// Compute the screen-space coordinates of a point in 3D
	// This returns actual pixels
	// Returns true if the point is behind the camera
	bool ScreenTransform(in Vector3 point, out Vector3 screen);

	void Push3DView(in ViewSetup view, ClearFlags flags, ITexture? renderTarget, Frustum frustumPlanes);
	void Push3DView(in ViewSetup view, ClearFlags flags, ITexture? renderTarget, Frustum frustumPlanes, ITexture? depthTexture);
	void Push2DView(in ViewSetup view, ClearFlags flags, ITexture? renderTarget, Frustum frustumPlanes);
	void PopView(Frustum frustumPlanes);
	void SetMainView(in Vector3 origin, in QAngle angles);
	void ViewSetupVisEx(bool novis, Span<Vector3> origin, out uint returnFlags);
	void OverrideViewFrustum(Frustum custom);
	void UpdateBrushModelLightmap(Model? model, IClientRenderable? renderable);
	void BeginUpdateLightmaps();
	void EndUpdateLightmaps();
	bool InLightmapUpdate();
}
