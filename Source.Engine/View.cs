
using Source.Common;
using Source.Common.Client;
using Source.Common.Engine;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;
using Source.Engine.Client;

using System.Numerics;
using System.Runtime.InteropServices;

namespace Source.Engine;

public class View(Host Host, IEngineVGuiInternal EngineVGui, IMaterialSystem materials,
	Render EngineRenderer, Shader Shader, IBaseClientDLL ClientDLL, IVideoMode videomode)
{
	public void RenderGuiOnly() {
		materials.BeginFrame(Host.FrameTime);
		EngineVGui.Simulate();
		EngineRenderer.FrameBegin();
		RenderGuiOnly_NoSwap();
		EngineRenderer.FrameEnd();
		materials.EndFrame();
		Shader.SwapBuffers();
	}

	private void RenderGuiOnly_NoSwap() {
		using MatRenderContextPtr renderContext = new(materials);
		renderContext.ClearBuffers(true, true);

		EngineVGui.Paint(PaintMode.UIPanels | PaintMode.Cursor);
	}

	internal void RenderView() {
		bool canRenderWorld = cl.IsActive();
		if (!canRenderWorld) {
			RenderGuiOnly_NoSwap();
		}
		else {
			EngineRenderer.CheckForLightingConfigChanges();
			ViewRects screenrect = videomode.GetClientViewRect();
			ClientDLL.View_Render(screenrect);
		}
	}

	public virtual void SetMainView(in Vector3 origin, in QAngle angles) {
		EngineRenderer.SetMainView(in origin, in angles);
	}

	internal void Shutdown() {

	}
}


public class RenderView(EngineVGui EngineVGui, Render engineRenderer) : IRenderView
{
	public virtual void Push2DView(ViewSetup view, ClearFlags flags, ITexture? renderTarget, Frustum frustumPlanes) {
		engineRenderer.Push2DView(in view, flags, renderTarget, frustumPlanes);
	}

	public virtual void PopView(Frustum frustumPlanes) {
		engineRenderer.PopView(frustumPlanes);
	}


	public virtual void VGui_Paint(PaintMode mode) {
		EngineVGui.Paint(mode);
	}

	public virtual void SetMainView(in Vector3 origin, in QAngle angles) {
		engineRenderer.SetMainView(in origin, in angles);
	}

	public void DrawBrushModel(IClientEntity baseentity, Model model, in Vector3 origin, in QAngle angles) {
		throw new NotImplementedException();
	}

	public void DrawIdentityBrushModel(IWorldRenderList list, Model model) {
		throw new NotImplementedException();
	}

	public void SceneBegin() => engineRenderer.DrawSceneBegin();
	public void SceneEnd() => engineRenderer.DrawSceneEnd();

	public void ViewSetupVisEx(bool novis, ReadOnlySpan<Vector3> origins, out uint returnFlags) => engineRenderer.ViewSetupVisEx(novis, origins, out returnFlags);

	public void DrawWorld(DrawWorldListFlags flags, float waterZAdjust) => engineRenderer.DrawWorld(flags, waterZAdjust);

	public void Push3DView(in ViewSetup viewRender, ClearFlags clearFlags, ITexture? rtColor, Frustum frustum, ITexture? rtDepth) => engineRenderer.Push3DView(in viewRender, clearFlags, rtColor, frustum, rtDepth);

	public int GetViewEntity() => cl.ViewEntity;

	public float r_blend = 1;
	public Vector3 r_colormod = new(1, 1, 1);
	public bool IsBlendingOrModulating { get; private set; }
	public void CheckBlend() {
		IsBlendingOrModulating = r_blend != 1.0f || r_colormod[0] != 1.0f || r_colormod[1] != 1.0f || r_colormod[2] != 1.0f;
	}
	public void SetBlend(float blend) {
		r_blend = blend;
		CheckBlend();
	}
	public float GetBlend() => r_blend;
	public void SetColorModulation(Vector3 mod) {
		r_colormod = mod;
		CheckBlend();
	}
	public void SetColorModulation(ReadOnlySpan<float> mod) {
		r_colormod = MemoryMarshal.Cast<float, Vector3>(mod)[0];
		CheckBlend();
	}
	public Vector3 GetColorModulation() => r_colormod;
}
