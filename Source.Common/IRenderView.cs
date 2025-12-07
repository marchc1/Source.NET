using Source.Common.Engine;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;
using Source.Engine;

using System.Numerics;

namespace Source.Common;

public interface IWorldRenderList
{

}

[Flags]
public enum DrawWorldListFlags : ulong
{
	StrictlyAboveWater = 0x001,
	StrictlyUnderWater = 0x002,
	IntersectsWater = 0x004,
	WaterSurface = 0x008,
	Skybox = 0x010,
	ClipSkybox = 0x020,
	ShadowDepth = 0x040,
	Refraction = 0x080,
	Reflection = 0x100,
	SSAO = 0x800,
}

public enum MatSortGroup
{
	StrictlyAboveWater = 0,
	StrictlyUnderwater,
	IntersectsWaterSurface,
	WaterSurface,

	Max
}

public enum RenderDepthMode
{
	Normal,
	Shadow,
	SSAO,
	Override,
	Max
}

/// <summary>
/// Analog of IVRenderView
/// </summary>
public interface IRenderView
{
	public const uint VIEW_SETUP_VIS_EX_RETURN_FLAGS_USES_RADIAL_VIS = 1;
	void DrawBrushModel(IClientEntity baseentity, Model model, in Vector3 origin, in QAngle angles);
	void DrawIdentityBrushModel(IWorldRenderList list, Model model);
	void VGui_Paint(PaintMode mode);
	void Push2DView(ViewSetup view, ClearFlags flags, ITexture? renderTarget, Frustum frustumPlanes);
	void PopView(Frustum frustumPlanes);
	void SetMainView(in Vector3 origin, in QAngle angles);
	void SceneBegin();
	void SceneEnd();
	void Draw3DDebugOverlays();
	void ViewSetupVisEx(bool novis, ReadOnlySpan<Vector3> origins, out uint visFlags);
	void DrawWorld(DrawWorldListFlags flags, float waterZAdjust);
	void Push3DView(in ViewSetup viewRender, ClearFlags clearFlags, ITexture? rtColor, Frustum frustum, ITexture? rtDepth);
	int GetViewEntity();
	void SetBlend(float blend);
	float GetBlend();
	Vector3 GetColorModulation();
	void SetColorModulation(ReadOnlySpan<float> mod);
	void SetColorModulation(Vector3 mod);
}
