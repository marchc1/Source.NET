using Source.Common.MaterialSystem;
using Source.Common.Mathematics;

using System.Drawing.Drawing2D;
using System.Numerics;

namespace Source.Common.Engine;

public enum ShadowFlags
{
	Flashlight = 1 << 0,
	Shadow = 1 << 1,
	LastFlag = Shadow,

	ProjectedTextureTypeMask = Flashlight | Shadow
}

public static class ShadowGlobals
{
	public const ShadowHandle_t SHADOW_HANDLE_INVALID = unchecked((ShadowHandle_t)~0);
	public const ShadowDecalHandle_t SHADOW_DECAL_HANDLE_INVALID = unchecked((ShadowDecalHandle_t)~0);
}

public enum ShadowCreateFlags
{
	CacheVerts = 1 << 0,
	Flashlight = 1 << 1,
	LastFlag = Flashlight,
}

/// <summary>
/// Information about a particular shadow
/// </summary>
public struct ShadowInfo_t
{
	// Transforms from world space into texture space of the shadow
	public Matrix4x4 m_WorldToShadow;

	// The shadow should no longer be drawn once it's further than MaxDist
	// along z in shadow texture coordinates.
	public float FalloffOffset;
	public float MaxDist;
	public float FalloffAmount;  // how much to lighten the shadow maximally
	public Vector2 TexOrigin;
	public Vector2 TexSize;
	public byte FalloffBias;
}

public interface IShadowMgr
{
	// Create, destroy shadows (see ShadowCreateFlags_t for creationFlags)
	ShadowHandle_t CreateShadow(IMaterial? material, IMaterial? modelMaterial, object? bindProxy, int creationFlags);
	void DestroyShadow(ShadowHandle_t handle);

	// Resets the shadow material (useful for shadow LOD.. doing blobby at distance) 
	void SetShadowMaterial(ShadowHandle_t handle, IMaterial? material, IMaterial? modelMaterial, object? bindProxy);

	// Project a shadow into the world
	// The two points specify the upper left coordinate and the lower-right
	// coordinate of the shadow specified in a shadow "viewplane". The
	// projection matrix is a shadow viewplane->world transformation,
	// and can be orthographic orperspective.

	// I expect that the client DLL will call this method any time the shadow
	// changes because the light changes, or because the entity casting the
	// shadow moves

	// Note that we can't really control the shadows from the engine because
	// the engine only knows about pevs, which don't exist on the client

	// The shadow matrix specifies a world-space transform for the shadow
	// the shadow is projected down the z direction, and the origin of the
	// shadow matrix is the origin of the projection ray. The size indicates
	// the shadow size measured in the space of the shadow matrix; the
	// shadow goes from +/- size.x/2 along the x axis of the shadow matrix
	// and +/- size.y/2 along the y axis of the shadow matrix.
	void ProjectShadow(ShadowHandle_t handle, in Vector3 origin, in Vector3 projectionDir, in Matrix4x4 worldToShadow, in Vector2 size, ReadOnlySpan<int> leafList, float maxHeight, float falloffOffset, float falloffAmount, in Vector3 casterOrigin);

	void ProjectFlashlight(ShadowHandle_t handle, in Matrix4x4 worldToShadow, ReadOnlySpan<int> leafList);

	// Gets at information about a particular shadow
	ref readonly ShadowInfo_t GetInfo(ShadowHandle_t handle);

	ref readonly Frustum GetFlashlightFrustum(ShadowHandle_t handle);

	// Methods related to shadows on brush models
	void AddShadowToBrushModel(ShadowHandle_t handle, Model? model, in Vector3 origin, in QAngle angles);

	// Removes all shadows from a brush model
	void RemoveAllShadowsFromBrushModel(Model? model);

	// Sets the texture coordinate range for a shadow...
	void SetShadowTexCoord(ShadowHandle_t handle, float x, float y, float w, float h);

	// Methods related to shadows on studio models
	void AddShadowToModel(ShadowHandle_t shadow, ModelInstanceHandle_t instance);
	void RemoveAllShadowsFromModel(ModelInstanceHandle_t instance);

	// Set extra clip planes related to shadows...
	// These are used to prevent pokethru and back-casting
	void ClearExtraClipPlanes(ShadowHandle_t shadow);
	void AddExtraClipPlane(ShadowHandle_t shadow, in Vector3 normal, float dist);

	// Allows us to disable particular shadows
	void EnableShadow(ShadowHandle_t shadow, bool bEnable);

	// Set the darkness falloff bias
	void SetFalloffBias(ShadowHandle_t shadow, byte bias);

	// Update the state for a flashlight.
	void UpdateFlashlightState(ShadowHandle_t shadowHandle, in FlashlightState lightState);

	void DrawFlashlightDepthTexture();

	void AddFlashlightRenderable(ShadowHandle_t shadow, IClientRenderable? renderable);
	ShadowHandle_t CreateShadowEx(IMaterial? material, IMaterial? modelMaterial, object? bindProxy, int creationFlags);

	void SetFlashlightDepthTexture(ShadowHandle_t shadowHandle, ITexture? flashlightDepthTexture, byte shadowStencilBit);

	ref readonly FlashlightState GetFlashlightState(ShadowHandle_t handle);

	void SetFlashlightRenderState(ShadowHandle_t handle);
}
