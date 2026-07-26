using Game.Shared;

using Source.Common;
using Source.Common.Engine;
using Source.Common.MaterialSystem;

using System.Numerics;

namespace Game.Client;

public enum ShadowReceiver
{
	BrushModel = 0,
	StaticProp,
	StudioModel,
}

public enum ClientShadowFlags
{
	UseRenderToTexture = (int)ShadowFlags.LastFlag << 1,
	AnimatingSource = (int)ShadowFlags.LastFlag << 2,
	UseDepthTexture = (int)ShadowFlags.LastFlag << 3,
	LastFlag = UseDepthTexture,
}

public interface IClientShadowMgr : IGameSystemPerFrame
{
	ClientShadowHandle_t CreateShadow(ClientEntityHandle entity, int flags);
	void DestroyShadow(ClientShadowHandle_t handle);

	ClientShadowHandle_t CreateFlashlight(in FlashlightState lightState);
	void UpdateFlashlightState(ClientShadowHandle_t shadowHandle, in FlashlightState lightState);
	void DestroyFlashlight(ClientShadowHandle_t handle);

	void UpdateProjectedTexture(ClientShadowHandle_t handle, bool force = false);

	void AddToDirtyShadowList(ClientShadowHandle_t handle, bool force = false);
	void AddToDirtyShadowList(IClientRenderable? renderable, bool force = false);

	void AddShadowToReceiver(ClientShadowHandle_t handle, IClientRenderable? renderable, ShadowReceiver type);

	void RemoveAllShadowsFromReceiver(IClientRenderable? renderable, ShadowReceiver type);

	void ComputeShadowTextures(in ViewSetup view, int leafCount, ReadOnlySpan<LeafIndex_t> leafList);

	void UnlockAllShadowDepthTextures();

	void RenderShadowTexture(int w, int h);

	void SetShadowDirection(in Vector3 dir);
	ref readonly Vector3 GetShadowDirection();

	void SetShadowColor(byte r, byte g, byte b);
	void SetShadowDistance(float maxDistance);
	void SetShadowBlobbyCutoffArea(float minArea);
	void SetFalloffBias(ClientShadowHandle_t handle, byte bias);

	void MarkRenderToTextureShadowDirty(ClientShadowHandle_t handle);

	void AdvanceFrame();

	void SetFlashlightTarget(ClientShadowHandle_t shadowHandle, EHANDLE targetEntity);

	void SetFlashlightLightWorld(ClientShadowHandle_t shadowHandle, bool lightWorld);

	void SetShadowsDisabled(bool disabled);

	void ComputeShadowDepthTextures(in ViewSetup view);
}
