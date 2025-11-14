using Source.Common.Mathematics;
using Source.Common;
using Source.Common.Client;
using Game.Shared;
using Source.Common.Commands;
using Source;
using Source.Common.MaterialSystem;
using Source.Common.Engine;
using System.Numerics;

namespace Game.Client;
using static ViewRenderConVars;
public static class ViewRenderConVars
{
	internal readonly static ConVar cl_maxrenderable_dist = new("3000", FCvar.Cheat, "Max distance from the camera at which things will be rendered");
	internal readonly static ConVar r_drawopaqueworld = new("1", FCvar.Cheat);
	internal readonly static ConVar r_drawtranslucentworld = new("1", FCvar.Cheat);
	internal readonly static ConVar r_3dsky = new("1", 0, "Enable the rendering of 3d sky boxes");
	internal readonly static ConVar r_skybox = new("1", FCvar.Cheat, "Enable the rendering of sky boxes");
	internal readonly static ConVar r_drawviewmodel = new("1", FCvar.Cheat);
	internal readonly static ConVar r_drawtranslucentrenderables = new("1", FCvar.Cheat);
	internal readonly static ConVar r_drawopaquerenderables = new("1", FCvar.Cheat);
	internal readonly static ConVar r_threaded_renderables = new("0", 0);
}

public class RenderExecutor
{
	public virtual void AddView(Rendering3dView view) { }
	public virtual void Execute() { }
	public RenderExecutor(ViewRender mainView) {
		this.mainView = mainView;
	}
	protected ViewRender mainView;
}

public class SimpleRenderExecutor : RenderExecutor
{
	public SimpleRenderExecutor(ViewRender mainView) : base(mainView) {

	}
	public override void AddView(Rendering3dView view) {
		Base3dView? prevRenderer = mainView.SetActiveRenderer(view);
		view.Draw();
		mainView.SetActiveRenderer(prevRenderer);
	}
	public override void Execute() {

	}
}

public enum ViewID
{
	Illegal = -2,
	None = -1,
	Main = 0,
	Sky3D = 1,
	Monitor = 2,
	Reflection = 3,
	Refraction = 4,
	IntroPlayer = 5,
	IntroCamera = 6,
	ShadowDepthTexture = 7,
	SSAO = 8,
	Count
}

public class Base3dView
{
	protected Frustum frustum;
	protected ViewRender mainView;
	public Base3dView(ViewRender mainView) {
		this.mainView = mainView;
		frustum = mainView.GetFrustum();
	}

	protected ViewSetup setup;
	public Frustum GetFrustrum() => frustum;
	public virtual DrawFlags GetDrawFlags() => 0;
}

public class Rendering3dView : Base3dView
{
	protected DrawFlags DrawFlags;
	protected ClearFlags ClearFlags;
	protected ViewSetup ViewSetup;

	ClientRenderablesList? RenderablesList;

	public Rendering3dView(ViewRender mainView) : base(mainView) {

	}
	public virtual void Setup(in ViewSetup setup) {
		ViewSetup = setup; // copy to our ViewSetup
	}
	public override DrawFlags GetDrawFlags() {
		return DrawFlags;
	}
	public virtual void Draw() {

	}

	protected void DrawOpaqueRenderables(RenderDepthMode depthMode) {
		if (!r_drawopaquerenderables.GetBool())
			return;

		if (!mainView.ShouldDrawEntities())
			return;

		render.SetBlend(1);
	}

	protected void EnableWorldFog() => throw new NotImplementedException();
	protected void SetupRenderablesList(int viewID) => throw new NotImplementedException();
	protected void DrawWorld(float waterZAdjust) {
		DrawWorldListFlags engineFlags = BuildEngineDrawWorldListFlags(DrawFlags);
		mainView.render.DrawWorld(engineFlags, waterZAdjust);
	}

	private DrawWorldListFlags BuildEngineDrawWorldListFlags(DrawFlags drawFlags) {
		DrawWorldListFlags engineFlags = 0;

		if ((drawFlags & DrawFlags.DrawSkybox) != 0)
			engineFlags |= DrawWorldListFlags.Skybox;
		
		if ((drawFlags & DrawFlags.RenderAbovewater) != 0) {
			engineFlags |= DrawWorldListFlags.StrictlyAboveWater;
			engineFlags |= DrawWorldListFlags.IntersectsWater;
		}

		if ((drawFlags & DrawFlags.RenderUnderwater) != 0) {
			engineFlags |= DrawWorldListFlags.StrictlyUnderWater;
			engineFlags |= DrawWorldListFlags.IntersectsWater;
		}

		if ((drawFlags & DrawFlags.RenderWater) != 0)
			engineFlags |= DrawWorldListFlags.WaterSurface;
		
		if ((drawFlags & DrawFlags.ClipSkybox) != 0)
			engineFlags |= DrawWorldListFlags.ClipSkybox;

		if ((drawFlags & DrawFlags.ShadowDepthMap) != 0)
			engineFlags |= DrawWorldListFlags.ShadowDepth;
		
		if ((drawFlags & DrawFlags.RenderRefraction) != 0)
			engineFlags |= DrawWorldListFlags.Refraction;
		
		if ((drawFlags & DrawFlags.RenderReflection) != 0)
			engineFlags |= DrawWorldListFlags.Reflection;
		
		if ((drawFlags & DrawFlags.SSAODepthPass) != 0) {
			engineFlags |= DrawWorldListFlags.SSAO | DrawWorldListFlags.StrictlyUnderWater | DrawWorldListFlags.IntersectsWater | DrawWorldListFlags.StrictlyAboveWater;
			engineFlags &= ~(DrawWorldListFlags.WaterSurface | DrawWorldListFlags.Refraction | DrawWorldListFlags.Reflection);
		}

		return engineFlags;
	}
}

public class SkyboxView : Rendering3dView
{
	SafeFieldPointer<PlayerLocalData, Sky3DParams> Sky3dParams = new();
	public SkyboxView(ViewRender mainView) : base(mainView) {

	}

	public bool Setup(in ViewSetup viewRender, ref ClearFlags clearFlags, ref SkyboxVisibility skyboxVisible) {
		base.Setup(in viewRender);

		skyboxVisible = ComputeSkyboxVisibility();
		Sky3dParams = PreRender3dSkyboxWorld(ref skyboxVisible);
		if (Sky3dParams.IsNull)
			return false;

		ClearFlags = clearFlags;
		clearFlags &= ~(ClearFlags.ClearColor | ClearFlags.ClearDepth | ClearFlags.ClearStencil | ClearFlags.ClearFullTarget);
		clearFlags |= ClearFlags.ClearDepth;

		DrawFlags = DrawFlags.RenderUnderwater | DrawFlags.RenderAbovewater | DrawFlags.RenderWater;
		if (r_skybox.GetBool())
			DrawFlags |= DrawFlags.DrawSkybox;

		return true;
	}

	public override void Draw() {
		ITexture? rtColor = null;
		ITexture? rtDepth = null;
		if (ViewSetup.StereoEye != StereoEye.Mono)
			throw new Exception("No support for multi-stereo-eye yet");
		DrawInternal(ViewID.Sky3D, true, rtColor, rtDepth);
	}

	private void DrawInternal(ViewID skyBoxViewID, bool invokePreAndPostRender, ITexture? rtColor, ITexture? rtDepth) {
		ref Sky3DParams sky3dParams = ref Sky3dParams.Get();

		ViewSetup.ZNear = 2;
		ViewSetup.ZFar = WorldSize.MAX_TRACE_LENGTH;
		if (sky3dParams.Scale > 0)
			ViewSetup.Origin *= 1f / sky3dParams.Scale;
		ViewSetup.Origin += sky3dParams.Origin;

		render.ViewSetupVisEx(false, new(ref sky3dParams.Origin), out _);
		render.Push3DView(in ViewSetup, ClearFlags, rtColor, GetFrustrum(), rtDepth);

		SetupCurrentView(in setup.Origin, in setup.Angles, skyBoxViewID);

		if (invokePreAndPostRender)
			IGameSystem.PreRenderAllSystems();

		DrawWorld(0);

		if (invokePreAndPostRender) {
			IGameSystem.PostRenderAllSystems();
			// FinishCurrentView();
		}

		render.PopView(GetFrustrum());
	}

	private void SetupCurrentView(in Vector3 origin, in QAngle angles, ViewID viewID) {
		ViewRender.CurrentRenderOrigin = origin;
		ViewRender.CurrentRenderAngles = angles;
		ViewRender.CurrentViewID = viewID;
	}

	private SafeFieldPointer<PlayerLocalData, Sky3DParams> PreRender3dSkyboxWorld(ref SkyboxVisibility skyboxVisible) {
		if ((skyboxVisible != SkyboxVisibility.Skybox3D) && r_3dsky.GetInt() != 1)
			return SafeFieldPointer<PlayerLocalData, Sky3DParams>.Null;

		if (r_3dsky.GetInt() == 0)
			return SafeFieldPointer<PlayerLocalData, Sky3DParams>.Null;

		C_BasePlayer? player = C_BasePlayer.GetLocalPlayer();
		if (player == null)
			return SafeFieldPointer<PlayerLocalData, Sky3DParams>.Null;

		PlayerLocalData local = player.Local;
		if (local.Skybox3D.Area == 255)
			return SafeFieldPointer<PlayerLocalData, Sky3DParams>.Null;

		return new(local, GetSkybox3DRef);
	}

	static ref Sky3DParams GetSkybox3DRef(PlayerLocalData local) => ref local.Skybox3D;

	private SkyboxVisibility ComputeSkyboxVisibility() {
		return engine.IsSkyboxVisibleFromPoint(ViewSetup.Origin);
	}
}
